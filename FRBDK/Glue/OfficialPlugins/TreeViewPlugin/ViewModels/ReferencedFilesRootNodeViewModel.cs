using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class ReferencedFilesRootNodeViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        public static HashSet<string> DirectoriesToIgnore = new HashSet<string>();

        static ReferencedFilesRootNodeViewModel()
        {
            DirectoriesToIgnore.Add(".svn");
        }


        public ReferencedFilesRootNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(TreeNodeType.ReferenedFileSaveContainerNode, parent)
        {
            this.glueElement = glueElement;
        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            string relativeDirectory = ((ITreeNode)this).GetRelativeFilePath();


            AddAndRemoveDirectoryNodesRecursively(GlueCommands.Self.GetAbsoluteFileName(relativeDirectory, true), this);



            #region Add new nodes or update the text of existing ones

            for (int i = 0; i < glueElement.ReferencedFiles.Count; i++)
            {
                var file = glueElement.ReferencedFiles[i];
                var nodeForFile = GetTreeNodeFor(glueElement.ReferencedFiles[i]);

                if (nodeForFile == null)
                {
                    string thisAbsolute = GlueCommands.Self.GetAbsoluteFileName(((ITreeNode)this).GetRelativeFilePath(), true);
                    string fullFile = GlueCommands.Self.GetAbsoluteFileName(file);
                    string directoryNodeToFind = FileManager.GetDirectory(fullFile);
                    directoryNodeToFind = FileManager.MakeRelative(directoryNodeToFind, thisAbsolute);
                    var nodeToAddTo = GetNodeForDirectory(directoryNodeToFind, this);

                    if (nodeToAddTo == null)
                    {
                        nodeToAddTo = this;
                    }
                    nodeForFile = new NodeViewModel( TreeNodeType.ReferencedFileSaveNode, nodeToAddTo);
                    nodeForFile.ImageSource = file.IsCreatedByWildcard
                        ? NodeViewModel.FileIconWildcard
                        : NodeViewModel.FileIcon;
                    nodeForFile.IsEditable = true;

                    nodeForFile.Tag = file;
                    nodeForFile.Text = FileManager.RemovePath(file.Name);




                    nodeToAddTo.Children.Add(nodeForFile);
                    nodeForFile.Tag = glueElement.ReferencedFiles[i];


                    if (!FileManager.FileExists(fullFile))
                    {
                        // todo - handle missing file:
                        //nodeForFile.ForeColor = ElementViewWindow.MissingObjectColor;
                    }
                }
                else
                {

                    var nodeList = nodeForFile.Parent.Children;

                    // Victor Chelaru
                    // May 4, 2014
                    // I don't think we want the files in Glue to identically match the order of the files in the 
                    // Glux - we will want to alphabetize them.
                    //int index = nodeList.IndexOf(nodeForFile);
                    // Update January 15, 2024 
                    // they should match because file order matters for things like Spine.
                    // This sucks but...we need to do it:
                    int index = nodeList.IndexOf(nodeForFile);

                    if (i != index)
                    {
                        nodeList.RemoveAt(index);
                        if(nodeList.Count == 0)
                        {
                            nodeList.Add(nodeForFile);
                        }
                        else
                        {
                            nodeList.Insert(i, nodeForFile);
                        }
                    }

                    string newText = FileManager.RemovePath(file.Name);
                    if (newText != nodeForFile.Text)
                    {
                        nodeForFile.Text = newText;
                    }
                }
            }
            #endregion


            RemoveTreeNodesForRemovedReferenceFileSavesIn(this.Children, glueElement.ReferencedFiles, glueElement);

            // no, don't sort! See above why this is important:
            //this.SortByTextConsideringDirectories(Children, true);
        }

        public NodeViewModel GetTreeNodeFor(ReferencedFileSave referencedFileSave)
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

                if (thisRelativePath.StartsWith(parentText))
                {
                    thisRelativePath = thisRelativePath.Substring(parentText.Length + 1); // add one for the slash '/'
                }

                var treeNodeToSearch = GetNodeForDirectory(thisRelativePath, this);

                if (treeNodeToSearch == null)
                {
                    treeNodeToSearch = this;
                }

                for (int i = 0; i < treeNodeToSearch.Children.Count; i++)
                {
                    var node = treeNodeToSearch.Children[i];

                    if (node.Tag == referencedFileSave)
                    {
                        return node;
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;

        }

        private NodeViewModel GetNodeForDirectory(string directory, NodeViewModel currentNode)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return currentNode;
            }

            if (((ITreeNode)currentNode).IsFilesContainerNode())
            {
                string currentNodeDirectory = FileManager.Standardize(((ITreeNode)currentNode).GetRelativeFilePath(), null, false);

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

                for (int i = 0; i < currentNode.Children.Count; i++)
                {
                    var subNode = currentNode.Children[i];

                    if (subNode.Text == nameToSearchFor && ((ITreeNode)subNode).IsFolderInFilesContainerNode())
                    {
                        return GetNodeForDirectory(directory.Substring(indexOfSlash + 1), subNode);
                    }
                }

                return null;
            }
        }

        private void AddAndRemoveDirectoryNodesRecursively(string currentDirectory, NodeViewModel treeNode)
        {
            if (System.IO.Directory.Exists(currentDirectory))
            {
                string[] directories = System.IO.Directory.GetDirectories(currentDirectory);

                #region Add new nodes

                foreach (string directory in directories)
                {

                    if (!DirectoriesToIgnore.Contains(FileManager.RemovePath(directory)))
                    {
                        // See if there is already a tree node with this name
                        string directoryRelativeToThisTreeNode = FileManager.MakeRelative(
                            directory, GlueCommands.Self.GetAbsoluteFileName(((ITreeNode)treeNode).GetRelativeFilePath(), true)) + "/";

                        var existingTreeNode = GetNodeForDirectory(directoryRelativeToThisTreeNode, treeNode);

                        if (existingTreeNode == null)
                        {
                            var newNode = new NodeViewModel(TreeNodeType.GeneralDirectoryNode, treeNode);
                            // We don't support this...yet
                            newNode.IsEditable = false;
                            newNode.Text = (FileManager.RemovePath(directory));

                            //newNode.ForeColor = ElementViewWindow.FolderColor;

                            treeNode.Children.Add(newNode);
                        }
                    }
                }

                #endregion


                foreach (var subNode in treeNode.Children.ToArray())
                {
                    if (((ITreeNode)subNode).IsFolderInFilesContainerNode())
                    {
                        string subDirectory = ((ITreeNode)subNode).GetRelativeFilePath();

                        subDirectory = GlueCommands.Self.GetAbsoluteFileName(subDirectory, true);

                        AddAndRemoveDirectoryNodesRecursively(subDirectory, subNode);

                    }

                }
            }
            else if((treeNode as ITreeNode).IsFilesContainerNode() == false &&
                (treeNode as ITreeNode).IsGlobalContentContainerNode() == false)
            {
                treeNode.Parent.Remove(treeNode);
            }
        }

        private void RemoveTreeNodesForRemovedReferenceFileSavesIn(ObservableCollection<NodeViewModel> currentNodeList, List<ReferencedFileSave> referencedFiles, IElement container)
        {
            #region Remove existing nodes if the ReferencedFileSave is gone

            for (int i = currentNodeList.Count - 1; i > -1; i--)
            {
                ReferencedFileSave referencedFileSave = currentNodeList[i].Tag as ReferencedFileSave;

                if (((ITreeNode)currentNodeList[i]).IsFolderInFilesContainerNode())
                {
                    RemoveTreeNodesForRemovedReferenceFileSavesIn(currentNodeList[i].Children, referencedFiles, container);
                }
                else if (ShouldTreeNodeBeRemoved(currentNodeList, referencedFiles, i, referencedFileSave, container))
                {
                    currentNodeList.RemoveAt(i);
                }
            }
            #endregion
        }

        private static bool ShouldTreeNodeBeRemoved(ObservableCollection<NodeViewModel> treeNodeCollection, List<ReferencedFileSave> referencedFiles, int i, ReferencedFileSave referencedFileSave, IElement container)
        {
            if (!referencedFiles.Contains(referencedFileSave))
            {
                return true;
            }
            else
            {
                string relativeFolder = container.Name + "\\";
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
                    relativeFolder = "GlobalContentFiles\\" + relativeFolder + "\\";
                }

                string treeNodeRelativePath = ((ITreeNode)treeNodeCollection[i]).GetRelativeFilePath().Replace("/", "\\");


                bool isInSameCategory = false;

                isInSameCategory = treeNodeRelativePath.StartsWith("Entities\\") && relativeFolder.StartsWith("Entities\\") ||
                    treeNodeRelativePath.StartsWith("Screens\\") && relativeFolder.StartsWith("Screens\\") ||
                    treeNodeRelativePath.StartsWith("GlobalContentFiles\\") && relativeFolder.StartsWith("GlobalContentFiles\\");

                if (isInSameCategory)
                {
                    string rfsName = referencedFileSave.Name.Replace("/", "\\");


                    if (rfsName.StartsWith("Entities\\"))
                    {
                        rfsName = rfsName.Substring("Entities\\".Length);
                        relativeFolder = relativeFolder.Substring("Entities\\".Length) + "\\";
                    }
                    else if (rfsName.StartsWith("Screens\\"))
                    {

                        rfsName = rfsName.Substring("Screens\\".Length);
                        relativeFolder = relativeFolder.Substring("Screens\\".Length) + "\\";
                    }

                    if (FileManager.IsRelativeTo(rfsName, relativeFolder))
                    {
                        rfsName = referencedFileSave.Name.Replace("/", "\\");
                        bool shouldRemove = rfsName != treeNodeRelativePath;

                        return shouldRemove;
                    }
                }
                return false;
            }
        }

    }
}