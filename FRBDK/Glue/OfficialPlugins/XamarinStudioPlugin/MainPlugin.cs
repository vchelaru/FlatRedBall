using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.XamarinStudioPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        
        public override void StartUp()
        {
            this.OpenSolutionHandler += HandleOpenInXamarinStudioClick;
        }

        private bool HandleOpenInXamarinStudioClick(string solution)
        {
            string standardizedSolution = FileManager.Standardize(solution).ToLowerInvariant();

            ProjectBase project = null;

            string mainSolution = ProjectSyncer.LocateSolution(GlueState.Self.CurrentMainContentProject.FullFileName);

            if (standardizedSolution == FileManager.Standardize(mainSolution).ToLowerInvariant())
            {
                project = GlueState.Self.CurrentMainContentProject;
            }

            if(project == null)
            {
                // Maybe this is a synced project?
                foreach(var potentialProject in GlueState.Self.SyncedProjects)
                {
                    string potentialSolutionName = FileManager.Standardize(ProjectSyncer.LocateSolution(potentialProject.FullFileName)).ToLowerInvariant();

                    if(potentialSolutionName == standardizedSolution)
                    {
                        project = potentialProject;
                        break;
                    }
                }
            }

            bool shouldHandle = project != null && project is AndroidProject;

            if(shouldHandle)
            {
                try
                {
                    string xamarinStudioLocation = GetProgramFilesx86() + "Xamarin Studio/bin/XamarinStudio.exe";
                    Process.Start(xamarinStudioLocation, solution);
                }
                catch(Exception ex)
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
