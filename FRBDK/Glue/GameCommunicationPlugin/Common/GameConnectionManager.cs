using FlatRedBall.Glue.Plugins;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GameJsonCommunicationPlugin.Common
{
    internal class GameConnectionManager : IDisposable
    {
        #region private
        private Object _lock = new Object();

        private Action<string, string> _eventCaller;
        private IPAddress _addr;
        private Socket _listener = null;
        
        private Socket glueToGameSocket = null;
        private Socket gameToGlueSocket = null;

        private bool _isListening = false;

        private bool _isConnected = false;
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Applies a connection state change, returning the notifications it produced (or null if the
        /// state was already <paramref name="value"/>).
        /// </summary>
        /// <remarks>
        /// The returned action must be invoked only AFTER <see cref="_lock"/> is released. Both the
        /// diagnostic log and the plugin event reach the editor's UI thread and block until it services
        /// them, while <see cref="StatusCheck"/> takes the same lock - so notifying while holding it
        /// deadlocks the editor: the notifying thread waits on the UI thread, which waits on the lock.
        /// </remarks>
        private Action SetIsConnected(bool value)
        {
            if (_isConnected == value)
            {
                return null;
            }
            _isConnected = value;

            return value
                ? new Action(() =>
                    {
                        LogDiagnostic("Connected (both sockets established)");
                        _eventCaller("GameCommunication_Connected", "");
                    })
                : new Action(() =>
                    {
                        LogDiagnostic("Disconnected");
                        _eventCaller("GameCommunication_Disconnected", "");
                    });
        }

        private CancellationTokenSource _periodicCheckTaskCancellationToken;

        #endregion

        #region properties
        private bool _doConnections = true;
        public bool DoConnections
        {
            get
            {
                return _doConnections;
            }
            set
            {
                _doConnections = value;
                if (!_doConnections)
                {
                    _listener?.Dispose();
                    glueToGameSocket?.Dispose();
                }
            }
        }

        private int _port;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if(_port != value)
                {
                    _port = value;
                    _listener?.Dispose();
                    glueToGameSocket?.Dispose();
                }
                
            }
        }
        private IPEndPoint EndPoint
        {
            get
            {
                return new IPEndPoint(_addr, Port);
            }
        }

        public double TimeoutInSeconds { get; set; } = 10;
        #endregion

        public static GameConnectionManager Self { get; set; }

        public GameConnectionManager(Action<string, string> eventCaller)
            : this(eventCaller, 0)
        {
        }

        /// <summary>
        /// Preferred over setting <see cref="Port"/> after construction: the constructor starts
        /// listening immediately, so assigning the port afterwards races the initial bind and can leave
        /// the listener on the wrong port with nothing to re-listen it.
        /// </summary>
        public GameConnectionManager(Action<string, string> eventCaller, int port)
        {
            _eventCaller = eventCaller;
            _addr = IPAddress.Loopback;
            _port = port;
            StartListening();
            _periodicCheckTaskCancellationToken = new CancellationTokenSource();
            Task task = StatusCheckTask(_periodicCheckTaskCancellationToken.Token);
        }

        #region privateMethods

        private void StartListening()
        {
            if (!_isListening)
            {
                lock (_lock)
                {
                    Debug.WriteLine("Entering StartListening()");

                    if (_listener != null)
                        _listener.Dispose();

                    if (glueToGameSocket != null)
                    {
                        glueToGameSocket.Dispose();
                        glueToGameSocket = null;
                    }

                    if (_doConnections)
                    {
                        _isListening = true;

                        Task.Run(() =>
                        {
                            try
                            {
                                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                                _listener.Bind(EndPoint);
                                _listener.Listen(2);

                                Debug.WriteLine($"Listening on port {Port}");

                                HandleConnection(_listener.Accept());
                                HandleConnection(_listener.Accept());

                                Action notify;
                                lock (_lock)
                                {
                                    notify = SetIsConnected(true);
                                }
                                notify?.Invoke();
                                // Vic asks - do we still need this?
                                _eventCaller("GameCommunication_Connected", "");

                                // Only now that the connection is marked live - the loop guards on
                                // IsConnected, so starting it any earlier races with the line above and
                                // can exit immediately, leaving nothing draining the game's messages.
                                StartGameToGlueReceiveLoop();

                                _listener.Dispose();
                                _listener = null;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Listening Error: {ex}");
                            }
                            finally
                            {
                                lock (_lock)
                                {
                                    _isListening = false;
                                }
                            }
                        });
                    }
                }
            }
        }

        private void HandleConnection(Socket socket)
        {
            // need to determine which way this socket is communicating
            var buffer = new byte[1];
            socket.Receive(buffer);

            if (buffer[0] == 1)
            {
                EstablishGlueToGameConnection(socket);
            }
            else if(buffer[0] == 2)
            {
                EstablishGameToGlueConnection(socket);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        private void EstablishGlueToGameConnection(Socket socket)
        {
            Debug.WriteLine("Connected Glue->Game");

            glueToGameSocket = socket;
        }

        private void EstablishGameToGlueConnection(Socket socket)
        {
            Debug.WriteLine("Connected Game->Glue");

            gameToGlueSocket = socket;
        }

        private void StartGameToGlueReceiveLoop()
        {
            // Captured once: ResetConnection nulls the field, so reading it mid-loop can NRE.
            var socket = gameToGlueSocket;

            Task.Run(async () =>
            {
                try
                {
                    while (IsConnected)
                    {
                        string stringFromGame = await ReceiveUnsolicitedString(socket);

                        if (stringFromGame == null)
                        {
                            // Socket closed or shut down by the other end. Tear down so StatusCheck
                            // re-handshakes rather than spinning on a dead socket.
                            ResetConnection("game->glue socket closed");
                            break;
                        }

                        string toReturn = null;

                        if (OnPacketReceived != null)
                        {
                            var packet = JsonConvert.DeserializeObject<Packet>(stringFromGame);

                            if (packet != null)
                            {
                                object response = await OnPacketReceived(packet);

                                if(response != null)
                                {
                                    try
                                    {
                                        if(response is string)
                                        {
                                            toReturn = response as string;
                                        }
                                        else
                                        {
                                            toReturn = JsonConvert.SerializeObject(response);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error serializing response: {ex}");
                                        break;
                                    }
                                }
                            }
                        }


                        // todo - need to send the string...
                        //socket.Send(new byte[] { 0 });
                        if (toReturn != null)
                        {
                            // This is a little noisy, but can be uncommented for more diagnostic info:
                            //PluginManager.CallPluginMethod("Compiler Plugin", "HandleOutput", $"Sending back {toReturn}");
                            var sendBytes = Encoding.ASCII.GetBytes(toReturn);
                            long size = sendBytes.LongLength;

                            //Send size
                            var bytesForSize = BitConverter.GetBytes(size);
                            socket.Send(bytesForSize);

                            System.Diagnostics.Debug.WriteLine($"{DateTime.Now}-Sent long for {size} size ({bytesForSize.Length})");

                            //Send payload
                            socket.Send(sendBytes);

                            System.Diagnostics.Debug.WriteLine($"{DateTime.Now}-Sent body with {sendBytes.Length} bytes");
                        }
                        else
                        {

                            var bytesForSize = BitConverter.GetBytes((long)0);
                            var sentBytes = socket.Send(bytesForSize);

                            System.Diagnostics.Debug.WriteLine($"{DateTime.Now}-Sent long(0) with {sentBytes} bytes");


                        }

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client Connection Failed: {ex}");
                }
                finally
                {
                    Action notify;
                    lock (_lock)
                    {
                        notify = SetIsConnected(false);
                    }
                    notify?.Invoke();
                }
            });
        }

        private async Task<string> SendItemImmediately(Packet item)
        {
            var socket = glueToGameSocket;
            try
            {
                var packet = JsonConvert.SerializeObject(item);
                var sendBytes = Encoding.ASCII.GetBytes(packet);
                long size = sendBytes.LongLength;

                //Send size
                socket.Send(BitConverter.GetBytes(size));

                //Send payload
                socket.Send(sendBytes);

                string responseString = await ReceiveResponseString(socket);
                return responseString;
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is NullReferenceException)
            {
                // The glueToGame socket died (game closed it, e.g. the game's own
                // reconnect loop disposed it). Nothing clears IsConnected on the SEND
                // path otherwise, so without this the manager stays "connected" on a
                // dead socket forever and every later send fails the same way. Tear the
                // whole connection down so StatusCheck re-listens and re-handshakes.
                ResetConnection($"send failed on '{item?.PacketType}': {ex.GetType().Name} {(ex as SocketException)?.SocketErrorCode}");
                throw;
            }
        }

        /// <summary>
        /// Fully tears down both sockets and marks the connection dead so the periodic
        /// StatusCheck re-listens. Must reset BOTH directions together: the handshake is
        /// order-dependent (byte 1 then byte 2, two accepts), so a half-reset would desync.
        /// </summary>
        private void ResetConnection(string reason)
        {
            Action notify = null;
            var didReset = false;

            lock (_lock)
            {
                if (!_isConnected && glueToGameSocket == null && gameToGlueSocket == null)
                {
                    // already torn down
                    return;
                }

                try { glueToGameSocket?.Dispose(); } catch { }
                try { gameToGlueSocket?.Dispose(); } catch { }
                glueToGameSocket = null;
                gameToGlueSocket = null;

                // Produces GameCommunication_Disconnected; StatusCheck then re-listens.
                notify = SetIsConnected(false);
                didReset = true;
            }

            // Outside the lock on purpose - see SetIsConnected. Logging blocks on the editor's UI
            // thread, and StatusCheck runs on that thread taking this same lock.
            if (didReset)
            {
                LogDiagnostic($"Resetting connection: {reason}");
            }
            notify?.Invoke();
        }

        /// <summary>
        /// Where diagnostics go. Injectable so tests can observe connect/reset transitions without a
        /// plugin host, and so the "never notify while holding the lock" guarantee above is testable.
        /// </summary>
        internal Action<string> LogAction { get; set; } = DefaultLogAction;

        private static void DefaultLogAction(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            try { PluginManager.CallPluginMethod("Compiler Plugin", "HandleOutput", message); } catch { }
        }

        private void LogDiagnostic(string message)
        {
            try { LogAction($"[GameConnection] {message}"); } catch { }
        }

        /// <summary>
        /// Reads a reply we are actively waiting for. A game that never answers (crashed mid-request,
        /// deadlocked in its own CommandReceiver) must not leave this awaiting forever, so the read is
        /// raced against <see cref="TimeoutInSeconds"/> and a timeout tears the connection down.
        /// </summary>
        private async Task<string> ReceiveResponseString(Socket socket)
        {
            try
            {
                byte[] bufferSize = new byte[sizeof(long)];

                // The synchronous Receive calls in ReadPayload honor ReceiveTimeout, but ReceiveAsync
                // does not - hence the explicit race below.
                socket.ReceiveTimeout = (int)(TimeoutInSeconds * 1000);

                ArraySegment<byte> buffer = new ArraySegment<byte>(bufferSize);
                var receiveTask = socket.ReceiveAsync(buffer, SocketFlags.None);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TimeoutInSeconds));
                if (await Task.WhenAny(receiveTask, timeoutTask) == timeoutTask)
                {
                    ResetConnection($"No response received within {TimeoutInSeconds} seconds");
                    return null;
                }
                await receiveTask;

                return ReadPayload(socket, BitConverter.ToInt64(bufferSize, 0));
            }
            catch (SocketException)
            {
                // This can mean the socket was closed, so return null
                return null;
            }
        }

        /// <summary>
        /// Reads the next unsolicited message from the game, waiting as long as it takes.
        /// </summary>
        /// <remarks>
        /// Deliberately has no timeout. This socket is idle whenever the user is not interacting with
        /// the game, which is most of the time, so applying the request/response timeout here would tear
        /// down a perfectly healthy connection every <see cref="TimeoutInSeconds"/> and leave the editor
        /// in a permanent reset/re-handshake loop. A genuinely dead socket still surfaces here promptly,
        /// as a SocketException or ObjectDisposedException rather than as silence.
        /// </remarks>
        private async Task<string> ReceiveUnsolicitedString(Socket socket)
        {
            try
            {
                byte[] bufferSize = new byte[sizeof(long)];

                // 0 means "no timeout" - the payload reads below should wait as long as the header did.
                socket.ReceiveTimeout = 0;

                var received = await socket.ReceiveAsync(new ArraySegment<byte>(bufferSize), SocketFlags.None);

                if (received == 0)
                {
                    // Orderly shutdown by the other end.
                    return null;
                }

                return ReadPayload(socket, BitConverter.ToInt64(bufferSize, 0));
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                // The socket was closed, so return null and let the caller tear the connection down.
                return null;
            }
        }

        private static string ReadPayload(Socket socket, long packetSize)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var remainingBytes = packetSize;
                while (remainingBytes > 0)
                {
                    var pullSize = remainingBytes > 1024 ? 1024 : remainingBytes;
                    byte[] bufferData = new byte[pullSize];
                    socket.Receive(bufferData);
                    stream.Write(bufferData, 0, bufferData.Length);
                    remainingBytes -= pullSize;
                }

                return Encoding.ASCII.GetString(stream.ToArray());
            }
        }

        private async Task StatusCheckTask(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                StatusCheck();
                // ConfigureAwait(false) keeps this loop off the editor's UI thread. Without it the
                // continuation resumes on the captured WinForms context, so StatusCheck takes _lock on
                // the UI thread ten times a second - one half of the deadlock SetIsConnected describes.
                await Task.Delay(100, cancellation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Re-listens if the connection is dead. Runs every 100ms; internal so tests can drive the
        /// lock contention it creates against <see cref="ResetConnection"/>.
        /// </summary>
        internal void StatusCheck()
        {
            lock (_lock)
            {
                if (!_isListening && !IsConnected && _doConnections)
                {
                    StartListening();
                }
            }
        }
        #endregion

        #region publicMethods

        public async Task SendItem(Packet item)
        {
            if (IsConnected)
            {
                await SendItemImmediately(item);
            }
        }

        public async Task<GeneralResponse<string>> SendItemWithResponse(Packet item)
        {
            var responseToReturn = new GeneralResponse<string>();

            if (IsConnected)
            {
                var responseString = await SendItemImmediately(item);

                responseToReturn.Succeeded = true;
                responseToReturn.Data = responseString;
            }
            else
            {
                responseToReturn.Succeeded = false;
                responseToReturn.Message = "Not connected";
            }
            return responseToReturn;
        }

        public void Dispose()
        {
            try { _periodicCheckTaskCancellationToken.Cancel(); } catch { }
            try { _listener?.Dispose(); } catch { }
            try { glueToGameSocket?.Dispose(); } catch { }
            try { gameToGlueSocket?.Dispose(); } catch { }
        }

        #endregion

        #region classes
        public class Packet
        {
            public string PacketType { get; set; }
            public string Payload { get; set; }
        }
#nullable enable
        public class WaitingPacket
        {
            public Guid WaitingFor { get; set; }
            public DateTime StartedWaitingAt { get; set; }
            public Packet? ReceivedPacket { get; set; }
        }

        #endregion

        public event Func<Packet, Task<object>> OnPacketReceived;
    }
}
