using System.Windows;
using System.Windows.Controls;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Projects;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    /// <summary>
    /// Interaction logic for SyncedProjectsControl.xaml
    /// </summary>
    public partial class SyncedProjectsControl : UserControl
    {
        SyncedProjectsViewModel ViewModel => DataContext as SyncedProjectsViewModel;
        public SyncedProjectsControl()
        {
            InitializeComponent();
        }

        private void AddNewProjectClick(object sender, RoutedEventArgs e)
        {
            NewProjectHelper.CreateNewSyncedProject();
        }

        private void AddProjectClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter =
                $@"{L.Texts.CSharpProjectFiles}(*.csproj)|*.csproj|{L.Texts.VisualStudioFiles} (*.vcproj)|*.vcproj|{L.Texts.ProjectAndroid} (*.project)|*.project";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                var newProject = ProjectManager.AddSyncedProject(openFileDialog.FileName);

                // If newProject is null, then no project was added
                if (newProject != null)
                {
                    ViewModel.Refresh();
                }
                else
                {
                    GlueGui.ShowMessageBox(L.Texts.ProjectIsAlreadySynced);
                }

                GluxCommands.Self.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();

            }
        }

        private void RemoveProjectClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = ViewModel.SelectedItem;
            if(selectedItem != null && selectedItem.ProjectBase != GlueState.Self.CurrentMainProject)
            {
                ProjectManager.RemoveSyncedProject(selectedItem.ProjectBase);
                ViewModel.Refresh();

                GluxCommands.Self.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

    }
}
