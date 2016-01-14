using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using FlatRedBall.Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using System.Diagnostics;
using FlatRedBall.Glue.IO;

namespace OfficialPlugins.StateInterpolation
{
    public enum AddFileBehavior
    {
        AlwaysCopy,
        CopyIfDoesntExist,
        NeverCopy
    }

    public class CodeBuildItemAdder
    {
        #region Fields

        List<string> mFilesToAdd = new List<string>();

        public string FolderInProject
        {
            get;
            set;
        }

        #endregion

        #region Properties

        public AddFileBehavior AddFileBehavior
        {
            get;
            set;
        }

        #endregion

        #region Constructor

        public CodeBuildItemAdder()
        {
            AddFileBehavior = StateInterpolation.AddFileBehavior.AlwaysCopy;

        }

        #endregion

        /// <summary>
        /// Adds the argument resourceName to the internal list.
        /// </summary>
        /// <param name="resourceName">The name of the resource.  This is usally in the format of
        /// ProjectNamespace.Folder.FileName.cs</param>
        public void Add(string resourceName)
        {
            mFilesToAdd.Add(resourceName);

        }

        public bool PerformAddAndSave(Assembly assembly)
        {
            bool succeeded = true;
            bool preserveCase = FileManager.PreserveCase;
            FileManager.PreserveCase = true;

            List<string> filesToAddToProject = new List<string>();

            succeeded = SaveFilesFromAssembly(assembly, filesToAddToProject);

            if (succeeded)
            {
                AddCodeItemsToProject(filesToAddToProject);
            }

            FileManager.PreserveCase = preserveCase;

            if (succeeded)
            {
                ProjectManager.SaveProjects();
            }

            return succeeded;
        }

        private static void AddCodeItemsToProject(List<string> filesToAddToProject)
        {
            // Add these files to the project and any synced project
            foreach (var file in filesToAddToProject)
            {
                ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(ProjectManager.ProjectBase, file);
            }
        }

        private bool SaveFilesFromAssembly(Assembly assembly, List<string> filesToAddToProject)
        {
            bool succeeded = true;
            foreach (string resourceName in mFilesToAdd)
            {
                succeeded = SaveAndAddResourceFileToProject(assembly, succeeded, filesToAddToProject, resourceName);

                if (!succeeded)
                {
                    break;
                }
            }
            return succeeded;
        }

        private bool SaveAndAddResourceFileToProject(Assembly assembly, bool succeeded, List<string> filesToAddToProject, string resourceName)
        {
            try
            {

                string destinationDirectory = ProjectManager.ProjectBase.Directory + FolderInProject + "/";

                string completelyStripped = FileManager.RemoveExtension(resourceName);
                int lastDot = completelyStripped.LastIndexOf('.');
                completelyStripped = completelyStripped.Substring(lastDot + 1);

                string destinationFile = destinationDirectory + completelyStripped + ".cs";


                bool shouldAdd = DetermineIfShouldCopyAndAdd(destinationFile);

                if (shouldAdd)
                {

                    Directory.CreateDirectory(destinationDirectory);

                    filesToAddToProject.Add(destinationFile);

                    var names = assembly.GetManifestResourceNames();

                    const int maxFailures = 6;
                    int numberOfFailures = 0;
                    while (true)
                    {
                        try
                        {
                            if (System.IO.File.Exists(destinationFile))
                            {
                                // Let's delete it and move it to the recycle bin
                                FileHelper.DeleteFile(destinationFile);
                                PluginManager.ReceiveOutput("Moving old file to recycle bin:" + destinationFile);
                            }


                            FileManager.SaveEmbeddedResource(assembly, resourceName, destinationFile);
                            break;
                        }
                        catch (Exception e)
                        {
                            numberOfFailures++;

                            if (numberOfFailures == maxFailures)
                            {
                                // failed - what do we do?
                                PluginManager.ReceiveOutput("Failed to copy over file " + resourceName + " because of the following error:\n" + e.ToString());
                                break;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(15);
                            }
                        }
                    }
                }
                else
                {
                    PluginManager.ReceiveOutput("Skipped copying file " + resourceName);
                }
            }
            catch (Exception e)
            {
                succeeded = false;

                MessageBox.Show("Could not copy the file " + resourceName + "\n\n" + e.ToString());
            }
            return succeeded;
        }

        private bool DetermineIfShouldCopyAndAdd(string destinationFile)
        {
            switch (AddFileBehavior)
            {
                case StateInterpolation.AddFileBehavior.AlwaysCopy:
                    return true;
                case StateInterpolation.AddFileBehavior.CopyIfDoesntExist:
                    return File.Exists(destinationFile) == false &&
                        AddFileBehavior == StateInterpolation.AddFileBehavior.CopyIfDoesntExist;
                case StateInterpolation.AddFileBehavior.NeverCopy:
                    return false;
                default:
                    return false;
            }
        }

    }
}
