using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
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


        public bool IsExplicitlySetRebuildAndRestartEnabled { get; set; }
        bool failedToRebuildAndRestart { get; set; }

        bool ShouldRestartOnChange => failedToRebuildAndRestart || IsExplicitlySetRebuildAndRestartEnabled;

        public int PortNumber { get; set; }

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
                StopAndRestartTask();
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
                StopAndRestartTask();
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
                StopAndRestartTask();
            }
        }

        internal void HandleVariableChanged(IElement arg1, CustomVariable arg2)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask();
            }
        }

        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            if (ShouldRestartOnChange)
            {
                StopAndRestartTask();
            }
        }

        private void StopAndRestartTask()
        {
            var runner = Runner.Self;
            if (runner.DidRunnerStartProcess || (runner.IsRunning == false && failedToRebuildAndRestart))
            {
                TaskManager.Self.Add(
                    () => StopAndRestartImmediately(PortNumber),
                   "Restarting due to Glue or file change",
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        private void StopAndRestartImmediately(int portNumber)
        {
            var runner = Runner.Self;
            var compiler = Compiler.Self;

            if(runner.DidRunnerStartProcess || (runner.IsRunning == false && failedToRebuildAndRestart))
            {

                if(runner.IsRunning)
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

                var succeeded = compiler.Compile(printOutput, printError).Result;

                if(succeeded)
                {
                    runner.Run(preventFocus:true, runArguments: screenToRestartOn).Wait();
                    failedToRebuildAndRestart = false;
                }
                else
                {
                    failedToRebuildAndRestart = true;
                }
            }

        }


    }
}
