using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using OfficialPlugins.Compiler.ViewModels;
using OfficialPluginsCore.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace OfficialPlugins.Compiler
{
    class Runner : Singleton<Runner>
    {
        #region Fields/Properties

        Process runningGameProcess;

        //public bool IsRunning => runningGameProcess != null;
        //public bool IsCompiling
        //{
        //    get; private set;
        //}
        //public event EventHandler IsRunningChanged;

        bool suppressNextExitCodeAnnouncement = false;
        bool foundAlreadyRunningProcess = false;

        WindowRectangle? lastWindowRectangle;

        System.Windows.Forms.Timer timer;

        public bool DidRunnerStartProcess =>
            runningGameProcess != null && foundAlreadyRunningProcess == false;

        public CompilerViewModel ViewModel
        {
            get; set;
        }

        #endregion

        #region DLL Import for GetWindowRect/MoveWindow

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out WindowRectangle lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowRectangle
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner

            public override string ToString()
            {
                return $"Left {Left}  Top {Top}";
            }
        }

        #endregion

        public Runner()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Tick += HandleTimerTick;
            timer.Interval = 3 * 2000;
            timer.Start();
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            var process = runningGameProcess;

            if(process == null)
            {
                ProjectBase projectBase = null;

                Process found = TryFindGameProcess();

                if (found != null)
                {
                    foundAlreadyRunningProcess = true;
                    runningGameProcess = found;

                    try
                    {
                        runningGameProcess.EnableRaisingEvents = true;
                        runningGameProcess.Exited += HandleProcessExit;

                        ViewModel.IsRunning = runningGameProcess != null;
                        ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

                    }
                    catch (InvalidOperationException)
                    {
                        // do nothing, the game just stopped running
                    }
                }
            }
        }

        private static Process TryFindGameProcess()
        {
            // find a process for game
            var processes = Process.GetProcesses()
                .OrderBy(item => item.ProcessName)
                .ToArray();

            var projectName = GlueState.Self.CurrentMainProject?.Name?.ToLowerInvariant();

            var found = processes
                .FirstOrDefault(item => item.ProcessName.ToLowerInvariant() == projectName && item.MainWindowHandle != IntPtr.Zero);
            return found;
        }

        internal async Task<GeneralResponse> Run(bool preventFocus, string runArguments = null)
        {
            // disable the timer so it doesn't grab the process while we're looking for it 
            // (be sure to re-enable it later):
            timer.Enabled = false;
            ViewModel.IsWaitingForGameToStart = true;

            GeneralResponse toReturn = GeneralResponse.UnsuccessfulResponse;

            foundAlreadyRunningProcess = false;
            var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;
            var projectDirectory = FileManager.GetDirectory(projectFileName);
            var executableName = FileManager.RemoveExtension(FileManager.RemovePath(projectFileName));
            // todo - make the plugin smarter so it knows where the .exe is really located
            var exeLocation = projectDirectory + "bin/x86/debug/" + executableName + ".exe";

            if(System.IO.File.Exists(exeLocation))
            {
                StartProcess(preventFocus, runArguments, exeLocation);

                runningGameProcess = TryFindGameProcess();
                int numberOfTimesToTryGettingProcess = 50;
                int timesTried = 0;
                while(runningGameProcess == null)
                {
                    // didn't find it, so let's wait a little and try again:

                    await Task.Delay(50);

                    runningGameProcess = TryFindGameProcess();

                    timesTried++;

                    if(timesTried >= numberOfTimesToTryGettingProcess)
                    {
                        break;
                    }
                }


                if (runningGameProcess != null)
                {

                    runningGameProcess.EnableRaisingEvents = true;
                    runningGameProcess.Exited += HandleProcessExit;
                    toReturn = GeneralResponse.SuccessfulResponse;
                    var windowRectDisplay = lastWindowRectangle?.ToString() ?? "null";

                    if (lastWindowRectangle != null)
                    {

                        int numberOfTimesToTry = 60;
                        for (int i = 0; i < numberOfTimesToTry; i++)
                        {
                            var id = runningGameProcess?.MainWindowHandle;

                            if (id == null ||  id == IntPtr.Zero)
                            {

                                await Task.Delay(100);
                                continue;
                            }
                            else
                            {
                                var rect = lastWindowRectangle.Value;
                                var didSucceed = MoveWindow(id.Value, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
                                if(didSucceed)
                                {

                                    lastWindowRectangle = null;
                                    break;
                                }

                            }
                        }
                    }


                    global::Glue.MainGlueWindow.Self.Invoke(() =>
                    {
                        ViewModel.IsRunning = runningGameProcess != null;
                        ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

                    });
                }
                else
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = $"Found the game .exe, but couldn't get it to launch. PreventFocus: {preventFocus}";
                }
            }
            else
            {
                toReturn.Succeeded = false;
                toReturn.Message = $"Could not find game .exe";
            }
            ViewModel.IsWaitingForGameToStart = false;

            timer.Enabled = true;

            return toReturn;
             
        }

        private static void StartProcess(bool preventFocus, string runArguments, string exeLocation)
        {
            if(preventFocus)
            {
                Win32ProcessStarter.StartProcessPreventFocus(runArguments, exeLocation);
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exeLocation;
                startInfo.WorkingDirectory = FileManager.GetDirectory(exeLocation);
                startInfo.Arguments = runArguments;
                System.Diagnostics.Process.Start(startInfo);
            }
        }

        private void HandleProcessExit(object sender, EventArgs e)
        {
            var process = sender as Process;

            //IntPtr id = process.MainWindowHandle;
            //Runner.MoveWindow(process.MainWindowHandle, 0, 0, 500, 500, true);
            //Runner.GetWindowRect(id, out RECT windowRect);

            if(foundAlreadyRunningProcess)
            {
                foundAlreadyRunningProcess = false;
            }
            else if (suppressNextExitCodeAnnouncement)
            {
                suppressNextExitCodeAnnouncement = false;
            }
            else
            {
                if(process.ExitCode != 0)
                {
                    System.Windows.MessageBox.Show("Oh no! The game crashed. Run from Visual Studio for more information on the error.");
                }
            }

            runningGameProcess = null;

            global::Glue.MainGlueWindow.Self.Invoke(() =>
            {
                ViewModel.IsRunning = runningGameProcess != null;
                ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

            });
        }

        internal void KillGameProcess()
        {
            var process = runningGameProcess;
            IntPtr id = IntPtr.Zero;

            try
            {
                id = process.MainWindowHandle;
            }
            catch
            {
                // process could be dead
            }

            if(id != null)
            {
                var gotWindow = Runner.GetWindowRect(id, out WindowRectangle windowRect);
                lastWindowRectangle = windowRect;
                var lastWindowRectValue = lastWindowRectangle?.ToString() ?? "null";
            }

            if (process != null)
            {
                suppressNextExitCodeAnnouncement = true;
                try
                {
                    process.Kill();
                }
                catch
                {
                    // do nothing
                    process = null;
                    ViewModel.IsRunning = false;
                    ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;
                }
            }
        }
    }
}
