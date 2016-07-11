using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using System.IO;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class ProjectCommands : IProjectCommands
    {
        public void SaveProjects()
        {
            ProjectManager.SaveProjects();
        }

        public void CreateAndAddPartialFile(IElement element, string partialName, string code)
        {
            var fileName = element.Name + ".Generated." + partialName + ".cs";
            var fullFileName = ProjectManager.ProjectBase.Directory + fileName;

            var save = false; // we'll be doing manual saving after it's created
            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(fileName, save);
            
            // Now we can save it:
            FileManager.SaveText(code, fullFileName);
        }

        public void AddContentFileToProject(string absoluteFileName, bool saveProjects = true)
        {
            string relativeFileName = FileManager.MakeRelative(absoluteFileName, ProjectManager.ProjectBase.ContentProject.Directory);
            ProjectManager.UpdateFileMembershipInProject(ProjectManager.ProjectBase, relativeFileName, false, false, null);
            if (saveProjects)
            {
                ProjectManager.SaveProjects();
            }
        }

        public void CopyToBuildFolder(ReferencedFileSave rfs)
        {
            string source = ProjectManager.ContentDirectory + rfs.Name;

            CopyToBuildFolder(rfs);
        }

        public void CopyToBuildFolder(string absoluteSource)
        {
            string buildFolder = FileManager.GetDirectory(GlueState.Self.CurrentGlueProjectFileName) + "bin/x86/debug/Content/";
            string destination = buildFolder +  FileManager.MakeRelative(absoluteSource, ProjectManager.ContentDirectory);

            string destinationFolder = FileManager.GetDirectory(destination);

            // We used to only check the bin folder, but we want to check the specific
            // destination folder. If this is a new entity or a new folder in an entity, 
            // there's no reason to copy this over yet - it means the game hasn't been built
            // with this file:
            if(System.IO.Directory.Exists(destinationFolder))
            {
                string projectName = FileManager.RemovePath(FileManager.RemoveExtension(GlueState.Self.CurrentGlueProjectFileName));

                try
                {
                    System.IO.File.Copy(absoluteSource, destination, true);

                    PluginManager.ReceiveOutput("Copied " + absoluteSource + " ==> " + destination);
                }
                catch (Exception e)
                {
                    // this could really overwhelm the user with popups, so let's just show output:
                    PluginManager.ReceiveOutput("Error copying file:\n\n" + e.ToString());
                }
            }
        }

        public void AddDirectory(string folderName, TreeNode treeNodeToAddTo)
        {

            if (treeNodeToAddTo.IsGlobalContentContainerNode())
            {
                string rootDirectory = FileManager.RelativeDirectory;
                if (ProjectManager.ContentProject != null)
                {
                    rootDirectory = ProjectManager.ContentProject.Directory;
                }

                string directory = rootDirectory + "GlobalContent/" + folderName;

                Directory.CreateDirectory(directory);
            }
            else if (treeNodeToAddTo.IsRootEntityNode())
            {
                string directory = FileManager.RelativeDirectory + "Entities/" +
                    folderName;

                Directory.CreateDirectory(directory);
            }
            else if (treeNodeToAddTo.IsDirectoryNode())
            {
                // This used to use RelativeDirectory, but
                // I think we want this to be content, so not
                // sure why it uses RelativeDirectory...
                //string directory = FileManager.RelativeDirectory +
                //    currentTreeNode.GetRelativePath() +
                //    tiw.Result;
                // Update October 16, 2011
                // An Enity has both folders
                // in the code folder (represented
                // by RelativeDirectory) as well as
                // in the Content project.  An Entity
                // may not have files in the Content folder,
                // but it must have code files.  Therefore, we
                // create folders in the code directory tree and
                // we worry about content when NamedObjectSaves are
                // added to a given Entity later.
                //string directory = currentTreeNode.GetRelativePath() +
                //    tiw.Result;
                // Update February 17, 2012
                // But...when we add a new folder
                // to an Entity, we want that folder
                // to show up in the tree view in Glue.
                // Glue only scans the content folder, so
                // we want to make sure this folder exists
                // so it shows up okay.

                string directory = FileManager.RelativeDirectory +
                        treeNodeToAddTo.GetRelativePath() +
                        folderName;
                directory = ProjectManager.MakeAbsolute(directory, true);

                Directory.CreateDirectory(directory);

                directory = ProjectManager.ContentDirectory +
                        treeNodeToAddTo.GetRelativePath() +
                        folderName;
                directory = ProjectManager.MakeAbsolute(directory, true);

                Directory.CreateDirectory(directory);

            }
            else if (treeNodeToAddTo.IsFilesContainerNode() || treeNodeToAddTo.IsFolderInFilesContainerNode())
            {
                string directory =
                    treeNodeToAddTo.GetRelativePath() + folderName;

                Directory.CreateDirectory(ProjectManager.MakeAbsolute(directory, true));

                if (EditorLogic.CurrentEntityTreeNode != null)
                {
                    EditorLogic.CurrentEntityTreeNode.UpdateReferencedTreeNodes();
                }
                else if (EditorLogic.CurrentScreenTreeNode != null)
                {
                    EditorLogic.CurrentScreenTreeNode.UpdateReferencedTreeNodes();
                }
            }
            else if (treeNodeToAddTo.IsFolderInFilesContainerNode())
            {

                throw new NotImplementedException();
            }

            var containingElementNode = treeNodeToAddTo.GetContainingElementTreeNode();

            IElement element = null;
            if (containingElementNode != null)
            {
                element = containingElementNode.Tag as IElement;
            }

            if (containingElementNode == null)
            {
                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
            }
            else
            {
                GlueCommands.Self.RefreshCommands.RefreshUi(element);
            }

            GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes();
        }

        public string MakeAbsolute(string relativeFileName, bool forceAsContent = false)
        {
            return ProjectManager.MakeAbsolute(relativeFileName, forceAsContent);

        }

    }
}
