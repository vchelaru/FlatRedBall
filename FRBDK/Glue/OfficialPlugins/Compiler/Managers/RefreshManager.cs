using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace OfficialPlugins.Compiler.Managers
{
    public class RefreshManager : Singleton<RefreshManager>
    {
        #region Fields/Properties

        Action<string> printOutput;
        Action<string> printError;
        string screenToRestartOn = null;


        bool isExplicitlySetRebuildAndRestartEnabled;
        public bool IsExplicitlySetRebuildAndRestartEnabled 
        {
            get => isExplicitlySetRebuildAndRestartEnabled;
            set
            {
                isExplicitlySetRebuildAndRestartEnabled = value;
                RefreshViewModelHotReload();

            }
        }
        bool failedToRebuildAndRestart { get; set; }

        public bool ShouldRestartOnChange => 
            (failedToRebuildAndRestart || IsExplicitlySetRebuildAndRestartEnabled || 
                (ViewModel.IsRunning && ViewModel.IsEditChecked)) &&
            GlueState.Self.CurrentGlueProject != null;

        public int PortNumber { get; set; }

        public CompilerViewModel ViewModel
        {
            get; set;
        }

        public bool IgnoreNextObjectAdd { get; set; }
        public bool IgnoreNextObjectSelect { get; set; }

        #endregion

        #region Initialize

        public void InitializeEvents(Action<string> printOutput, Action<string> printError)
        {
            this.printOutput = printOutput;
            this.printError = printError;
        }

        #endregion

        internal async void HandleItemSelected(TreeNode selectedTreeNode)
        {
            if(IgnoreNextObjectSelect)
            {
                IgnoreNextObjectSelect = false;
            }
            else if(ViewModel.IsEditChecked)
            {
                var dto = new SelectObjectDto();

                var nos = GlueState.Self.CurrentNamedObjectSave;
                var element = GlueState.Self.CurrentElement;

                if(nos != null)
                {
                    dto.ObjectName = nos.InstanceName;
                    dto.ElementName = element.Name;

                    await CommandSender.Send(dto, ViewModel.PortNumber);
                }
                else if(element != null)
                {
                    await HandleElementSelected(dto, element);
                }
            }

        }

        #region File

        public async void HandleFileChanged(FilePath fileName)
        {
            var shouldReactToFileChange =
                ShouldRestartOnChange &&
                GetIfShouldReactToFileChange(fileName);

            if(shouldReactToFileChange)
            {
                var rfs = GlueCommands.Self.FileCommands.GetReferencedFile(fileName.FullPath);

                var isGlobalContent = rfs != null && rfs.GetContainer() == null;

                bool canSendCommands = ViewModel.IsGenerateGlueControlManagerInGame1Checked;

                var handled = false;

                if(canSendCommands)
                {
                    string strippedName = null;
                    if (rfs != null)
                    {
                        strippedName = FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name));
                    }
                    if(isGlobalContent && rfs.GetAssetTypeInfo().CustomReloadFunc != null)
                    {
                        printOutput($"Waiting for Glue to copy reload global file {strippedName}");

                        // just give the file time to copy:
                        await Task.Delay(500);

                        // it's part of global content and can be reloaded, so let's just tell
                        // it to reload:
                        await CommandSender.SendCommand($"ReloadGlobal:{strippedName}", ViewModel.PortNumber);

                        printOutput($"Reloading global file {strippedName}");

                        handled = true;
                    }
                    else if(rfs != null)
                    {
                        // Right now we'll assume the screen owns this file, although it is possible that it's 
                        // global but not part of global content. That's a special case we'll have to handle later
                        printOutput($"Waiting for Glue to copy reload global file {strippedName}");
                        await Task.Delay(500);
                        try
                        {
                            printOutput($"Telling game to restart screen");

                            var result = await CommandSender.SendCommand("RestartScreen", ViewModel.PortNumber);

                            handled = true;
                        }
                        catch(Exception e)
                        {
                            printError($"Error trying to send command:{e.ToString()}");
                        }
                    }
                }
                if(!handled)
                {
                    StopAndRestartTask($"File {fileName} changed");
                }
            }
        }

        private bool GetIfShouldReactToFileChange(FilePath filePath )
        {
            if(filePath.FullPath.Contains(".Generated.") && filePath.FullPath.EndsWith(".cs"))
            {
                return false;
            }
            if(filePath.FullPath.EndsWith(".Generated.xml"))
            {
                return false;
            }


            return true;
        }


        #endregion

        #region Entity Created

        internal void HandleNewEntityCreated(EntitySave arg1)
        {
            if(ShouldRestartOnChange)
            {
                StopAndRestartTask($"{arg1} created");
            }
        }

        #endregion

        internal void HandleNewScreenCreated()
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask($"New screen created");
            }
        }

        #region Selected Object

        private async Task HandleElementSelected(SelectObjectDto dto, GlueElement element)
        {
            dto.ObjectName = String.Empty;
            dto.ElementName = element.Name;

            await CommandSender.Send(dto, ViewModel.PortNumber);
        }

        #endregion

        #region New NamedObject

        internal async void HandleNewObjectCreated(NamedObjectSave newNamedObject)
        {
            if(IgnoreNextObjectAdd)
            {
                IgnoreNextObjectAdd = false;
            }
            else if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                var tempSerialized = JsonConvert.SerializeObject(newNamedObject);
                var addObjectDto = JsonConvert.DeserializeObject<AddObjectDto>(tempSerialized);
                var containerElement = ObjectFinder.Self.GetElementContaining(newNamedObject);
                if (containerElement != null)
                {
                    addObjectDto.ElementName =
                        GlueState.Self.ProjectNamespace + "." + containerElement.Name.Replace("\\", ".");

                }

                var addResponseAsString = await CommandSender.Send(addObjectDto, PortNumber);

                AddObjectDtoResponse addResponse = null;
                if(!string.IsNullOrEmpty(addResponseAsString))
                {
                    addResponse = JsonConvert.DeserializeObject<AddObjectDtoResponse>(addResponseAsString);
                }

                if(addResponse?.WasObjectCreated == true)
                {
                    await AdjustNewObjectToCameraPosition(newNamedObject);
                }
                else
                {
                    StopAndRestartTask($"Restarting because of added object {newNamedObject}");
                }
            }
        }

        private async Task AdjustNewObjectToCameraPosition(NamedObjectSave newNamedObject)
        {
            if (GlueState.Self.CurrentScreenSave != null)
            {
                // If it's in a screen, then we position the object on the camera:

                var cameraPosition = Microsoft.Xna.Framework.Vector3.Zero;

                cameraPosition = await CommandSender.GetCameraPosition(PortNumber);

                var gluxCommands = GlueCommands.Self.GluxCommands;

                bool didSetValue = false;

                Vector2 newPosition = new Vector2(cameraPosition.X, cameraPosition.Y);

                var list = GlueState.Self.CurrentElement.NamedObjects.FirstOrDefault(item =>
                    item.ContainedObjects.Contains(newNamedObject));

                var shouldIncreasePosition = false;
                do
                {
                    shouldIncreasePosition = false;

                    var listToLoopThrough = list?.ContainedObjects ?? GlueState.Self.CurrentElement.NamedObjects;

                    const int incrementForNewObject = 16;
                    const int minimumDistanceForObjects = 3;
                    foreach (var item in listToLoopThrough)
                    {
                        if (item != newNamedObject)
                        {
                            Vector2 itemPosition = new Vector2(
                                (item.GetCustomVariable("X")?.Value as float?) ?? 0,
                                (item.GetCustomVariable("Y")?.Value as float?) ?? 0);

                            var distance = (itemPosition - newPosition).Length();


                            if (distance < minimumDistanceForObjects)
                            {
                                shouldIncreasePosition = true;
                                break;
                            }

                        }
                    }
                    if (shouldIncreasePosition)
                    {
                        newPosition.X += incrementForNewObject;
                    }

                } while (shouldIncreasePosition);

                if (newPosition.X != 0)
                {
                    gluxCommands.SetVariableOn(newNamedObject, "X", newPosition.X);
                    didSetValue = true;
                }
                if (newPosition.Y != 0)
                {
                    gluxCommands.SetVariableOn(newNamedObject, "Y", newPosition.Y);

                    didSetValue = true;
                }



                if (didSetValue)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
            }
        }


        #endregion

        #region Variable Changed


        internal void HandleNamedObjectValueChanged(string variableName, object oldValue, NamedObjectSave nos, AssignOrRecordOnly assignOrRecordOnly)
        {
            VariableSendingManager.Self.HandleNamedObjectValueChanged(variableName, oldValue, nos, assignOrRecordOnly);
        }

        internal void HandleVariableChanged(IElement variableElement, CustomVariable variable)
        {
            VariableSendingManager.Self.HandleVariableChanged(variableElement, variable);
        }


        #endregion

        internal void HandleVariableAdded(CustomVariable newVariable)
        {
            StopAndRestartTask($"Restarting because of added variable {newVariable}");
        }

        #region Object Container (List, Layer, ShapeCollection) changed
        internal async void HandleObjectContainerChanged(NamedObjectSave objectMoving, 
            NamedObjectSave newContainer)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                bool handledByGame = false;

                var element = ObjectFinder.Self.GetElementContaining(objectMoving);
                if(element != null)
                {
                    var dto = new MoveObjectToContainerDto
                    {
                        ElementName = element.Name,
                        ObjectName = objectMoving.InstanceName,
                        ContainerName = newContainer?.InstanceName

                    };


                    var responseAsString = await CommandSender.Send(dto, ViewModel.PortNumber);

                    if(!string.IsNullOrEmpty(responseAsString))
                    {
                        try
                        {
                            var response = JsonConvert.DeserializeObject<MoveObjectToContainerDtoResponse>(responseAsString);
                            handledByGame = response.WasObjectMoved;
                        }
                        catch
                        {
                            handledByGame = false;
                            Output.Print($"!!!Error parsing {responseAsString}");
                        }
                    }
                }

                if(!handledByGame)
                {
                    StopAndRestartTask($"Restarting due to changed container for {objectMoving}");
                }
            }
        }
        #endregion

        #region Object Removed
        internal async Task HandleObjectRemoved(IElement owner, NamedObjectSave nos)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                var dto = new Dtos.RemoveObjectDto();
                dto.ElementName = //ToGameType((GlueElement)owner);
                    owner.Name;
                dto.ObjectName = nos.InstanceName;
                var responseAsstring = await CommandSender.Send(dto, ViewModel.PortNumber);

                var response = JsonConvert.DeserializeObject<RemoveObjectDtoResponse>(responseAsstring);
                if(response.DidScreenMatch && response.WasObjectRemoved == false)
                {
                    StopAndRestartTask(
                        $"Restarting because {nos} was deleted from Glue but not from game");
                }

            }
        }
        #endregion

        #region Stop/Restart

        const string stopRestartDetails =
                   "Restarting due to Glue or file change";

        bool CanRestart =>
            Runner.Self.DidRunnerStartProcess || (ViewModel.IsRunning == false && failedToRebuildAndRestart) ||
                (ViewModel.IsRunning && ViewModel.IsEditChecked);

        public void StopAndRestartTask(string reason)
        {
            if (CanRestart)
            {
                var wasInEditMode = ViewModel.IsEditChecked;
                TaskManager.Self.Add(
                    () =>
                    {
                        if(!string.IsNullOrEmpty(reason))
                        {
                            printOutput($"Restarting because: {reason}");
                        }
                        var task = StopAndRestartImmediately(PortNumber);
                        task.Wait();
                        if(wasInEditMode)
                        {
                            ViewModel.IsEditChecked = true;
                        }
                    },
                    stopRestartDetails,
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }


        private async Task StopAndRestartImmediately(int portNumber)
        {
            bool DoesTaskManagerHaveAnotherRestartTask()
            {
                var actions = TaskManager.Self.SyncedActions;

                var restartTask = actions.FirstOrDefault(item => item != actions[0] &&
                    item.DisplayInfo == stopRestartDetails);

                return restartTask != null;
            }

            var runner = Runner.Self;
            var compiler = Compiler.Self;

            if(CanRestart)
            {

                if (ViewModel.IsRunning)
                {
                    try
                    {
                        screenToRestartOn = await CommandSending.CommandSender.GetScreenName(portNumber);
                    }
                    catch (AggregateException)
                    {
                        printOutput("Could not get the game's screen, restarting game from startup screen");

                    }
                    catch (SocketException)
                    {
                        // do nothing, may not have been able to communicate, just output
                        printOutput("Could not get the game's screen, restarting game from startup screen");
                    }

                    runner.KillGameProcess();
                }

                bool compileSucceeded = false;
                if(!DoesTaskManagerHaveAnotherRestartTask())
                {
                    compileSucceeded = await compiler.Compile(printOutput, printError);
                }

                if (compileSucceeded)
                {
                    if(!DoesTaskManagerHaveAnotherRestartTask())
                    {
                        var response = await runner.Run(preventFocus: true, runArguments: screenToRestartOn);
                        if(response.Succeeded == false)
                        {
                            printError(response.Message);
                        }
                        failedToRebuildAndRestart = response.Succeeded == false;
                    }
                }
                else
                {
                    failedToRebuildAndRestart = true;
                }
                RefreshViewModelHotReload();
            }

        }

        #endregion

        private void RefreshViewModelHotReload()
        {
            ViewModel.IsHotReloadAvailable = ShouldRestartOnChange;
        }
    }
}
