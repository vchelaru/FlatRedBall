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
using OfficialPlugins.Compiler.ViewModels;
using OfficialPluginsCore.Compiler.CommandSending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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

        bool ShouldRestartOnChange => failedToRebuildAndRestart || IsExplicitlySetRebuildAndRestartEnabled;

        public int PortNumber { get; set; }

        public CompilerViewModel ViewModel
        {
            get; set;
        }


        #endregion

        public void InitializeEvents(Action<string> printOutput, Action<string> printError)
        {
            this.printOutput = printOutput;
            this.printError = printError;
        }

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

        internal void HandleNewEntityCreated(EntitySave arg1, AddEntityWindow arg2)
        {
            if(ShouldRestartOnChange)
            {
                StopAndRestartTask($"{arg1} created");
            }
        }

        internal void HandleNewScreenCreated(ScreenSave obj)
        {
            // don't worry about new screen, to be used additional changes have to be made

            //StopAndRestartTask();
        }

        internal void HandleNewObjectCreated(NamedObjectSave newNamedObject)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask($"Object {newNamedObject} created");
            }
        }

        internal async void HandleVariableChanged(IElement variableElement, CustomVariable variable)
        {
            if (ShouldRestartOnChange)
            {
                var screen = GlueState.Self.CurrentScreenSave;
                var entity = GlueState.Self.CurrentEntitySave;
                var name = "this." + variable.Name;
                var type = variable.Type;
                var value = variable.DefaultValue?.ToString();

                // todo - I haven't tested this, variable.Name is probably wrong
                await TryPushVariableOrRestart(null, name, type, value, screen, entity);
            }
            else
            {
                StopAndRestartTask($"Object variable {variable.Name} changed");
            }
        }

        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            var nos = GlueState.Self.CurrentNamedObjectSave;

            var instruction = nos.GetInstructionFromMember(changedMember);
            if (instruction != null)
            {
                var screen = GlueState.Self.CurrentScreenSave;
                var entity = GlueState.Self.CurrentEntitySave;
                var nosName = nos.InstanceName;
                var type = instruction.Type;
                var value = instruction.Value?.ToString();
                TaskManager.Self.Add(async () =>
                {
                    try
                    {
                        await TryPushVariableOrRestart(nosName, changedMember, type, value, screen, entity);
                    }
                    catch
                    {
                        // no biggie...
                    }
                }, "Pushing variable to game", TaskExecutionPreference.Asap);
            }
            else
            {
                StopAndRestartTask($"Object variable {changedMember} changed");
            }
        }

        private async Task TryPushVariableOrRestart(string variableOwningNosName, string rawMemberName, string type, string value, ScreenSave currentGlueScreen, EntitySave currentEntitySave)
        {
            if (ShouldRestartOnChange)
            {
                string currentInGameScreen = null;

                try
                {
                    currentInGameScreen = await CommandSending.CommandSender
                    .SendCommand("GetCurrentScreen", PortNumber);
                }
                catch
                {
                    // do nothing, maybe not connected
                }
                if(currentGlueScreen != null)
                {
                    var areSame = currentInGameScreen == GlueState.Self.ProjectNamespace + "." + currentGlueScreen?.Name.Replace("\\", ".");

                    if (areSame)
                    {
                        var data = new GlueVariableSetData();
                        data.Type = type;
                        data.Value = value;
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
                else if(currentEntitySave != null)
                {
                    var screens = GlueState.Self.CurrentGlueProject.Screens.ToArray();

                    var matchingScreen = screens.FirstOrDefault(item => GlueState.Self.ProjectNamespace + "." + item.Name.Replace("\\", ".") == currentInGameScreen);

                    if(matchingScreen != null)
                    {
                        // don't do "all" here, just do top-level which will catch all lists:
                        foreach(var nos in matchingScreen.NamedObjects)
                        {
                            var needsToBeUpdated = nos.IsList && nos.SourceClassGenericType == currentEntitySave.Name;

                            if(needsToBeUpdated)
                            {
                                var data = new GlueVariableSetData();
                                data.Type = type;
                                data.Value = value;
                                data.VariableName = $"this.{nos.InstanceName}.{variableOwningNosName}.{rawMemberName}";

                                var serialized = JsonConvert.SerializeObject(data);

                                await CommandSender.SendCommand($"SetVariable:{serialized}", PortNumber);
                            }
                        }
                    }
                }

            }
        }

        internal void HandleObjectRemoved(IElement arg1, NamedObjectSave arg2)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask($"Object {arg2} removed");
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
                        screenToRestartOn = CommandSending.CommandSender
                            .SendCommand("GetCurrentScreen", portNumber)
                            .Result;
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
