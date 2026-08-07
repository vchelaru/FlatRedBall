#define IncludeSetVariable
// The following #defines come from the version of your GLUJ/GLUX file. For more information see https://docs.flatredball.com/flatredball/glue-reference/glujglux
#define PreVersion
#define HasFormsObject
#define AddedGeneratedGame1
#define ListsHaveAssociateWithFactoryBool
#define GumGueHasGetAnimation
#define CsvInheritanceSupport
#define IPositionedSizedObjectInEngine
#define NugetPackageInCsproj
#define SupportsEditMode
#define SupportsShapeCollectionAddToManagerMakeAutomaticallyUpdated
#define ScreensHaveActivityEditMode
#define SupportsNamedSubcollisions
#define TimeManagerHasDelaySeconds
#define GumTextHasIsBold
#define GlueSavedToJson
#define IEntityInFrb
#define SeparateJsonFilesForElements
#define GumSupportsAchxAnimation
#define StartupInGeneratedGame
#define RemoveAutoLocalizationOfVariables
#define GumHasMIsLayoutSuspendedPublic
#define SpriteHasUseAnimationTextureFlip
#define RemoveIsScrollableEntityList
#define HasGetGridLine
#define HasScreenManagerAfterScreenDestroyed
#define ScreenManagerHasPersistentPolygons
#define ShapeManagerCollideAgainstClosest
#define SpriteHasTolerateMissingAnimations
#define AnimationLayerHasName
#define IPlatformer
#define GumDefaults2
#define IStackableInEngine
#define ICollidableHasItemsCollidedAgainst
#define CollisionRelationshipManualPhysics
#define GumSupportsStackSpacing
#define CollisionRelationshipsSupportMoveSoft
#define GeneratedCameraSetupFile
#define ShapeCollectionHasMaxAxisAlignedRectanglesRadiusX
#define AutoNameCollisionListsAsSingle
#define GumHasIgnoredByParentSize
#define GumTextObjectsUpdateTextWith0ChildDepth
#define HasFrameworkElementManager
#define HasGumSkiaElements
#define ITiledTileMetadataInFrb
#define DamageableHasHealth
#define HasGame1GenerateEarly
#define ICollidableHasObjectsCollidedAgainst
#define HasIRepeatPressableInput
#define AllTiledFilesGenerated
#define RemoveRedundantDerivedData
#define GraphicalUiElementProtectedAnimationProperties
#define GraphicalUiElementINotifyPropertyChanged
#define GumTextObjectsHaveTextOverflowProperties
#define TileShapeCollectionIsICollidable
#define TileShapeCollectionAddToLayerSupportsAutomaticallyUpdated
#define ISongInFrb
#define RendererHasExternalEffectManager
#define SpriteHasSetCollisionFromAnimation
#define HasIGumScreenOwner
#define ScreenIsINameable
#define SpriteManagerHasInsertLayer
#define GumUsesSystemTypes
#define GumCommonCodeReferencing
#define GumTextSupportsBbCode
#define DamageDealingToggles
#define VariantsInsteadOfTypes
#define ITopDownEntity
#define CaseSensitiveLoading
#define ScreensHaveDefaultLayer
#define HasFrbServicesGraphicsDeviceManager
#define ShapeCollectionHasLastCollisionCallDeepCheckCount
#define ScreenHasCancellationToken
#define GameCanStartInEditMode
#define GumHasRenderableCloneLogic
#define ShapeCollectionHasIsPointOnOrInside
#define AudioManagerStopSongTakesBool
#define GraphicalUiElementRemoveFromManagersIsVirtual
#define GumVisualHasRenderTarget
#define GumNineSliceHasAnimate
#define ObsoleteGumDimensionUnitTypes
#define GumHasIRenderTargetTextureReferencer
#define GumHasGueVirtualIsPointInside
#define PositionedNodeHasTag
#define NineSliceHasTilingMiddleSections
#define GumHasFrbRuntimeInterfaces
using EditorTest1;

﻿using Newtonsoft.Json;
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

            Task.Run(async () =>
            {
                try
                {
                    while (_isConnected)
                    {
                        string stringFromGlue = await ReceiveStringAsync(glueToGameSocket);

#if GLUE

#else
                        var packet = JsonConvert.DeserializeObject<Packet>(stringFromGlue);

                        if(packet != null)
                        {

                            var returnValue = await GlueControl.GlueControlManager.Self?.ProcessMessage(packet.Payload);

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

        public async Task<string> SendItem(Packet item)
        {
            if (_isConnected)
            {
                return await SendItemImmediately(item);
            }
            else
            {
                return String.Empty;
            }
        }

        // we can't have multiple sends overlap, so use a semaphore to prevent that:
        SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        Packet lastSentPacket = null;
        DateTime? lastSentPacketTime;
        SendStatus? lastSendStatus;

        enum SendStatus
        {
            Serializing,
            SendingSize,
            SendingBytes,
            GettingResponse,
            GettingResponseBytes,
            GettingResponseBody
        }

        private async Task<string> SendItemImmediately(Packet item)
        {

            await sendSemaphore.WaitAsync();

            try
            {
                lastSentPacket = item;
                lastSentPacketTime = DateTime.Now;
                lastSendStatus = SendStatus.Serializing;
                var packet = JsonConvert.SerializeObject(item);
                var sendBytes = Encoding.ASCII.GetBytes(packet);
                long size = sendBytes.LongLength;

                lastSendStatus = SendStatus.SendingSize;
                //Send size
                gameToGlueSocket.Send(BitConverter.GetBytes(size));

                lastSendStatus = SendStatus.SendingBytes;
                //Send payload
                gameToGlueSocket.Send(sendBytes);

                lastSendStatus = SendStatus.GettingResponse;

                // August 24, 2024
                // Vic was able to get
                // a deadlock by making
                // this async. By making
                // it not async, the game
                // freezes a little bit when
                // waiting for commands from Glue,
                // but it's better than it breaking.
                // Also by not making this async we might
                // be able to track down other desyncs better
                // so I'm going to leave it like this for now.
                // Update - by making this not async, we do solve
                // the problem of selecting one screen then the other
                // but we break being able to launch the game and view
                // it in the editor, so it must be async
#if ScreenHasCancellationToken || REFERENCES_FRB_SOURCE
                string responseString = await ReceiveStringBackFromGame(gameToGlueSocket);
#else
                string responseString = await ReceiveStringAsync(gameToGlueSocket);
#endif
                return responseString;
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is NullReferenceException)
            {
                // Mirror of the Glue-side fix: the send direction dying must tear the
                // whole connection down, otherwise _isConnected stays true on a dead
                // socket and StatusCheck never reconnects.
                ResetConnection($"send failed on '{item?.PacketType}': {ex.GetType().Name}");
                throw;
            }
            finally
            {
                lastSendStatus = null;
                sendSemaphore.Release();
            }
        }

        private void ResetConnection(string reason)
        {
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine($"[GameConnection] Resetting connection: {reason}");

                _isConnected = false;

                try { glueToGameSocket?.Dispose(); } catch { }
                try { gameToGlueSocket?.Dispose(); } catch { }
                glueToGameSocket = null;
                gameToGlueSocket = null;
                // StatusCheck sees !_isConnected and calls StartConnecting to re-handshake.
            }
        }

#if ScreenHasCancellationToken || REFERENCES_FRB_SOURCE
        private async Task<string> ReceiveStringBackFromGame(Socket socket)
        {
            byte[] bufferSize = new byte[sizeof(long)];
            ArraySegment<byte> buffer = new ArraySegment<byte>(bufferSize);
            lastSendStatus = SendStatus.GettingResponseBytes;

            string responseString = null;
            try
            {
                var amountReceived = await socket.ReceiveAsync(buffer,
                    SocketFlags.None);
                var packetSize = BitConverter.ToInt64(bufferSize, 0);

                System.Diagnostics.Debug.WriteLine($"{DateTime.Now}-Received {amountReceived}, body size {packetSize}");

                if (packetSize > 0)
                {
                    lastSendStatus = SendStatus.GettingResponseBody;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        var remainingBytes = packetSize;
                        while (remainingBytes > 0)
                        {
                            var pullSize = remainingBytes > 1024 ? 1024 : remainingBytes;
                            byte[] bufferData = new byte[pullSize];
                            var amountRead = socket.Receive(bufferData);
                            stream.Write(bufferData, 0, bufferData.Length);
                            remainingBytes -= amountRead;
                        }

                        responseString = Encoding.ASCII.GetString(stream.ToArray());
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            return responseString;
        }
#endif
        private async Task<string> ReceiveStringAsync(Socket socket)
        {
            byte[] bufferSize = new byte[sizeof(long)];
            ArraySegment<byte> buffer = new ArraySegment<byte>(bufferSize);
            lastSendStatus = SendStatus.GettingResponseBytes;

            await socket.ReceiveAsync(buffer, SocketFlags.None);

            var packetSize = BitConverter.ToInt64(bufferSize, 0);

            string responseString = null;

            if (packetSize > 0)
            {
                lastSendStatus = SendStatus.GettingResponseBody;

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
            }

            return responseString;
        }


        public async Task<string> SendItemWithResponse(Packet item)
        {
            if (_isConnected)
            {
                var responseString = await SendItemImmediately(item);

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
            public string PacketType { get; set; }
            public string Payload { get; set; }
        }

        
        public class WaitingPacket
        {
            public Guid WaitingFor { get; set; }
            public DateTime StartedWaitingAt { get; set; }
            public Packet ReceivedPacket { get; set; }
        }
        #endregion

    }
}
