using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    partial class MainTreeViewViewModel
    {
        #region Directories (top level)

        internal void AddDirectoryNodes()
        {
            AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", EntityRootNode, ScreenOrEntity.Entity);
            AddDirectoryNodes(FileManager.RelativeDirectory + "Screens/", ScreenRootNode, ScreenOrEntity.Screen);

            #region Add global content directories

            string contentDirectory = FileManager.RelativeDirectory;

            if (ProjectManager.ContentProject != null)
            {
                contentDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
            }

            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode, screenOrEntity:null);
            #endregion
        }

        #endregion

        #region Refresh

        internal void RefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            var elementTreeNode = GetElementTreeNode(element);

            var project = GlueState.Self.CurrentGlueProject;

            var shouldShow = !element.IsHiddenInTreeView &&
                (
                (element is ScreenSave asScreen && project.Screens.Contains(asScreen)) ||
                (element is EntitySave asEntity && project.Entities.Contains(asEntity)));

            if (elementTreeNode == null)
            {
                if (shouldShow)
                {
                    if (element is ScreenSave screen)
                    {
                        elementTreeNode = AddScreenTreeNode(screen);
                    }
                    else if (element is EntitySave entitySave)
                    {
                        elementTreeNode = AddEntityTreeNode(entitySave);
                    }
                    elementTreeNode?.RefreshTreeNodes(treeNodeRefreshType);
                }
            }
            else
            {
                if (!shouldShow)
                {
                    elementTreeNode.Parent?.Children.Remove(elementTreeNode);
                }
                else
                {
                    if (treeNodeRefreshType == TreeNodeRefreshType.All)
                    {
                        var treeNodeRelativeDirectory = ((ITreeNode)elementTreeNode).GetRelativeFilePath();

                        var elementNameModified = element.Name.Replace("\\", "/") + "/";

                        if (treeNodeRelativeDirectory != elementNameModified)
                        {
                            var desiredFolderForElement = FileManager.GetDirectory(element.Name, RelativeType.Relative);

                            var newParentTreeNode = GetTreeNodeByRelativePath(desiredFolderForElement);

                            // on a rename, the parent node hasn't yet been renamed yet, so let's tolerate nulls:
                            if(newParentTreeNode != null)
                            {
                                elementTreeNode.Parent.Remove(elementTreeNode);

                                newParentTreeNode.Add(elementTreeNode);
                                elementTreeNode.Parent = newParentTreeNode;
                            }
                        }
                    }


                    elementTreeNode?.RefreshTreeNodes(treeNodeRefreshType);
                }
            }
        }



        public NodeViewModel GetTreeNodeByRelativePath(string relativePath)
        {
            var start = StartOfRelative(relativePath, out string remainder);

            NodeViewModel treeNode;
            if (start ==  Localization.Texts.Screens)
            {
                treeNode = ScreenRootNode;
            }
            else if (start == Localization.Texts.Entities)
            {
                treeNode = EntityRootNode;
            }
            else
            {
                treeNode = GlobalContentRootNode;
            }



            if (!string.IsNullOrEmpty(remainder))
            {
                treeNode = GetByRelativePath(remainder, treeNode);
            }

            return treeNode;
        }


        static NodeViewModel GetByRelativePath(string path, NodeViewModel treeNode)
        {
            var start = StartOfRelative(path, out string remainder);

            var matchingChild = treeNode.Children.FirstOrDefault(item => item.Text == start);

            if (matchingChild != null)
            {
                if (string.IsNullOrEmpty(remainder))
                {
                    return matchingChild;
                }
                else
                {
                    return GetByRelativePath(remainder, matchingChild);
                }
            }
            else
            {
                return null;
            }
        }

        static string StartOfRelative(string relativePath, out string remainder)
        {
            if (relativePath.Contains('/'))
            {
                var indexOfSlash = relativePath.IndexOf('/');
                remainder = relativePath.Substring(indexOfSlash + 1);
                return relativePath.Substring(0, indexOfSlash);
            }
            else
            {
                remainder = string.Empty;
                return relativePath;
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
                    // May 6 2022
                    // This code used to remove files
                    // that were missing on disk from the Glue project.
                    // I don't know why - that seems wrong, because we want to
                    // show all files that are part of the project. The project should
                    // decide what to show, not what is on disk.
                    var absoluteRfs = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                    var nodeToAddTo = TreeNodeForDirectory(absoluteRfs.GetDirectoryContainingThis()) ??
                        GlobalContentRootNode;
                    nodeForFile = new NodeViewModel(TreeNodeType.ReferencedFileSaveNode, nodeToAddTo );

                    nodeToAddTo.Children.Add(nodeForFile);

                    nodeToAddTo.SortByTextConsideringDirectories();

                    nodeForFile.Tag = rfs;
                }

                #endregion

                string textToSet = FileManager.RemovePath(rfs.Name);
                nodeForFile.Text = textToSet;
                nodeForFile.IsEditable = true;
                nodeForFile.ImageSource =
                    rfs.IsCreatedByWildcard
                    ? NodeViewModel.FileIconWildcard
                    : NodeViewModel.FileIcon;


            }

            #endregion

            string contentDirectory = GlueState.Self.ContentDirectory;
            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode, null);


            #region Do cleanup - remove tree nodes that exist but represent objects no longer in the project


            for (int i = GlobalContentRootNode.Children.Count - 1; i > -1; i--)
            {
                var treeNode = GlobalContentRootNode.Children[i];

                treeNode.RemoveGlobalContentTreeNodesIfDoesntExist(treeNode);
            }

            #endregion

            GlobalContentRootNode.SortByTextConsideringDirectories(recursive: true);

        }

        internal void RefreshDirectoryNodes()
        {
            AddDirectoryNodes(GlueState.Self.CurrentGlueProjectDirectory + "Entities/", EntityRootNode, ScreenOrEntity.Entity);
            AddDirectoryNodes(GlueState.Self.CurrentGlueProjectDirectory + "Screens/", ScreenRootNode, ScreenOrEntity.Screen);

            string contentDirectory = GlueState.Self.ContentDirectory;

            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode, screenOrEntity:null);
        }

        public void RefreshBookmarks()
        {
            Bookmarks.Clear();
            var project = GlueState.Self.CurrentGlueProject;

            if(project?.Bookmarks == null)
            {
                return;
            }

            foreach(var bookmark in project.Bookmarks)
            {
                var vm = new BookmarkViewModel();
                vm.Text = bookmark.Name;
                vm.ImageSource = NodeViewModel.FromSource(bookmark.ImageSource);

                // The image sources were invalidated when the plugin changed from
                // having the "Core" suffix to not. Someting like this could happen in
                // the future so let's handle it here.
                if(vm.ImageSource == null)
                {
                    var node = GetTreeNodeByQualifiedPath(bookmark.Name);
                    if(node != null)
                    {
                        bookmark.ImageSource = node.ImageSource.UriSource.OriginalString;
                        vm.ImageSource = NodeViewModel.FromSource(bookmark.ImageSource);
                    }
                }

                this.Bookmarks.Add(vm);
            }
        }

        internal void AddDirectoryNodes(string parentDirectory, NodeViewModel parentTreeNode, ScreenOrEntity? screenOrEntity)
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

                        NodeViewModel? treeNode = null;

                        if(screenOrEntity == ScreenOrEntity.Entity)
                        {
                            treeNode = TreeNodeForDirectoryOrEntityNode(relativePath, parentTreeNode);
                        }
                        else if(screenOrEntity == ScreenOrEntity.Screen)
                        {
                            treeNode = TreeNodeForDirectoryOrScreenNode(relativePath, parentTreeNode);
                        }
                        else // null
                        {
                            // This handles both entities and global content
                            treeNode = TreeNodeForDirectoryOrEntityNode(relativePath, parentTreeNode);
                        }

                        if (treeNode == null)
                        {
                            treeNode = new NodeViewModel(TreeNodeType.GeneralDirectoryNode, parentTreeNode);

                            treeNode.IsEditable = true;

                            treeNode.Text = FileManager.RemovePath(directory);
                            parentTreeNode.Children.Add(treeNode);
                        }

                        AddDirectoryNodes(parentDirectory + relativePath + "/", treeNode, screenOrEntity);
                    }
                }

                // Now see if there are any directory tree nodes that don't have a matching directory

                // Let's make the directories lower case
                for (int i = 0; i < directories.Length; i++)
                {
                    directories[i] = FileManager.Standardize(directories[i]);

                    if (!directories[i].EndsWith("/", StringComparison.OrdinalIgnoreCase) && !directories[i].EndsWith("\\", StringComparison.OrdinalIgnoreCase))
                    {
                        directories[i] += "/";
                    }
                }

                ITreeNode root = parentTreeNode.Root();
                bool isGlobalContent = root.IsGlobalContentContainerNode();


                for (int i = parentTreeNode.Children.Count - 1; i > -1; i--)
                {
                    ITreeNode treeNode = parentTreeNode.Children[i];

                    if (((ITreeNode)treeNode).IsDirectoryNode())
                    {

                        string directory = GlueCommands.Self.GetAbsoluteFileName(treeNode.GetRelativeFilePath(), isGlobalContent);

                        directory = FileManager.Standardize(directory);

                        if (!directories.Contains(directory, StringComparer.OrdinalIgnoreCase))
                        {
                            parentTreeNode.Children.RemoveAt(i);
                        }
                    }
                }
            }
        }

        internal void RefreshSelectedItemInfoDisplay(List<ITreeNode> selectedTreeNodes)
        {
            if(selectedTreeNodes.Count == 0)
            {
                SelectedItemInfoDisplay = string.Empty;
            }
            else if(selectedTreeNodes.Count == 1)
            {
                SelectedItemInfoDisplay = selectedTreeNodes[0].Tag?.ToString();
            }
            else
            {
                SelectedItemInfoDisplay = $"{selectedTreeNodes.Count} items selected";
            }
        }

        #endregion

        private NodeViewModel AddEntityTreeNode(EntitySave entitySave)
        {
            string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name));

            NodeViewModel? treeNodeToAddTo;
            if (containingDirectory == $"Entities/")
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
                    string absoluteDirectory = GlueCommands.Self.GetAbsoluteFileName(containingDirectory, false);
                    if (!Directory.Exists(absoluteDirectory))
                    {
                        Directory.CreateDirectory(absoluteDirectory);

                    }
                    AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", EntityRootNode, ScreenOrEntity.Entity);

                    // now try again
                    treeNodeToAddTo = TreeNodeForDirectoryOrEntityNode(
                        directory, EntityRootNode);
                }
            }

            var treeNode = new GlueElementNodeViewModel(treeNodeToAddTo, entitySave, true);
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

            treeNode.RefreshTreeNodes(TreeNodeRefreshType.All);

            return treeNode;
        }

        private NodeViewModel AddScreenTreeNode(ScreenSave screenSave)
        {
            string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(screenSave.Name));

            NodeViewModel? treeNodeToAddTo;

            if (containingDirectory == $"Screens/")
            {
                treeNodeToAddTo = ScreenRootNode;
            }
            else
            {
                string directory = containingDirectory.Substring("Screens/".Length);

                treeNodeToAddTo = TreeNodeForDirectoryOrScreenNode(directory, ScreenRootNode);
                if(treeNodeToAddTo == null && !string.IsNullOrEmpty(directory))
                {
                    // If it's null that may mean the directory doesn't exist.  We should make it
                    string absoluteDirectory = GlueCommands.Self.GetAbsoluteFileName(containingDirectory, false);
                    if (!Directory.Exists(absoluteDirectory))
                    {
                        Directory.CreateDirectory(absoluteDirectory);

                    }
                    AddDirectoryNodes(FileManager.RelativeDirectory + "Screens/", EntityRootNode, ScreenOrEntity.Screen);

                    // now try again
                    treeNodeToAddTo = TreeNodeForDirectoryOrEntityNode(
                        directory, ScreenRootNode);
                }
            }

            var treeNode = new GlueElementNodeViewModel(treeNodeToAddTo, screenSave, true);
            treeNode.Text = FileManager.RemovePath(screenSave.Name);
            treeNode.Tag = screenSave;

            if (treeNodeToAddTo != null)
            {
                treeNodeToAddTo.Children.Add(treeNode);
                treeNodeToAddTo.SortByTextConsideringDirectories();
            }
            string generatedFile = screenSave.Name + ".Generated.cs";

            ScreenRootNode.SortByTextConsideringDirectories();

            treeNode.RefreshTreeNodes(TreeNodeRefreshType.All);

            return treeNode;
        }

        public void Clear()
        {
            ScreenRootNode.Children.Clear();
            EntityRootNode.Children.Clear();
            GlobalContentRootNode.Children.Clear();
        }

        internal void DeselectResursively(bool callSelectionLogic)
        {
            ScreenRootNode.DeselectResursively(callSelectionLogic);
            EntityRootNode.DeselectResursively(callSelectionLogic);
            GlobalContentRootNode.DeselectResursively(callSelectionLogic);
        }
    }
}
