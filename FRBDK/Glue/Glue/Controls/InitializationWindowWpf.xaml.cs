using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            GlueCommands.Self.DialogCommands.MoveToCursor(this);

        }
    }
}
