using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Glue;
using Timer = System.Threading.Timer;

namespace FlatRedBall.Glue
{
    public static class ProcessManager
    {
        // ShowWindowAsync Flags
        private const int SwShownormal = 1;
        private const int SwShowminimized = 2;
        private const int SwShowmaximized = 3;

        /// SetWindowPos Flags
        private static readonly uint
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;

        private static Timer _timer = new Timer(Tick, null, 0, 500);
        private static readonly Dictionary<Process, ProcessData> Processes = new Dictionary<Process, ProcessData>();
        private static readonly object ProcessLock = new object();

        private class ProcessData
        {
            public IntPtr ParentWindowHandle;
            public DateTime StartTime;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        /// <summary>
        /// Changes the size, position, and Z order of a child, pop-up or top-level window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="hWndInsertAfter">A handle to the window to precede the positioned window in the Z order. (HWND value)</param>
        /// <param name="X">The new position of the left side of the window, in client coordinates.</param>
        /// <param name="Y">The new position of the top of the window, in client coordinates.</param>
        /// <param name="W">The new width of the window, in pixels.</param>
        /// <param name="H">The new height of the window, in pixels.</param>
        /// <param name="uFlags">The window sizing and positioning flags. (SWP value)</param>
        /// <returns>Nonzero if function succeeds, zero if function fails.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        public static void OpenProcess(string path, string parameters)
        {
            OpenProcess(Process.Start(path, parameters));
        }

        private static void Tick(object data)
        {
            lock (ProcessLock)
            {
                var removeProcess = new List<Process>();


                foreach (var key in Processes.Keys)
                {

                    try
                    {
                        var process = key;
                        var startTime = Processes[key].StartTime;
                        var handle = Processes[key].ParentWindowHandle;

                        try
                        {
                            if (SetParent(key.MainWindowHandle, handle) == (IntPtr)0)
                            {
                                if ((DateTime.Now - startTime).TotalSeconds <= 10)
                                {
                                    continue;
                                }

                                removeProcess.Add(process);
                                continue;
                            }
                        }
                        catch
                        {
                            // The app may have already exited.  This also
                            // fails in Notepad++ for some reason.
                            removeProcess.Add(process);
                            continue;
                        }

                        removeProcess.Add(process);

                        key.EnableRaisingEvents = true;

                        var newItem = new ToolStripMenuItem(key.MainWindowTitle);
                        var focusItem = new ToolStripMenuItem("Focus");
                        var restoreItem = new ToolStripMenuItem("Restore");
                        var minimizeItem = new ToolStripMenuItem("Minimize");
                        var maximizeItem = new ToolStripMenuItem("Maximize");
                        //var grabItem = new ToolStripMenuItem("Grab Window");
                        var releaseItem = new ToolStripMenuItem("Release");
                        var resetPositionItem = new ToolStripMenuItem("Reset Position");
                        var closeItem = new ToolStripMenuItem("Close");

                        newItem.DropDownItems.Add(focusItem);
                        newItem.DropDownItems.Add(restoreItem);
                        newItem.DropDownItems.Add(minimizeItem);
                        newItem.DropDownItems.Add(maximizeItem);
                        //newItem.DropDownItems.Add(grabItem);
                        newItem.DropDownItems.Add(releaseItem);
                        newItem.DropDownItems.Add(resetPositionItem);
                        newItem.DropDownItems.Add(closeItem);


                        focusItem.Click += delegate
                                                {
                                                    SetForegroundWindow(process.MainWindowHandle);
                                                };
                        restoreItem.Click += delegate
                                                    {
                                                        ShowWindowAsync(process.MainWindowHandle, SwShownormal);
                                                    };
                        minimizeItem.Click += delegate
                                                    {
                                                        ShowWindowAsync(process.MainWindowHandle, SwShowminimized);
                                                    };
                        maximizeItem.Click += delegate
                                                    {
                                                        ShowWindowAsync(process.MainWindowHandle, SwShowmaximized);
                                                    };
                        //grabItem.Click += delegate
                        //                      {
                        //                          while (SetParent(process.MainWindowHandle, Form1.Self.Handle) ==
                        //                                 (IntPtr)0)
                        //                          {
                        //                          }
                        //                      };
                        releaseItem.Click += delegate
                                                    {
                                                        while (SetParent(process.MainWindowHandle, (IntPtr)0) ==
                                                            (IntPtr)0)
                                                        {
                                                        }
                                                    };
                        resetPositionItem.Click += delegate
                                                        {
                                                            SetWindowPos(process.MainWindowHandle, (IntPtr)0, 0, 0, 0, 0, NOZORDER | NOSIZE | SHOWWINDOW);
                                                        };
                        closeItem.Click += delegate
                                                {
                                                    process.CloseMainWindow();
                                                };

                        key.Exited += delegate
                                            {
                                                MainGlueWindow.Self.msProcesses.BeginInvoke(
                                                    new EventHandler(
                                                        delegate { MainGlueWindow.Self.msProcesses.Items.Remove(newItem); }));
                                            };

                        //Reset position of window
                        // March 3, 2012
                        // Victor Chelaru
                        // Not sure why we
                        // are resetting the
                        // position of the window
                        // but it makes it snap to
                        // the top left.  For some users
                        // this makes the position hide behind
                        // the taskbar, making it hard to move the
                        // window.  For reference: http://www.hostedredmine.com/issues/154415                        
                        //SetWindowPos(process.MainWindowHandle, (IntPtr)0, 0, 0, 0, 0, NOZORDER | NOSIZE | SHOWWINDOW);

                        MainGlueWindow.Self.BeginInvoke(
                            new EventHandler(delegate { MainGlueWindow.Self.msProcesses.Items.Add(newItem); }));
                    }
                    catch(Exception exception)
                    {
                        // Unknown error, but we'll just keep going.
                        removeProcess.Add(key);
                        continue;
                    }
                }


                foreach (var process in removeProcess)
                {
                    Processes.Remove(process);
                }
            }
        }

        public static void OpenProcess(Process process)
        {
            //Grab code is commented out until XNA programs can fix their bug where they won't resize correctly.

            if (process == null || process.HasExited) return;

            lock (ProcessLock)
            {
                Processes.Add(process, new ProcessData
                                           {
                                               ParentWindowHandle = EditorData.PreferenceSettings.ChildExternalApps
                                                    ? MainGlueWindow.Self.Handle
                                                    : (IntPtr)0,
                                               StartTime = DateTime.Now
                                           });
            }
        }
    }
}
