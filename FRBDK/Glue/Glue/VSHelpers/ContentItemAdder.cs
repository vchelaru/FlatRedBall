using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers
{
    public class ContentItemAdder
    {
        /// <summary>
        /// The list of files which are contained in a library as embedded resources.
        /// </summary>
        List<string> mFilesToAdd = new List<string>();

        public string OutputFolderInProject
        {
            get;
            set;
        }

        public void Add(string resourceName)
        {
            mFilesToAdd.Add(resourceName);

        }



        public bool PerformAddAndSave(Assembly assemblyContainingResource)
        {
            bool succeeded = true;
            bool preserveCase = FileManager.PreserveCase;
            FileManager.PreserveCase = true;

            List<string> filesToAddToProject = new List<string>();

            foreach (string resourceName in mFilesToAdd)
            {
                // User may have shut down the project:
                if (ProjectManager.ProjectBase != null)
                {
                    succeeded = SaveResourceFileToProject(assemblyContainingResource, succeeded, filesToAddToProject, resourceName);
                }
                else
                {
                    succeeded = false;
                }

                if (!succeeded)
                {
                    break;
                }
            }

            if (succeeded)
            {
                // Add these files to the project and any synced project
                foreach (var file in filesToAddToProject)
                {
                    bool wasAdded = ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(ProjectManager.ProjectBase, file);
                    if (wasAdded)
                    {
                        PluginManager.ReceiveOutput("Added file to project: " + file);
                    }
                }
            }

            FileManager.PreserveCase = preserveCase;

            if (succeeded)
            {
                ProjectManager.SaveProjects();
            }

            return succeeded;
        }

        private bool SaveResourceFileToProject(Assembly assemblyContainingResource, bool succeeded, List<string> filesToAddToProject, string resourceName)
        {
            // Vic says - I started implementing this but realized we may not want it because files should probably be added as RFS's rather
            // than standalone files. That way Glue can perform its reference tracking.
            throw new NotImplementedException();
        }
    }
}
