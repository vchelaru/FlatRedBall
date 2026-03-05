using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    partial class MainTreeViewViewModel
    {
        #region Search for Nodes

        List<NodeViewModel> tempListForSortingFilteredResults = new List<NodeViewModel>();
        private void RefreshFlattenedList()
        {
            ///////////////////Early Out///////////////////
            if (GlueState.Self.CurrentGlueProject == null)
            {
                return;
            }
            /////////////////End Early Out////////////////

            var searchToLower = SearchText?.ToLowerInvariant();
            var searchTermCaseSensitive = SearchText;

            tempListForSortingFilteredResults.Clear();

            var hasPrefix = !string.IsNullOrEmpty(PrefixText);

            List<StateSaveCategory> categories = new List<StateSaveCategory>();
            List<StateSave> states = new List<StateSave>();
            List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();
            List<CustomVariable> variables = new List<CustomVariable>();
            List<EventResponseSave> events = new List<EventResponseSave>();

            var showStates = !hasPrefix;
            var showCategories = !hasPrefix;
            var showObjects = !hasPrefix || PrefixText == "o";
            var showVariables = !hasPrefix || PrefixText == "v";
            var showEvents = !hasPrefix;
            var showFolders = !hasPrefix;

            foreach (var entity in GlueState.Self.CurrentGlueProject.Entities.ToArray())
            {
                if (!hasPrefix || PrefixText == "e")
                {
                    // strip off "entities\\"
                    // Actually, strip off folder completely, as this can cause confusion:
                    //var name = entity.Name.Substring("entities\\".Length);
                    var name = entity.GetStrippedName();
                    var matchWeight = SearchMatcher.GetMatchWeight(name, SearchText);
                    if (matchWeight > 0)
                    {
                        var vm = new GlueElementNodeViewModel(null, entity, false);
                        vm.SearchTermMatchWeight = matchWeight;
                        tempListForSortingFilteredResults.Add(vm);
                    }
                }

                AddInternalObjectsToLists(entity);
            }

            foreach (var screen in GlueState.Self.CurrentGlueProject.Screens.ToArray())
            {
                if (!hasPrefix || PrefixText == "s")
                {
                    //var name = screen.Name.Substring("screens\\".Length);
                    var name = screen.GetStrippedName();
                    var matchWeight = SearchMatcher.GetMatchWeight(name, SearchText);
                    if (matchWeight > 0)
                    {
                        var vm = new GlueElementNodeViewModel(null, screen, false);
                        vm.SearchTermMatchWeight = matchWeight;
                        tempListForSortingFilteredResults.Add(vm);
                    }
                }

                AddInternalObjectsToLists(screen);
            }

            if (!hasPrefix || PrefixText == "f")
            {
                foreach (var file in ObjectFinder.Self.GetAllReferencedFiles())
                {
                    var matchWeight = SearchMatcher.GetMatchWeight(file.Name, SearchText);
                    if (matchWeight > 0)
                    {
                        var vm = NodeFor(file);
                        vm.SearchTermMatchWeight = matchWeight;
                        tempListForSortingFilteredResults.Add(vm);
                    }
                }
            }

            if(showFolders)
            {
                AddFoldersRecurisively(tempListForSortingFilteredResults, ScreenRootNode);
                AddFoldersRecurisively(tempListForSortingFilteredResults, EntityRootNode);
                AddFoldersRecurisively(tempListForSortingFilteredResults, GlobalContentRootNode);
            }

            foreach (var nos in namedObjects)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(nos.InstanceName, SearchText, nos.DefinedByBase);
                if (matchWeight > 0)
                {
                    var node = new NodeViewModel(TreeNodeType.NamedObjectSaveNode );

                    node.ImageSource = NamedObjectsRootNodeViewModel.GetIcon(nos);

                    // ToString leads with the type not the name, so let's lead with the name instead
                    //node.Text = nos.ToString();
                    // don't use field name, that has the 'm' prefix in some cases
                    //node.Text = $"{nos.FieldName} ({nos.ClassType}) in {nos.GetContainer()}";
                    node.SearchTermMatchWeight = matchWeight;
                    node.Text = $"{nos.InstanceName} ({nos.ClassType}) in {nos.GetContainer()}";
                    node.Tag = nos;
                    //LayersTreeNode.SelectedImageKey = "layerList.png";
                    //LayersTreeNode.ImageKey = "layerList.png";

                    tempListForSortingFilteredResults.Add(node);
                }
            }

            foreach (var category in categories)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(category.Name, SearchText);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(TreeNodeType.StateCategoryNode );
                    treeNode.ImageSource = NodeViewModel.FolderClosedIcon;
                    treeNode.Text = category.ToString();
                    treeNode.Tag = category;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }
            }
            foreach (var state in states)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(state.Name, SearchText);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(TreeNodeType.StateNode);
                    treeNode.ImageSource = NodeViewModel.StateIcon;
                    treeNode.Text = state.ToString();
                    treeNode.Tag = state;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }

            }

            foreach (var variable in variables)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(variable.Name, SearchText, variable.DefinedByBase);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(TreeNodeType.CustomVariableNode);
                    if(variable.DefinedByBase)
                    {
                        treeNode.ImageSource = NodeViewModel.VariableIconDerived;
                    }
                    else
                    {
                        treeNode.ImageSource = NodeViewModel.VariableIcon;
                    }
                    treeNode.Text = variable.ToString();
                    treeNode.Tag = variable;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }

            }

            foreach (var eventItem in events)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(eventItem.EventName, SearchText);
                if(matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(TreeNodeType.EventNode);
                    treeNode.ImageSource = NodeViewModel.EventIcon;
                    treeNode.Text = eventItem.ToString();
                    treeNode.Tag = eventItem;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }
            }

            NodeViewModel NodeFor(ReferencedFileSave rfs)
            {
                var nodeForFile = new NodeViewModel(TreeNodeType.ReferencedFileSaveNode);
                nodeForFile.ImageSource =
                    rfs.IsCreatedByWildcard
                        ? NodeViewModel.FileIconWildcard
                        : NodeViewModel.FileIcon;
                nodeForFile.Tag = rfs;
                nodeForFile.Text = rfs.ToString();
                return nodeForFile;
            }

            var sorted = tempListForSortingFilteredResults
                .OrderByDescending(item => item.SearchTermMatchWeight)
                .ThenBy(item => item.Text);

            FlattenedItems.Clear();

            void AddFoldersRecurisively(List<NodeViewModel> tempListForSortingFilteredResults, NodeViewModel possibleFolderNode)
            {
                var matchWeight = SearchMatcher.GetMatchWeight(possibleFolderNode.Text, SearchText);
                if(matchWeight > 0 && possibleFolderNode.Text.EndsWith(".cs") == false && possibleFolderNode.Tag == null &&
                    (((ITreeNode)possibleFolderNode).IsFolderForGlobalContentFiles() || ((ITreeNode)possibleFolderNode).IsFolderInFilesContainerNode()))
                {
                    var node = new NodeViewModel(TreeNodeType.GeneralDirectoryNode);
                    node.ImageSource = NodeViewModel.FolderClosedIcon;
                    node.Tag = possibleFolderNode;
                    node.Text = $"{possibleFolderNode.Text} ({((ITreeNode) possibleFolderNode).GetRelativeFilePath()})";
                    node.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(node);
                }

                foreach(var child in possibleFolderNode.Children)
                {
                    AddFoldersRecurisively(tempListForSortingFilteredResults, child);
                }
            }

            foreach (var item in sorted)
            {
                FlattenedItems.Add(item);
            }

            void AddInternalObjectsToLists(GlueElement element)
            {
                if (showStates)
                {
                    foreach (var state in element.AllStates)
                    {
                        // We can't do Contains checks anymore, because there are search terms like CCS (CamelCaseSearch)
                        //if (state.Name.ToLowerInvariant().Contains(searchToLower))
                        {
                            states.Add(state);
                        }
                    }

                }
                if (showCategories)
                {
                    foreach (var category in element.StateCategoryList)
                    {
                        //if (category.Name.ToLowerInvariant().Contains(searchToLower))
                        {
                            categories.Add(category);
                        }
                    }
                }

                if (showObjects)
                {
                    foreach (var item in element.AllNamedObjects)
                    {
                        //if(item.InstanceName.ToLowerInvariant().Contains(searchToLower))
                        {
                            namedObjects.Add(item);
                        }
                    }
                }
                if (showVariables)
                {
                    foreach (var variable in element.CustomVariables)
                    {
                        //if(variable.Name.ToLowerInvariant().Contains(searchToLower))
                        {
                            variables.Add(variable);
                        }
                    }
                }
                if (showEvents)
                {
                    foreach (var eventItem in element.Events)
                    {
                        //if(eventItem.EventName.ToLowerInvariant().Contains(searchToLower))
                        {
                            events.Add(eventItem);
                        }
                    }
                }
            }

            if (FlattenedSelectedItem == null)
            {
                FlattenedSelectedItem = FlattenedItems.FirstOrDefault();
            }
        }



        private NodeViewModel GetElementTreeNode(GlueElement element)
        {
            return GetTreeNodeByTag(element);
        }

        public NodeViewModel? TreeNodeForDirectoryOrEntityNode(string containingDirection, NodeViewModel containingNode)
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

        public NodeViewModel? TreeNodeForDirectoryOrScreenNode(string containingDirection, NodeViewModel containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return ScreenRootNode;
            }
            else
            {
                return TreeNodeByDirectory(containingDirection, containingNode);
            }
            }

        public NodeViewModel? TreeNodeByDirectory(string containingDirection, NodeViewModel containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return null;
            }
            else
            {
                int indexOfSlash = containingDirection.IndexOf("/", StringComparison.OrdinalIgnoreCase);

                string rootDirectory = containingDirection;

                if (indexOfSlash != -1)
                {
                    rootDirectory = containingDirection.Substring(0, indexOfSlash);
                }

                for (int i = 0; i < containingNode.Children.Count; i++)
                {
                    var subNode = containingNode.Children[i];

                    if (((ITreeNode)subNode).IsDirectoryNode() && String.Equals(subNode.Text, rootDirectory, StringComparison.OrdinalIgnoreCase))
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

        public NodeViewModel? GetTreeNodeForGlobalContent(ReferencedFileSave rfs, NodeViewModel nodeToStartAt)
        {
            NodeViewModel? containerTreeNode = nodeToStartAt;

            if (rfs.Name.StartsWith("globalcontent/", StringComparison.OrdinalIgnoreCase) && nodeToStartAt == GlobalContentRootNode)
            {
                string directory = FileManager.GetDirectoryKeepRelative(rfs.Name);

                int globalContentConstLength = "globalcontent/".Length;

                if (directory.Length > globalContentConstLength)
                {

                    string directoryToLookFor = directory.Substring(globalContentConstLength, directory.Length - globalContentConstLength);

                    containerTreeNode = TreeNodeForDirectoryOrEntityNode(directoryToLookFor, nodeToStartAt);
                }
            }


            if (rfs.Name.StartsWith("content/globalcontent/", StringComparison.OrdinalIgnoreCase) && nodeToStartAt == GlobalContentRootNode)
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

        public NodeViewModel? TreeNodeForDirectory(FilePath containingDirectory)
        {
            bool isEntity = true;

            // Let's see if this thing is really an Entity

            string relativeToProject = FileManager.Standardize(containingDirectory.FullPath).ToLowerInvariant();

            if (FileManager.IsRelativeTo(relativeToProject, FileManager.RelativeDirectory))
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject);
            }
            else if (ProjectManager.ContentProject != null)
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject, ProjectManager.ContentProject.GetAbsoluteContentFolder());
            }

            if (relativeToProject.StartsWith("content/globalcontent", StringComparison.OrdinalIgnoreCase)
                || relativeToProject.StartsWith("globalcontent", StringComparison.OrdinalIgnoreCase))
            {
                isEntity = false;
            }

            if (isEntity)
            {
                var relativeToEntities = FileManager.MakeRelative(containingDirectory.FullPath,
                        FileManager.RelativeDirectory + "Entities/");
                return TreeNodeForDirectoryOrEntityNode(relativeToEntities, EntityRootNode);
            }
            else
            {
                string subdirectory = FileManager.RelativeDirectory;

                if (ProjectManager.ContentProject != null)
                {
                    subdirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
                }
                subdirectory += "GlobalContent/";

                var relative = FileManager.MakeRelative(containingDirectory.FullPath, subdirectory);

                if (relative == "")
                {
                    return GlobalContentRootNode;
                }
                else
                {

                    return TreeNodeForDirectoryOrEntityNode(relative, GlobalContentRootNode);
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

        public NodeViewModel? GetTreeNodeByQualifiedPath(string qualifiedPath)
        {
            var separated = qualifiedPath.Split('/').ToList();

            var node = GetTreeNode(separated);

            return node;
        }

        private NodeViewModel? GetTreeNode(List<string> separated)
        {
            var first = separated[0];
            separated.RemoveAt(0);

            NodeViewModel? nodeViewModel = null;

            if (first == EntityRootNode.Text)
            {
                nodeViewModel = EntityRootNode;
            }
            else if (first == ScreenRootNode.Text)
            {
                nodeViewModel = ScreenRootNode;
            }
            else if (first == GlobalContentRootNode.Text)
            {
                nodeViewModel = GlobalContentRootNode;
            }
            else
            {
                throw new InvalidOperationException();
            }

            return GetTreeNode(separated, nodeViewModel);
        }


        private NodeViewModel? GetTreeNode(List<string> separated, NodeViewModel nodeViewModel)
        {
            if (separated.Count == 0)
            {
                return nodeViewModel;
            }
            else
            {
                var child = nodeViewModel.Children.FirstOrDefault(item => item.Text == separated[0]);
                separated.RemoveAt(0);
                if (child != null)
                {
                    return GetTreeNode(separated, child);
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsInTreeView(NodeViewModel node)
        {
            var rootParent = node.Root();

            return
                rootParent == GlobalContentRootNode ||
                rootParent == EntityRootNode ||
                rootParent == ScreenRootNode;

        }

        #endregion

        private void PushSearchToContainedObject()
        {
            var searchToLower = SearchText?.ToLowerInvariant();

            if (searchToLower != null)
            {
                RefreshFlattenedList();
            }
            else
            {
                if (!VisibleRoot.Contains(EntityRootNode))
                {
                    VisibleRoot.Insert(0, EntityRootNode);
                }

                if (!VisibleRoot.Contains(ScreenRootNode))
                {
                    VisibleRoot.Insert(1, ScreenRootNode);
                }

                if (!VisibleRoot.Contains(GlobalContentRootNode))
                {
                    VisibleRoot.Add(GlobalContentRootNode);
                }
            }



        }
    }
}
