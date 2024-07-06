using Newtonsoft.Json;
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
        private bool IsConnected
        {
            get
            {
                return _isConnected;
            }

            set
            {
                _isConnected = value;

                if(_isConnected)
                    _eventCaller("GameCommunication_Connected", "");
                else
                    _eventCaller("GameCommunication_Disconnected", "");
            }
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
        {
            _eventCaller = eventCaller;
            _addr = IPAddress.Loopback;
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
                                IsConnected = true;
                                // Vic asks - do we still need this?
                                _eventCaller("GameCommunication_Connected", "");

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

            Task.Run(async () =>
            {
                try
                {
                    while (IsConnected)
                    {
                        string stringFromGame = await ReceiveString(gameToGlueSocket);

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
                                        toReturn = JsonConvert.SerializeObject(response);
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
                        gameToGlueSocket.Send(new byte[] { 0 });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client Connection Failed: {ex}");
                }
                finally { IsConnected = false; }
            });
        }

        private async Task<string> SendItemImmediately(Packet item)
        {
            var packet = JsonConvert.SerializeObject(item);
            var sendBytes = Encoding.ASCII.GetBytes(packet);
            long size = sendBytes.LongLength;

            //Send size
            glueToGameSocket.Send(BitConverter.GetBytes(size));

            //Send payload
            glueToGameSocket.Send(sendBytes);

            string responseString = await ReceiveString(glueToGameSocket);
            return responseString;
        }

        private static async Task<string> ReceiveString(Socket socket)
        {
            try
            {
                byte[] bufferSize = new byte[sizeof(long)];
                //socket.Receive(bufferSize);

                ArraySegment<byte> buffer = new ArraySegment<byte>(bufferSize);
                await socket.ReceiveAsync(buffer, SocketFlags.None);

                var packetSize = BitConverter.ToInt64(bufferSize, 0);

                string responseString = null;

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

                    responseString = Encoding.ASCII.GetString(stream.ToArray());
                }

                return responseString;

            }
            catch(SocketException)
            {
                // This can mean the socket was closed, so return null
                return null;
            }
        }

        private async Task StatusCheckTask(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                StatusCheck();
                await Task.Delay(100, cancellation);
            }
        }

        private void StatusCheck()
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
