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
    class Runner
    {
        #region Fields/Properties

        Process runningGameProcess;

        public bool IsRunning => runningGameProcess != null;
        public event EventHandler IsRunningChanged;

        bool suppressNextExitCodeAnnouncement = false;
        bool foundAlreadyRunningProcess = false;

        WindowRectangle? lastWindowRectangle;

        System.Windows.Forms.Timer timer;

        #endregion

        #region DLL Import 

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
                // find a process for game
                var processes = Process.GetProcesses()
                    .OrderBy(item => item.ProcessName)
                    .ToArray();

                var projectName = GlueState.Self.CurrentMainProject?.Name;

                var found = processes
                    .FirstOrDefault(item => item.ProcessName.ToLowerInvariant() == projectName);

                if(found != null)
                {
                    foundAlreadyRunningProcess = true;
                    runningGameProcess = found;

                    runningGameProcess.EnableRaisingEvents = true;
                    runningGameProcess.Exited += HandleProcessExit;

                    IsRunningChanged?.Invoke(this, null);
                }
            }
        }

        internal async void Run()
        {
            foundAlreadyRunningProcess = false;
            var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;
            var projectDirectory = FileManager.GetDirectory(projectFileName);
            var executableName = FileManager.RemoveExtension(FileManager.RemovePath(projectFileName));
            // todo - make the plugin smarter so it knows where the .exe is really located
            var exeLocation = projectDirectory + "bin/x86/debug/" + executableName + ".exe";

            if(System.IO.File.Exists(exeLocation))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exeLocation;
                startInfo.WorkingDirectory = FileManager.GetDirectory(exeLocation);

                runningGameProcess = System.Diagnostics.Process.Start(startInfo);

                runningGameProcess.EnableRaisingEvents = true;
                runningGameProcess.Exited += HandleProcessExit;

                if(lastWindowRectangle != null)
                {
                    int numberOfTimesToTry = 30;
                    for(int i = 0; i < numberOfTimesToTry; i++)
                    {
                        IntPtr id = runningGameProcess.MainWindowHandle;

                        if(id == IntPtr.Zero)
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

        private void HandleProcessExit(object sender, EventArgs e)
        {
            var process = sender as Process;

            //IntPtr id = process.MainWindowHandle;
            //Runner.MoveWindow(process.MainWindowHandle, 0, 0, 500, 500, true);
            //Runner.GetWindowRect(id, out RECT windowRect);

            int m = 3;

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
            IntPtr id = process.MainWindowHandle;

            Runner.GetWindowRect(id, out WindowRectangle windowRect);
            lastWindowRectangle = windowRect;

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
