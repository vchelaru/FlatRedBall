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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace OfficialPlugins.GameHost
{
    [Export(typeof(PluginBase))]
    public class MainGameHostPlugin : PluginBase
    {
        #region DLLImports
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf?view=netframeworkdesktop-4.8
        internal const int
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          LBS_NOTIFY = 0x00000001,
          HOST_ID = 0x00000002,
          LISTBOX_ID = 0x00000001,
          WS_VSCROLL = 0x00200000,
          WS_BORDER = 0x00800000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        #endregion

        public override string FriendlyName => "Game Host";

        public override Version Version => new Version();

        PluginTab pluginTab;
        System.Windows.Forms.Panel winformsPanel;

        Process gameProcess;

        public override void StartUp()
        {
            // winforms stuff is here:
            // https://social.msdn.microsoft.com/Forums/en-US/f6e28fe1-03b2-4df5-8cfd-7107c2b6d780/hosting-external-application-in-windowsformhost?forum=wpf

            winformsPanel = new System.Windows.Forms.Panel();
            winformsPanel.BackColor = System.Drawing.Color.FromArgb(20,20,20);
            winformsPanel.Width = 20;
            winformsPanel.Height = 30;
            winformsPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            //GameHostView = new GameHostView();
            //GameHostView.DoItClicked += MoveGameToHost;
            pluginTab = base.CreateAndAddTab(winformsPanel, "Game", TabLocation.Center);
            pluginTab.CanClose = false;
            pluginTab.AfterHide += (_, __) => TryKillGame();
            //pluginTab = base.CreateAndAddTab(GameHostView, "Game Contrll", TabLocation.Bottom);
        }

        public async void MoveGameToHost()
        {
            gameProcess = Runner.TryFindGameProcess();
            var handle = gameProcess?.MainWindowHandle;

            if(handle != null)
            {
                SetParent(handle.Value, winformsPanel.Handle);

                PluginManager.CallPluginMethod("Glue Compiler", "MakeGameBorderless", new object[] { true });
                WindowRectangle rectangle = new WindowRectangle();
                do
                {
                    WindowMover.GetWindowRect(handle.Value, out rectangle);

                    var delay = 140;
                    await Task.Delay(delay);

                    WindowMover.MoveWindow(handle.Value, 0, 0, rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top, true);

                } while (rectangle.Left != 0 && rectangle.Left != 0);

            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            TryKillGame();
            return true;
        }

        private void TryKillGame()
        {
            if (gameProcess != null)
            {
                try
                {
                    gameProcess?.Kill();
                }
                catch
                {
                    // no biggie, It hink
                }
            }
        }
    }

    #region WPF solution (unused)

    // This is a WPF solution as a host which works great except keyboard events don't make it through. That sucks! So we winforms it and that seems to work
    class HwndHostEx : HwndHost, IKeyboardInputSink 
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf?view=netframeworkdesktop-4.8
        internal const int
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          LBS_NOTIFY = 0x00000001,
          HOST_ID = 0x00000002,
          LISTBOX_ID = 0x00000001,
          WS_VSCROLL = 0x00200000,
          WS_BORDER = 0x00800000;

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        internal static extern bool DestroyWindow(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        protected override bool TabIntoCore(TraversalRequest request)
        {
            //if (request.FocusNavigationDirection == FocusNavigationDirection.Next)
                this.SetFocusCSharp();
            //else
            //    SetFocus(lastWin32Control);

            return true;
        }

        //Handle this event to focus back to the WPF conrol.
        protected override bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers)
        {
            //if (msg.message == WM_KEYDOWN && msg.wParam == IntPtr(VK_TAB))
            //{
            //    // Handle Shift+Tab
            //    if (GetKeyState(VK_SHIFT))
            //    {
            //        if (GetFocus() == hwndOfFirstControl)
            //        {
            //            // We're at the beginning, so send focus to the previous WPF element
            //            return this->KeyboardInputSite->OnNoMoreTabStops(
            //                gcnew TraversalRequest(FocusNavigationDirection::Previous));
            //        }
            //        else
            //            return (SetFocus(hwndOfPreviousControl) != NULL);
            //    }
            //    // Handle Shift without Tab
            //    else
            //    {
            //        if (GetFocus() == hwndOfLastControl)
            //        {
            //            // We're at the end, so send focus to the next WPF element
            //            return this->KeyboardInputSite->OnNoMoreTabStops(
            //                gcnew TraversalRequest(FocusNavigationDirection::Next));
            //        }
            //        else
            //            return (SetFocus(hwndOfNextControl) != NULL);
            //    }
            //}

            return true;
        }

        private void SetFocusCSharp()
        {
            //call win32 SetFocus function to focus the win32 control.
            SetFocus(ChildHandle);

        }

        private IntPtr ChildHandle = IntPtr.Zero;

        public HwndHostEx(IntPtr handle)
        {
            this.ChildHandle = handle;

            this.Loaded += HandleLoaded;

            this.MessageHook += HandleMessagHook;
        }

        private IntPtr HandleMessagHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            this.Focusable = true;
            this.FocusableChanged += (_,__) => SetFocusCSharp();
            this.GotFocus += (_,__) => SetFocusCSharp();
            this.GotKeyboardFocus += (_,__) => SetFocusCSharp();
        }

        protected override System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent)
        {
            HandleRef href = new HandleRef();

            if (ChildHandle != IntPtr.Zero)
            {
                //const int GWL_STYLE = (-16);

                //SetWindowLong(this.ChildHandle, GWL_STYLE, WS_CHILD);


                SetParent(this.ChildHandle, hwndParent.Handle);
                href = new HandleRef(this, this.ChildHandle);
            }

            return href;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        
    }

    #endregion
}
