using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPlugins.Compiler;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPlugins.GameHost.Views
{
    /// <summary>
    /// Interaction logic for GameHostView.xaml
    /// </summary>
    public partial class GameHostView : UserControl
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

        #region Fields/Properties

        System.Windows.Forms.Panel winformsPanel;

        CompilerViewModel ViewModel => DataContext as CompilerViewModel;

        // It seems like once the game is moved, we can't get the handle to this again. Not sure why, but it's
        // simple enough to hold on to it
        IntPtr gameHandle;

        #endregion

        #region Events

        public event EventHandler DoItClicked;


        public event EventHandler StopClicked;
        public event EventHandler RestartGameClicked;
        public event EventHandler RestartGameCurrentScreenClicked;
        public event EventHandler RestartScreenClicked;
        public event EventHandler AdvanceOneFrameClicked;
        public event EventHandler PauseClicked;
        public event EventHandler UnpauseClicked;
        public event EventHandler SettingsClicked;

        #endregion

        public GameHostView()
        {
            InitializeComponent();


            winformsPanel = new System.Windows.Forms.Panel();
            winformsPanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            winformsPanel.Width = 20;
            winformsPanel.Height = 30;
            winformsPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;

            this.WinformsHost.Child = winformsPanel;

        }

        public async Task EmbedHwnd(IntPtr handle)
        {
            SetParent(handle, winformsPanel.Handle);
            gameHandle = handle;
            PluginManager.CallPluginMethod("Glue Compiler", "MakeGameBorderless", new object[] { true });
            WindowRectangle rectangle = new WindowRectangle();

            WindowMover.GetWindowRect(handle, out rectangle);

            // I used to have this code check if the window was at 0,0,
            // but that doesn't seem to actually work - the loop would run 
            // indefinitely, continually changing the position of the cursor.
            // Now I just do it 5 times and it seems to work
            for(int i = 0; i < 5; i++)
            {
                var delay = 180;
                await Task.Delay(delay);

                //var width = rectangle.Right - rectangle.Left;
                //var height = rectangle.Bottom - rectangle.Top;

                //var displaySettings = GlueState.Self.CurrentGlueProject?.DisplaySettings;
                //if(displaySettings != null)
                //{
                //    width = FlatRedBall.Math.MathFunctions.RoundToInt (displaySettings.ResolutionWidth * displaySettings.Scale / 100.0);
                //    height = FlatRedBall.Math.MathFunctions.RoundToInt(displaySettings.ResolutionHeight * displaySettings.Scale / 100.0);
                //}

                var width = (int)WinformsHost.ActualWidth;
                var height = (int)WinformsHost.ActualHeight;

                WindowMover.MoveWindow(handle, 0, 0, width, height, true);

            }

        }



        public void AddChild(UIElement child)
        {
            MainGrid.Children.Add(child);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoItClicked(this, null);
        }

        private void WhileRunningView_StopClicked(object sender, EventArgs e)
        {
            StopClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartGameClicked(object sender, EventArgs e)
        {
            RestartGameClicked?.Invoke(this, null);
        }

        private void WinformsHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel.IsRunning && ViewModel.IsGenerateGlueControlManagerInGame1Checked && gameHandle != IntPtr.Zero)
            {
                var newWidth = (int)WinformsHost.ActualWidth;
                var newHeight = (int)WinformsHost.ActualHeight;

                WindowMover.MoveWindow(gameHandle, 0, 0, newWidth, newHeight, true);
            }
        }

        private void WhileRunningView_RestartGameCurrentScreenClicked(object sender, EventArgs e)
        {
            RestartGameCurrentScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartScreenClicked(object sender, EventArgs e)
        {
            RestartScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_AdvanceOneFrameClicked(object sender, EventArgs e)
        {
            AdvanceOneFrameClicked?.Invoke(this, null);
        }

        private void WhileRunningView_PauseClicked(object sender, EventArgs e)
        {
            PauseClicked?.Invoke(this, null);
        }

        private void WhileRunningView_UnpauseClicked(object sender, EventArgs e)
        {
            UnpauseClicked?.Invoke(this, null);
        }

        private void GlueViewSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            SettingsClicked?.Invoke(this, null);
        }

        public void ReactToMainWindowMoved()
        {
            var oldWidth = WinformsHost.Width;
            var actualWidth = WinformsHost.ActualWidth;
            WinformsHost.Width = actualWidth;
            WinformsHost.UpdateLayout();
            WinformsHost_SizeChanged(null, null);
            WinformsHost.Width = double.NaN;
            WinformsHost.UpdateLayout();
        }
    }
}
