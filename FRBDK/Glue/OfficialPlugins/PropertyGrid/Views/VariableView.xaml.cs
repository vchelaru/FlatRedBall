using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPluginsCore.PropertyGrid.Views
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
