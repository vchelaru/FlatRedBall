using System;
using System.Collections.Generic;
using System.Linq;
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
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Projects;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    /// <summary>
    /// Interaction logic for SyncedProjectsControl.xaml
    /// </summary>
    public partial class SyncedProjectsControl : UserControl
    {
        SyncedProjectsViewModel ViewModel
        {
            get
            {
                return DataContext as SyncedProjectsViewModel;
            }
        }


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
            openFileDialog.Filter = "C# Project files (*.csproj)|*.csproj|VS Project files (*.vcproj)|*.vcproj|Android Project (*.project)|*.project";

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
                    GlueGui.ShowMessageBox("The selected project is already a synced project.");
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
