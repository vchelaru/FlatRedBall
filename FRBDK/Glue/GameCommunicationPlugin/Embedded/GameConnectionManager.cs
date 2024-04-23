using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GlueCommunication
{
    internal class GameConnectionManager : IDisposable
    {
        public static bool CanUseJsonManager = false;

        #region private
        private Object _lock = new Object();
        private IPAddress _addr;
        private Socket glueToGameSocket = null;
        private Socket gameToGlueSocket = null;
        private bool _isConnected = false;
        private bool _isConnecting = false;
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
                    glueToGameSocket?.Dispose();
                    gameToGlueSocket?.Dispose();
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

        public GameConnectionManager(int port)
        {
            _port = port;
            _addr = IPAddress.Loopback;
            StartConnecting();
            _periodicCheckTaskCancellationToken = new CancellationTokenSource();
            var task = StatusCheckTask(_periodicCheckTaskCancellationToken.Token);
        }

        #region privateMethods

        private void StartConnecting()
        {
            if (gameToGlueSocket != null)
                gameToGlueSocket.Dispose();
            if(glueToGameSocket != null)
                glueToGameSocket.Dispose();

            Task.Run(() =>
            {
                try
                {
                    if (!_isConnecting)
                    {
                        _isConnecting = true;

                        glueToGameSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        glueToGameSocket.Connect(EndPoint);
                        glueToGameSocket.Send(new byte[] { 1 });

                        gameToGlueSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        gameToGlueSocket.Connect(EndPoint);
                        gameToGlueSocket.Send(new byte[] { 2 });

                        Connected();
                    }
                }
                catch (Exception)
                {
                    //Debug.WriteLine($"Listening Error: {ex}");
                }
                finally
                {
                    _isConnecting = false;
                }
            });
        }

        private void Connected()
        {
            Debug.WriteLine("Connected");
            _isConnected = true;

            Task.Run(() =>
            {
                try
                {
                    while (_isConnected)
                    {
                        string stringFromGlue = ReceiveString(glueToGameSocket);

#if GLUE

#else
                        var packet = JsonConvert.DeserializeObject<Packet>(stringFromGlue);

                        if(packet != null)
                        {

                            var returnValue = GlueControl.GlueControlManager.Self?.ProcessMessage(packet.Payload);

                            var sendBytes = returnValue != null
                                ? Encoding.ASCII.GetBytes(returnValue)
                                : new byte[0];
                            long size = sendBytes.LongLength;

                            //Send size
                            var bytes = BitConverter.GetBytes(size);
                            try
                            {
                                glueToGameSocket.Send(bytes);
                            }
                            catch (ObjectDisposedException) { }

                            try
                            {
                                //Send payload
                                if (size > 0)
                                {
                                    //Send payload
                                    glueToGameSocket.Send(sendBytes);
                                }
                            }
                            catch (ObjectDisposedException) { }
                        }
#endif

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Server Connection Failed: {ex}");
                }
                finally { _isConnected = false; }
            });
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
            if (!_isConnecting && !_isConnected && _doConnections)
            {
                StartConnecting();
            }
        }
#endregion

        #region publicMethods

        public void SendItem(Packet item)
        {
            if (_isConnected)
                SendItemImmediately(item);
        }

        private string SendItemImmediately(Packet item)
        {
            var packet = JsonConvert.SerializeObject(item);
            var sendBytes = Encoding.ASCII.GetBytes(packet);
            long size = sendBytes.LongLength;

            //Send size
            gameToGlueSocket.Send(BitConverter.GetBytes(size));

            //Send payload
            gameToGlueSocket.Send(sendBytes);

            string responseString = ReceiveString(gameToGlueSocket);
            return responseString;
        }

        private static string ReceiveString(Socket socket)
        {
            byte[] bufferSize = new byte[sizeof(long)];

            socket.Receive(bufferSize);
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


        public async Task<string> SendItemWithResponse(Packet item)
        {
            if (_isConnected)
            {
                var responseString = SendItemImmediately(item);

                return responseString;
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _periodicCheckTaskCancellationToken.Cancel(); } catch { }
            try { gameToGlueSocket?.Dispose(); } catch { }
            try { glueToGameSocket?.Dispose(); } catch { }
        }

        #endregion

        #region classes
        public class Packet
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public Guid? InResponseTo { get; set; }
            public string PacketType { get; set; }
            public string Payload { get; set; }
        }

        public class PacketReceivedArgs : EventArgs
        {
            public Packet Packet { get; set; }
        }

        public class WaitingPacket
        {
            public Guid WaitingFor { get; set; }
            public DateTime StartedWaitingAt { get; set; }
            public Packet ReceivedPacket { get; set; }
        }
        #endregion

        #region events

        public delegate void PacketReceivedDelegate(PacketReceivedArgs packetReceivedArgs);


        #endregion
    }
}
