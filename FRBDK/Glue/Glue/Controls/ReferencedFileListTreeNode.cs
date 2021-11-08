using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
    public class ReferencedFileListTreeNode : TreeNode
    {
        public ReferencedFileListTreeNode(string text)
            : base(text)
        {
        }

		public TreeNode GetTreeNodeFor(ReferencedFileSave referencedFileSave)
		{
            try
            {
                // the directory may be like Entities/Something but the ReferencedFileSave might be Content/Entities/Something
                // so if the RFS starts with Content, then get rid of it

                string rfsName = referencedFileSave.Name;
                if (rfsName.StartsWith("Content/"))
                {
                    rfsName = rfsName.Substring("Content/".Length);
                }

                string thisRelativePath = FileManager.GetDirectoryKeepRelative(rfsName);
                string parentText = this.Parent.Text.Replace("\\", "/");
                
                // Not sure why this is a .Contains...
                // If we're going to do a substring shouldn't
                // it be a .StartsWith?
                //if (thisRelativePath.Contains(parentText))
                if (thisRelativePath.StartsWith(parentText))
                {
                    thisRelativePath = thisRelativePath.Substring(parentText.Length + 1); // add one for the slash '/'
                }


                
                //this.GetRelativePath().Replace("\\", "/");

                //if (referencedFileSave.Name.Contains(thisRelativePath))
                //{
                //    thisRelativePath = referencedFileSave.Name.Substring(thisRelativePath.Length);
                //}

                TreeNode treeNodeToSearch = GetNodeForDirectory(thisRelativePath, this);

                if (treeNodeToSearch == null)
                {
                    treeNodeToSearch = this;
                }

                for (int i = 0; i < treeNodeToSearch.Nodes.Count; i++)
                {
                    TreeNode node = treeNodeToSearch.Nodes[i];

                    if (node.Tag == referencedFileSave)
                    {
                        return node;
                    }
                }
            }
            catch (Exception e)
            {
                int m = 3;
            }
            return null;

        }

        public void UpdateToReferencedFiles(List<ReferencedFileSave> referencedFiles, IElement container)
        {

            string relativeDirectory = this.GetRelativePath();


            AddDirectoryNodesRecursively(ProjectManager.MakeAbsolute(relativeDirectory, true), this);



            #region Add new nodes or update the text of existing ones

            for (int i = 0; i < referencedFiles.Count; i++)
            {

                TreeNode nodeForFile = GetTreeNodeFor(referencedFiles[i]);

                if (nodeForFile == null)
                {
                    string fullFile = ProjectManager.MakeAbsolute(referencedFiles[i].GetRelativePath(), true);
                    nodeForFile = new TreeNode(FileManager.RemovePath(referencedFiles[i].Name));

                    nodeForFile.ImageKey = "file.png";
                    nodeForFile.SelectedImageKey = "file.png";

                    string directoryNodeToFind = FileManager.GetDirectory(fullFile);
                    string thisAbsolute = ProjectManager.MakeAbsolute(this.GetRelativePath(), true);

                    directoryNodeToFind = FileManager.MakeRelative(directoryNodeToFind, thisAbsolute);

                    TreeNode nodeToAddTo = GetNodeForDirectory(directoryNodeToFind, this);

                    if (nodeToAddTo == null)
                    {
                        nodeToAddTo = this;
                    }

                    nodeToAddTo.Nodes.Add(nodeForFile);
                    nodeForFile.Tag = referencedFiles[i];
                    

                    if (!FileManager.FileExists(fullFile))
                    {
                        nodeForFile.ForeColor = ElementViewWindow.MissingObjectColor;
                    }
                }
                else
                {

                    TreeNodeCollection nodeList = nodeForFile.Parent.Nodes;
                    
                    // Victor Chelaru
                    // May 4, 2014
                    // I don't think we want the files in Glue to identically match the order of the files in the 
                    // Glux - we will want to alphabetize them.
                    //int index = nodeList.IndexOf(nodeForFile);

                    //if (i != index)
                    //{
                    //    nodeList.RemoveAt(index);
                    //    nodeList.Insert(i, nodeForFile);
                    //}

                    string newText = FileManager.RemovePath( referencedFiles[i].Name);
                    if (newText != nodeForFile.Text)
                    {
                        nodeForFile.Text = newText;
                    }
                }
            }
            #endregion


            RemoveTreeNodesForRemovedReferenceFileSavesIn(this.Nodes, referencedFiles, container);

            this.Nodes.SortByTextConsideringDirectories(true);
        }

        private void RemoveTreeNodesForRemovedReferenceFileSavesIn(TreeNodeCollection treeNodeCollection, List<ReferencedFileSave> referencedFiles, IElement container)
        {
            #region Remove existing nodes if the ReferencedFileSave is gone

            for (int i = treeNodeCollection.Count - 1; i > -1; i--)
            {
                ReferencedFileSave referencedFileSave = treeNodeCollection[i].Tag as ReferencedFileSave;

                if (treeNodeCollection[i].IsFolderInFilesContainerNode())
                {
                    RemoveTreeNodesForRemovedReferenceFileSavesIn(treeNodeCollection[i].Nodes, referencedFiles, container);
                }
                else if (ShouldTreeNodeBeRemoved(treeNodeCollection, referencedFiles, i, referencedFileSave, container))
                {
                    treeNodeCollection.RemoveAt(i);
                }
            }
            #endregion
        }

        private static bool ShouldTreeNodeBeRemoved(TreeNodeCollection treeNodeCollection, List<ReferencedFileSave> referencedFiles, int i, ReferencedFileSave referencedFileSave, IElement container)
        {
            if(!referencedFiles.Contains(referencedFileSave))
            {
                return true;
            }
            else
            {
                string elementNameWithTypePrefix = container.Name;
                if (container is ScreenSave)
                {
                    // I think the elementNameWithPrefix will always have "Screens\\" at the beginning:
                    // Update Feb 10, 2014
                    // Seems like it already
                    // has "Screens\\" in front of it.
                    //elementNameWithTypePrefix = "Screens\\" + elementNameWithTypePrefix;
                }
                else if (container is EntitySave)
                {
                    // see comment above
                    //elementNameWithTypePrefix = "Entities\\" + elementNameWithTypePrefix;
                }
                else
                {
                    elementNameWithTypePrefix = "GlobalContentFiles\\" + elementNameWithTypePrefix;
                }

                string treeNodeRelativePath = treeNodeCollection[i].GetRelativePath().Replace("/", "\\");


                bool isInSameCategory = false;

                isInSameCategory = treeNodeRelativePath.StartsWith("Entities\\") && elementNameWithTypePrefix.StartsWith("Entities\\") ||
                    treeNodeRelativePath.StartsWith("Screens\\") && elementNameWithTypePrefix.StartsWith("Screens\\") ||
                    treeNodeRelativePath.StartsWith("GlobalContentFiles\\") && elementNameWithTypePrefix.StartsWith("GlobalContentFiles\\");

                if (isInSameCategory)
                {
                    string rfsName = referencedFileSave.Name.Replace("/", "\\") ;


                    if (rfsName.StartsWith("Entities\\"))
                    {
                        rfsName = rfsName.Substring("Entities\\".Length);
                        elementNameWithTypePrefix = elementNameWithTypePrefix.Substring("Entities\\".Length);
                    }
                    else if (rfsName.StartsWith("Screens\\"))
                    {

                        rfsName = rfsName.Substring("Screens\\".Length);
                        elementNameWithTypePrefix = elementNameWithTypePrefix.Substring("Screens\\".Length);
                    }

                    if (FileManager.IsRelativeTo(rfsName, elementNameWithTypePrefix))
                    {
                        rfsName = referencedFileSave.Name.Replace("/", "\\");
                        bool shouldRemove = rfsName != treeNodeRelativePath;

                        return shouldRemove;
                    }
                }
                return false;
            }
        }


        private TreeNode GetNodeForDirectory(string directory, TreeNode currentNode)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return currentNode;
            }

            if (currentNode.IsFilesContainerNode())
            {
                string currentNodeDirectory = FileManager.Standardize(currentNode.GetRelativePath(), null, false);

                directory = FileManager.Standardize(directory, null, false);

                if (directory.StartsWith(currentNodeDirectory))
                {
                    directory = directory.Substring(currentNodeDirectory.Length);
                }
                
            }

            int indexOfSlash = directory.IndexOf("/");

            if (string.IsNullOrEmpty(directory))
            {
                return this;
            }
            else
            {
                string nameToSearchFor = directory.Substring(0, indexOfSlash);

                for (int i = 0; i < currentNode.Nodes.Count; i++)
                {
                    TreeNode subNode = currentNode.Nodes[i];

                    if (subNode.Text == nameToSearchFor && subNode.IsFolderInFilesContainerNode())
                    {
                        return GetNodeForDirectory(directory.Substring(indexOfSlash + 1), subNode);
                    }
                }

                return null;
            }
        }

        private void AddDirectoryNodesRecursively(string currentDirectory, TreeNode treeNode)
        {
            if (System.IO.Directory.Exists(currentDirectory))
            {
                string[] directories = System.IO.Directory.GetDirectories(currentDirectory);

                #region Add new nodes

                foreach (string directory in directories)
                {

                    if (!ElementViewWindow.DirectoriesToIgnore.Contains(FileManager.RemovePath(directory)))
                    {
                        bool alreadyHasNode = false;

                        // See if there is already a tree node with this name
                        string directoryRelativeToThisTreeNode = FileManager.MakeRelative(
                            directory, ProjectManager.MakeAbsolute(treeNode.GetRelativePath(), true)) + "/";

                        TreeNode existingTreeNode = GetNodeForDirectory(directoryRelativeToThisTreeNode, treeNode);

                        if (existingTreeNode == null)
                        {
                            TreeNode newNode = new TreeNode(FileManager.RemovePath(directory));

                            newNode.ForeColor = ElementViewWindow.FolderColor;

                            newNode.ImageKey = "folder.png";
                            newNode.SelectedImageKey = "folder.png";

                            treeNode.Nodes.Add(newNode);
                        }
                    }
                }

                #endregion


                foreach (TreeNode subNode in treeNode.Nodes)
                {
                    if (subNode.IsFolderInFilesContainerNode())
                    {
                        string subDirectory = subNode.GetRelativePath();

                        subDirectory = ProjectManager.MakeAbsolute(subDirectory, true);

                        AddDirectoryNodesRecursively(subDirectory, subNode);

                    }

                }
            }

        }
    }
}
