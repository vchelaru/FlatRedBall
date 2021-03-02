using FlatRedBall.Glue.Plugins.ExportedImplementations;
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

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects.Controls
{
    /// <summary>
    /// Interaction logic for ToolbarControl.xaml
    /// </summary>
    public partial class ToolbarControl : UserControl
    {
        public ToolbarControl()
        {
            InitializeComponent();
        }

        private void VisualStudioButtonClick(object sender, RoutedEventArgs e)
        {
            if(GlueState.Self.CurrentMainProject != null)
            {
                ProjectListEntry.OpenInVisualStudio(GlueState.Self.CurrentMainProject);
            }
            else
            {
                MessageBox.Show("No Glue project loaded");
            }
        }

        private void FolderButtonClick(object sender, RoutedEventArgs args)
        {
            if (GlueState.Self.CurrentMainProject != null)
            {
                ProjectListEntry.OpenInExplorer(GlueState.Self.CurrentMainProject);

            }
            else
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("No Glue project loaded");
            }
        }
    }
}
