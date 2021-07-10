{CompilerDirectives}


using {ProjectNamespace}.GlueControl.Dtos;
using {ProjectNamespace}.GlueControl;

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

#if SupportsEditMode
using Newtonsoft.Json;
#endif



namespace {ProjectNamespace}
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
        
    
        /// <summary>
        /// Stores all commands that have been sent from Glue to game 
        /// which should always be re-run.
        /// </summary>
        
        private Queue<string> GlobalGlueToGameCommands = new Queue<string>();
        private Dictionary<string, Queue<string>> ScreenSpecificGlueToGameCommands =
            new Dictionary<string, Queue<string>>();



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
            isRunning = true;

            listener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    await HandleClient(client);

                    client.Close();
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

        public void Kill()
        {
            isRunning = false;
            listener.Stop();

        }

        private async Task HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            var stringBuilder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                stringBuilder.AppendLine(reader.ReadLine());
            }

            var response = await ProcessMessage(stringBuilder.ToString()?.Trim());
            if (response == null)
            {
                response = "true";
            }
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(response);
            var clientStream = client.GetStream();

            var length = messageAsBytes.Length;
            var lengthAsBytes =
                BitConverter.GetBytes(length);
            clientStream.Write(lengthAsBytes, 0, 4);
            if(messageAsBytes.Length > 0)
            {
                clientStream.Write(messageAsBytes, 0, messageAsBytes.Length);

            }

        }
        #endregion

        #region Glue -> Game

        #region General Functions

        private async Task<string> ProcessMessage(string message, bool runSetImmediately = false, Func<object, bool> runPredicate = null)
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

            if(runPredicate == null)
            {

                switch (action)
                {
                    case "GetCurrentScreen":
                        isGet = true;
                        return screen.GetType().FullName;

    #if SupportsEditMode
                    case "GetCommands":
                        isGet = true;
                        string toReturn = string.Empty;
                        if (GameToGlueCommands.Count != 0)
                        {
                            List<string> tempList = new List<string>();
                            while (GameToGlueCommands.TryDequeue(out GameToGlueCommand gameToGlueCommand))
                            {
                                tempList.Add(gameToGlueCommand.Command);
                            }
                            toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(tempList.ToArray());
                        }
                        return toReturn;
                    case nameof(GlueControl.Dtos.GetCameraPosition):
                        isGet = true;
                        var getCameraPositionResponse = new GlueControl.Dtos.GetCameraPositionResponse();
                        getCameraPositionResponse.X = Camera.Main.X;
                        getCameraPositionResponse.Y = Camera.Main.Y;
                        getCameraPositionResponse.Z = Camera.Main.Z;
                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(getCameraPositionResponse);
                        return toReturn;
                        break;
    #endif
                }
            }

            var response = "true";

            if (!isGet)
            {
                if(runSetImmediately)
                {
                    response = ApplySetMessage(message, screen, data, action, runPredicate) ?? null;
                }
                else
                {
                    await FlatRedBall.Instructions.InstructionManager.DoOnMainThreadAsync(
                        () => response = ApplySetMessage(message, screen, data, action));
                        
                }
            }

            return response;
        }

        private string ApplySetMessage(string message, Screen screen, string data, string action, Func<object, bool> runPredicate = null)
        {
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new System.InvalidOperationException("Objects can only be added, removed, made visible, or made invisible on the primary thread");
            }
            string response = null;

            switch (action)
            {
#if SupportsEditMode
                case nameof(GlueControl.Dtos.GlueVariableSetData):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.GlueVariableSetData>(data);

                        if(runPredicate == null || runPredicate(dto))
                        {
                            var shouldEnqueue = true;
                            if(dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
                            {
                                var setVariableResponse =
                                    GlueControl.CommandReceiver.ReceiveDto(dto) as GlueVariableSetDataResponse;
                                response = Newtonsoft.Json.JsonConvert.SerializeObject(setVariableResponse);
                                shouldEnqueue = setVariableResponse.WasVariableAssigned;
                            }
                            else
                            {
                                // If it's a record-only, then we'll always want to enqueue it
                                // need to change the record only back to assign so future re-runs will assign
                                dto.AssignOrRecordOnly = AssignOrRecordOnly.Assign;
                                message = $"{action}:{JsonConvert.SerializeObject(dto)}";
                                shouldEnqueue = true;
                            }
                            if(shouldEnqueue && runPredicate == null)
                            {
                                EnqueueToOwner(message, dto.InstanceOwner);
                            }
                        }
                    }
                    break;
                case nameof(GlueControl.Dtos.AddObjectDto):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.AddObjectDto>(data);
                        if(runPredicate == null || runPredicate(dto))
                        {
                            var addResponse = CommandReceiver.ReceiveDto(dto);
                            response = Newtonsoft.Json.JsonConvert.SerializeObject(addResponse);
                            if(runPredicate == null)
                            {
                                EnqueueToOwner(message, dto.ElementNameGame);
                            }
                        }
                    }
                    break;

                case nameof(GlueControl.Dtos.RemoveObjectDto):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.RemoveObjectDto>(data);

                        if(runPredicate == null || runPredicate(dto))
                        {
                            bool matchesCurrentScreen =
                                GetIfMatchesCurrentScreen(dto.ElementName, out System.Type ownerType, out Screen currentScreen);

                            string gameTypeName = ownerType?.FullName;

                            response = CommandReceiver.Receive(message);

                            if (runPredicate == null)
                            {
                                EnqueueToOwner(message, gameTypeName);
                            }
                        }

                    }
                    break;

                case nameof(GlueControl.Dtos.MoveObjectToContainerDto):
                    {
                        var dto = JsonConvert.DeserializeObject<GlueControl.Dtos.MoveObjectToContainerDto>(data);
                        if(runPredicate == null || runPredicate(dto))
                        {
                            response = CommandReceiver.Receive(message);
                            if(runPredicate == null)
                            {
                                EnqueueToOwner(message, dto.ElementName);
                            }
                        }
                    }
                    break;
                default:
                    response = CommandReceiver.Receive(message, runPredicate);

                    break;
#endif
            }

            return response;
        }

        /// <summary>
        /// Enqueues the message to the owner type, where the owner is the Qualified type name like
        /// "MyGame.Screens.Level1"
        /// </summary>
        /// <param name="message">The message which is of the form "DtoType:Json"</param>
        /// <param name="owner">The owner in qualified type name</param>
        public void EnqueueToOwner(string message, string owner)
        {
            if (string.IsNullOrEmpty(owner))
            {
                GlobalGlueToGameCommands.Enqueue(message);
            }
            else
            {
                var ownerType = typeof(GlueControlManager).Assembly.GetType(owner);
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType) ||
                    InstanceLogic.Self.CustomGlueElements.ContainsKey(owner);
                if (isEntity)
                {
                    // If it's on an entity, then it needs to be applied globally
                    GlobalGlueToGameCommands.Enqueue(message);
                }
                else
                {
                    EnqueueScreenSpecificMessage(message, owner);
                }
            }
        }

        private void EnqueueScreenSpecificMessage(string message, string owner)
        {
            Queue<string> queue = null;
            if(ScreenSpecificGlueToGameCommands.ContainsKey(owner))
            {
                queue = ScreenSpecificGlueToGameCommands[owner];
            }
            else
            {
                queue = new Queue<string>();
                ScreenSpecificGlueToGameCommands.Add(owner, queue);
            }
            queue.Enqueue(message);
        }

        public void ReRunAllGlueToGameCommands(Func<object, bool> runPredicate = null)
        {
            var toProcess = GlobalGlueToGameCommands.ToArray();
            if(runPredicate == null)
            {
                GlobalGlueToGameCommands.Clear();
            }
            foreach (var message in toProcess)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                // intentionally not awaited because awaiting would mean waiting a whole frame for it
                // to finish processing. Don't want to do that, just fire and forget.
                ProcessMessage(message, true, runPredicate);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            var assembly = typeof(GlueControlManager).Assembly;
            var currentScreenType = ScreenManager.CurrentScreen.GetType();

            foreach(var kvp in ScreenSpecificGlueToGameCommands)
            {
                var type = assembly.GetType(kvp.Key);
                if(type != null && type.IsAssignableFrom(currentScreenType))
                {
                    toProcess = kvp.Value.ToArray();
                    if(runPredicate == null)
                    {
                        kvp.Value.Clear();
                    }

                    foreach(var message in toProcess)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // see above for explanation
                        ProcessMessage(message, true, runPredicate);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            }
        }

        #endregion

        private bool GetIfMatchesCurrentScreen(string elementName, out System.Type ownerType, out Screen currentScreen)
        {
            var ownerTypeName = "EditModeProject." + elementName.Replace("\\", ".");

            ownerType = GetType().Assembly.GetType(ownerTypeName);
            currentScreen = ScreenManager.CurrentScreen;
            var currentScreenType = currentScreen.GetType();

            return currentScreenType == ownerType || ownerType?.IsAssignableFrom(currentScreenType) == true;
        }

        #endregion

        #region Game -> Glue

        private void HandlePropertyChanged(PositionedObject item, string propertyName, object value)
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
            dto.VariableValue = value;
            dto.Type = value?.GetType().Name;
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

    private void HandleObjectSelected(PositionedObject item)
        {
            var dto = new SelectObjectDto();
            dto.ObjectName = item.Name;

            if(ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen")
            {
                var entityInstance = SpriteManager.ManagedPositionedObjects[0];
                if (entityInstance is GlueControl.Runtime.DynamicEntity dynamicEntity)
                {
                    dto.ElementName = dynamicEntity.EditModeType;
                }
                else
                {
                    // todo - handle inheritance
                    dto.ElementName = entityInstance.GetType().FullName;
                }
            }
            else
            {
                dto.ElementName = ScreenManager.CurrentScreen.GetType().Name;
            }

            var message = $"{nameof(SelectObjectDto)}:{Newtonsoft.Json.JsonConvert.SerializeObject(dto)}";
            SendCommandToGlue(message);
        }

        public void SendToGlue(object dto)
        {
            var type = dto.GetType().Name;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            SendCommandToGlue($"{type}:{json}");
        }

        public void SendCommandToGlue(string command)
        {
            GameToGlueCommands.Enqueue(new GameToGlueCommand { Command = command });
        }

        #endregion
    }
}
