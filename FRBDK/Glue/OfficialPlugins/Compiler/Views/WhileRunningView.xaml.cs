using System;
using System.Collections.Generic;
using System.Linq;
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

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for WhileRunningView.xaml
    /// </summary>
    public partial class WhileRunningView : UserControl
    {
        public event EventHandler StopClicked;
        public event EventHandler RestartGameClicked;
        public event EventHandler RestartScreenClicked;
        public event EventHandler PauseClicked;
        public event EventHandler UnpauseClicked;

        public WhileRunningView()
        {
            InitializeComponent();
        }

        private void HandleStopClicked(object sender, RoutedEventArgs e)
        {
            StopClicked?.Invoke(this, null);
        }

        private void HandleRestartGameClicked(object sender, RoutedEventArgs e)
        {
            RestartGameClicked?.Invoke(this, null);
        }

        private void HandleRestartScreenClicked(object sender, RoutedEventArgs e)
        {
            RestartScreenClicked?.Invoke(this, null);
        }

        private void HandlePauseClicked(object sender, RoutedEventArgs e)
        {
            PauseClicked?.Invoke(this, null);
        }

        private void HandleUnpauseClicked(object sender, RoutedEventArgs e)
        {
            UnpauseClicked?.Invoke(this, null);
        }
    }
}
