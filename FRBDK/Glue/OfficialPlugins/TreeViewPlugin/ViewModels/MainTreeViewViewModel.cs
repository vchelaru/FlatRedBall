using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    #region BookmarkViewModel

    class BookmarkViewModel
    {
        public string Text { get; set; }
        public ImageSource ImageSource { get; set; }

        public override string ToString() => Text;
    }

    #endregion

    class MainTreeViewViewModel : ViewModel, ISearchBarViewModel
    {
        #region Search-related

        public static string SearchText;
        public static string PrefixText;

        public string SearchBoxText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    PrefixText = String.Empty;
                    if (string.IsNullOrEmpty(value))
                    {
                        SearchText = String.Empty;
                    }
                    else
                    {
                        if (
                            value.StartsWith("f ") ||
                            value.StartsWith("e ") ||
                            value.StartsWith("s ") ||
                            value.StartsWith("o ") ||
                            value.StartsWith("v ")
                            )
                        {
                            SearchText = value.Substring(2);
                            PrefixText = value.Substring(0, 1).ToLowerInvariant();

                        }
                        else
                        {
                            SearchText = value;
                        }

                    }
                    PushSearchToContainedObject();
                }
            }
        }

        public bool IsSearchBoxFocused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchButtonVisibility => (!string.IsNullOrEmpty(SearchBoxText)).ToVisibility();

        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchListVisibility => (!string.IsNullOrEmpty(SearchBoxText)).ToVisibility();

        [DependsOn(nameof(IsSearchBoxFocused))]
        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchPlaceholderVisibility =>
            (IsSearchBoxFocused == false && string.IsNullOrWhiteSpace(SearchBoxText)).ToVisibility();

        #endregion

        #region Fields/Properties

        public NodeViewModel ScreenRootNode { get; private set; }
        public NodeViewModel EntityRootNode { get; private set; }
        public NodeViewModel GlobalContentRootNode { get; private set; }

        public NodeViewModel RootModel { get; set; }

        public ObservableCollection<NodeViewModel> FlattenedItems { get; private set; } = new ObservableCollection<NodeViewModel>();

        public NodeViewModel FlattenedSelectedItem
        {
            get => Get<NodeViewModel>();
            set => Set(value);
        }


        public IEnumerable Root
        {
            get;
            private set;
        }

        public ObservableCollection<NodeViewModel> VisibleRoot { get; private set; } = new ObservableCollection<NodeViewModel>();

        public IEnumerable Children
        {
            get
            {
                return RootModel.Children;
            }
        }

        public string Title { get; set; }

        public int Count { get; set; }

        [DependsOn(nameof(SearchBoxText))]
        public Visibility MainTreeViewVisibility => (string.IsNullOrEmpty(SearchBoxText)).ToVisibility();
        public bool HasUserDismissedTips
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasUserDismissedTips))]
        public Visibility TipsVisibility
        {
            get
            {
                if (HasUserDismissedTips)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;

                }
            }
        }



        [DependsOn(nameof(SearchBoxText))]
        public string FilterResultsInfo =>
            SearchBoxText?.StartsWith("f ") == true ? Localization.Texts.FilteredToFiles :
            SearchBoxText?.StartsWith("e ") == true ? Localization.Texts.FilteredToEntities :
            SearchBoxText?.StartsWith("s ") == true ? Localization.Texts.FilteredToScreens :
            SearchBoxText?.StartsWith("o ") == true ? Localization.Texts.FilteredToObjects :
            SearchBoxText?.StartsWith("v ") == true ? Localization.Texts.FilteredToVariables :
            Localization.Texts.FilterResultsDescription;

        public bool IsForwardButtonEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBackButtonEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string SelectedItemInfoDisplay
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        #region Bookmark

        public bool IsBookmarkListVisible
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    if(value== false)
                    {
                        OldBookmarkRowHeight = BookmarkRowHeight;
                        BookmarkRowHeight = new GridLength(0, GridUnitType.Pixel);
                    }
                    else
                    {
                        BookmarkRowHeight = OldBookmarkRowHeight;
                    }
                }
            }
        }

        [DependsOn(nameof(IsBookmarkListVisible))]
        public Visibility BookmarkListVisibility => IsBookmarkListVisible.ToVisibility();

        public ObservableCollection<BookmarkViewModel> Bookmarks { get; private set; } = new ObservableCollection<BookmarkViewModel>();

        public BookmarkViewModel SelectedBookmark
        {
            get => Get<BookmarkViewModel>();
            set => Set(value);
        }

        public GridLength OldBookmarkRowHeight { get; set; }

        public GridLength BookmarkRowHeight
        {
            get=> Get<GridLength>();
            set => Set(value);
        }

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

            BookmarkRowHeight = GridLength.Auto;

            PushSearchToContainedObject();

            //this.AddRecursive(ScreenRootNode, 4, 4);
            //this.Title = "TreeListBox (N=" + this.Count + ")";

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
                        elementTreeNode = new GlueElementNodeViewModel(ScreenRootNode, element, true);
                        ScreenRootNode.Children.Add(elementTreeNode);
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

                            elementTreeNode.Parent.Remove(elementTreeNode);

                            newParentTreeNode.Add(elementTreeNode);
                            elementTreeNode.Parent = newParentTreeNode;
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
                    nodeForFile = new NodeViewModel(nodeToAddTo);

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
            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode);


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
            AddDirectoryNodes(GlueState.Self.CurrentGlueProjectDirectory + "Entities/", EntityRootNode);

            string contentDirectory = GlueState.Self.ContentDirectory;

            AddDirectoryNodes(contentDirectory + "GlobalContent/", GlobalContentRootNode);
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
                this.Bookmarks.Add(vm);
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

                            treeNode.IsEditable = true;

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
            //NodeViewModel elementTreeNode = new GlueElementNodeViewModel(EntityRootNode, entitySave);
            //EntityRootNode.Children.Add(elementTreeNode);
            //return elementTreeNode;

            string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name));

            NodeViewModel treeNodeToAddTo;
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
                    AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", EntityRootNode);

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

        public void Clear()
        {
            ScreenRootNode.Children.Clear();
            EntityRootNode.Children.Clear();
            GlobalContentRootNode.Children.Clear();
        }

        internal void DeselectResursively()
        {
            ScreenRootNode.DeselectResursively();
            EntityRootNode.DeselectResursively();
            GlobalContentRootNode.DeselectResursively();
        }

        #region Collapse

        internal void CollapseAll()
        {
            foreach (var node in VisibleRoot)
            {
                node.CollapseRecursively();
            }

        }

        internal void CollapseToDefinitions()
        {
            foreach (var node in VisibleRoot)
            {
                node.CollapseToDefinitions();
            }

            // make sure the top level tree nodes are expanded
            ScreenRootNode.IsExpanded = true;
            EntityRootNode.IsExpanded = true;
            GlobalContentRootNode.IsExpanded = true;
        }

        #endregion

        #region Search for Nodes

        List<NodeViewModel> tempListForSortingFilteredResults = new List<NodeViewModel>();
        private void RefreshFlattenedList()
        {
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
                    var matchWeight = GetMatchWeight(name);
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
                    var matchWeight = GetMatchWeight(name);
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
                    var matchWeight = GetMatchWeight(file.Name);
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
                var matchWeight = GetMatchWeight(nos.InstanceName, nos.DefinedByBase);
                if (matchWeight > 0)
                {
                    var node = new NodeViewModel();

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
                var matchWeight = GetMatchWeight(category.Name);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel();
                    treeNode.ImageSource = NodeViewModel.FolderClosedIcon;
                    treeNode.Text = category.ToString();
                    treeNode.Tag = category;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }
            }
            foreach (var state in states)
            {
                var matchWeight = GetMatchWeight(state.Name);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(null);
                    treeNode.ImageSource = NodeViewModel.StateIcon;
                    treeNode.Text = state.ToString();
                    treeNode.Tag = state;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }

            }

            foreach (var variable in variables)
            {
                var matchWeight = GetMatchWeight(variable.Name, variable.DefinedByBase);
                if (matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(null);
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
                var matchWeight = GetMatchWeight(eventItem.EventName);
                if(matchWeight > 0)
                {
                    var treeNode = new NodeViewModel(null);
                    treeNode.ImageSource = NodeViewModel.EventIcon;
                    treeNode.Text = eventItem.ToString();
                    treeNode.Tag = eventItem;
                    treeNode.SearchTermMatchWeight = matchWeight;
                    tempListForSortingFilteredResults.Add(treeNode);
                }
            }

            NodeViewModel NodeFor(ReferencedFileSave rfs)
            {
                var nodeForFile = new NodeViewModel(null);
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
                var matchWeight = GetMatchWeight(possibleFolderNode.Text);
                if(matchWeight > 0 && possibleFolderNode.Text.EndsWith(".cs") == false && possibleFolderNode.Tag == null &&
                    (((ITreeNode)possibleFolderNode).IsFolderForGlobalContentFiles() || ((ITreeNode)possibleFolderNode).IsFolderInFilesContainerNode()))
                {
                    var node = new NodeViewModel(null);
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

            double GetMatchWeight(string itemName, bool isDefinedByBase = false)
            {
                var itemNameToLower = itemName.ToLowerInvariant();

                var weight = 0.0;

                if (itemName == searchTermCaseSensitive)
                {
                    // Search: Sprite 
                    // Actual: Sprite
                    weight = 1;
                }
                else if (itemName.StartsWith(searchTermCaseSensitive))
                {
                    // Search: Spri
                    // Actual: Sprite
                    weight = 0.8;
                }
                else if (itemNameToLower == searchToLower)
                {
                    // Search: sprite
                    // Actual: Sprite
                    weight = 0.7;
                }
                else if (CamelCaseMatchUpper(itemName, searchTermCaseSensitive))
                {
                    // Search MGE
                    // Actual: MachineGunEnemy
                    weight = 0.65;
                }
                else if (itemNameToLower.StartsWith(searchToLower))
                {
                    // Search: spri
                    // Actual: Sprite
                    weight = 0.6;
                }
                else if (itemName.Contains(searchTermCaseSensitive))
                {
                    // Search: rit
                    // Actual: Sprite
                    weight = 0.5;
                }
                else if (itemNameToLower.Contains(searchToLower))
                {
                    // Search: magetod
                    // Actual: DamageToDeal
                    weight = 0.4;
                }

                // if defined by base, then we make this weigh slightly less so that it shows up after the original definitions
                if(isDefinedByBase)
                {
                    weight -= .001f;
                }

                return weight;
            }
            bool CamelCaseMatchUpper(string itemName, string searchTermCaseSensitive)
            {
                string upperCaseLetters = string.Empty;
                for (int i = 0; i < itemName.Length; i++)
                {
                    if (char.IsUpper(itemName[i]))
                    {
                        upperCaseLetters += itemName[i];
                    }
                }

                return upperCaseLetters.StartsWith(searchTermCaseSensitive);
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

        public NodeViewModel GetTreeNodeForGlobalContent(ReferencedFileSave rfs, NodeViewModel nodeToStartAt)
        {
            NodeViewModel containerTreeNode = nodeToStartAt;

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

        public NodeViewModel TreeNodeForDirectory(FilePath containingDirectory)
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
