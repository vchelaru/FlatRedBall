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

namespace GlueCommunication
{
    internal class GameConnectionManager : IDisposable
    {
        public static bool CanUseJsonManager = false;

        private void HandleOnPacketReceived(GameConnectionManager.PacketReceivedArgs packetReceivedArgs)
        {
            if (packetReceivedArgs?.Packet?.Payload != "GetCommandsDto:{}")
                Debug.WriteLine($"Packet Type: {packetReceivedArgs.Packet.PacketType}, Payload: {packetReceivedArgs.Packet.Payload}");
        }

        #region private
        private Object _lock = new Object();
        private ConcurrentQueue<Packet> _sendItems = new ConcurrentQueue<Packet>();
        private ConcurrentDictionary<Guid, WaitingPacket> _waitingPackets = new ConcurrentDictionary<Guid, WaitingPacket>();
        private IPAddress _addr;
        private Socket _server = null;
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
                    _server?.Dispose();
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
            OnPacketReceived += HandleOnPacketReceived;
            _addr = IPAddress.Loopback;
            StartConnecting();
            _periodicCheckTaskCancellationToken = new CancellationTokenSource();
            var task = StatusCheckTask(_periodicCheckTaskCancellationToken.Token);
        }

        #region privateMethods

        private void StartConnecting()
        {
            if (_server != null)
                _server.Dispose();

            Task.Run(() =>
            {
                try
                {
                    if (!_isConnecting)
                    {
                        _isConnecting = true;

                        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        Debug.WriteLine($"Connecting on port {Port}");
                        _server.Connect(EndPoint);

                        Connected();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Listening Error: {ex}");
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
                        byte[] bufferSize = new byte[sizeof(long)];
                        _server.Receive(bufferSize);
                        var packetSize = BitConverter.ToInt64(bufferSize, 0);

                        using (MemoryStream stream = new MemoryStream())
                        {
                            var remainingBytes = packetSize;
                            while (remainingBytes > 0)
                            {
                                var pullSize = remainingBytes > 1024 ? 1024 : remainingBytes;
                                byte[] bufferData = new byte[pullSize];
                                _server.Receive(bufferData);
                                stream.Write(bufferData, 0, bufferData.Length);
                                remainingBytes -= pullSize;
                            }

                            var payload = Encoding.ASCII.GetString(stream.ToArray());

                            if (OnPacketReceived != null)
                            {
                                var packet = JsonConvert.DeserializeObject<Packet>(payload);

                                if (packet != null)
                                {
                                    if (packet.InResponseTo.HasValue && _waitingPackets.TryGetValue(packet.InResponseTo.Value, out var waitingPacket))
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
                    Debug.WriteLine($"Server Connection Failed: {ex}");
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
                            try
                            {
                                var packet = JsonConvert.SerializeObject(item);
                                var sendBytes = Encoding.ASCII.GetBytes(packet);
                                long size = sendBytes.LongLength;

                                //Send size
                                var bytes = BitConverter.GetBytes(size);
                                try
                                {
                                    _server.Send(bytes);
                                }
                                catch(ObjectDisposedException) { }

                                try
                                {
                                    //Send payload
                                    _server.Send(sendBytes);
                                }
                                catch (ObjectDisposedException) { }

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
                _sendItems.Enqueue(item);
        }

        public async Task<Packet> SendItemWithResponse(Packet item)
        {
            if (_isConnected)
            {
                _sendItems.Enqueue(item);
                _waitingPackets.TryAdd(item.Id, new WaitingPacket
                {
                    StartedWaitingAt = DateTime.Now,
                    WaitingFor = item.Id
                });

                return await Task.Run(async () =>
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
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _periodicCheckTaskCancellationToken.Cancel(); } catch { }
            try { _server?.Dispose(); } catch { }
            OnPacketReceived -= HandleOnPacketReceived;
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
        public event PacketReceivedDelegate OnPacketReceived;

        #endregion
    }
}
