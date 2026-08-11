using GameJsonCommunicationPlugin.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// Stands in for the game process on the other end of live edit's two loopback sockets. No game is
    /// launched - the handshake is just two connections, each identifying its direction with a single
    /// byte (1 = glue->game, 2 = game->glue).
    /// </summary>
    /// <remarks>
    /// Deliberately speaks the wire format by hand (8-byte little-endian length, then an ASCII body)
    /// rather than reusing any production helper, so a change to how Glue frames a message shows up here
    /// as a failing test instead of being mirrored into the test and hidden.
    /// </remarks>
    internal sealed class FakeGameSide : IDisposable
    {
        private readonly Socket glueToGame;
        private readonly Socket gameToGlue;
        private CancellationTokenSource? responderCancellation;

        private FakeGameSide(Socket glueToGame, Socket gameToGlue)
        {
            this.glueToGame = glueToGame;
            this.gameToGlue = gameToGlue;
        }

        public static int GetFreePort()
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
        public static async Task<FakeGameSide> ConnectAsync(GameConnectionManager manager, int port)
        {
            var glueToGame = await ConnectWithRetryAsync(port);
            glueToGame.Send(new byte[] { 1 });

            var gameToGlue = await ConnectWithRetryAsync(port);
            gameToGlue.Send(new byte[] { 2 });

            await WaitUntilAsync(() => manager.IsConnected, TimeSpan.FromSeconds(10));

            if (!manager.IsConnected)
            {
                glueToGame.Dispose();
                gameToGlue.Dispose();
                throw new TimeoutException("The handshake never completed.");
            }

            return new FakeGameSide(glueToGame, gameToGlue);
        }

        /// <summary>
        /// Answers every incoming request on a background thread, the way the real game's receive loop
        /// does. <paramref name="respond"/> receives the raw request body (the serialized
        /// <see cref="GameConnectionManager.Packet"/>) and returns the reply payload; returning null
        /// replies with a zero-length body, which is what the game sends when it has nothing to say.
        /// </summary>
        public void StartResponder(Func<string, string?> respond)
        {
            responderCancellation = new CancellationTokenSource();
            var cancellation = responderCancellation.Token;

            Task.Run(() =>
            {
                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        var request = ReadFramedString(glueToGame);
                        if (request == null)
                        {
                            return;
                        }

                        WriteFramedString(glueToGame, respond(request));
                    }
                }
                catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
                {
                    // The test finished and disposed the sockets.
                }
            });
        }

        /// <summary>
        /// Sends an unsolicited game-&gt;Glue message, the shape live edit uses for drag/resize results.
        /// </summary>
        public void SendUnsolicited(string body) => WriteFramedString(gameToGlue, body);

        private static string? ReadFramedString(Socket socket)
        {
            var sizeBuffer = new byte[sizeof(long)];
            if (!ReadExactly(socket, sizeBuffer))
            {
                return null;
            }

            var size = BitConverter.ToInt64(sizeBuffer, 0);
            if (size == 0)
            {
                return string.Empty;
            }

            var body = new byte[size];
            return ReadExactly(socket, body)
                ? Encoding.ASCII.GetString(body)
                : null;
        }

        private static bool ReadExactly(Socket socket, byte[] buffer)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var amount = socket.Receive(buffer, read, buffer.Length - read, SocketFlags.None);
                if (amount == 0)
                {
                    return false;
                }
                read += amount;
            }
            return true;
        }

        private static void WriteFramedString(Socket socket, string? body)
        {
            var bytes = body == null ? Array.Empty<byte>() : Encoding.ASCII.GetBytes(body);
            socket.Send(BitConverter.GetBytes(bytes.LongLength));
            if (bytes.Length > 0)
            {
                socket.Send(bytes);
            }
        }

        private static async Task<Socket> ConnectWithRetryAsync(int port)
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

        public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(25);
            }
        }

        public void Dispose()
        {
            try { responderCancellation?.Cancel(); } catch { }
            try { glueToGame.Dispose(); } catch { }
            try { gameToGlue.Dispose(); } catch { }
        }
    }
}
