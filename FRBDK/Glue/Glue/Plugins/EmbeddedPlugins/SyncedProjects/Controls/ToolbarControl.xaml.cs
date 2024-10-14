using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;
using System.Windows.Controls;
using L = Localization;

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
                MessageBox.Show("There is no loaded Glue project");
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
                GlueCommands.Self.DialogCommands.ShowMessageBox("There is no loaded Glue project");
            }
        }
    }
}
