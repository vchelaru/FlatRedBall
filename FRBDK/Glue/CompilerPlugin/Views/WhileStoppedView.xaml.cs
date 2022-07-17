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

namespace CompilerPlugin.Views
{
    /// <summary>
    /// Interaction logic for WhileStoppedView.xaml
    /// </summary>
    public partial class WhileStoppedView : UserControl
    {
        public event Action BuildClicked;
        public event Action BuildContentClicked;
        public event Action RunClicked;
        public event Action MSBuildSettingsClicked;

        public WhileStoppedView()
        {
            InitializeComponent();
        }

        private void HandleCompileClick(object sender, RoutedEventArgs e)
        {
            BuildClicked?.Invoke();
        }

        private void MSBuildSettingsButtonClicked(object sender, RenderingEventArgs e)
        {
            MSBuildSettingsClicked?.Invoke();
        }

        private void MSBuildSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            MSBuildSettingsClicked?.Invoke();
        }
    }
}
