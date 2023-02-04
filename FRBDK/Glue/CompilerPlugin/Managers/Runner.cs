using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using CompilerPlugin.ViewModels;
using CompilerPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CompilerLibrary.ViewModels;
using System.Threading;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using System.ComponentModel;

namespace CompilerPlugin.Managers
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

    class Runner
    {
        #region Fields/Properties

        Process runningGameProcess;

        bool suppressNextExitCodeAnnouncement = false;
        bool foundAlreadyRunningProcess = false;
        private Action<string, string> _eventCaller;
        private CompilerViewModel _compilerViewModel;
        System.Windows.Forms.Timer timer;

        private bool _isWaitingForGameToStart;
        public bool IsWaitingForGameToStart
        {
            get
            {
                return _isWaitingForGameToStart;
            }

            set
            {
                if (_isWaitingForGameToStart != value)
                {
                    _isWaitingForGameToStart = value;
                    _compilerViewModel.IsWaitingForGameToStart = value;
                    CallStatusUpdate();
                }
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    _compilerViewModel.IsRunning = value;
                    CallStatusUpdate();
                }
            }
        }

        private bool _didRunnerStartProcess;
        public bool DidRunnerStartProcess
        {
            get
            {
                return _didRunnerStartProcess;
            }

            set
            {
                if (_didRunnerStartProcess != value)
                {
                    _didRunnerStartProcess = value;
                    _compilerViewModel.DidRunnerStartProcess = value;
                    CallStatusUpdate();
                }
            }
        }



        private void CallStatusUpdate()
        {
            _eventCaller("Runner_StatusUpdate", JsonConvert.SerializeObject(new RunnerStatus
            {
                IsWaitingForGameToStart = IsWaitingForGameToStart
            }));
        }

        public bool GetDidRunnerStartProcess()
        {
            return runningGameProcess != null && foundAlreadyRunningProcess == false;
        }

        //public CompilerViewModel ViewModel
        //{
        //    get; set;
        //}

        #endregion

        #region Events

        public event Action AfterSuccessfulRun;
        public event Action<string> OutputReceived;
        public event Action<string> ErrorReceived;
        #endregion

        public Runner(Action<string, string> eventCaller, CompilerViewModel compilerViewModel)
        {
            _eventCaller = eventCaller;
            _compilerViewModel = compilerViewModel;

            _compilerViewModel.IsWaitingForGameToStart = IsWaitingForGameToStart;
            _compilerViewModel.IsRunning = IsRunning;
            _compilerViewModel.DidRunnerStartProcess = DidRunnerStartProcess;

            timer = new System.Windows.Forms.Timer();
            timer.Tick += HandleTimerTick;
            // This was 6 seconds, but that seems a bit much...
            timer.Interval = 3 * 1000;
            timer.Start();
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            ///////////////Early Out////////////////////
            if (_isWaitingForGameToStart)
            {
                return;
            }
            ////////////End Early Out///////////////////

            var process = runningGameProcess;

            if (process == null)
            {
                ProjectBase projectBase = null;

                // don't require a game window - the user should be able to shut it down:
                var foundProcess = TryFindGameProcess(mustHaveWindow:false);

                if (foundProcess != null)
                {
                    foundAlreadyRunningProcess = true;
                    runningGameProcess = foundProcess;

                    try
                    {
                        runningGameProcess.EnableRaisingEvents = true;
                        runningGameProcess.Exited += HandleProcessExit;

                        IsRunning = runningGameProcess != null;
                        DidRunnerStartProcess = GetDidRunnerStartProcess();

                    }
                    catch (InvalidOperationException)
                    {
                        // do nothing, the game just stopped running
                    }
                    catch(Win32Exception)
                    {
                        // There's an exception happening, possibly because the game just stopped.
                    }
                }
            }
            else if (IsRunning == false)
            {
                // we ahve a process, so let's mark the view model as running:
                IsRunning = runningGameProcess != null;
                DidRunnerStartProcess = GetDidRunnerStartProcess();
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
                .Where(item => item.ProcessName.ToLowerInvariant() == projectName &&
                    (mustHaveWindow == false || item.MainWindowHandle != IntPtr.Zero))
                .ToArray();



            return found.FirstOrDefault();
        }

        SemaphoreSlim runCallSemaphore = new SemaphoreSlim(1, 1);


        internal async Task<GeneralResponse> Run(bool preventFocus, string runArguments = null)
        {
            // early out
            if(TryFindGameProcess(false) != null)
            {
                return GeneralResponse.UnsuccessfulWith("Found game process, so not building");
            }


            int numberOfTimesToTryGettingProcess = 140;
            int numberOfTimesToTryGettingHandle = 60;
            int millisecondsToWaitBeforeRetry = 100;
            bool didTimeoutOnProcess = false;

            IsWaitingForGameToStart = true;

            foundAlreadyRunningProcess = false;
            string exeLocation = GetGameExeLocation();

            ToolsUtilities.GeneralResponse<Process> startResponse = ToolsUtilities.GeneralResponse<Process>.UnsuccessfulResponse;

            try
            {
                var isAvailable = runCallSemaphore.Wait(0);
                /////////////////////More Early Out/////////////
                if(!isAvailable)
                {
                    return startResponse;
                }
                /////////////////End Early Out/////////////////
                if (System.IO.File.Exists(exeLocation))
                {
                    if(string.IsNullOrEmpty(runArguments))
                    {
                        runArguments = "LaunchedByEditor";
                    }
                    else
                    {
                        runArguments += " LaunchedByEditor";
                    }
                    startResponse = await StartProcess(preventFocus, runArguments, exeLocation);

                    if (startResponse.Succeeded)
                    {
                        runningGameProcess = TryFindGameProcess();

                        int timesTried = 0;
                        while (runningGameProcess == null)
                        {
                            // didn't find it, so let's wait a little and try again:

                            await Task.Delay(millisecondsToWaitBeforeRetry);

                            runningGameProcess = TryFindGameProcess();

                            timesTried++;

                            if (timesTried >= numberOfTimesToTryGettingProcess)
                            {
                                didTimeoutOnProcess = true;
                                break;
                            }
                        }


                        if (runningGameProcess != null)
                        {
                            this._compilerViewModel.DidRunnerStartProcess = true;
                            runningGameProcess.EnableRaisingEvents = true;
                            runningGameProcess.Exited += HandleProcessExit;

                            // wait for a handle
                            for (int i = 0; i < numberOfTimesToTryGettingHandle; i++)
                            {
                                var id = runningGameProcess?.MainWindowHandle;

                                if (id == null || id == IntPtr.Zero)
                                {

                                    await Task.Delay(millisecondsToWaitBeforeRetry);
                                    continue;
                                }
                                else
                                {
                                    // we got the handle, so break out:
                                    break;
                                }
                            }

                            TaskManager.Self.OnUiThread(() =>
                            {
                                IsRunning = runningGameProcess != null;
                                DidRunnerStartProcess = GetDidRunnerStartProcess();

                                AfterSuccessfulRun();
                            });

                            return new GeneralResponse
                            {
                                Succeeded = true
                            };
                        }
                        else
                        {
                            this._compilerViewModel.DidRunnerStartProcess = false;

                            string error;
                            if (didTimeoutOnProcess)
                            {
                                error = $"Launching game .exe timed out after {numberOfTimesToTryGettingProcess * millisecondsToWaitBeforeRetry / 1000} seconds.";
                            }
                            else
                            {
                                error = $"Found the game .exe, but couldn't get it to launch. PreventFocus: {preventFocus}";
                            }


                            if (startResponse.Data != null)
                            {
                                if (startResponse.Data.HasExited && startResponse.Data.ExitCode != 0)
                                {
                                    var message = await GetCrashMessage();

                                    if (!string.IsNullOrEmpty(message))
                                    {
                                        error += "\n" + message;
                                    }
                                }
                            }

                            return new GeneralResponse
                            {
                                Succeeded = false,
                                Message = error
                            };
                        }
                    }
                    else
                    {
                        return new GeneralResponse
                        {
                            Succeeded = false
                        };
                    }
                }
                else
                {
                    return new GeneralResponse
                    {
                        Succeeded = false,
                        Message = $"Could not find game .exe"
                    };
                }
            }
            finally
            {
                runCallSemaphore.Release();
                IsWaitingForGameToStart = false;
            }
        }

        internal void MoveWindow(int x, int y, int width, int height, bool repaint)
        {
            var handle = runningGameProcess?.MainWindowHandle;

            if (handle.HasValue)
            {
                var succeededToMove = WindowMover.MoveWindow(handle.Value, x, y, width, height, repaint);

                var succeeded = WindowMover.GetWindowRect(handle.Value, out WindowRectangle outValue);


            }
        }

        private static string GetGameExeLocation()
        {
            var projectFileName = GlueState.Self.CurrentMainProject?.FullFileName.FullPath;
            if (string.IsNullOrEmpty(projectFileName))
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
            if (preventFocus)
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

                OutputReceived?.Invoke($"{exeLocation} WorkingDirectory:{startInfo.WorkingDirectory} Args:{runArguments}");

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

            if (foundAlreadyRunningProcess)
            {
                foundAlreadyRunningProcess = false;
            }
            else if (suppressNextExitCodeAnnouncement)
            {
                suppressNextExitCodeAnnouncement = false;
            }
            else
            {
                if (process.ExitCode != 0)
                {
                    string message = await GetCrashMessage();
                    if (!string.IsNullOrEmpty(message))
                    {
                        System.Windows.MessageBox.Show(message);
                    }
                }
            }

            runningGameProcess = null;
            this._compilerViewModel.DidRunnerStartProcess = false;


            if (!global::Glue.MainGlueWindow.Self.IsDisposed)
            {
                // This can get disposed in the meantime...
                try
                {
                    global::Glue.MainGlueWindow.Self.Invoke(() =>
                    {
                        IsRunning = runningGameProcess != null;
                        DidRunnerStartProcess = GetDidRunnerStartProcess();

                    });
                }
                catch (ObjectDisposedException)
                {
                    // do nothing.
                }
            }
        }

        private async Task<string> GetCrashMessage()
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
                    message = $"The game has crashed. See the Build tab for a callstack";
                    var contents = System.IO.File.ReadAllText(logFile);

                    GlueCommands.Self.DialogCommands.FocusTab("Build");

                    ErrorReceived?.Invoke(contents);
                    
                }
                else
                {
                    message = "Oh no! The game crashed. Run from Visual Studio for more information on the error.";
                }

            }

            return message;
        }

        internal Task<string> KillGameProcess()
        {
            return Task.Run(() =>
            {
                if (runningGameProcess != null)
                {
                    suppressNextExitCodeAnnouncement = true;
                    try
                    {
                        runningGameProcess.Kill();
                        var maxWaitTimeMs = 1000;
                        if (runningGameProcess.HasExited == false)
                        {
                            runningGameProcess.WaitForExit(maxWaitTimeMs);
                        }
                    }
                    catch
                    {
                        // do nothing
                        runningGameProcess = null;
                    }
                    IsRunning = false;
                    DidRunnerStartProcess = GetDidRunnerStartProcess();
                }

                return "";
            });
        }

        private class RunnerStatus
        {
            public bool IsWaitingForGameToStart { get; set; }
        }
    }
}
