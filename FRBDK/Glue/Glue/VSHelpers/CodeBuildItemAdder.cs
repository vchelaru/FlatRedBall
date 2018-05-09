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
using FlatRedBall.Glue.Plugins.ExportedImplementations;

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

        /// <summary>
        /// The list of files which are contained in a library as embedded resources.
        /// </summary>
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
            List<string> filesInFolder = GetItemsInFolder(folderName, assembly);

            mFilesToAdd.AddRange(filesInFolder);
        }

        public List<string> GetItemsInFolder(string folderName, Assembly assembly)
        {
            List<string> filesInFolder = new List<string>();
            var named = assembly.GetManifestResourceNames();
            string libraryWithDotAtEnd = folderName + ".";

            foreach (var item in named)
            {
                if (item.StartsWith(libraryWithDotAtEnd))
                {
                    filesInFolder.Add(item);
                }
            }

            return filesInFolder;
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

        public void PerformRemoveAndSave(Assembly assemblyContainingResource)
        {
            foreach (string resourceName in mFilesToAdd)
            {
                string destinationDirectory, destination;
                GetDestination(resourceName, out destinationDirectory, out destination);

                if(ProjectManager.ProjectBase?.IsFilePartOfProject(destination, Projects.BuildItemMembershipType.Any) == true)
                {
                    ProjectManager.ProjectBase.RemoveItem(destination);
                    GlueCommands.Self.PrintOutput($"Removing {destination} from project");
                }
            }

            if(ProjectManager.ProjectBase != null)
            {
                ProjectManager.SaveProjects();
            }

        }

        private bool SaveResourceFileToProject(Assembly assemblyContainingResource, bool succeeded, List<string> filesToAddToProject, string resourceName)
        {
            if (ProjectManager.ProjectBase == null)
            {
                throw new Exception("There is no project loaded.  You can't save a resource to a project without a project being loaded");
            }

            try
            {
                string destinationDirectory, destination;
                GetDestination(resourceName, out destinationDirectory, out destination);
                bool shouldAdd = GlueState.Self.CurrentGlueProject != null &&
                    DetermineIfShouldCopyAndAdd(destination, assemblyContainingResource);

                if (shouldAdd)
                {
                    SaveResource(assemblyContainingResource, filesToAddToProject, resourceName, destinationDirectory, destination);

                    if (IsVerbose)
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

        private void GetDestination(string resourceName, out string destinationDirectory, out string destination)
        {
            if(resourceName == null)
            {
                throw new ArgumentNullException(nameof(resourceName));
            }
            if(ProjectManager.ProjectBase == null)
            {
                destinationDirectory = null;
                destination = null;
            }
            else
            {
                destinationDirectory = ProjectManager.ProjectBase?.Directory + OutputFolderInProject + "/";
                destination = null;
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
            }
        }

        private static void SaveResource(Assembly assemblyContainingResource, List<string> filesToAddToProject, string resourceName, string destinationDirectory, string destination)
        {
            bool succeeded = false;
            Directory.CreateDirectory(destinationDirectory);

            filesToAddToProject.Add(destination);

            var names = assemblyContainingResource.GetManifestResourceNames();

            const int maxFailures = 6;
            int numberOfFailures = 0;
            while (true)
            {
                try
                {
                    FileManager.SaveEmbeddedResource(assemblyContainingResource, resourceName.Replace("/", "."), destination);
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

                string contents = "";
                Plugins.ExportedImplementations.GlueCommands.Self.TryMultipleTimes(() => contents = System.IO.File.ReadAllText(destination), 5);
                
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
