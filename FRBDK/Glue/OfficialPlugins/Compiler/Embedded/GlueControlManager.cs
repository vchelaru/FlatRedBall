{CompilerDirectives}

using {ProjectNamespace};
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
using Newtonsoft.Json;
#endif



namespace GlueControl
{

    public class GlueControlManager
    {
        #region Classes

        class GameToGlueCommand
        {
            public string Command { get; set; }
        }

        #endregion

        #region Fields/Properties

        bool isRunning;
        private TcpListener listener;
        private ConcurrentQueue<GameToGlueCommand> GameToGlueCommands = new ConcurrentQueue<GameToGlueCommand>();
        private GlueControl.Editing.EditingManager EditingManager;


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
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            Thread serverThread = new Thread(new ThreadStart(Run));

            serverThread.Start();
        }

        private async void Run()
        {
            var isGameAlreadyRunning = false;
            try
            {
                listener.Start();
                isRunning = true;
            }
            catch (System.Net.Sockets.SocketException e)
            {
                isGameAlreadyRunning = true;
            }

            if (!isGameAlreadyRunning)
            {
                TcpClient client = listener.AcceptTcpClient();
                var stream = client.GetStream();

                while (isRunning)
                {
                    try
                    {
                        await HandleStream(stream);
                    }
                    catch (System.Exception e)
                    {
                        if (isRunning)
                        {
                            throw e;
                        }
                    }
                }

                isRunning = false;

                listener.Stop();
            }
        }

        public void Kill()
        {
            isRunning = false;
            listener.Stop();

        }

        private async Task HandleStream(Stream clientStream)
        {
            var messageFromClient = await ReadFromClient(clientStream);

            var response = await ProcessMessage(messageFromClient?.Trim());

            if (response == null)
            {
                response = "true";
            }

            WriteMessageToStream(clientStream, response);

        }

        const int bufferSize = 2048;
        static byte[] buffer = new byte[bufferSize];
        private static async Task<string> ReadFromClient(Stream stm)
        {
            //// Read response from server.
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);

            byte[] intBytes = await GetByteArrayFromStream(stm, 4);
            var messageLength = 0;

            if (intBytes.Length > 0)
            {
                messageLength = BitConverter.ToInt32(intBytes, 0);
            }

            if (messageLength > 0)
            {
                byte[] byteArray = await GetByteArrayFromStream(stm, messageLength);
                var response = Encoding.UTF8.GetString(byteArray, 0, (int)byteArray.Length);

                if (!string.IsNullOrWhiteSpace(response) && response.Contains("}") && response[response.Length - 1] != '}')
                {
                    var messageCharacterCount = response.Length;
                    int m = 3;
                }

                return response;
            }
            else
            {
                return string.Empty;
            }
            //Console.ReadLine();
        }

        static TimeSpan readFromClientTimeout = TimeSpan.FromSeconds(4);

        private static async Task<byte[]> GetByteArrayFromStream(Stream stm, int totalLength)
        {
            using (var memoryStream = new MemoryStream())
            {
                int totalBytesRead = 0;

                var timeStarted = DateTime.Now;
                int lastReadNumberOfBytes = 0;

                int bytesLeftToRead = totalLength;

                bool hasMoreToRead = totalBytesRead < totalLength;
                do
                {
                    lastReadNumberOfBytes = await stm.ReadAsync(buffer, 0, Math.Min(bytesLeftToRead, buffer.Length));
                    memoryStream.Write(buffer, 0, lastReadNumberOfBytes);
                    totalBytesRead += lastReadNumberOfBytes;

                    bytesLeftToRead -= lastReadNumberOfBytes;
                    hasMoreToRead = bytesLeftToRead > 0;
                } while (hasMoreToRead && lastReadNumberOfBytes != 0);

                var byteArray =
                    memoryStream.ToArray();
                return byteArray;

            }
        }

        private static void WriteMessageToStream(Stream clientStream, string message)
        {
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(message);

            var length = messageAsBytes.Length;
            var lengthAsBytes =
                BitConverter.GetBytes(length);
            clientStream.Write(lengthAsBytes, 0, 4);
            if (messageAsBytes.Length > 0)
            {
                clientStream.Write(messageAsBytes, 0, messageAsBytes.Length);

            }
        }

        #endregion

        #region Glue -> Game

        private async Task<string> ProcessMessage(string message, bool runSetImmediately = false)
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

                case "GetCommands":
                    isGet = true;
                    string toReturn = string.Empty;
#if SupportsEditMode
                    if (GameToGlueCommands.Count != 0)
                    {
                        List<string> tempList = new List<string>();
                        while (GameToGlueCommands.TryDequeue(out GameToGlueCommand gameToGlueCommand))
                        {
                            tempList.Add(gameToGlueCommand.Command);
                        }
                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(tempList.ToArray());
                    }
#endif

                    return toReturn;
                case nameof(GlueControl.Dtos.GetCameraPosition):
                    toReturn = string.Empty;
#if SupportsEditMode

                    isGet = true;
                    var getCameraPositionResponse = new GlueControl.Dtos.GetCameraPositionResponse();
                    getCameraPositionResponse.X = Camera.Main.X;
                    getCameraPositionResponse.Y = Camera.Main.Y;
                    getCameraPositionResponse.Z = Camera.Main.Z;
                    toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(getCameraPositionResponse);
#endif
                    return toReturn;
                    break;
            }

            var response = "true";

            if (!isGet)
            {
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

        private void HandlePropertyChanged(INameable item, string propertyName, object newValue)
        {
#if SupportsEditMode

            var screen = ScreenManager.CurrentScreen;
            var isEditingEntity =
                screen.GetType() == typeof(Screens.EntityViewingScreen);
            string ownerType;
            if(isEditingEntity)
            {
                var entityInstance = SpriteManager.ManagedPositionedObjects[0];
                if(entityInstance is GlueControl.Runtime.DynamicEntity dynamicEntity)
                {
                    ownerType = dynamicEntity.EditModeType;
                }
                else
                {
                    // todo - handle inheritance
                    ownerType = entityInstance.GetType().FullName;
                }
            }
            else
            {
                ownerType = screen.GetType().FullName;

            }

            var dto = new SetVariableDto();
            dto.InstanceOwner = ownerType;
            dto.ObjectName = item.Name;
            dto.VariableName = propertyName;
            dto.VariableValue = newValue;
            dto.Type = newValue?.GetType().Name;
            var message = $"{nameof(SetVariableDto)}:{Newtonsoft.Json.JsonConvert.SerializeObject(dto)}";

            SendCommandToGlue(message);

            // the game used to set this itself, but the game doesn't know which screen defines an object
            // so let Glue hande that
            //var fromGlueDto = new GlueVariableSetData();
            //fromGlueDto.InstanceOwner = ownerType;
            //fromGlueDto.VariableName = $"this.{item.Name}.{propertyName}";
            //fromGlueDto.VariableValue = value.ToString();
            //fromGlueDto.Type = "float";
            //var simulatedGlueToGameCommand = $"SetVariable:{Newtonsoft.Json.JsonConvert.SerializeObject(fromGlueDto)}";

            //if(isEditingEntity)
            //{
            //    GlobalGlueToGameCommands.Enqueue(simulatedGlueToGameCommand);
            //}
            //else
            //{
                
            //    EnqueueMessage(screen.GetType().FullName, simulatedGlueToGameCommand);
            //}
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
                var entityInstance = SpriteManager.ManagedPositionedObjects[0];
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
                elementGameType = ScreenManager.CurrentScreen.GetType().Name;
            }


            var split = elementGameType.Split('.').ToList().Skip(1);
            dto.ElementNameGlue = string.Join("\\", split);

            SendToGlue(dto);
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
