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

    public class GlueVariableSetData
    {
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string Type { get; set; }
    }

    public class GlueControlManager
    {
bool isRunning;
        private TcpListener listener;
        private ConcurrentQueue<string> GameToGlueCommands = new ConcurrentQueue<string>();
        private GlueControl.Editing.EditingManager EditingManager; 

        
        public static GlueControlManager Self { get; private set; }
        
    
        /// <summary>
        /// Stores all commands that have been sent from Glue to game so they can be re-run when a Screen is re-loaded.
        /// </summary>
        private Queue<string> GlueToGameCommands = new Queue<string>();

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

        public void SendCommandToGlue(string command)
        {
            lock(GameToGlueCommands)
            {
                GameToGlueCommands.Enqueue(command);
            }
        }

        public void SendToGlue(object dto)
        {
            var type = dto.GetType().Name;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            SendCommandToGlue($"{type}:{json}");
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

        public void ReRunAllGlueToGameCommands()
        {
            var toProcess = GlueToGameCommands.ToArray();
            GlueToGameCommands.Clear();
            foreach (var message in toProcess)
            {
                ProcessMessage(message);
            }
        }

        private string ProcessMessage(string message)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool handledImmediately = false;

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
                    handledImmediately = true;
                    return screen.GetType().FullName;

                case "GetCommands":
                    handledImmediately = true;
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

            if (!handledImmediately)
            {
                FlatRedBall.Instructions.InstructionManager.AddSafe(() =>
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
                            HandleSetVariable(data);
                            GlueToGameCommands.Enqueue(message);
                            break;
                        case "AddObject":
                            HandleAddObject(data);
                            GlueToGameCommands.Enqueue(message);
                            break;
#if SupportsEditMode

                        case nameof(GlueControl.Dtos.RemoveObjectDto):

                            HandleRemoveObject(
                                Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Dtos.RemoveObjectDto>(data));
                            GlueToGameCommands.Enqueue(message);
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
                });
            }

            return "true";
        }

        private void HandleSelectObjectCommand(SelectObjectDto selectObjectDto)
        {
            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(selectObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);

            if(matchesCurrentScreen)
            {
                EditingManager.Select(selectObjectDto.ObjectName);
            }
            else
            {
                // it's a different screen. See if we can select that screen:
                var ownerTypeName = "EditModeProject." + selectObjectDto.ElementName.Replace("\\", ".");

                ownerType = GetType().Assembly.GetType(ownerTypeName);

                if(ownerType != null && typeof(Screen).IsAssignableFrom(ownerType))
                {
                    void AssignSelection(Screen screen)
                    {
                        EditingManager.Select(selectObjectDto.ObjectName);
                        ScreenManager.ScreenLoaded -= AssignSelection;
                    }

                    if(!string.IsNullOrEmpty(selectObjectDto.ObjectName))
                    {
                        ScreenManager.ScreenLoaded += AssignSelection;
                    }
                }
            }
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

        private void HandleSetEditMode(string data)
        {
            var value = bool.Parse(data);
#if SupportsEditMode
            FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
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

        public void HandleSetVariable(string data)
        {
#if IncludeSetVariable

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;

            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GlueVariableSetData>(data);

            object variableValue = deserialized.VariableValue;

            switch (deserialized.Type)
            {
                case "float":
                    variableValue = float.Parse(deserialized.VariableValue);
                    break;
                case "int":
                    variableValue = int.Parse(deserialized.VariableValue);
                    break;
                case "bool":
                    variableValue = bool.Parse(deserialized.VariableValue);
                    break;
                case "double":
                    variableValue = double.Parse(deserialized.VariableValue);
                    break;
                case "Microsoft.Xna.Framework.Color":
                    variableValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(deserialized.VariableValue).GetValue(null);
                    break;
            }

            screen.ApplyVariable(deserialized.VariableName, variableValue);
#endif
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

        private void HandlePropertyChanged(PositionedObject item, string propertyName, object value)
        {
#if SupportsEditMode

            var dto = new SetVariableDto();
            dto.ObjectName = item.Name;
            dto.VariableName = propertyName;
            dto.VariableValue = value;
            var message = $"{nameof(SetVariableDto)}:{Newtonsoft.Json.JsonConvert.SerializeObject(dto)}";

            GameToGlueCommands.Enqueue(message);

            var fromGlueDto = new GlueVariableSetData();
            fromGlueDto.VariableName = $"this.{item.Name}.{propertyName}";
            fromGlueDto.VariableValue = value.ToString();
            fromGlueDto.Type = "float";
            var glueToGameCommand = $"SetVariable:{Newtonsoft.Json.JsonConvert.SerializeObject(fromGlueDto)}";
            GlueToGameCommands.Enqueue(glueToGameCommand);
#endif
        }

        private void HandleObjectSelected(PositionedObject item)
        {
            var dto = new SelectObjectDto();
            dto.ObjectName = item.Name;
            

            var message = $"{nameof(SelectObjectDto)}:{Newtonsoft.Json.JsonConvert.SerializeObject(dto)}";
            GameToGlueCommands.Enqueue(message);
        }

    }
}
