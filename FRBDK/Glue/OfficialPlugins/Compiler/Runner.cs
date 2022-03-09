using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
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
    #region WindowRectangle

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

    #region WindowMover

    public static class WindowMover
    {
        #region DLL Import for GetWindowRect/MoveWindow

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out WindowRectangle lpRect);


        #endregion
    }
    #endregion

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

        System.Windows.Forms.Timer timer;

        public bool DidRunnerStartProcess =>
            runningGameProcess != null && foundAlreadyRunningProcess == false;

        public CompilerViewModel ViewModel
        {
            get; set;
        }

        #endregion

        #region Events

        public event Action AfterSuccessfulRun;
        public event Action<string> OutputReceived;

        #endregion

        public Runner()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Tick += HandleTimerTick;
            // This was 6 seconds, but that seems a bit much...
            timer.Interval = 3 * 1000;
            timer.Start();
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            ///////////////Early Out////////////////////
            if(ViewModel.IsWaitingForGameToStart)
            {
                return;
            }
            ////////////End Early Out///////////////////
            
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
            else if(ViewModel.IsRunning == false)
            {
                // we ahve a process, so let's mark the view model as running:
                ViewModel.IsRunning = runningGameProcess != null;
                ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;
            }
        }

        public Process TryFindGameProcess(bool mustHaveWindow = true)
        {
            // find a process for game
            var processes = Process.GetProcesses()
                .OrderBy(item => item.ProcessName)
                .ToArray();

            var projectName = GlueState.Self.CurrentMainProject?.Name?.ToLowerInvariant();

            var found = processes
                .FirstOrDefault(item => item.ProcessName.ToLowerInvariant() == projectName && 
                (mustHaveWindow == false || item.MainWindowHandle != IntPtr.Zero));
            return found;
        }

        internal async Task<GeneralResponse> Run(bool preventFocus, string runArguments = null)
        {
            ViewModel.IsWaitingForGameToStart = true;

            GeneralResponse toReturn = GeneralResponse.UnsuccessfulResponse;

            foundAlreadyRunningProcess = false;
            string exeLocation = GetGameExeLocation();
            ToolsUtilities.GeneralResponse<Process> startResponse = ToolsUtilities.GeneralResponse<Process>.UnsuccessfulResponse;

            if (System.IO.File.Exists(exeLocation))
            {
                startResponse = await StartProcess(preventFocus, runArguments, exeLocation);

                if(startResponse.Succeeded)
                {
                    runningGameProcess = TryFindGameProcess();
                    int numberOfTimesToTryGettingProcess = 50;
                    int timesTried = 0;
                    while (runningGameProcess == null)
                    {
                        // didn't find it, so let's wait a little and try again:

                        await Task.Delay(50);

                        runningGameProcess = TryFindGameProcess();

                        timesTried++;

                        if (timesTried >= numberOfTimesToTryGettingProcess)
                        {
                            break;
                        }
                    }


                    if (runningGameProcess != null)
                    {

                        runningGameProcess.EnableRaisingEvents = true;
                        runningGameProcess.Exited += HandleProcessExit;
                        toReturn = GeneralResponse.SuccessfulResponse;

                        // wait for a handle
                        int numberOfTimesToTry = 60;
                        for (int i = 0; i < numberOfTimesToTry; i++)
                        {
                            var id = runningGameProcess?.MainWindowHandle;

                            if (id == null || id == IntPtr.Zero)
                            {

                                await Task.Delay(100);
                                continue;
                            }
                        }

                        global::Glue.MainGlueWindow.Self.Invoke(() =>
                        {
                            ViewModel.IsRunning = runningGameProcess != null;
                            ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

                            AfterSuccessfulRun();
                        });
                    }
                    else
                    {
                        toReturn.Succeeded = false;
                        toReturn.Message = $"Found the game .exe, but couldn't get it to launch. PreventFocus: {preventFocus}";

                        if(startResponse.Data != null)
                        {
                            if(startResponse.Data.HasExited && startResponse.Data.ExitCode != 0)
                            {
                                var message = await GetCrashMessage();

                                if(!string.IsNullOrEmpty(message))
                                {
                                    toReturn.Message += "\n" + message;
                                }
                            }
                        }
                    }
                }
                else
                {
                    toReturn = startResponse;
                }
            }
            else
            {
                toReturn.Succeeded = false;
                toReturn.Message = $"Could not find game .exe";
            }
            ViewModel.IsWaitingForGameToStart = false;

            return toReturn;

        }

        private static string GetGameExeLocation()
        {
            var projectFileName = GlueState.Self.CurrentMainProject?.FullFileName.FullPath;
            if(string.IsNullOrEmpty(projectFileName))
            {
                return null;
            }
            else
            {
                var projectDirectory = FileManager.GetDirectory(projectFileName);
                var executableName = FileManager.RemoveExtension(FileManager.RemovePath(projectFileName));
                // todo - make the plugin smarter so it knows where the .exe is really located
                var exeLocation = projectDirectory + "bin/x86/debug/" + executableName + ".exe";
                return exeLocation;
            }
        }

        private async Task<ToolsUtilities.GeneralResponse<Process>> StartProcess(bool preventFocus, string runArguments, string exeLocation)
        {
            if(preventFocus)
            {
                Win32ProcessStarter.StartProcessPreventFocus(runArguments, exeLocation);
                // we don't know, so just assume all went okay:
                return ToolsUtilities.GeneralResponse<Process>.SuccessfulResponse;
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = exeLocation;
                startInfo.WorkingDirectory = FileManager.GetDirectory(exeLocation);
                startInfo.Arguments = runArguments;
                var process = System.Diagnostics.Process.Start(startInfo);

                // December 30, 2021
                // Vic thought - we should
                // wait here to know why it failed
                // if it crashed immediately. The problem
                // is that a crash may take some time to write
                // the problem that occurred to a text file, and 
                // waiting and checking this here could be really slow.
                // Therefore, we'll return the process as part of the response
                var hasExited = process?.HasExited == true;

                if (hasExited && process?.ExitCode != 0)
                {

                    var message = await GetCrashMessage();
                    var response = ToolsUtilities.GeneralResponse<Process>.UnsuccessfulWith(message);
                    response.Data = process;
                    return response;
                }
                else
                {
                    // from https://stackoverflow.com/questions/5044412/reading-console-stream-continually-in-c-sharp

                    process.OutputDataReceived += HandleExeOutput;
                    process.BeginOutputReadLine();

                    var response = ToolsUtilities.GeneralResponse<Process>.SuccessfulResponse;
                    response.Data = process;
                    return response;
                }
            }
        }

        private void HandleExeOutput(object sender, DataReceivedEventArgs e)
        {
            var output = e.Data;
            OutputReceived?.Invoke(output);
        }

        private async void HandleProcessExit(object sender, EventArgs e)
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
                    string message = await GetCrashMessage();
                    if (!string.IsNullOrEmpty(message))
                    {
                        System.Windows.MessageBox.Show(message);
                    }
                }
            }

            runningGameProcess = null;

            if(!global::Glue.MainGlueWindow.Self.IsDisposed)
            {
                // This can get disposed in the meantime...
                try
                {
                    global::Glue.MainGlueWindow.Self.Invoke(() =>
                    {
                        ViewModel.IsRunning = runningGameProcess != null;
                        ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

                    });
                }
                catch(ObjectDisposedException)
                {
                    // do nothing.
                }
            }
        }

        private static async Task<string> GetCrashMessage()
        {
            string exeLocation = GetGameExeLocation();

            string message = string.Empty;
            if (!string.IsNullOrEmpty(exeLocation))
            {
                // await a little to see if the crash.txt file gets written...
                await Task.Delay(2);

                var directory = FileManager.GetDirectory(exeLocation);
                var logFile = directory + "CrashInfo.txt";

                if (System.IO.File.Exists(logFile))
                {
                    var contents = System.IO.File.ReadAllText(logFile);
                    message = $"The game has crashed:\n\n{contents}";
                }
                else
                {
                    message = "Oh no! The game crashed. Run from Visual Studio for more information on the error.";
                }

            }

            return message;
        }

        internal void KillGameProcess(Process process = null)
        {
            process = process ?? runningGameProcess;

            if (process != null)
            {
                suppressNextExitCodeAnnouncement = true;
                try
                {
                    process.Kill();
                    var maxWaitTimeMs = 1000;
                    if(process.HasExited == false)
                    {
                        process.WaitForExit(maxWaitTimeMs);
                    }
                }
                catch
                {
                    // do nothing
                    process = null;
                }
                ViewModel.IsRunning = false;
                ViewModel.DidRunnerStartProcess = DidRunnerStartProcess;
            }
        }
    }
}
