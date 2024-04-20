using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    /// <summary>
    /// Interaction logic for ProjectListEntry.xaml
    /// </summary>
    public partial class ProjectListEntry : UserControl
    {
        ProjectBase Project => (this.DataContext as SyncedProjectViewModel).ProjectBase;

        public ProjectListEntry()
        {
            InitializeComponent();
        }

        private void OpenInVisualStudio(object sender, RoutedEventArgs e)
        {
            var project = Project;

            OpenInVisualStudio(project);
        }

        public static void OpenInVisualStudio(ProjectBase project)
        {
            string solutionName = null;

            try
            {
                solutionName = ProjectSyncer.LocateSolution(project.FullFileName.FullPath);
            }
            catch(FileNotFoundException fnfe)
            {
                MessageBox.Show(fnfe.Message);
            }

            string fileToOpen = null;

            if (!string.IsNullOrEmpty(solutionName))
            {
                if (!PluginManager.OpenSolution(solutionName))
                {
                    fileToOpen = solutionName;
                }
            }

            if (!string.IsNullOrEmpty(fileToOpen))
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = fileToOpen;
                startInfo.UseShellExecute = true; // needed in .net core according to:
                // https://github.com/dotnet/corefx/issues/10361

                // To load a .NET 5+ project, we have to call
                // Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults(); in MainGlueWindow which adjusts
                // MSBUILD_EXE_PATH environment variable.
                // Unfortuantely, changing this variable also affects Visual Studio so that it can't open
                // projects. To make VS open projects correctly, undo this variable assignment.
                var environmentBefore = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");
                try
                {

                    Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", null);

                    var startedProcess = Process.Start(startInfo);
                    var openedWithGlue = startedProcess?.ProcessName == "Glue";

                    if(openedWithGlue)
                    {
                        MessageBox.Show(String.Format(L.Texts.GlueFileAssociation, fileToOpen));
                    }
                }
                catch(InvalidOperationException)
                {
                    // An error with this code has been reported, but I'm not sure why. It's not damaging to just ignore it, and say that there was a failure:
                    GlueCommands.Self.PrintOutput(L.Texts.ErrorOpeningProjectVisualStudio);
                }
                catch(Exception e)
                {
                    GlueCommands.Self.PrintOutput(String.Format(L.Texts.ErrorCannotOpenProject, fileToOpen, e));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", environmentBefore);

                }

            }
        }

        private void ViewErrorsClicked(object sender, RoutedEventArgs e)
        {
            SyncedProjectViewModel viewModel = new SyncedProjectViewModel();
            viewModel.ProjectBase = this.Project;

            ProjectReferencesWindow window = new ProjectReferencesWindow();
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        public static void OpenInExplorer(ProjectBase project)
        {
            Process.Start("explorer.exe", "/select," + project.FullFileName.Standardized.Replace("/", "\\"));
        }

        private void OpenInExplorer(object sender, RoutedEventArgs e)
        {
            OpenInExplorer(Project);
        }

        static string GetProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)") + "/";
            }

            return Environment.GetEnvironmentVariable("ProgramFiles") + "/";
        }
    }
}
