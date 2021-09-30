using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.Compiler;
using OfficialPlugins.GameHost.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace OfficialPlugins.GameHost
{
    [Export(typeof(PluginBase))]
    public class MainGameHostPlugin : PluginBase
    {
        public override string FriendlyName => "Game Host";

        public override Version Version => new Version();

        PluginTab pluginTab;
        GameHostView GameHostView;

        public override void StartUp()
        {
            GameHostView = new GameHostView();
            GameHostView.DoItClicked += HandleGameHostDoItClicked;
            pluginTab = base.CreateAndAddTab(GameHostView, "???", TabLocation.Left);
        }

        private void HandleGameHostDoItClicked(object sender, EventArgs e)
        {
            var process = Runner.TryFindGameProcess();
            var handle = process?.MainWindowHandle;

            if(handle != null)
            {
                var host = new HwndHostEx(handle.Value);

                GameHostView.AddChild(host);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }


    }

    class HwndHostEx : HwndHost
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        private IntPtr ChildHandle = IntPtr.Zero;

        public HwndHostEx(IntPtr handle)
        {
            this.ChildHandle = handle;
        }

        protected override System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent)
        {
            HandleRef href = new HandleRef();

            if (ChildHandle != IntPtr.Zero)
            {
                const int GWL_STYLE = (-16);
                const int WS_CHILD = 0x40000000;

                SetWindowLong(this.ChildHandle, GWL_STYLE, WS_CHILD);


                SetParent(this.ChildHandle, hwndParent.Handle);
                href = new HandleRef(this, this.ChildHandle);
            }

            return href;
        }

        protected override void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd)
        {

        }
    }

    //public class NotepadHwndHost
    //{
    //    private Process _process;

    //    protected override HWND BuildWindowOverride(HWND hwndParent)
    //    {
    //        ProcessStartInfo psi = new ProcessStartInfo("notepad.exe");
    //        _process = Process.Start(psi);
    //        _process.WaitForInputIdle();

    //        // The main window handle may be unavailable for a while, just wait for it
    //        while (_process.MainWindowHandle == IntPtr.Zero)
    //        {
    //            Thread.Yield();
    //        }

    //        HWND hwnd = new HWND(_process.MainWindowHandle);

    //        const int GWL_STYLE = -16;
    //        const int BORDER = 0x00800000;
    //        const int DLGFRAME = 0x00400000;
    //        const int WS_CAPTION = BORDER | DLGFRAME;
    //        const int WS_THICKFRAME = 0x00040000;
    //        const int WS_CHILD = 0x40000000;

    //        int style = GetWindowLong(notepadHandle, GWL_STYLE);
    //        style = style & ~WS_CAPTION & ~WS_THICKFRAME; // Removes Caption bar and the sizing border
    //        style |= WS_CHILD; // Must be a child window to be hosted

    //        NativeMethods.SetWindowLong(hwnd, GWL.STYLE, style);

    //        return hwnd;
    //    }

    //    protected override void DestroyWindowOverride(HWND hwnd)
    //    {
    //        _process.CloseMainWindow();

    //        _process.WaitForExit(5000);

    //        if (_process.HasExited == false)
    //        {
    //            _process.Kill();
    //        }

    //        _process.Close();
    //        _process.Dispose();
    //        _process = null;
    //        hwnd.Dispose();
    //        hwnd = null;
    //    }
    //}




}
