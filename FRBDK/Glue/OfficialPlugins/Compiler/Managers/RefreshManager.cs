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
        Action<string> printOutput;
        Action<string> printError;
        public void InitializeEvents(Action<string> printOutput, Action<string> printError)
        {
            this.printOutput = printOutput;
            this.printError = printError;
        }

        public void HandleFileChanged(FilePath fileName)
        {
            var shouldReactToFileChange = GetIfShouldReactToFileChange(fileName);

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
            StopAndRestartTask();
        }

        internal void HandleNewScreenCreated(ScreenSave obj)
        {
            // don't worry about new screen, to be used additional changes have to be made

            //StopAndRestartTask();
        }

        internal void HandleNewObjectCreated(NamedObjectSave newNamedObject)
        {
            StopAndRestartTask();
        }

        internal void HandleVariableChanged(IElement arg1, CustomVariable arg2)
        {
            StopAndRestartTask();
        }

        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            StopAndRestartTask();
        }

        private void StopAndRestartTask()
        {
            var runner = Runner.Self;
            if (runner.DidRunnerStartProcess)
            {
                TaskManager.Self.Add(StopAndRestartImmediately,
                   "Restarting due to Glue or file change",
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        private void StopAndRestartImmediately()
        {
            var runner = Runner.Self;
            var compiler = Compiler.Self;

            if(runner.DidRunnerStartProcess)
            {

                string screenName = null;

                try
                {
                    screenName = CommandSending.CommandSender
                        .SendCommand("GetCurrentScreen")
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
                var succeeded = compiler.Compile(printOutput, printError).Result;

                if(succeeded)
                {
                    runner.Run().Wait();
                }
            }

        }


    }
}
