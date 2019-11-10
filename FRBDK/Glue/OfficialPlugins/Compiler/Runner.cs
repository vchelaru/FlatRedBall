using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler
{
    class Runner : Singleton<Runner>
    {
        #region Fields/Properties

        Process runningGameProcess;

        public bool IsRunning => runningGameProcess != null;
        public event EventHandler IsRunningChanged;

        bool suppressNextExitCodeAnnouncement = false;
        bool foundAlreadyRunningProcess = false;

        WindowRectangle? lastWindowRectangle;

        System.Windows.Forms.Timer timer;

        public bool DidRunnerStartProcess => IsRunning && foundAlreadyRunningProcess == false;

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
                Process found = TryFindGameProcess();

                if (found != null)
                {
                    foundAlreadyRunningProcess = true;
                    runningGameProcess = found;

                    try
                    {
                        runningGameProcess.EnableRaisingEvents = true;
                        runningGameProcess.Exited += HandleProcessExit;

                        IsRunningChanged?.Invoke(this, null);

                    }
                    catch(InvalidOperationException)
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
                .FirstOrDefault(item => item.ProcessName.ToLowerInvariant() == projectName);
            return found;
        }

        internal async Task Run(bool preventFocus, string runArguments = null)
        {
            foundAlreadyRunningProcess = false;
            var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;
            var projectDirectory = FileManager.GetDirectory(projectFileName);
            var executableName = FileManager.RemoveExtension(FileManager.RemovePath(projectFileName));
            // todo - make the plugin smarter so it knows where the .exe is really located
            var exeLocation = projectDirectory + "bin/x86/debug/" + executableName + ".exe";

            if(System.IO.File.Exists(exeLocation))
            {
                StartProcess(preventFocus, runArguments, exeLocation);

                await Task.Delay(200);

                runningGameProcess = TryFindGameProcess();
                int numberOfTimesToTryGettingProcess = 5;
                int timesTried = 0;
                while(runningGameProcess == null)
                {
                    // didn't find it, so let's wait a little and try again:

                    await Task.Delay(500);

                    runningGameProcess = TryFindGameProcess();

                    timesTried++;

                    if(timesTried >= numberOfTimesToTryGettingProcess)
                    {
                        break;
                    }
                }

                if(runningGameProcess != null)
                {

                    runningGameProcess.EnableRaisingEvents = true;
                    runningGameProcess.Exited += HandleProcessExit;

                    if (lastWindowRectangle != null)
                    {
                        int numberOfTimesToTry = 30;
                        for (int i = 0; i < numberOfTimesToTry; i++)
                        {
                            IntPtr id = runningGameProcess.MainWindowHandle;

                            if (id == IntPtr.Zero)
                            {
                                await Task.Delay(250);
                                continue;
                            }
                            else
                            {
                                var rect = lastWindowRectangle.Value;
                                MoveWindow(id, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
                                lastWindowRectangle = null;
                                break;
                            }
                        }
                    }


                    global::Glue.MainGlueWindow.Self.Invoke(() =>
                    {
                        IsRunningChanged?.Invoke(this, null);
                    });
                }
            }
        }

        private static void StartProcess(bool preventFocus, string runArguments, string exeLocation)
        {
            // from here:
            // https://stackoverflow.com/questions/12586957/how-do-i-open-a-process-so-that-it-doesnt-have-focus
            if(preventFocus)
            {
                RestartGamePreventFocus(runArguments, exeLocation);
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

        #region Restart Game, Prevent Focus

        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        const int STARTF_USESHOWWINDOW = 1;
        const int SW_SHOWNOACTIVATE = 4;
        const int SW_SHOWMINNOACTIVE = 7;


        private static void StartProcessNoActivate(string cmdLine)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);


            // Set si.wShowWindow to SW_SHOWNOACTIVATE to show the window normally but without stealing focus
            // and SW_SHOWMINNOACTIVE to start the app minimised, again without stealing focus.
            si.dwFlags = STARTF_USESHOWWINDOW;
            si.wShowWindow = SW_SHOWNOACTIVATE;

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, true,
                0, IntPtr.Zero, null, ref si, out pi);

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }



        private static void RestartGamePreventFocus(string runArguments, string exeLocation)
        {
            if(!string.IsNullOrWhiteSpace(runArguments))
            {
                StartProcessNoActivate(exeLocation + " " + runArguments);
            }
            else
            {
                StartProcessNoActivate(exeLocation);
            }
        }

        #endregion

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
                IsRunningChanged?.Invoke(this, null);
            });
        }

        internal void Stop()
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
                Runner.GetWindowRect(id, out WindowRectangle windowRect);
                lastWindowRectangle = windowRect;
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
                    IsRunningChanged?.Invoke(this, null);
                }
            }
        }
    }
}
