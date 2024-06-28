using System.Windows;
using System.Windows.Controls;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Projects;
using L = Localization;
using FlatRedBall.Glue.Controls;
using ToolsUtilities;
using FlatRedBall.Glue.Utilities;
using Npc.ViewModels;
using Npc;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.VSHelpers;

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
            var newProjectVm = NewProjectHelper.CreateNewSyncedProject();

            if (newProjectVm != null)
            {
                ViewModel.CurrentProject = GlueState.Self.CurrentMainProject;
                ViewModel.SyncedProjects = GlueState.Self.SyncedProjects;
                ViewModel.Refresh();
            }
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

                GluxCommands.Self.SaveProjectAndElements();
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

                GluxCommands.Self.SaveProjectAndElements();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        private async void RenameProjectClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = ViewModel.SelectedItem;

            if(selectedItem == null)
            {
                return;
            }

            var tiw = new CustomizableTextInputWindow();
            tiw.Label.Text = "Renaming projects is considered experimental. Only do this if you have your project backed up. Are you sure you want to rename?" +
                "\n\nIf you decide to do this, make sure to close Visual Studio, any apps which may be referencing these files, and any Windows Explorer windows." +
                "\n\nIf you do not close these, the rename may fail halfway through due to file access errors.";
            tiw.TextBox.Text = selectedItem.ProjectBase.Name;
            var result = tiw.ShowDialog();

            if(result == true)
            {
                var newProjectName = tiw.TextBox.Text;
                var whyIsInvalid = ProjectCreationHelper.GetWhyProjectNameIsntValid(newProjectName);
                var oldProjectName = selectedItem.ProjectBase.Name;
                var solutionLocation = GlueState.Self.CurrentSlnFileName;
                var rootDirectory = solutionLocation.GetDirectoryContainingThis();

                if(newProjectName == oldProjectName)
                {
                    whyIsInvalid = "The new project name is the same as the old project name. You must provide a different name.";
                }

                if(!string.IsNullOrEmpty(whyIsInvalid))
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(whyIsInvalid);
                }
                else
                {
                    var dialogCommands = GlueCommands.Self.DialogCommands;
                    dialogCommands.ShowSpinner("Waiting for tasks to complete...");
                    await TaskManager.Self.WaitForAllTasksFinished();
                    dialogCommands.ShowSpinner("Closing Project...");

                    GlueCommands.Self.CloseGlueProject();
                    dialogCommands.ShowSpinner("Performing Rename...");

                    string? newNamespace = null;
                    Npc.ProjectCreationHelper.RenameProject(newProjectName, oldProjectName, newNamespace, rootDirectory.FullPath);

                    dialogCommands.HideSpinner();

                    GlueCommands.Self.DialogCommands.ShowMessageBox("The project has been renamed. You can now re-open the project.");
                }
            }
        }
    }
}
