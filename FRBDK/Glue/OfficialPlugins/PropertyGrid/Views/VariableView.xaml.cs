using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.PropertyGrid.Views
{
    /// <summary>
    /// Interaction logic for VariableView.xaml
    /// </summary>
    public partial class VariableView : UserControl
    {
        public VariableView()
        {
            InitializeComponent();
        }

        private void AddVariableButtonClick(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(FlatRedBall.Glue.Controls.CustomVariableType.New);
        }
    }
}
