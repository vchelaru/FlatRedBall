using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class MainTreeViewViewModel
    {
        #region Fields/Properties

        NodeViewModel ScreenRootNode;
        NodeViewModel EntityRootNode;
        NodeViewModel GlobalContentRootNode;


        public NodeViewModel RootModel { get; set; }

        public IEnumerable Root
        {
            get;
            private set;
        }

        public IEnumerable Children
        {
            get
            {
                return RootModel.Children;
            }
        }

        public string Title { get; set; }

        public int Count { get; set; }

        #endregion

        public MainTreeViewViewModel()
        {
            ScreenRootNode =
                new NodeViewModel(null) { Text = "Screens" };

            EntityRootNode =
                new NodeViewModel(null) { Text = "Entities" };

            GlobalContentRootNode =
                new NodeViewModel(null) { Text = "Global Content Files" };

            Root = new List<NodeViewModel>()
            {
                EntityRootNode,
                ScreenRootNode,
                GlobalContentRootNode,
            };
            //this.AddRecursive(ScreenRootNode, 4, 4);
            this.Title = "TreeListBox (N=" + this.Count + ")";
            
        }

        #region Directories (top level)

        internal void AddDirectoryNodes()
        {
            AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", EntityRootNode);

            #region Add global content directories

            string contentDirectory = FileManager.RelativeDirectory;

            if (ProjectManager.ContentProject != null)
            {
                contentDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
            }

            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode);
            #endregion
        }

        #endregion

        internal void RefreshTreeNodeFor(GlueElement element)
        {
            var elementTreeNode = GetElementTreeNode(element);

            if (elementTreeNode == null)
            {
                if (!element.IsHiddenInTreeView)
                {
                    if (element is ScreenSave screen)
                    {
                        elementTreeNode = new GlueElementNodeViewModel(ScreenRootNode, element);
                        ScreenRootNode.Children.Add(elementTreeNode);
                    }
                    else if (element is EntitySave entitySave)
                    {
                        elementTreeNode = AddEntityTreeNode(entitySave);
                    }
                    elementTreeNode?.RefreshTreeNodes();
                }
            }
            else
            {
                if (element.IsHiddenInTreeView)
                {
                    // remove it!
                    if (element is ScreenSave screen)
                    {
                        ScreenRootNode.Children.Remove(elementTreeNode);
                    }
                    else if (element is EntitySave entitySave)
                    {
                        EntityRootNode.Children.Remove(elementTreeNode);
                    }
                }
                else
                {
                    elementTreeNode?.RefreshTreeNodes();
                }
            }
        }

        internal void RefreshGlobalContentTreeNodes()
        {
            #region Loop through all referenced files.  Create a tree node if needed, or remove it from the project if the file doesn't exist.

            for (int i = 0; i < ProjectManager.GlueProjectSave.GlobalFiles.Count; i++)
            {
                ReferencedFileSave rfs = ProjectManager.GlueProjectSave.GlobalFiles[i];

                var nodeForFile = GetTreeNodeForGlobalContent(rfs, GlobalContentRootNode);

                #region If there is no tree node for this file, make one

                if (nodeForFile == null)
                {
                    string fullFileName = ProjectManager.MakeAbsolute(rfs.Name, true);

                    if (FileManager.FileExists(fullFileName))
                    {
                        string absoluteRfs = ProjectManager.MakeAbsolute(rfs.Name, true);
                        var nodeToAddTo = TreeNodeForDirectory(FileManager.GetDirectory(absoluteRfs)) ??
                            GlobalContentRootNode;
                        nodeForFile = new NodeViewModel(nodeToAddTo);
                        nodeForFile.Text = FileManager.RemovePath(rfs.Name);
                        nodeForFile.ImageSource = NodeViewModel.FileIcon;

                        //nodeForFile.ImageKey = "file.png";
                        //nodeForFile.SelectedImageKey = "file.png";


                        nodeToAddTo.Children.Add(nodeForFile);

                        nodeToAddTo.SortByTextConsideringDirectories();

                        nodeForFile.Tag = rfs;
                    }

                    else
                    {
                        ProjectManager.GlueProjectSave.GlobalFiles.RemoveAt(i);
                        // Do we want to do this?
                        // ProjectManager.GlueProjectSave.GlobalContentHasChanged = true;

                        i--;
                    }
                }

                #endregion

                #region else, there is already one

                else
                {
                    string textToSet = FileManager.RemovePath(rfs.Name);
                    if (nodeForFile.Text != textToSet)
                    {
                        nodeForFile.Text = textToSet;
                    }
                }

                #endregion
            }

            #endregion

            #region Do cleanup - remove tree nodes that exist but represent objects no longer in the project


            for (int i = GlobalContentRootNode.Children.Count - 1; i > -1; i--)
            {
                var treeNode = GlobalContentRootNode.Children[i];

                treeNode.RemoveGlobalContentTreeNodesIfDoesntExist(treeNode);
            }

            #endregion

        }

        private NodeViewModel AddEntityTreeNode(EntitySave entitySave)
        {
            //NodeViewModel elementTreeNode = new GlueElementNodeViewModel(EntityRootNode, entitySave);
            //EntityRootNode.Children.Add(elementTreeNode);
            //return elementTreeNode;

            string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name));

            NodeViewModel treeNodeToAddTo;
            if (containingDirectory == "Entities/")
            {
                treeNodeToAddTo = EntityRootNode;
            }
            else
            {
                string directory = containingDirectory.Substring("Entities/".Length);

                treeNodeToAddTo = TreeNodeForDirectoryOrEntityNode(directory, EntityRootNode);
                if (treeNodeToAddTo == null && !string.IsNullOrEmpty(directory))
                {
                    // If it's null that may mean the directory doesn't exist.  We should make it
                    string absoluteDirectory = ProjectManager.MakeAbsolute(containingDirectory);
                    if (!Directory.Exists(absoluteDirectory))
                    {
                        Directory.CreateDirectory(absoluteDirectory);

                    }
                    AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", EntityRootNode);

                    // now try again
                    treeNodeToAddTo = TreeNodeForDirectoryOrEntityNode(
                        directory, EntityRootNode);
                }
            }


            var treeNode = new GlueElementNodeViewModel(treeNodeToAddTo, entitySave);
            treeNode.Text = FileManager.RemovePath(entitySave.Name);
            treeNode.Tag = entitySave;

            // Someone in the chat room got a crash on the Add call.  Not sure why
            // so adding these to help find out what's up.
            if (treeNodeToAddTo == null)
            {
                throw new NullReferenceException("treeNodeToAddTo is null.  This is bad");
            }
            if (treeNode == null)
            {
                throw new NullReferenceException("treeNode is null.  This is bad");
            }

            treeNodeToAddTo.Children.Add(treeNode);
            treeNodeToAddTo.SortByTextConsideringDirectories();

            string generatedFile = entitySave.Name + ".Generated.cs";

            EntityRootNode.SortByTextConsideringDirectories();

            treeNode.RefreshTreeNodes();

            return treeNode;


        }


        private NodeViewModel GetElementTreeNode(GlueElement element)
        {
            if (element is ScreenSave)
            {
                return ScreenRootNode.Children.FirstOrDefault(item => item.Tag == element);
            }
            else if(element is EntitySave)
            {
                return EntityRootNode.Children.FirstOrDefault(item => item.Tag == element);
            }
            return null;
        }

        public void Select(int count)
        {
            var children = this.RootModel.Children as IList<NodeViewModel>;
            for (int i = 0; i < count; i++)
            {
                children[i].IsSelected = true;
            }
        }

        internal void AddDirectoryNodes(string parentDirectory, NodeViewModel parentTreeNode)
        {
            if (parentTreeNode == null)
            {
                throw new ArgumentNullException(nameof(parentTreeNode));
            }

            if (Directory.Exists(parentDirectory))
            {
                string[] directories = Directory.GetDirectories(parentDirectory);

                foreach (string directory in directories)
                {
                    string relativePath = FileManager.MakeRelative(directory, parentDirectory);

                    string nameOfNewNode = relativePath;

                    if (relativePath.Contains('/'))
                    {
                        nameOfNewNode = relativePath.Substring(0, relativePath.IndexOf('/'));
                    }

                    if (!ReferencedFilesRootNodeViewModel.DirectoriesToIgnore.Contains(nameOfNewNode))
                    {

                        var treeNode = TreeNodeForDirectoryOrEntityNode(relativePath, parentTreeNode);

                        if (treeNode == null)
                        {
                            treeNode = new NodeViewModel(parentTreeNode);
                            treeNode.Text = FileManager.RemovePath(directory);
                            parentTreeNode.Children.Add(treeNode);
                        }

                        //treeNode.ImageKey = "folder.png";
                        //treeNode.SelectedImageKey = "folder.png";

                        //treeNode.ForeColor = ElementViewWindow.FolderColor;

                        AddDirectoryNodes(parentDirectory + relativePath + "/", treeNode);
                    }
                }

                // Now see if there are any directory tree nodes that don't have a matching directory

                // Let's make the directories lower case
                for (int i = 0; i < directories.Length; i++)
                {
                    directories[i] = FileManager.Standardize(directories[i]).ToLower();

                    if (!directories[i].EndsWith("/") && !directories[i].EndsWith("\\"))
                    {
                        directories[i] = directories[i] + "/";
                    }
                }

                ITreeNode root = parentTreeNode.Root();
                bool isGlobalContent = root.IsGlobalContentContainerNode();


                for (int i = parentTreeNode.Children.Count - 1; i > -1; i--)
                {
                    ITreeNode treeNode = parentTreeNode.Children[i];

                    if (((ITreeNode)treeNode).IsDirectoryNode())
                    {

                        string directory = ProjectManager.MakeAbsolute(treeNode.GetRelativePath(), isGlobalContent);

                        directory = FileManager.Standardize(directory.ToLower());

                        if (!directories.Contains(directory))
                        {
                            parentTreeNode.Children.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public NodeViewModel TreeNodeForDirectoryOrEntityNode(string containingDirection, NodeViewModel containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return EntityRootNode;
            }
            else
            {
                return TreeNodeByDirectory(containingDirection, containingNode);
            }
        }


        public NodeViewModel TreeNodeByDirectory(string containingDirection, NodeViewModel containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return null;
            }
            else
            {
                int indexOfSlash = containingDirection.IndexOf("/");

                string rootDirectory = containingDirection;

                if (indexOfSlash != -1)
                {
                    rootDirectory = containingDirection.Substring(0, indexOfSlash);
                }

                for (int i = 0; i < containingNode.Children.Count; i++)
                {
                    var subNode = containingNode.Children[i];

                    if (((ITreeNode)subNode).IsDirectoryNode() && subNode.Text.ToLower() == rootDirectory.ToLower())
                    {
                        // use the containingDirectory here
                        if (indexOfSlash == -1 || indexOfSlash == containingDirection.Length - 1)
                        {
                            return subNode;
                        }
                        else
                        {
                            return TreeNodeByDirectory(containingDirection.Substring(indexOfSlash + 1), subNode);
                        }
                    }
                }

                return null;
            }
        }

        public NodeViewModel GetTreeNodeForGlobalContent(ReferencedFileSave rfs, NodeViewModel nodeToStartAt)
        {
            NodeViewModel containerTreeNode = nodeToStartAt;

            if (rfs.Name.ToLower().StartsWith("globalcontent/") && nodeToStartAt == GlobalContentRootNode)
            {
                string directory = FileManager.GetDirectoryKeepRelative(rfs.Name);

                int globalContentConstLength = "globalcontent/".Length;

                if (directory.Length > globalContentConstLength)
                {

                    string directoryToLookFor = directory.Substring(globalContentConstLength, directory.Length - globalContentConstLength);

                    containerTreeNode = TreeNodeForDirectoryOrEntityNode(directoryToLookFor, nodeToStartAt);
                }
            }


            if (rfs.Name.ToLower().StartsWith("content/globalcontent/") && nodeToStartAt == GlobalContentRootNode)
            {
                string directory = FileManager.GetDirectoryKeepRelative(rfs.Name);

                int globalContentConstLength = "content/globalcontent/".Length;

                if (directory.Length > globalContentConstLength)
                {

                    string directoryToLookFor = directory.Substring(globalContentConstLength, directory.Length - globalContentConstLength);

                    containerTreeNode = TreeNodeForDirectoryOrEntityNode(directoryToLookFor, nodeToStartAt);
                }
            }


            if (containerTreeNode != null)
            {
                for (int i = 0; i < containerTreeNode.Children.Count; i++)
                {
                    var subnode = containerTreeNode.Children[i];

                    if (subnode.Tag == rfs)
                    {
                        return subnode;
                    }
                    //else if (subnode.IsDirectoryNode())
                    //{
                    //    TreeNode foundNode = GetTreeNodeForGlobalContent(rfs, subnode);

                    //    if (foundNode != null)
                    //    {
                    //        return foundNode;
                    //    }
                    //}
                }
            }
            return null;
        }

        public NodeViewModel TreeNodeForDirectory(string containingDirectory)
        {
            bool isEntity = true;

            // Let's see if this thing is really an Entity


            string relativeToProject = FileManager.Standardize(containingDirectory).ToLower();

            if (FileManager.IsRelativeTo(relativeToProject, FileManager.RelativeDirectory))
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject);
            }
            else if (ProjectManager.ContentProject != null)
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject, ProjectManager.ContentProject.GetAbsoluteContentFolder());
            }

            if (relativeToProject.StartsWith("content/globalcontent") || relativeToProject.StartsWith("globalcontent")
                )
            {
                isEntity = false;
            }

            if (isEntity)
            {
                if (!FileManager.IsRelative(containingDirectory))
                {
                    containingDirectory = FileManager.MakeRelative(containingDirectory,
                        FileManager.RelativeDirectory + "Entities/");
                }

                return TreeNodeForDirectoryOrEntityNode(containingDirectory, EntityRootNode);
            }
            else
            {
                string subdirectory = FileManager.RelativeDirectory;

                if (ProjectManager.ContentProject != null)
                {
                    subdirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
                }
                subdirectory += "GlobalContent/";


                containingDirectory = FileManager.MakeRelative(containingDirectory, subdirectory);

                if (containingDirectory == "")
                {
                    return GlobalContentRootNode;
                }
                else
                {

                    return TreeNodeForDirectoryOrEntityNode(containingDirectory, GlobalContentRootNode);
                }
            }
        }

        public NodeViewModel GetTreeNodeByTag(object tag)
        {
            var found =
                ScreenRootNode.GetNodeByTag(tag) ??
                EntityRootNode.GetNodeByTag(tag) ??
                GlobalContentRootNode.GetNodeByTag(tag);
            return found;
        }
    }
}
