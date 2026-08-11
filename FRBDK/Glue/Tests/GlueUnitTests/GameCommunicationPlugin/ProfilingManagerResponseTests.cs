using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Managers;
using GameCommunicationPlugin.GlueControl.ViewModels;
using GameJsonCommunicationPlugin.Common;
using GlueUnitTests.TestSupport;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// The profiling tab polls the game once a second. It used to carry a
    /// "for some reason the data can be null here" workaround, which was this issue seen from the far end:
    /// a dropped command arrived as Succeeded with no data, so the tab silently kept showing whatever it
    /// last managed to read.
    /// </summary>
    public class ProfilingManagerResponseTests : IDisposable
    {
        const double ShortTimeoutInSeconds = 0.3;

        readonly GlueProjectSave? previousGlueProject;

        public ProfilingManagerResponseTests()
        {
            GlueTestBootstrap.EnsureInitialized();

            previousGlueProject = ObjectFinder.Self.GlueProject;
            ObjectFinder.Self.GlueProject = new GlueProjectSave();
        }

        public void Dispose() => ObjectFinder.Self.GlueProject = previousGlueProject;

        sealed class Fixture : IDisposable
        {
            public required GameConnectionManager Manager { get; init; }
            public required FakeGameSide Game { get; init; }
            public required ProfilingManager Profiling { get; init; }
            public required ProfilingControlViewModel ViewModel { get; init; }

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

            CompilerLibrary.ViewModels.CompilerViewModel.Self.IsPrintEditorToGameCheckboxChecked = false;

            var sender = new CommandSender
            {
                ConnectionManager = manager,
                PrintOutput = _ => { },
                CompilerViewModel = CompilerLibrary.ViewModels.CompilerViewModel.Self,
                GlueViewSettingsViewModel = new GlueViewSettingsViewModel { EnableLiveEdit = true }
            };

            var viewModel = new ProfilingControlViewModel
            {
                SummaryText = "stale numbers from the last successful poll"
            };

            return new Fixture
            {
                Manager = manager,
                Game = game,
                ViewModel = viewModel,
                Profiling = new ProfilingManager { ProfilingViewModel = viewModel, CommandSender = sender }
            };
        }

        [Fact]
        public async Task WhenTheGameIsNotReady_TheTabExplainsInsteadOfShowingStaleNumbers()
        {
            using var fixture = await ConnectAsync(_ => GameConnectionManager.NotReadyPayload);

            await fixture.Profiling.RefreshProfilingData();

            fixture.ViewModel.SummaryText.ShouldContain("not ready");
        }

        [Fact]
        public async Task WhenTheGameSendsNothingBack_TheTabExplainsInsteadOfShowingStaleNumbers()
        {
            using var fixture = await ConnectAsync(_ => null);

            await fixture.Profiling.RefreshProfilingData();

            fixture.ViewModel.SummaryText.ShouldNotContain("stale numbers");
            fixture.ViewModel.SummaryText.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenTheGameAnswers_TheTabShowsTheProfilingData()
        {
            using var fixture = await ConnectAsync(_ =>
                "{\"SummaryData\":\"1 Sprite\",\"CollisionData\":[]}");

            await fixture.Profiling.RefreshProfilingData();

            fixture.ViewModel.SummaryText.ShouldBe("1 Sprite");
        }
    }
}
