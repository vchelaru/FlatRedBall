using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace FlatRedBall.Glue.Controls
{
    public class EmbeddedProgramPanel : Panel
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        const int SW_MINIMIZE = 6;
        const int SW_MAXIMIZE = 3;

        const int GWL_STYLE = -16;

        Process mP;

        System.Windows.Forms.Timer sync;

        static int _gId = 1;

        int _id;

        public delegate void ProcessLoadedDelegate(EmbeddedProgramPanel obj);

        public event ProcessLoadedDelegate ProcessLoaded;

        public EmbeddedProgramPanel(string path, string arguments) : this(Process.Start(path, arguments))
        {}

        public EmbeddedProgramPanel(Process process)
        {
            _id = _gId++;
            mP = process;

            Resize += new EventHandler(Resize_Handler);

            sync = new System.Windows.Forms.Timer();

            sync.Tick += new EventHandler(sync_Tick);
            sync.Interval = 100;
            sync.Start();
        }

        void sync_Tick(object sender, EventArgs e)
        {
            IntPtr windowBefore = mP.MainWindowHandle;
            if (windowBefore != (IntPtr)0x00000000)
            {
                if (SetParent(mP.MainWindowHandle, this.Handle) != (IntPtr)0x00000000)
                {
                    SetWindowLong(mP.MainWindowHandle, GWL_STYLE, (IntPtr)0x10000000);
                    MoveWindow(mP.MainWindowHandle, 0, 0, Width, Height, true);
                    sync.Stop();

                    if (ProcessLoaded != null)
                        ProcessLoaded(this);
                }
            }
        }

        public string Title()
        {
            Thread.Sleep(100);
            StringBuilder bldr;

            int length = GetWindowTextLength(mP.MainWindowHandle);

            bldr = new StringBuilder(length + 1);

            GetWindowText(mP.MainWindowHandle, bldr, bldr.Capacity);

            return bldr.ToString();
        }

        public int Id()
        {
            return _id;
        }

        private void Resize_Handler(object sender, EventArgs e)
        {
            MoveWindow(mP.MainWindowHandle, 0, 0, Width, Height, true);
        }

        ~EmbeddedProgramPanel()
        {
            if(mP != null && !mP.HasExited)
                mP.Kill();
        }
    }
}
