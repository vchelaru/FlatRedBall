using GameJsonCommunicationPlugin.Common;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// Drives the real <see cref="GameConnectionManager"/> over loopback sockets, standing in for the
    /// game side of live edit. No game process is launched - the handshake is just two sockets, each
    /// identifying its direction with a single byte (1 = glue->game, 2 = game->glue).
    /// </summary>
    public class GameConnectionManagerTests
    {
        const double ShortTimeoutInSeconds = 0.3;

        #region Harness

        static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>
        /// Performs the game side of the handshake and waits until the manager reports connected.
        /// </summary>
        static async Task<(Socket glueToGame, Socket gameToGlue)> ConnectAsGameAsync(GameConnectionManager manager, int port)
        {
            var glueToGame = await ConnectWithRetryAsync(port);
            glueToGame.Send(new byte[] { 1 });

            var gameToGlue = await ConnectWithRetryAsync(port);
            gameToGlue.Send(new byte[] { 2 });

            await WaitUntilAsync(() => manager.IsConnected, TimeSpan.FromSeconds(10));
            manager.IsConnected.ShouldBeTrue("the handshake should have completed");

            return (glueToGame, gameToGlue);
        }

        static async Task<Socket> ConnectWithRetryAsync(int port)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

            while (true)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(IPAddress.Loopback, port);
                    return socket;
                }
                catch (SocketException)
                {
                    socket.Dispose();
                    if (DateTime.UtcNow > deadline)
                    {
                        throw;
                    }
                    // The manager listens from a Task.Run started in its constructor, so the first
                    // attempts can beat the bind.
                    await Task.Delay(25);
                }
            }
        }

        static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(25);
            }
        }

        #endregion

        /// <summary>
        /// An editor sitting in edit mode with nobody touching it sends and receives nothing. The
        /// game->glue socket is idle by design, so the request/response timeout must not apply to it -
        /// otherwise the connection is torn down and re-handshaked every TimeoutInSeconds forever.
        /// </summary>
        [Fact]
        public async Task IdleConnection_IsNotTornDownByTheResponseTimeout()
        {
            var port = GetFreePort();
            var logMessages = new ConcurrentQueue<string>();

            using var manager = new GameConnectionManager((_, __) => { }, port)
            {
                TimeoutInSeconds = ShortTimeoutInSeconds,
                LogAction = logMessages.Enqueue
            };

            var (glueToGame, gameToGlue) = await ConnectAsGameAsync(manager, port);

            try
            {
                // Well over the timeout, with no traffic in either direction.
                await Task.Delay(TimeSpan.FromSeconds(ShortTimeoutInSeconds * 8));

                manager.IsConnected.ShouldBeTrue("an idle connection is healthy and must stay connected");
                logMessages.Any(x => x?.Contains("Resetting connection") == true)
                    .ShouldBeFalse("idle time is not a failure: " + string.Join(" | ", logMessages));
            }
            finally
            {
                glueToGame.Dispose();
                gameToGlue.Dispose();
            }
        }

        /// <summary>
        /// The other half of the above: a reply we are actively waiting for still has to time out, or a
        /// game that died mid-request leaves the editor awaiting forever.
        /// </summary>
        [Fact]
        public async Task UnansweredRequest_TimesOutAndResetsTheConnection()
        {
            var port = GetFreePort();
            var logMessages = new ConcurrentQueue<string>();

            using var manager = new GameConnectionManager((_, __) => { }, port)
            {
                TimeoutInSeconds = ShortTimeoutInSeconds,
                LogAction = logMessages.Enqueue
            };

            var (glueToGame, gameToGlue) = await ConnectAsGameAsync(manager, port);

            try
            {
                // Nothing on the far end reads this or replies to it.
                await manager.SendItemWithResponse(new GameConnectionManager.Packet
                {
                    PacketType = "Test",
                    Payload = "no one is going to answer this"
                });

                manager.IsConnected.ShouldBeFalse("an unanswered request should tear the connection down");
                logMessages.Any(x => x?.Contains("No response received within") == true)
                    .ShouldBeTrue("the reset reason should name the timeout: " + string.Join(" | ", logMessages));
            }
            finally
            {
                glueToGame.Dispose();
                gameToGlue.Dispose();
            }
        }

        /// <summary>
        /// Reproduces the editor freeze: diagnostics reach the UI thread and block until it services
        /// them, and StatusCheck takes the connection lock on that same UI thread ten times a second. If
        /// a reset logs while holding the lock, the logging thread waits on the UI thread and the UI
        /// thread waits on the lock - Glue hangs with no CPU use and nothing written to the output.
        /// </summary>
        [Fact]
        public async Task ResetConnection_DoesNotLogWhileHoldingTheConnectionLock()
        {
            var port = GetFreePort();
            var statusCheckFinished = new ManualResetEventSlim(false);
            GameConnectionManager manager = null;

            using (manager = new GameConnectionManager((_, __) => { }, port) { TimeoutInSeconds = ShortTimeoutInSeconds })
            {
                manager.LogAction = message =>
                {
                    if (message?.Contains("Resetting connection") != true)
                    {
                        return;
                    }

                    // Stands in for the UI thread this log call would be blocking on.
                    var contender = new Thread(() =>
                    {
                        try
                        {
                            manager.StatusCheck();
                        }
                        finally
                        {
                            statusCheckFinished.Set();
                        }
                    })
                    { IsBackground = true };

                    contender.Start();
                    statusCheckFinished.Wait(TimeSpan.FromSeconds(5));
                };

                var (glueToGame, gameToGlue) = await ConnectAsGameAsync(manager, port);

                try
                {
                    // Times out with no reply, which triggers the reset and therefore the log above.
                    await manager.SendItemWithResponse(new GameConnectionManager.Packet
                    {
                        PacketType = "Test",
                        Payload = "no one is going to answer this"
                    });

                    statusCheckFinished.IsSet.ShouldBeTrue(
                        "the reset logged while holding the connection lock, so a thread needing that lock could not proceed - this is the editor deadlock");
                }
                finally
                {
                    glueToGame.Dispose();
                    gameToGlue.Dispose();
                }
            }
        }
    }
}
