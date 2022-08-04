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
using System.Windows.Shapes;

namespace CompilerPlugin.Views
{
    /// <summary>
    /// Interaction logic for BuildSettingsWindow.xaml
    /// </summary>
    public partial class BuildSettingsWindow : Window
    {
        public BuildSettingsWindow()
        {
            InitializeComponent();

            this.Loaded += (not, used) => GlueCommands.Self.DialogCommands.MoveToCursor(this);

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataUiGrid.Instance = this.DataContext;

            DataUiGrid.InsertSpacesInCamelCaseMemberNames();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
