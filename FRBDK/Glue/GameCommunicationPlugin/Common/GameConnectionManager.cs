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

namespace GameJsonCommunicationPlugin.Common
{
    internal class GameConnectionManager : IDisposable
    {
        #region static
        public static GameConnectionManager Instance { get; private set; }

        private CancellationTokenSource _periodicCheckTaskCancellationToken;
        #endregion

        static GameConnectionManager()
        {
            Instance = new GameConnectionManager();

        }

        #region private
        private ConcurrentQueue<Packet> _sendItems = new ConcurrentQueue<Packet>();
        private IPAddress _addr;
        private Socket _listener = null;
        private Socket _client = null;
        private bool _isListening = false;
        private bool _isConnected = false;

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
                    _client?.Dispose();
                }
            }
        }

        private int _port = 8888;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
                StartListening();
            }
        }
        private IPEndPoint EndPoint
        {
            get
            {
                return new IPEndPoint(_addr, Port);
            }
        }
        #endregion

        public GameConnectionManager()
        {
            _addr = IPAddress.Loopback;
            StartListening();
            _periodicCheckTaskCancellationToken = new CancellationTokenSource();
            Task task = StatusCheckTask(_periodicCheckTaskCancellationToken.Token);
        }

        #region privateMethods

        private void StartListening()
        {
            if (_listener != null)
                _listener.Dispose();

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
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
                        _listener.Listen(1);

                        Debug.WriteLine($"Listening on port {Port}");

                        EstablishConnection(_listener.Accept());
                        _listener.Dispose();
                        _listener = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Listening Error: {ex}");
                    }
                    finally
                    {
                        _isListening = false;
                    }
                });
            }
        }

        private void EstablishConnection(Socket socket)
        {
            Debug.WriteLine("Connected");

            _client = socket;
            _sendItems.Clear();

            _isConnected = true;

            Task.Run(() =>
            {
                try
                {
                    while (_isConnected)
                    {
                        byte[] bufferSize = new byte[sizeof(long)];
                        _client.Receive(bufferSize);
                        var packetSize = BitConverter.ToInt64(bufferSize, 0);

                        using (MemoryStream stream = new MemoryStream())
                        {
                            var remainingBytes = packetSize;
                            while (remainingBytes > 0)
                            {
                                var pullSize = remainingBytes > 1024 ? 1024 : remainingBytes;
                                byte[] bufferData = new byte[pullSize];
                                _client.Receive(bufferData);
                                stream.Write(bufferData, 0, bufferData.Length);
                                remainingBytes -= pullSize;
                            }

                            var payload = Encoding.ASCII.GetString(stream.ToArray());

                            if (OnPacketReceived != null)
                            {
                                var packet = JsonConvert.DeserializeObject<Packet>(payload);

                                if (packet != null)
                                    OnPacketReceived(new PacketReceivedArgs
                                    {
                                        Packet = packet
                                    });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client Connection Failed: {ex}");
                }
                finally { _isConnected = false; }
            });

            Task.Run(() =>
            {
                try
                {
                    while (_isConnected)
                    {
                        if (_sendItems.TryDequeue(out var item))
                        {
                            var packet = JsonConvert.SerializeObject(item);
                            var sendBytes = Encoding.ASCII.GetBytes(packet);
                            long size = sendBytes.LongLength;

                        //Send size
                            _client.Send(BitConverter.GetBytes(size));

                        //Send payload
                            _client.Send(sendBytes);
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write($"Client Connection Failed: {ex}");
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
            if (!_isListening && !_isConnected && _doConnections)
            {
                StartListening();
            }
        }
        #endregion

        #region publicMethods

        public void SendItem(Packet item)
        {
            if (_isConnected)
                _sendItems.Enqueue(item);
        }

        public void Dispose()
        {
            try { _periodicCheckTaskCancellationToken.Cancel(); } catch { }
            try { _listener?.Dispose(); } catch { }
            try { _client?.Dispose(); } catch { }
        }

        #endregion

        #region classes
        public class Packet
        {
            public string PacketType { get; set; }
            public string Payload { get; set; }
        }

        public class PacketReceivedArgs : EventArgs
        {
            public Packet Packet { get; set; }
        }
        #endregion

        #region events

        public delegate void PacketReceivedDelegate(PacketReceivedArgs packetReceivedArgs);
        public event PacketReceivedDelegate OnPacketReceived;

        #endregion
    }
}
