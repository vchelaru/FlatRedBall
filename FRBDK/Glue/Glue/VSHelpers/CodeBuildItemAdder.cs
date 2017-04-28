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

namespace FlatRedBall.Glue.VSHelpers
{
    public enum AddFileBehavior
    {
        AlwaysCopy,
        CopyIfDoesntExist,
        IfOutOfDate,
        NeverCopy
    }

    /// <summary>
    /// Extracts code files from an assembly and saves them to disk, relative to the current project.
    /// </summary>
    public class CodeBuildItemAdder
    {
        #region Fields

        List<string> mFilesToAdd = new List<string>();

        public bool IsVerbose { get; set; } = false;

        #endregion


        #region Properties
        public string OutputFolderInProject
        {
            get;
            set;
        }
        public AddFileBehavior AddFileBehavior
        {
            get;
            set;
        }

        #endregion

        #region Constructor

        public CodeBuildItemAdder()
        {
            AddFileBehavior = AddFileBehavior.AlwaysCopy;

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

        public void AddFolder(string folderName, Assembly assembly)
        {
            var named = assembly.GetManifestResourceNames();
            string libraryWithDotAtEnd = folderName + ".";

            foreach (var item in named)
            {
                if (item.StartsWith(libraryWithDotAtEnd))
                {
                    mFilesToAdd.Add(item);
                }
            }
        }

        public bool PerformAddAndSave(Assembly assembly)
        {
            bool succeeded = true;
            bool preserveCase = FileManager.PreserveCase;
            FileManager.PreserveCase = true;

            List<string> filesToAddToProject = new List<string>();

            foreach (string resourceName in mFilesToAdd)
            {
                succeeded = SaveResourceFileToProject(assembly, succeeded, filesToAddToProject, resourceName);

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
                    if(wasAdded)
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

        private bool SaveResourceFileToProject(Assembly assembly, bool succeeded, List<string> filesToAddToProject, string resourceName)
        {
            if (ProjectManager.ProjectBase == null)
            {
                throw new Exception("There is no project loaded.  You can't save a resource to a project without a project being loaded");
            }


            try
            {

                string destinationDirectory = ProjectManager.ProjectBase.Directory + OutputFolderInProject + "/";

                string destination = null;

                if (resourceName.Contains("/"))
                {
                    destination = destinationDirectory + FileManager.RemovePath(resourceName);
                }
                else
                {
                    string completelyStripped = FileManager.RemoveExtension(resourceName);
                    int lastDot = completelyStripped.LastIndexOf('.');
                    completelyStripped = completelyStripped.Substring(lastDot + 1);

                    destination = destinationDirectory + completelyStripped + ".cs";
                }
                bool shouldAdd = DetermineIfShouldCopyAndAdd(destination, assembly);

                if (shouldAdd)
                {
                    SaveResource(assembly, filesToAddToProject, resourceName, destinationDirectory, destination);

                    if(IsVerbose)
                    {
                        PluginManager.ReceiveOutput("Updating file: " + destination);
                    }
                }
                else
                {
                    if (IsVerbose)
                    {
                        PluginManager.ReceiveOutput("Skipping updating of file: " + destination);
                    }
                }
            }
            catch (Exception e)
            {
                succeeded = false;

                MessageBox.Show("Could not copy the file " + resourceName + "\n\n" + e.ToString());
            }
            return succeeded;
        }

        private static void SaveResource(Assembly assembly, List<string> filesToAddToProject, string resourceName, string destinationDirectory, string destination)
        {
            bool succeeded = false;
            Directory.CreateDirectory(destinationDirectory);

            filesToAddToProject.Add(destination);

            var names = assembly.GetManifestResourceNames();

            const int maxFailures = 6;
            int numberOfFailures = 0;
            while (true)
            {
                try
                {
                    FileManager.SaveEmbeddedResource(assembly, resourceName.Replace("/", "."), destination);
                    succeeded = true;
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

            if (succeeded)
            {
                // But after it's been saved we gotta see if it includes any
                // special string sequences like $PROJECT_NAMESPACE$
                string contents = System.IO.File.ReadAllText(destination);

                if (contents.Contains("$PROJECT_NAMESPACE$"))
                {
                    contents = contents.Replace("$PROJECT_NAMESPACE$", ProjectManager.ProjectNamespace);

                    numberOfFailures = 0;
                    while (numberOfFailures < maxFailures)
                    {
                        try
                        {
                            System.IO.File.WriteAllText(destination, contents);
                            break;
                        }
                        catch(Exception e)
                        {
                            numberOfFailures++;

                            if(numberOfFailures == maxFailures)
                            {
                                PluginManager.ReceiveOutput("Failed to save file " + 
                                    resourceName + " because of the following error:\n" +
                                    e.ToString());

                            }
                            else
                            {
                                System.Threading.Thread.Sleep(30);

                            }
                        }
                    }

                }

            }
        }

        private bool DetermineIfShouldCopyAndAdd(string destinationFile, Assembly assembly)
        {
            switch (AddFileBehavior)
            {
                case AddFileBehavior.AlwaysCopy:
                    return true;
                case AddFileBehavior.CopyIfDoesntExist:
                    {
                        bool isFileThere = File.Exists(destinationFile);
                        bool isAlreadyLinked = ProjectManager.ProjectBase.IsFilePartOfProject(destinationFile, Projects.BuildItemMembershipType.Any);
                        return isFileThere == false && isAlreadyLinked == false;
                    }
                case AddFileBehavior.IfOutOfDate:
                    {
                        bool isFileThere = File.Exists(destinationFile);
                        bool isAlreadyLinked = ProjectManager.ProjectBase.IsFilePartOfProject(destinationFile, Projects.BuildItemMembershipType.Any);
                        
                        if(isFileThere == false || isAlreadyLinked == false)
                        {
                            return true;
                        }
                        // The file is there and it's already linked.  We need to now
                        // check dates to see if it's out of date:

                        var existingFileDate = new FileInfo(destinationFile).LastWriteTime;
                        var assemblyDate = new FileInfo(assembly.Location).LastWriteTime;

                        return existingFileDate < assemblyDate;
                    }
                case AddFileBehavior.NeverCopy:
                    return false;
                default:
                    return false;
            }
        }

    }
}
