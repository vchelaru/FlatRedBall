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

        bool ShouldRestartOnChange => (failedToRebuildAndRestart || IsExplicitlySetRebuildAndRestartEnabled) &&
            GlueState.Self.CurrentGlueProject != null;

        public int PortNumber { get; set; }

        public CompilerViewModel ViewModel
        {
            get; set;
        }


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
            if(ViewModel.IsEditChecked)
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

        #region Glue Element

        private async Task HandleElementSelected(SelectObjectDto dto, GlueElement element)
        {
            dto.ObjectName = String.Empty;
            dto.ElementName = element.Name;

            await CommandSender.Send(dto, ViewModel.PortNumber);
        }

        #endregion

        internal async void HandleNewObjectCreated(NamedObjectSave newNamedObject)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
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

        internal async void HandleVariableChanged(IElement variableElement, CustomVariable variable)
        {
            if (ShouldRestartOnChange)
            {
                var type = variable.Type;
                var value = variable.DefaultValue?.ToString();
                string name = null;
                if(variable.IsShared)
                {
                    name = GlueState.Self.ProjectNamespace + "." + variableElement.Name.Replace("/", ".").Replace("\\", ".") + "." + variable.Name;
                }
                else
                {
                    name = "this." + variable.Name;
                }
                await TryPushVariable(null, name, type, value, GlueState.Self.CurrentElement);
            }
            else
            {
                StopAndRestartTask($"Object variable {variable.Name} changed");
            }
        }

        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            var nos = GlueState.Self.CurrentNamedObjectSave;
            HandleNamedObjectValueChanged(changedMember, oldValue, nos);
        }

        public void HandleNamedObjectValueChanged(string changedMember, object oldValue, NamedObjectSave nos)
        { 

            var instruction = nos?.GetCustomVariable(changedMember);
            var currentElement = GlueState.Self.CurrentElement;
            var nosName = nos.InstanceName;
            var ati = nos.GetAssetTypeInfo();
            string type;
            string value;

            var originalMemberName = changedMember;

            if(currentElement is EntitySave && nos.AttachToContainer && 
                (changedMember == "X" || changedMember == "Y" || changedMember == "Z"))
            {
                changedMember = $"Relative{changedMember}";
            }

            if(changedMember == nameof(NamedObjectSave.InstanceName))
            {
                type = "string";
                value = nos.InstanceName;
                changedMember = "Name";
                nosName = (string)oldValue;
            }
            else if(ati?.VariableDefinitions.Any(item => item.Name == originalMemberName) == true)
            {
                var variableDefinition = ati.VariableDefinitions.First(item => item.Name == originalMemberName);
                type = variableDefinition.Type;
                value = instruction?.Value?.ToString();
                if(value == null)
                {
                    if(type == "float" || type == "int" || type == "long" || type == "double")
                    {
                        value = "0";
                    }
                }
            }
            else
            {
                type = instruction?.Type ?? instruction?.Value?.GetType().Name;
                value = instruction?.Value?.ToString();
            }
            TaskManager.Self.Add(async () =>
            {
                try
                {
                    await TryPushVariable(nosName, changedMember, type, value, currentElement);
                }
                catch
                {
                    // no biggie...
                }
            }, "Pushing variable to game", TaskExecutionPreference.Asap);
            // Vic says - I don't think we want to restart anymore because there could be stray variables
            // assigned by plugins. Instead we should try to make everything work through hotreload
            //else
            //{
            //    StopAndRestartTask($"Object variable {changedMember} changed");
            //}
        }

        private async Task TryPushVariable(string variableOwningNosName, string rawMemberName, string type, string value, GlueElement currentElement)
        {
            if (ViewModel.IsRunning)
            {
                if(currentElement != null)
                {
                    var data = new GlueVariableSetData();
                    data.InstanceOwner = GlueState.Self.ProjectNamespace + "." + currentElement.Name.Replace("\\", ".");
                    data.Type = type;
                    data.VariableValue = value;
                    data.VariableName = rawMemberName;
                    if(!string.IsNullOrEmpty(variableOwningNosName))
                    {
                        data.VariableName = "this." + variableOwningNosName + "." + data.VariableName;
                    }
                    else
                    {
                        data.VariableName = "this." + data.VariableName; 
                    }

                    var serialized = JsonConvert.SerializeObject(data);

                    await CommandSender.SendCommand($"SetVariable:{serialized}", PortNumber);
                }
            }
        }

        private async Task PushNosChangeInEntityToCurrentScreen(string variableOwningNosName, string rawMemberName, string type, string value, EntitySave currentEntitySave, ScreenSave matchingScreen)
        {
            // we want to update even if it's defined in a base class, so let's get all the screens that inherit
            var screensToLoopThrough = ObjectFinder.Self.GetAllBaseElementsRecursively(matchingScreen);
            screensToLoopThrough.Add(matchingScreen);

            var possibleEntities = ObjectFinder.Self.GetAllBaseElementsRecursively(currentEntitySave);
            possibleEntities.Add(currentEntitySave);

            foreach (var screen in screensToLoopThrough)
            {
                // don't do "all" here, just do top-level which will catch all lists:
                foreach (var nos in screen.NamedObjects)
                {
                    var managedAtThisInheritanceLevel = nos.DefinedByBase == false;

                    var needsToBeUpdated = false;

                    if (managedAtThisInheritanceLevel && nos.IsList)
                    {
                        needsToBeUpdated = possibleEntities.Any(item => item.Name == nos.SourceClassGenericType);
                    }

                    if (needsToBeUpdated)
                    {
                        var data = new GlueVariableSetData();
                        data.Type = type;
                        data.VariableValue = value;

                        string variableName = rawMemberName;

                        bool shouldAttach = false;

                        if (nos.IsList && !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                            ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType) != null)
                        {
                            shouldAttach = true;
                        }
                        else
                        {
                            AssetTypeInfo ati = null;
                            if (nos.IsList)
                            {
                                ati = nos.GetContainedListItemAssetTypeInfo();
                            }
                            else
                            {
                                ati = nos.GetAssetTypeInfo();
                            }
                            if (ati != null)
                            {
                                shouldAttach = ati.ShouldAttach;
                            }
                        }


                        if (shouldAttach &&
                            // What if it ignores parent attachment? Need to consider that here...
                            (rawMemberName == "X" || rawMemberName == "Y" || rawMemberName == "Z" ||
                            rawMemberName == "RotationX" || rawMemberName == "RotationY" || rawMemberName == "RotationZ"))
                        {
                            variableName = "Relative" + rawMemberName;
                        }

                        data.VariableName = $"this.{nos.InstanceName}.{variableOwningNosName}.{variableName}";

                        var serialized = JsonConvert.SerializeObject(data);

                        await CommandSender.SendCommand($"SetVariable:{serialized}", PortNumber);
                    }
                }

            }
        }

        internal async Task HandleObjectRemoved(IElement owner, NamedObjectSave nos)
        {
            if (ViewModel.IsRunning)
            {
                var dto = new Dtos.RemoveObjectDto();
                dto.ElementName = owner.Name;
                dto.ObjectName = nos.InstanceName;
                await CommandSender.Send(dto, ViewModel.PortNumber);
            }
        }

        const string stopRestartDetails =
                   "Restarting due to Glue or file change";

        public void StopAndRestartTask(string reason)
        {
            var runner = Runner.Self;
            if (runner.DidRunnerStartProcess || (ViewModel.IsRunning == false && failedToRebuildAndRestart))
            {
                TaskManager.Self.Add(
                    () =>
                    {
                        if(!string.IsNullOrEmpty(reason))
                        {
                            printOutput($"Restarting because: {reason}");
                        }
                        StopAndRestartImmediately(PortNumber);
                    },
                    stopRestartDetails,
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }


        private async void StopAndRestartImmediately(int portNumber)
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

            if(runner.DidRunnerStartProcess || (ViewModel.IsRunning == false && failedToRebuildAndRestart))
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

                    runner.Stop();
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

        private void RefreshViewModelHotReload()
        {
            ViewModel.IsHotReloadAvailable = ShouldRestartOnChange;
        }
    }
}
