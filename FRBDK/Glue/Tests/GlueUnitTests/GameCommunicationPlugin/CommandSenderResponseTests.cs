using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameJsonCommunicationPlugin.Common;
using GlueUnitTests.TestSupport;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// Drives the real <see cref="CommandSender"/> against a fake game over real loopback sockets, so the
    /// whole Glue-side stack (serialize -> packet -> socket -> classify -> deserialize) is exercised. What
    /// is being pinned is a single rule: a caller can tell whether the game carried out its command.
    /// </summary>
    public class CommandSenderResponseTests : IDisposable
    {
        const double ShortTimeoutInSeconds = 0.3;

        readonly GlueProjectSave? previousGlueProject;

        public CommandSenderResponseTests()
        {
            GlueTestBootstrap.EnsureInitialized();

            // SendCommand early-outs with "No project loaded" without one.
            previousGlueProject = ObjectFinder.Self.GlueProject;
            ObjectFinder.Self.GlueProject = new GlueProjectSave();
        }

        public void Dispose() => ObjectFinder.Self.GlueProject = previousGlueProject;

        #region Harness

        sealed class Fixture : IDisposable
        {
            public required GameConnectionManager Manager { get; init; }
            public required FakeGameSide Game { get; init; }
            public required CommandSender Sender { get; init; }

            public void Dispose()
            {
                Game.Dispose();
                Manager.Dispose();
            }
        }

        static async Task<Fixture> ConnectAsync(Func<string, string?> respond)
        {
            var port = FakeGameSide.GetFreePort();
            var manager = new GameConnectionManager((_, __) => { }, port)
            {
                TimeoutInSeconds = ShortTimeoutInSeconds,
                LogAction = _ => { }
            };

            var game = await FakeGameSide.ConnectAsync(manager, port);
            game.StartResponder(respond);

            // CompilerViewModel is a private-constructor singleton; the flag only gates printing every
            // sent command to the Build tab, which a test has no use for.
            CompilerLibrary.ViewModels.CompilerViewModel.Self.IsPrintEditorToGameCheckboxChecked = false;

            var sender = new CommandSender
            {
                ConnectionManager = manager,
                PrintOutput = _ => { },
                CompilerViewModel = CompilerLibrary.ViewModels.CompilerViewModel.Self
            };

            return new Fixture { Manager = manager, Game = game, Sender = sender };
        }

        #endregion

        #region Send<T> - callers that need data back

        /// <summary>
        /// A caller asking for a typed response and getting nothing did not get its command carried out -
        /// the game either dropped it or its handler failed. This used to report Succeeded = true with
        /// null Data, which is what left MainCompilerPlugin, RefreshManager and ProfilingManager each
        /// guessing differently about what null meant.
        /// </summary>
        [Fact]
        public async Task TypedSend_WhenTheGameSendsNothingBack_ReportsFailure()
        {
            using var fixture = await ConnectAsync(_ => null);

            var response = await fixture.Sender.Send<GeneralCommandResponse>(new SetEditMode());

            response.Succeeded.ShouldBeFalse();
            response.Message.ShouldNotBeNullOrEmpty("the caller has to be able to print why it failed");
        }

        [Fact]
        public async Task TypedSend_WhenTheGameIsNotReady_ReportsFailure()
        {
            using var fixture = await ConnectAsync(_ => GameConnectionManager.NotReadyPayload);

            var response = await fixture.Sender.Send<GeneralCommandResponse>(new SetEditMode());

            response.Succeeded.ShouldBeFalse();
            response.Message.ShouldContain("not ready");
        }

        /// <summary>
        /// Pins issue #2174: MainCompilerPlugin sends the initial SetEditMode with
        /// SendImportance.RetryOnFailure specifically to outlast the game's own startup, where
        /// Runner_GameStarted can fire before the game's socket has even connected. A retry loop that
        /// doesn't actually wait between attempts burns through its whole budget before that real time
        /// has a chance to pass, so it fails exactly the race it exists to survive. The fake game here
        /// only becomes ready after real wall-clock time elapses, so this only passes if retrying
        /// actually waits between attempts.
        /// </summary>
        [Fact]
        public async Task TypedSend_WithRetryOnFailure_WaitsForTheGameToBecomeReady()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var fixture = await ConnectAsync(_ =>
                stopwatch.ElapsedMilliseconds < 200
                    ? GameConnectionManager.NotReadyPayload
                    : "{\"Succeeded\":true,\"Message\":\"ready now\"}");

            var response = await fixture.Sender.Send<GeneralCommandResponse>(
                new SetEditMode(), SendImportance.RetryOnFailure);

            response.Succeeded.ShouldBeTrue(
                "retrying with real delays between attempts must eventually catch the game becoming ready");
        }

        [Fact]
        public async Task TypedSend_WhenTheGameNeverAnswers_ReportsFailure()
        {
            using var fixture = await ConnectAsync(_ => throw new NotSupportedException("unreachable"));
            fixture.Game.Dispose();

            var response = await fixture.Sender.Send<GeneralCommandResponse>(new SetEditMode());

            response.Succeeded.ShouldBeFalse();
        }

        [Fact]
        public async Task TypedSend_WithARealPayload_SucceedsAndDeserializes()
        {
            using var fixture = await ConnectAsync(_ =>
                "{\"Succeeded\":true,\"Message\":\"all good\"}");

            var response = await fixture.Sender.Send<GeneralCommandResponse>(new SetEditMode());

            response.Succeeded.ShouldBeTrue();
            response.Data.ShouldNotBeNull();
            response.Data.Message.ShouldBe("all good");
        }

        [Fact]
        public async Task TypedSend_WithUnparseablePayload_ReportsFailure()
        {
            using var fixture = await ConnectAsync(_ => "this is not json");

            var response = await fixture.Sender.Send<GeneralCommandResponse>(new SetEditMode());

            response.Succeeded.ShouldBeFalse();
        }

        #endregion

        #region Send - fire-and-forget callers

        /// <summary>
        /// Most DTOs have a void handler, so an empty reply is the game saying "done". Turning that into a
        /// failure would make every fire-and-forget command in live edit look broken - and RefreshManager
        /// answers a failure by restarting the game.
        /// </summary>
        [Fact]
        public async Task UntypedSend_WhenTheGameHandlesItWithNothingToReturn_Succeeds()
        {
            using var fixture = await ConnectAsync(_ => null);

            var response = await fixture.Sender.Send(new RestartScreenDto());

            response.Succeeded.ShouldBeTrue();
        }

        [Fact]
        public async Task UntypedSend_WhenTheGameIsNotReady_ReportsFailure()
        {
            using var fixture = await ConnectAsync(_ => GameConnectionManager.NotReadyPayload);

            var response = await fixture.Sender.Send(new RestartScreenDto());

            response.Succeeded.ShouldBeFalse();
            response.Data.ShouldBeNull("the marker must never reach a caller as if it were a payload");
        }

        #endregion

        #region Every command the game accepts

        /// <summary>
        /// The set of commands the game can dispatch, read out of the game-side CommandReceiver's HandleDto
        /// overloads. Derived rather than hand-listed so a DTO added later is covered without anyone
        /// remembering to add it here.
        /// </summary>
        public static IEnumerable<object[]> EveryCommandDtoTypeName()
        {
            foreach (var name in GameSideCommandDtoNames())
            {
                yield return new object[] { name };
            }
        }

        static IReadOnlyList<string> GameSideCommandDtoNames()
        {
            using var stream = typeof(CommandSender).Assembly
                .GetManifestResourceStream("GameCommunicationPlugin.GlueControl.Embedded.CommandReceiver.cs")
                ?? throw new InvalidOperationException("The game-side CommandReceiver source is not embedded.");

            var source = new System.IO.StreamReader(stream).ReadToEnd();

            return Regex.Matches(source, @"HandleDto\(\s*(?:Dtos\.)?(\w+)\s+\w+\s*\)")
                .Select(match => match.Groups[1].Value)
                // Resolvable on the Glue side, and constructible - a couple of game-side handlers take
                // types that only exist inside a game project.
                .Where(name => ResolveGlueSideDto(name) != null)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        static Type? ResolveGlueSideDto(string name)
        {
            var type = typeof(CommandSender).Assembly
                .GetType($"GameCommunicationPlugin.GlueControl.Dtos.{name}");

            return type?.GetConstructor(Type.EmptyTypes) == null ? null : type;
        }

        /// <summary>
        /// Guards the theory below against quietly covering nothing if the source shape or the resource
        /// name ever changes.
        /// </summary>
        [Fact]
        public void TheCommandListIsDerivedFromTheGameAndIsNotEmpty()
        {
            GameSideCommandDtoNames().Count.ShouldBeGreaterThan(20);
        }

        /// <summary>
        /// The issue's actual claim, applied to every command rather than the three that happened to be
        /// noticed: if the game could not handle it, the caller must not be told it worked.
        /// </summary>
        [Theory]
        [MemberData(nameof(EveryCommandDtoTypeName))]
        public async Task EveryCommand_DroppedByANotReadyGame_ReportsFailure(string dtoTypeName)
        {
            var dtoType = ResolveGlueSideDto(dtoTypeName).ShouldNotBeNull();
            var dto = Activator.CreateInstance(dtoType)!;

            using var fixture = await ConnectAsync(_ => GameConnectionManager.NotReadyPayload);

            var response = await fixture.Sender.Send(dto);

            response.Succeeded.ShouldBeFalse($"{dtoTypeName} was dropped, so the caller must not read it as success");
        }

        /// <summary>
        /// The other half: the same command, handled with nothing to send back, still has to read as
        /// success. Without this the theory above would pass just as well if everything failed.
        /// </summary>
        [Theory]
        [MemberData(nameof(EveryCommandDtoTypeName))]
        public async Task EveryCommand_HandledWithNoDataToReturn_ReportsSuccess(string dtoTypeName)
        {
            var dtoType = ResolveGlueSideDto(dtoTypeName).ShouldNotBeNull();
            var dto = Activator.CreateInstance(dtoType)!;

            using var fixture = await ConnectAsync(_ => null);

            var response = await fixture.Sender.Send(dto);

            response.Succeeded.ShouldBeTrue($"{dtoTypeName} was handled, so the caller must not read it as a failure");
        }

        #endregion
    }
}
