using GameJsonCommunicationPlugin.Common;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// "The game dropped this command" and "the game handled this command" have to be distinguishable at
    /// the call site. These pin the classification <see cref="GameConnectionManager.SendItemWithResponse"/>
    /// applies to whatever comes back over the socket.
    /// </summary>
    public class GameConnectionResponseTests
    {
        const double ShortTimeoutInSeconds = 0.3;

        static GameConnectionManager CreateManager(int port) =>
            new GameConnectionManager((_, __) => { }, port)
            {
                TimeoutInSeconds = ShortTimeoutInSeconds,
                LogAction = _ => { }
            };

        static GameConnectionManager.Packet AnyPacket() => new GameConnectionManager.Packet
        {
            PacketType = "OldDTO",
            Payload = "RestartScreenDto:{}"
        };

        /// <summary>
        /// The game answering with nothing is its normal "handled it, no data to send back" reply - most
        /// DTOs have a void handler. That has to stay a success or every fire-and-forget command in live
        /// edit starts reporting failure.
        /// </summary>
        [Fact]
        public async Task EmptyReply_IsSuccessBecauseMostHandlersHaveNothingToSendBack()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);
            using var game = await FakeGameSide.ConnectAsync(manager, port);

            game.StartResponder(_ => null);

            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Succeeded.ShouldBeTrue("a handler with nothing to return is not a failure");
        }

        [Fact]
        public async Task PayloadReply_IsSuccessAndCarriesThePayload()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);
            using var game = await FakeGameSide.ConnectAsync(manager, port);

            game.StartResponder(_ => "{\"Succeeded\":true}");

            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Succeeded.ShouldBeTrue();
            response.Data.ShouldBe("{\"Succeeded\":true}");
        }

        /// <summary>
        /// The game never answering is a dropped command, not a successful one. Before this was pinned,
        /// the timeout tore the connection down (correctly) but still handed the caller
        /// Succeeded = true with null data.
        /// </summary>
        [Fact]
        public async Task NoReplyAtAll_IsReportedAsFailure()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);
            using var game = await FakeGameSide.ConnectAsync(manager, port);

            // No responder started - nothing on the far end reads the request or answers it.
            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Succeeded.ShouldBeFalse("a command the game never answered was not carried out");
            response.Message.ShouldNotBeNullOrEmpty("the caller needs to be able to say why it failed");
        }

        /// <summary>
        /// The window this issue is about: the game's socket is up but GlueControlManager has not been
        /// constructed yet, so nothing can dispatch the DTO. The game says so explicitly instead of
        /// replying with an empty body, which is indistinguishable from the success above.
        /// </summary>
        [Fact]
        public async Task NotReadyReply_IsReportedAsFailure()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);
            using var game = await FakeGameSide.ConnectAsync(manager, port);

            game.StartResponder(_ => GameConnectionManager.NotReadyPayload);

            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Succeeded.ShouldBeFalse("the game said it could not handle the command");
            response.Data.ShouldBeNull("the marker is not a payload the caller should try to read");
        }

        [Fact]
        public async Task NotReadyReply_ExplainsThatTheGameIsNotReady()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);
            using var game = await FakeGameSide.ConnectAsync(manager, port);

            game.StartResponder(_ => GameConnectionManager.NotReadyPayload);

            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Message.ShouldContain("not ready");
        }

        [Fact]
        public async Task WhenNotConnected_IsReportedAsFailure()
        {
            var port = FakeGameSide.GetFreePort();
            using var manager = CreateManager(port);

            var response = await manager.SendItemWithResponse(AnyPacket());

            response.Succeeded.ShouldBeFalse();
            response.Message.ShouldContain("Not connected");
        }

        /// <summary>
        /// The game-side copy of GameConnectionManager is an embedded resource, not compiled into this
        /// assembly - it only compiles inside a game project. The two halves of the marker therefore
        /// cannot share a constant, so pin that they still agree.
        /// </summary>
        [Fact]
        public void TheGameSideSourceUsesTheSameNotReadyMarker()
        {
            // Read straight out of the manifest rather than through GlueControlCodeGenerator, which also
            // stamps in project namespace and #defines and so needs a loaded project.
            using var stream = typeof(global::GameCommunicationPlugin.GlueControl.CodeGeneration.GlueControlCodeGenerator)
                .Assembly
                .GetManifestResourceStream("GameCommunicationPlugin.GlueControl.Embedded.GameConnectionManager.cs");

            stream.ShouldNotBeNull("the game-side source should ship as an embedded resource");

            var gameSideSource = new System.IO.StreamReader(stream).ReadToEnd();

            gameSideSource.ShouldContain(
                GameConnectionManager.NotReadyPayload,
                customMessage: "the game replies with this literal and Glue recognizes it - if they drift, " +
                    "a dropped command silently reads as success again");
        }
    }
}
