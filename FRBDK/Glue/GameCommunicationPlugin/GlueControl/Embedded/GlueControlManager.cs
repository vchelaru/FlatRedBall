using GlueControl.Dtos;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using GlueControl.Editing;
using FlatRedBall.Utilities;

#if SupportsEditMode
using GlueControl.Managers;
using Newtonsoft.Json;
#endif

namespace GlueControl
{
    public class GlueControlManager
    {
        #region Classes

        public class GameToGlueCommand
        {
            public string Command { get; set; }
        }

        public class AwaitedResponse
        {
            public SemaphoreSlim Semaphore { get; set; }
            public object Response { get; set; }
        }

        #endregion

        #region Fields/Properties

        //bool isRunning;
        //private TcpListener listener;
        public static ConcurrentQueue<GameToGlueCommand> GameToGlueCommands { get; private set; }
            = new ConcurrentQueue<GameToGlueCommand>();
        private GlueControl.Editing.EditingManager EditingManager;

        ConcurrentDictionary<int, AwaitedResponse> AwaitedResponses = new ConcurrentDictionary<int, AwaitedResponse>();

        public static GlueControlManager Self { get; private set; }

        #endregion

        #region Init/Start/Stop
        public GlueControlManager(int port)
        {
            Self = this;
            EditingManager = new GlueControl.Editing.EditingManager();
            FlatRedBallServices.AddManager(EditingManager);
            EditingManager.PropertyChanged += HandlePropertyChanged;
            EditingManager.ObjectSelected += HandleObjectSelected;
            //listener = new TcpListener(IPAddress.Any, port);
        }

        //public void Start()
        //{
        //    Thread serverThread = new Thread(new ThreadStart(Run));

        //    serverThread.Start();
        //}

        //private async void Run()
        //{
        //    var isGameAlreadyRunning = false;
        //    try
        //    {
        //        listener.Start();
        //        isRunning = true;
        //    }
        //    catch (System.Net.Sockets.SocketException e)
        //    {
        //        isGameAlreadyRunning = true;
        //    }

        //    if (!isGameAlreadyRunning)
        //    {
        //        TcpClient client = null;

        //        // This can throw an exception when the game is exiting:

        //        while (isRunning)
        //        {
        //            if (client == null)
        //            {
        //                try
        //                {
        //                    client = listener.AcceptTcpClient();
        //                }
        //                catch
        //                {
        //                    // so it doesn't spam:
        //                    await Task.Delay(100);
        //                }
        //            }

        //            if (client != null)
        //            {
        //                var stream = client.GetStream();
        //                try
        //                {
        //                    await HandleStream(stream);
        //                }
        //                catch (IOException)
        //                {
        //                    client = null;
        //                }
        //                catch (System.Exception e)
        //                {
        //                    if (isRunning)
        //                    {
        //                        throw e;
        //                    }
        //                }
        //            }
        //        }

        //        isRunning = false;

        //        listener.Stop();
        //    }
        //}

        //public void Kill()
        //{
        //    isRunning = false;
        //    listener.Stop();

        //}

        //private async Task HandleStream(Stream clientStream)
        //{
        //    var messageFromClient = await ReadFromClient(clientStream);

        //    var response = await ProcessMessage(messageFromClient?.Trim());

        //    if (response == null)
        //    {
        //        response = "true";
        //    }

        //    WriteMessageToStream(clientStream, response);

        //}

        private static async Task<string> ReadFromClient(Stream stm)
        {
            //// Read response from server.
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);

            byte[] intBytes = await GetByteArrayFromStream(stm, 4, new byte[4]);
            var length = BitConverter.ToInt32(intBytes, 0);

            if (length > 1_000_000)
            {
                int m = 3;
            }
            if (length > 0)
            {
                byte[] byteArray = await GetByteArrayFromStream(stm, length);
                var response = Encoding.UTF8.GetString(byteArray, 0, length);
                return response;
            }
            else
            {
                return string.Empty;
            }
            //Console.ReadLine();
        }

        static byte[] defaultBuffer = new byte[8192];
        private static async Task<byte[]> GetByteArrayFromStream(Stream stm, int totalLength, byte[] buffer = null)
        {
            byte[] toReturn = null;

            using (var memoryStream = new MemoryStream())
            {
                buffer = buffer ?? defaultBuffer;
                int bytesRead;
                int bytesLeft = totalLength;
                while (bytesLeft > 0 && (bytesRead = await stm.ReadAsync(buffer, 0, Math.Min(buffer.Length, bytesLeft))) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                    bytesLeft -= bytesRead;
                }
                toReturn = memoryStream.ToArray();
            }
            return toReturn;
            //var memoryStream = new MemoryStream();
            //await stm.CopyToAsync(memoryStream, totalLength);
            //memoryStream.Position = 0;

            //var array =
            //    memoryStream.ToArray();
            //if(array.Length != totalLength)
            //{
            //    int m = 3;
            //}
            //return array;

            //if (buffer == null && totalLength > defaultBuffer.Length)
            //{
            //    defaultBuffer = new byte[totalLength];
            //}
            //buffer = buffer ?? defaultBuffer;
            //var amountRead = await stm.ReadAsync(buffer, 0, totalLength);

            //if (amountRead != totalLength)
            //{
            //    int m = 3;
            //}

            //return buffer;
        }
        private static void WriteMessageToStream(Stream clientStream, string message)
        {
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(message);

            if (messageAsBytes.Length > 0)
            {
                var lengthAsBytes =
                    BitConverter.GetBytes(messageAsBytes.Length);
                clientStream.Write(lengthAsBytes, 0, lengthAsBytes.Length);
                clientStream.Write(messageAsBytes, 0, messageAsBytes.Length);

            }
        }

        #endregion

        #region Glue -> Game

        public async Task<string> ProcessMessage(string message, bool runSetImmediately = false)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool isGet = false;

            string data = null;

            string action = message;

            if (message.Contains(":"))
            {
                data = message.Substring(message.IndexOf(":") + 1);
                action = message.Substring(0, message.IndexOf(":"));
            }


            switch (action)
            {
                case "GetCurrentScreen":
                    isGet = true;
                    return screen?.GetType().FullName;
            }

            var response = "true";

            if (!isGet)
            {
                if (message.Contains(":") == false)
                {
                    int m = 3;
                }
                if (runSetImmediately)
                {
                    response = ApplySetMessage(message) ?? null;
                }
                else
                {
                    await FlatRedBall.Instructions.InstructionManager.DoOnMainThreadAsync(
                        () => response = ApplySetMessage(message));

                }
            }

            return response;
        }

        private string ApplySetMessage(string message)
        {
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new System.InvalidOperationException("Objects can only be added, removed, made visible, or made invisible on the primary thread");
            }
            return CommandReceiver.Receive(message);
        }

        public void ReRunAllGlueToGameCommands()
        {
            var toProcess = CommandReceiver.GlobalGlueToGameCommands.ToArray();
            CommandReceiver.GlobalGlueToGameCommands.Clear();

            foreach (var dto in toProcess)
            {
                CommandReceiver.ReceiveDto(dto);
            }
        }

        private bool GetIfMatchesCurrentScreen(string elementName, out System.Type ownerType, out Screen currentScreen)
        {
            var game1FullName = typeof(Game1).FullName;
            var topNamespace = game1FullName.Substring(0, game1FullName.IndexOf('.'));
            //var ownerTypeName = "WhateverNamespace." + elementName.Replace("\\", ".");
            var ownerTypeName = $"{topNamespace}.{elementName.Replace("\\", ".")}";

            ownerType = GetType().Assembly.GetType(ownerTypeName);
            currentScreen = ScreenManager.CurrentScreen;
            var currentScreenType = currentScreen.GetType();

            return currentScreenType == ownerType || ownerType?.IsAssignableFrom(currentScreenType) == true;
        }

        #endregion

        #region Game -> Glue

        private async void HandlePropertyChanged(List<PropertyChangeArgs> propertyChangeArgs)
        {
#if SupportsEditMode
            var currentElement = GlueState.Self.CurrentElement;
            List<NosVariableAssignment> nosVariableAssignments = new List<NosVariableAssignment>();
            foreach (var change in propertyChangeArgs)
            {
                var nos = currentElement?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == change.Nameable.Name);
                if (nos != null)
                {
                    nosVariableAssignments.Add(new NosVariableAssignment
                    {
                        NamedObjectSave = nos,
                        VariableName = change.PropertyName,
                        Value = change.PropertyValue
                    });

                }
            }

            await Managers.GlueCommands.Self.GluxCommands.SetVariableOnList(nosVariableAssignments, currentElement, performSaveAndGenerateCode: true, updateUi: true, echoToGame: false);
#endif
        }

        private void HandleObjectSelected(INameable item)
        {
            var dto = new SelectObjectDto();
            dto.NamedObject = new Models.NamedObjectSave();
            dto.NamedObject.InstanceName = item.Name;

            string elementGameType = null;

            if (ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen")
            {
                var entityInstance = (ScreenManager.CurrentScreen as Screens.EntityViewingScreen)?.CurrentEntity;

                if (entityInstance is GlueControl.Runtime.DynamicEntity dynamicEntity)
                {
                    elementGameType = dynamicEntity.EditModeType;
                }
                else
                {
                    // todo - handle inheritance
                    elementGameType = entityInstance.GetType().FullName;
                }
            }
            else
            {
                elementGameType = ScreenManager.CurrentScreen?.GetType().Name;
            }

            if (!string.IsNullOrEmpty(elementGameType))
            {
                var split = elementGameType.Split('.').ToList().Skip(1);
                dto.ElementNameGlue = string.Join("\\", split);

                SendToGlue(dto);
            }
        }

        int nextRespondableId = 1;
        public async Task<object> SendToGlue(RespondableDto respondableDto)
        {
            var semaphoreSlim = new SemaphoreSlim(0, 1);

            var idToUse = nextRespondableId;
            nextRespondableId++;

            var awaitedResponse = new AwaitedResponse
            {
                Semaphore = semaphoreSlim
            };

            AwaitedResponses[idToUse] = awaitedResponse;

            respondableDto.Id = idToUse;

            SendToGlue((object)respondableDto);

            await semaphoreSlim.WaitAsync();
            AwaitedResponses.TryRemove(idToUse, out AwaitedResponse _);
            semaphoreSlim.Dispose();

            return awaitedResponse.Response;
            // return response?
        }

        public void NotifyResponse(int id, object response)
        {
            if (AwaitedResponses.ContainsKey(id))
            {
                var awaitedResponse = AwaitedResponses[id];
                awaitedResponse.Response = response;
                awaitedResponse.Semaphore.Release();
            }
        }

        public void SendToGlue(object dto)
        {
            var type = dto.GetType().Name;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            SendCommandToGlue($"{type}:{json}");
        }

        private void SendCommandToGlue(string command)
        {
            GameToGlueCommands.Enqueue(new GameToGlueCommand { Command = command });
        }

        #endregion
    }
}
