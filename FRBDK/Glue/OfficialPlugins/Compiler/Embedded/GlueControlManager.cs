{CompilerDirectives}


using {ProjectNamespace}.GlueControl.Dtos;
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

            var response = "true";

            if (!isGet)
            {
                if(runSetImmediately)
                {
                    response = ApplySetMessage(message, screen, data, action) ?? null;
                }
                else
                {
                    await FlatRedBall.Instructions.InstructionManager.DoOnMainThreadAsync(
                        () => response = ApplySetMessage(message, screen, data, action));
                        
                }
            }

            return response;
        }

        private string ApplySetMessage(string message, Screen screen, string data, string action)
        {
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new System.InvalidOperationException("Objects can only be added, removed, made visible, or made invisible on the primary thread");
            }
            string response = "true";

            switch (action)
            {
                case "RestartScreen":
                    screen.RestartScreen(true);
                    break;
                case "ReloadGlobal":
                    GlobalContent.Reload(GlobalContent.GetFile(data));
                    break;
                case "TogglePause":

                    if (screen.IsPaused)
                    {
                        screen.UnpauseThisScreen();
                    }
                    else
                    {
                        screen.PauseThisScreen();
                    }

                    break;

                case "AdvanceOneFrame":
                    screen.UnpauseThisScreen();
                    var delegateInstruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
                    {
                        screen.PauseThisScreen();
                    });
                    delegateInstruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + .001;

                    FlatRedBall.Instructions.InstructionManager.Instructions.Add(delegateInstruction);
                    break;

                case "SetSpeed":
                    var timeFactor = int.Parse(data);
                    FlatRedBall.TimeManager.TimeFactor = timeFactor / 100.0f;
                    break;

                case "SetVariable":
#if SupportsEditMode
                case nameof(GlueControl.Dtos.GlueVariableSetData):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.GlueVariableSetData>(data);
                        var shouldEnqueue = false;
                        if(dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
                        {
                            var setVariableResponse = HandleSetVariable(dto);
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
                        if(shouldEnqueue)
                        {
                            EnqueueToOwner(message, dto.InstanceOwner);
                        }
                    }
                    break;
                case nameof(GlueControl.Dtos.AddObjectDto):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.AddObjectDto>(data);
                        var addResponse = HandleAddObject(dto);
                        response = Newtonsoft.Json.JsonConvert.SerializeObject(addResponse);
                        EnqueueToOwner(message, dto.ElementName);
                    }
                    break;

                case nameof(GlueControl.Dtos.RemoveObjectDto):
                    {
                        var dto =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.RemoveObjectDto>(data);
                        
                        var removeResponse = HandleRemoveObject(dto, out string gameTypeName);
                        response = JsonConvert.SerializeObject(removeResponse);
                        EnqueueToOwner(message, gameTypeName);
                    }
                    break;

                case nameof(GlueControl.Dtos.SelectObjectDto):
                    HandleSelectObjectCommand(
                        JsonConvert.DeserializeObject<GlueControl.Dtos.SelectObjectDto>(data));
                    break;
                case nameof(GlueControl.Dtos.SetEditMode):
                    HandleSetEditMode(
                        JsonConvert.DeserializeObject<GlueControl.Dtos.SetEditMode>(data));
                    break;
                case nameof(GlueControl.Dtos.MoveObjectToContainerDto):
                    {
                        var dto = JsonConvert.DeserializeObject<GlueControl.Dtos.MoveObjectToContainerDto>(data);
                        var moveResponse = HandleMoveObjectToContainerDto(dto);
                        response = JsonConvert.SerializeObject(moveResponse);
                        EnqueueToOwner(message, dto.ElementName);
                    }
                    break;
#endif
            }

            return response;
        }

        private MoveObjectToContainerDtoResponse HandleMoveObjectToContainerDto(MoveObjectToContainerDto dto)
        {
            var toReturn = new MoveObjectToContainerDtoResponse();

            var matchesCurrentScreen = GetIfMatchesCurrentScreen(
                dto.ElementName, out System.Type ownerType, out Screen currentScreen);
            if(matchesCurrentScreen)
            {
                toReturn.WasObjectMoved = GlueControl.Editing.MoveObjectToContainerLogic.TryMoveObjectToContainer(
                    dto.ObjectName, dto.ContainerName, EditingManager.ElementEditingMode);
            }
            else
            {
                // we don't know if it can be moved. We'll assume it can, and when that screen is loaded, it will re-run that and...if it 
                // fails, then I guess we'll figure out a way to communicate back to Glue that it needs to restart. Actually this may never
                // happen because moving objects is done in the current screen, but I gues it's technically a possibility so I'll leave this
                // comment here.
            }

            return toReturn;
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
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType);
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

        public void ReRunAllGlueToGameCommands()
        {
            var toProcess = GlobalGlueToGameCommands.ToArray();
            GlobalGlueToGameCommands.Clear();
            foreach (var message in toProcess)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                // intentionally not awaited because awaiting would mean waiting a whole frame for it
                // to finish processing. Don't want to do that, just fire and forget.
                ProcessMessage(message, true);
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
                    kvp.Value.Clear();
                    foreach(var message in toProcess)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // see above for explanation
                        ProcessMessage(message, true);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            }
        }

        #endregion

        private void HandleSelectObjectCommand(SelectObjectDto selectObjectDto)
        {
            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(selectObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);
            
            var ownerTypeName = "EditModeProject." + selectObjectDto.ElementName.Replace("\\", ".");
            ownerType = GetType().Assembly.GetType(ownerTypeName);

            bool isOwnerScreen = false;

            if(matchesCurrentScreen)
            {
                EditingManager.Select(selectObjectDto.ObjectName);
                EditingManager.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
                isOwnerScreen = true;
            }
            else
            {
                // it's a different screen. See if we can select that screen:


                if(ownerType != null && typeof(Screen).IsAssignableFrom(ownerType))
                {
#if SupportsEditMode

                    void AssignSelection(Screen screen)
                    {
                        // Select this even if it's null so the EditingManager deselects 
                        EditingManager.Select(selectObjectDto.ObjectName);
                        ReRunAllGlueToGameCommands();
                        screen.ScreenDestroy += HandleScreenDestroy;
                        ScreenManager.ScreenLoaded -= AssignSelection;
                    }
                    ScreenManager.ScreenLoaded += AssignSelection;

                    ScreenManager.CurrentScreen.MoveToScreen(ownerType);

                    isOwnerScreen = true;
                    EditingManager.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
#endif
                }
            }

            if(!isOwnerScreen)
            {
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType);

                if (isEntity)
                {
                    var isAlreadyViewingThisEntity = ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen" &&
                        SpriteManager.ManagedPositionedObjects.Count > 0 &&
                        SpriteManager.ManagedPositionedObjects[0].GetType() == ownerType;

                    if(!isAlreadyViewingThisEntity)
                    {
#if SupportsEditMode

                        void CreateEntityInstance(Screen screen)
                        {
                            var instance = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]) as IDestroyable;
                            (screen as Screens.EntityViewingScreen).CurrentEntity = instance;
                            var instanceAsPositionedObject = (PositionedObject)instance;
                            instanceAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                            instanceAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                            ScreenManager.ScreenLoaded -= CreateEntityInstance;

                            EditingManager.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingEntity;

                            Camera.Main.X = 0;
                            Camera.Main.Y = 0;
                            Camera.Main.Detach();

                            ReRunAllGlueToGameCommands();
                            screen.ScreenDestroy += HandleScreenDestroy;

                            EditingManager.Select(selectObjectDto.ObjectName);
                        }
                        ScreenManager.ScreenLoaded += CreateEntityInstance;

                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
#endif
                    }
                    else
                    {
                        EditingManager.Select(selectObjectDto.ObjectName);
                    }
                }
            }
        }

        private void HandleScreenDestroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();
        }

        private void HandleSetEditMode(GlueControl.Dtos.SetEditMode setEditMode)
        {
            var value = setEditMode.IsInEditMode;
#if SupportsEditMode
            FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
            FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

            if(value)
            {
                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;
                // user may go into edit mode after moving through a level and wouldn't want it to restart fully....or would they? What if they
                // want to change the Player start location. Need to think that through...

                void HandleScreenLoaded(Screen newScreen)
                {
                    ReRunAllGlueToGameCommands();
                    newScreen.ScreenDestroy += HandleScreenDestroy;

                    FlatRedBall.Screens.ScreenManager.ScreenLoaded -= HandleScreenLoaded;
                }

                FlatRedBall.Screens.ScreenManager.ScreenLoaded += HandleScreenLoaded;

                screen?.RestartScreen(reloadContent: true, applyRestartVariables:true);
            }
#endif
    }

        private GlueVariableSetDataResponse HandleSetVariable(GlueVariableSetData dto)
        {
            GlueVariableSetDataResponse valueToReturn = null;
#if IncludeSetVariable

            valueToReturn = GlueControl.Editing.VariableAssignmentLogic.SetVariable(dto);
#endif

            return valueToReturn;
        }

        public AddObjectDtoResponse HandleAddObject(GlueControl.Dtos.AddObjectDto dto)
        {
            AddObjectDtoResponse valueToReturn = new AddObjectDtoResponse();
#if IncludeSetVariable

            var createdObject = GlueControl.InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(dto);
            valueToReturn.WasObjectCreated = createdObject != null;
#endif
            return valueToReturn;
        }

        private RemoveObjectDtoResponse HandleRemoveObject(RemoveObjectDto removeObjectDto, out string typeName)
        {
            RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();
            response.DidScreenMatch = false;
            response.WasObjectRemoved = false;

            bool matchesCurrentScreen = 
                GetIfMatchesCurrentScreen(removeObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);

            typeName = ownerType?.FullName;

            if (matchesCurrentScreen)
            {
                response.DidScreenMatch = true;
                var isEditingEntity =
                    ScreenManager.CurrentScreen?.GetType() == typeof(Screens.EntityViewingScreen);
                var editingMode = isEditingEntity 
                    ? GlueControl.Editing.ElementEditingMode.EditingEntity 
                    : GlueControl.Editing.ElementEditingMode.EditingScreen;

                var available = GlueControl.Editing.SelectionLogic.GetAvailableObjects(editingMode)
                        .FirstOrDefault(item => item.Name == removeObjectDto.ObjectName);

                if(available is IDestroyable asDestroyable)
                {
                    asDestroyable.Destroy();
                    response.WasObjectRemoved = true;
                }
                else if(available is AxisAlignedRectangle rectangle)
                {
                    ShapeManager.Remove(rectangle);
                    response.WasObjectRemoved = true;
                }
                else if(available is Circle circle)
                {
                    ShapeManager.Remove(circle);
                    response.WasObjectRemoved = true;
                }
                else if(available is Polygon polygon)
                {
                    ShapeManager.Remove(polygon);
                    response.WasObjectRemoved = true;
                }
                else if(available is Sprite sprite)
                {
                    SpriteManager.RemoveSprite(sprite);
                    response.WasObjectRemoved = true;
                }
            }
            return response;
        }

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
            var ownerType = isEditingEntity
                ? SpriteManager.ManagedPositionedObjects[0].GetType().FullName
                : screen.GetType().FullName;

            var dto = new SetVariableDto();
            dto.InstanceOwner = ownerType;
            dto.ObjectName = item.Name;
            dto.VariableName = propertyName;
            dto.VariableValue = value;
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
                dto.ElementName = SpriteManager.ManagedPositionedObjects[0].GetType().FullName;
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
