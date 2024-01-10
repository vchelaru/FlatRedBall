using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for InitializationWindowWpf.xaml
    /// </summary>
    public partial class InitializationWindowWpf : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public string Message
        {
            set
            {
                FlatRedBall.Glue.Managers.TaskManager.Self.OnUiThread(() =>
                {
                    this.TopLevelLabel.Text = value;
                    this.SubLabel.Text = "";

                });

            }
        }

        public string SubMessage
        {
            set
            {
                FlatRedBall.Glue.Managers.TaskManager.Self.OnUiThread(() =>
                {
                    this.SubLabel.Text = value;
                });
            }
        }

        public InitializationWindowWpf()
        {
            InitializeComponent();
            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
