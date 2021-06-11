{CompilerDirectives}


using EditModeProject.GlueControl.Dtos;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace {ProjectNamespace}
{

    public class GlueControlManager
    {
        #region Fields/Properties

        bool isRunning;
        private TcpListener listener;
        private ConcurrentQueue<string> GameToGlueCommands = new ConcurrentQueue<string>();
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

        private void Run()
        {
            isRunning = true;

            listener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    HandleClient(client);

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

        private void HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            var stringBuilder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                stringBuilder.AppendLine(reader.ReadLine());
            }

            var response = ProcessMessage(stringBuilder.ToString()?.Trim());
            if (response == null)
            {
                response = "true";
            }
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(response);
            client.GetStream().Write(messageAsBytes, 0, messageAsBytes.Length);

        }
        #endregion

        #region Glue -> Game

        private string ProcessMessage(string message, bool runSetImmediately = false)
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

                case "GetCommands":
                    isGet = true;
                    string toReturn = string.Empty;
                    if (GameToGlueCommands.Count != 0)
                    {
                        lock (GameToGlueCommands)
                        {
                            List<string> tempList = new List<string>();
                            while (GameToGlueCommands.TryDequeue(out string tempString))
                            {
                                tempList.Add(tempString);
                            }
                            toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(tempList.ToArray());
                    }
                    }
                    return toReturn;
            }

            if (!isGet)
            {
                if(runSetImmediately)
                {
                    ApplySetMessage(message, screen, data, action);
                }
                else
                {
                    FlatRedBall.Instructions.InstructionManager.AddSafe(() =>
                    {
                        ApplySetMessage(message, screen, data, action);
                    });
                }
            }

            return "true";
        }

        private void ApplySetMessage(string message, Screen screen, string data, string action)
        {
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
                    var owner = HandleSetVariable(data);
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
                            EnqueueMessage(owner, message);
                        }
                    }
                    break;
                case "AddObject":
                    HandleAddObject(data);
                    GlobalGlueToGameCommands.Enqueue(message);
                    break;
#if SupportsEditMode

                case nameof(GlueControl.Dtos.RemoveObjectDto):

                    HandleRemoveObject(
                        Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.RemoveObjectDto>(data));
                    GlobalGlueToGameCommands.Enqueue(message);
                    break;

                case nameof(GlueControl.Dtos.SelectObjectDto):
                    HandleSelectObjectCommand(
                        Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.SelectObjectDto>(data));
                    break;
#endif

                case "SetEditMode":
                    HandleSetEditMode(data);
                    break;

            }
        }

        private void EnqueueMessage(string owner, string message)
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
                ProcessMessage(message, true);
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
                        ProcessMessage(message, true);
                    }
                }
            }
        }

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
                    void AssignSelection(Screen screen)
                    {
                        // Select this even if it's null so the EditingManager deselects 
                        EditingManager.Select(selectObjectDto.ObjectName);
                        ReRunAllGlueToGameCommands();
                        ScreenManager.ScreenLoaded -= AssignSelection;
                    }
                    ScreenManager.ScreenLoaded += AssignSelection;

                    ScreenManager.CurrentScreen.MoveToScreen(ownerType);

                    isOwnerScreen = true;
                    EditingManager.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;

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

                            EditingManager.Select(selectObjectDto.ObjectName);
                        }
                        ScreenManager.ScreenLoaded += CreateEntityInstance;

                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
                    }
                    else
                    {
                        EditingManager.Select(selectObjectDto.ObjectName);
                    }
                }
            }
        }

        private void HandleSetEditMode(string data)
        {
            var value = bool.Parse(data);
#if SupportsEditMode
            FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
            FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

            if(value)
            {
                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;
                // user may go into edit mode after moving through a level and wouldn't want it to restart fully....or would they? What if they
                // want to change the Player start location. Need to think that through...

                void HandleScreenLoaded(Screen _)
                {
                    ReRunAllGlueToGameCommands();

                    FlatRedBall.Screens.ScreenManager.ScreenLoaded -= HandleScreenLoaded;
                }

                FlatRedBall.Screens.ScreenManager.ScreenLoaded += HandleScreenLoaded;

                screen?.RestartScreen(reloadContent: true, applyRestartVariables:true);
            }
#endif
    }

    private string HandleSetVariable(string data)
        {
            string valueToReturn = null;
#if IncludeSetVariable
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GlueVariableSetData>(data);
            GlueControl.Editing.VariableAssignmentLogic.SetVariable(deserialized);
            valueToReturn = deserialized.InstanceOwner;
#endif

            return valueToReturn;
        }

        public void HandleAddObject(string data)
        {
#if IncludeSetVariable
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Models.NamedObjectSave>(data);
            if(deserialized.SourceType == GlueControl.Models.SourceType.Entity)
            {
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(deserialized.SourceClassType);
                var instance = factory?.CreateNew() as FlatRedBall.PositionedObject;
                instance.Name = deserialized.InstanceName;
                instance.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                instance.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
            }
#endif

    }

        private void HandleRemoveObject(RemoveObjectDto removeObjectDto)
        {
            bool matchesCurrentScreen = 
                GetIfMatchesCurrentScreen(removeObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);

            if (matchesCurrentScreen)
            {
                bool removedByReflection = false;
                var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
                // yes, remove it
                var propertyInfo = ownerType.GetProperty(removeObjectDto.ObjectName, bindingFlags);
                if (propertyInfo != null)
                {
                    var objectToRemove = propertyInfo.GetValue(currentScreen) as IDestroyable;
                    objectToRemove?.Destroy();
                    removedByReflection = true;
                }
                else
                {
                    // it's null, so try the field
                    var fieldInfo = ownerType.GetField(removeObjectDto.ObjectName, bindingFlags);
                    if (fieldInfo != null)
                    {
                        var objectToRemove = fieldInfo.GetValue(currentScreen) as IDestroyable;
                        objectToRemove?.Destroy();
                        removedByReflection = true;
                    }
                }

                if (!removedByReflection)
                {
                    var foundObject = SpriteManager.ManagedPositionedObjects
                        .FirstOrDefault(item => item.Name == removeObjectDto.ObjectName) as IDestroyable;
                    foundObject?.Destroy();
                }
            }
        }

        private bool GetIfMatchesCurrentScreen(string elementName, out System.Type ownerType, out Screen currentScreen)
        {
            var ownerTypeName = "EditModeProject." + elementName.Replace("\\", ".");

            ownerType = GetType().Assembly.GetType(ownerTypeName);
            currentScreen = ScreenManager.CurrentScreen;
            var currentScreenType = currentScreen.GetType();

            return currentScreenType == ownerType || ownerType.IsAssignableFrom(currentScreenType);
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

            GameToGlueCommands.Enqueue(message);

            var fromGlueDto = new GlueVariableSetData();
            fromGlueDto.InstanceOwner = ownerType;
            fromGlueDto.VariableName = $"this.{item.Name}.{propertyName}";
            fromGlueDto.VariableValue = value.ToString();
            fromGlueDto.Type = "float";
            var glueToGameCommand = $"SetVariable:{Newtonsoft.Json.JsonConvert.SerializeObject(fromGlueDto)}";

            if(isEditingEntity)
            {
                GlobalGlueToGameCommands.Enqueue(glueToGameCommand);
            }
            else
            {
                
                EnqueueMessage(screen.GetType().FullName, message);
            }
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
            GameToGlueCommands.Enqueue(message);
        }

        public void SendToGlue(object dto)
        {
            var type = dto.GetType().Name;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            SendCommandToGlue($"{type}:{json}");
        }

        public void SendCommandToGlue(string command)
        {
            lock(GameToGlueCommands)
            {
                GameToGlueCommands.Enqueue(command);
            }
        }

        #endregion
    }
}
