using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    /// <summary>
    /// Interaction logic for ProjectListEntry.xaml
    /// </summary>
    public partial class ProjectListEntry : UserControl
    {
        ProjectBase Project
        {
            get
            {
                return (this.DataContext as SyncedProjectViewModel).ProjectBase;
            }

        }

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

            if (string.IsNullOrEmpty(solutionName))
            {
                // I don't think we should do anything here. This could confuse users if
                // the solution isn't found. Let LocateSolution do its job, dont' second guess it...
                //if (!PluginManager.OpenProject(project.FullFileName))
                //{
                //    fileToOpen = ProjectManager.ProjectBase.FullFileName;
                //}
            }
            else
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
                        MessageBox.Show("Your machine has the file\n\n" + fileToOpen + "\n\nassociated with Glue.  " +
                            "It should probably be associated with a programming IDE like Visual Studio");
                    }
                }
                catch(InvalidOperationException)
                {
                    // An error with this code has been reported, but I'm not sure why. It's not damaging to just ignore it, and say that there was a failure:
                    GlueCommands.Self.PrintOutput("An error has occurred when trying to open the project. Please try again if Visual Studio has not opened.");
                }
                catch(Exception e)
                {
                    GlueCommands.Self.PrintOutput($"An error has occurred when trying to open the project file {fileToOpen}." +
                        $"You can still open this project manually through Windows Explorer or Visual Studio. Additional info:\n\n{e}");
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

        private void OpenInXamarinStudio(object sender, RoutedEventArgs e)
        {
            string solutionName = ProjectSyncer.LocateSolution(Project.FullFileName.FullPath);

            HandleOpenInXamarinStudioClick(solutionName);
        }


        private bool HandleOpenInXamarinStudioClick(string solution)
        {
            string standardizedSolution = FileManager.Standardize(solution).ToLowerInvariant();

            ProjectBase project = null;

            string mainSolution = ProjectSyncer.LocateSolution(GlueState.Self.CurrentMainContentProject.FullFileName.FullPath);

            if (standardizedSolution == FileManager.Standardize(mainSolution).ToLowerInvariant())
            {
                project = GlueState.Self.CurrentMainContentProject;
            }

            if (project == null)
            {
                // Maybe this is a synced project?
                foreach (var potentialProject in GlueState.Self.SyncedProjects)
                {
                    string potentialSolutionName = FileManager.Standardize(ProjectSyncer.LocateSolution(potentialProject.FullFileName.FullPath)).ToLowerInvariant();

                    if (potentialSolutionName == standardizedSolution)
                    {
                        project = potentialProject;
                        break;
                    }
                }
            }

            bool shouldHandle = project != null && project is AndroidProject;

            if (shouldHandle)
            {
                try
                {
                    string xamarinStudioLocation = GetProgramFilesx86() + "Xamarin Studio/bin/XamarinStudio.exe";
                    Process.Start(xamarinStudioLocation, solution);
                }
                catch (Exception ex)
                {
                    PluginManager.ReceiveError(ex.ToString());
                    MessageBox.Show("Error opening Xamarin Studio - see error in output");
                }
            }
            return shouldHandle;
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
