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

namespace OfficialPlugins.Compiler
{
    /// <summary>
    /// Interaction logic for RunnerToolbar.xaml
    /// </summary>
    public partial class RunnerToolbar : UserControl
    {
        public event EventHandler RunClicked;

        public bool IsOpen
        {
            get => SplitButton.IsOpen;
            set => SplitButton.IsOpen = value;
        }

        public RunnerToolbar()
        {
            InitializeComponent();
        }

        private void HandleButtonClick(object sender, RoutedEventArgs args)
        {
            RunClicked?.Invoke(this, null);
        }
    }
}
