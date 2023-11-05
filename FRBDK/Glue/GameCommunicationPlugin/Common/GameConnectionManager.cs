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
        private ConcurrentQueue<Packet> _sendItems = new ConcurrentQueue<Packet>();
        private ConcurrentDictionary<Guid, WaitingPacket> _waitingPackets = new ConcurrentDictionary<Guid, WaitingPacket>();
        private Action<string, string> _eventCaller;
        private IPAddress _addr;
        private Socket _listener = null;
        private Socket _client = null;
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
                    _client?.Dispose();
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
                    _client?.Dispose();
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

        private void EstablishConnection(Socket socket)
        {
            Debug.WriteLine("Connected");

            _client = socket;
            _sendItems.Clear();
            _waitingPackets.Clear();

            IsConnected = true;

            _eventCaller("GameCommunication_Connected", "");

            Task.Run(() =>
            {
                try
                {
                    while (IsConnected)
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
                                {
                                    if(packet.InResponseTo.HasValue && _waitingPackets.TryGetValue(packet.InResponseTo.Value, out var waitingPacket))
                                    {
                                        waitingPacket.ReceivedPacket = packet;
                                    }
                                    else
                                    {
                                        OnPacketReceived(new PacketReceivedArgs
                                        {
                                            Packet = packet
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client Connection Failed: {ex}");
                }
                finally { IsConnected = false; }
            });

            Task.Run(() =>
            {
                try
                {
                    while (IsConnected)
                    {
                        if (_sendItems.TryDequeue(out var item))
                        {
                            try
                            {
                                var packet = JsonConvert.SerializeObject(item);
                                var sendBytes = Encoding.ASCII.GetBytes(packet);
                                long size = sendBytes.LongLength;

                                //Send size
                                _client.Send(BitConverter.GetBytes(size));

                                //Send payload
                                _client.Send(sendBytes);
                            }
                            catch
                            {
                                Debug.WriteLine($"Removing Wait Id: {item.Id} due to error");
                                _waitingPackets.TryRemove(item.Id, out var tempValue);
                                throw;
                            }
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
                finally { IsConnected = false; }
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

        public void SendItem(Packet item)
        {
            if (IsConnected)
                _sendItems.Enqueue(item);
        }

        public async Task<GeneralResponse<Packet>> SendItemWithResponse(Packet item)
        {
            var responseToReturn = new GeneralResponse<Packet>();

            if (IsConnected)
            {
                _sendItems.Enqueue(item);
                _waitingPackets.TryAdd(item.Id, new WaitingPacket
                {
                    StartedWaitingAt = DateTime.Now,
                    WaitingFor = item.Id
                });

                var packet = await Task.Run(async () =>
                {
                    do
                    {
                        await Task.Delay(10);

                        if (_waitingPackets.TryGetValue(item.Id, out var waitingPacket))
                        {
                            if (waitingPacket.ReceivedPacket != null)
                            {
                                _waitingPackets.TryRemove(item.Id, out var tempPacket);
                                return waitingPacket.ReceivedPacket;
                            }
                            else if ((DateTime.Now - waitingPacket.StartedWaitingAt).TotalSeconds > TimeoutInSeconds)
                            {
                                Debug.WriteLine($"Removing Wait Id: {item.Id} due to timeout");
                                _waitingPackets.TryRemove(item.Id, out var tempPacket);
                                return (Packet)null;
                            }
                        }
                        else
                        {
                            return (Packet)null;
                        }
                    }
                    while (true);
                });

                responseToReturn.Succeeded = packet != null;
                responseToReturn.Data = packet;
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
            try { _client?.Dispose(); } catch { }
        }

        #endregion

        #region classes
        public class Packet
        {
            public Guid Id { get; private set; } = Guid.NewGuid();
            public Guid? InResponseTo { get; set; }
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

        public class PacketReceivedArgs : EventArgs
        {
            public Packet? Packet { get; set; }
        }
        #endregion

        #region events

        public delegate void PacketReceivedDelegate(PacketReceivedArgs packetReceivedArgs);
        public event PacketReceivedDelegate OnPacketReceived;

        #endregion
    }
}
