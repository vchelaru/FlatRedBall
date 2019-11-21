using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

        public void HandleFileChanged(FilePath fileName)
        {
            var shouldReactToFileChange =
                ShouldRestartOnChange &&
                GetIfShouldReactToFileChange(fileName);

            if(shouldReactToFileChange)
            {
                StopAndRestartTask($"File {fileName} changed");
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

        internal void HandleVariableChanged(IElement arg1, CustomVariable arg2)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask($"{arg2} changed");
            }
        }

        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask($"Object variable {changedMember} changed");
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


        private void StopAndRestartImmediately(int portNumber)
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
                    compileSucceeded = compiler.Compile(printOutput, printError).Result;
                }

                if (compileSucceeded)
                {
                    if(!DoesTaskManagerHaveAnotherRestartTask())
                    {
                        runner.Run(preventFocus: true, runArguments: screenToRestartOn).Wait();
                        failedToRebuildAndRestart = false;
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
            ViewModel.IsHotReloadEnabled = ShouldRestartOnChange;
        }
    }
}
