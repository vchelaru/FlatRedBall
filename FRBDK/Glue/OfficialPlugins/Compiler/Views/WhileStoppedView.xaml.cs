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
    /// Interaction logic for WhileStoppedView.xaml
    /// </summary>
    public partial class WhileStoppedView : UserControl
    {
        public event EventHandler BuildClicked;
        public event EventHandler BuildContentClicked;
        public event EventHandler RunClicked;

        public WhileStoppedView()
        {
            InitializeComponent();
        }

        private void HandleCompileClick(object sender, RoutedEventArgs e)
        {
            BuildClicked?.Invoke(this, null);
        }

        private void HandleBuildContentClick(object sender, RoutedEventArgs e)
        {

            BuildContentClicked?.Invoke(this, null);
        }

        private void HandleRunClick(object sender, RoutedEventArgs e)
        {
            RunClicked?.Invoke(this, null);
        }
    }
}
