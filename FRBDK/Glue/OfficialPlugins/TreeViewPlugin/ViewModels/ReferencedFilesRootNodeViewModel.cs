using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


        public ReferencedFilesRootNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(parent)
        {
            this.glueElement = glueElement;
        }

        public override void RefreshTreeNodes()
        {
            string relativeDirectory = this.GetRelativePath();


            AddDirectoryNodesRecursively(ProjectManager.MakeAbsolute(relativeDirectory, true), this);



            #region Add new nodes or update the text of existing ones

            for (int i = 0; i < glueElement.ReferencedFiles.Count; i++)
            {
                var file = glueElement.ReferencedFiles[i];
                var nodeForFile = GetTreeNodeFor(glueElement.ReferencedFiles[i]);

                if (nodeForFile == null)
                {
                    string thisAbsolute = ProjectManager.MakeAbsolute(this.GetRelativePath(), true);
                    string fullFile = ProjectManager.MakeAbsolute(file.GetRelativePath(), true);
                    string directoryNodeToFind = FileManager.GetDirectory(fullFile);
                    directoryNodeToFind = FileManager.MakeRelative(directoryNodeToFind, thisAbsolute);
                    var nodeToAddTo = GetNodeForDirectory(directoryNodeToFind, this);

                    if (nodeToAddTo == null)
                    {
                        nodeToAddTo = this;
                    }
                    nodeForFile = new NodeViewModel(nodeToAddTo);
                    nodeForFile.ImageSource = FileIcon;
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

                    //if (i != index)
                    //{
                    //    nodeList.RemoveAt(index);
                    //    nodeList.Insert(i, nodeForFile);
                    //}

                    string newText = FileManager.RemovePath(file.Name);
                    if (newText != nodeForFile.Text)
                    {
                        nodeForFile.Text = newText;
                    }
                }
            }
            #endregion


            RemoveTreeNodesForRemovedReferenceFileSavesIn(this.Children, glueElement.ReferencedFiles, glueElement);

            this.SortByTextConsideringDirectories(Children, true);
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
            catch (Exception e)
            {
                int m = 3;
            }
            return null;

        }

        private NodeViewModel GetNodeForDirectory(string directory, NodeViewModel currentNode)
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

                for (int i = 0; i < currentNode.Children.Count; i++)
                {
                    var subNode = currentNode.Children[i];

                    if (subNode.Text == nameToSearchFor && subNode.IsFolderInFilesContainerNode())
                    {
                        return GetNodeForDirectory(directory.Substring(indexOfSlash + 1), subNode);
                    }
                }

                return null;
            }
        }

        private void AddDirectoryNodesRecursively(string currentDirectory, NodeViewModel treeNode)
        {
            if (System.IO.Directory.Exists(currentDirectory))
            {
                string[] directories = System.IO.Directory.GetDirectories(currentDirectory);

                #region Add new nodes

                foreach (string directory in directories)
                {

                    if (!DirectoriesToIgnore.Contains(FileManager.RemovePath(directory)))
                    {
                        bool alreadyHasNode = false;

                        // See if there is already a tree node with this name
                        string directoryRelativeToThisTreeNode = FileManager.MakeRelative(
                            directory, ProjectManager.MakeAbsolute(treeNode.GetRelativePath(), true)) + "/";

                        var existingTreeNode = GetNodeForDirectory(directoryRelativeToThisTreeNode, treeNode);

                        if (existingTreeNode == null)
                        {
                            var newNode = new NodeViewModel(treeNode);
                            newNode.Text = (FileManager.RemovePath(directory));

                            //newNode.ForeColor = ElementViewWindow.FolderColor;

                            treeNode.Children.Add(newNode);
                        }
                    }
                }

                #endregion


                foreach (var subNode in treeNode.Children)
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

        private void RemoveTreeNodesForRemovedReferenceFileSavesIn(ObservableCollection<NodeViewModel> currentNodeList, List<ReferencedFileSave> referencedFiles, IElement container)
        {
            #region Remove existing nodes if the ReferencedFileSave is gone

            for (int i = currentNodeList.Count - 1; i > -1; i--)
            {
                ReferencedFileSave referencedFileSave = currentNodeList[i].Tag as ReferencedFileSave;

                if (currentNodeList[i].IsFolderInFilesContainerNode())
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
                    string rfsName = referencedFileSave.Name.Replace("/", "\\");


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

    }
}