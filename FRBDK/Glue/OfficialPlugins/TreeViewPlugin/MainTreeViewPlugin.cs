using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.TreeViewPlugin
{
    [Export(typeof(PluginBase))]
    class MainTreeViewPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Tree View Plugin";

        public override Version Version => new Version(1, 0);

        MainTreeViewControl mainView;

        MainTreeViewViewModel MainViewModel = new MainTreeViewViewModel();

        PluginTab pluginTab;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var pixelHeight = GlueState.Self.GlueSettingsSave.BookmarkRowHeight > 0
                ? GlueState.Self.GlueSettingsSave.BookmarkRowHeight
                : 100;
            MainViewModel.OldBookmarkRowHeight = new System.Windows.GridLength(
                pixelHeight, System.Windows.GridUnitType.Pixel);

            MainViewModel.IsBookmarkListVisible = GlueState.Self.GlueSettingsSave.IsBookmarksListVisible;

            MainViewModel.PropertyChanged += HandleMainViewModelPropertyChanged;

            var findManager = new FindManager(MainViewModel);
            GlueState.Self.Find = findManager;
            mainView = new MainTreeViewControl();
            MainViewModel.HasUserDismissedTips = GlueState.Self.GlueSettingsSave.Properties
                .GetValue<bool>(nameof(MainViewModel.HasUserDismissedTips));
            mainView.DataContext = MainViewModel;


            SelectionLogic.Initialize(MainViewModel, mainView);

            pluginTab = CreateTab(mainView, "Explorer", TabLocation.Left);
            pluginTab.CanClose = false;
            AssignEvents();

        }

        private void HandleMainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(MainViewModel.IsBookmarkListVisible):
                case nameof(MainViewModel.BookmarkRowHeight):
                    if(GlueState.Self.GlueSettingsSave != null)
                    {
                        GlueState.Self.GlueSettingsSave.IsBookmarksListVisible = MainViewModel.IsBookmarkListVisible;
                        if(MainViewModel.IsBookmarkListVisible)
                        {
                            GlueState.Self.GlueSettingsSave.BookmarkRowHeight = MainViewModel.BookmarkRowHeight.Value;
                        }
                        else
                        {
                            GlueState.Self.GlueSettingsSave.BookmarkRowHeight = MainViewModel.OldBookmarkRowHeight.Value;
                        }
                        GlueCommands.Self.GluxCommands.SaveSettings();

                    }
                    break;
            }
        }

        private void AssignEvents()
        {
            ReactToLoadedGluxEarly += HandleGluxLoadedEarly;
            ReactToLoadedGlux += HandleGluxLoadLate;
            ReactToUnloadedGlux += HandleUnloadedGlux;
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
            RefreshGlobalContentTreeNode += HandleRefreshGlobalContentTreeNode;
            RefreshDirectoryTreeNodes += HandleRefreshDirectoryTreeNodes;
            FocusOnTreeView += HandleFocusOnTreeView;
            ReactToCtrlF += HandleCtrlF;
            ReactToItemsSelected += HandleItemsSelected;
            TryHandleTreeNodeDoubleClicked += TryHandleTreeNodeDoubleClick;
        }

        private bool TryHandleTreeNodeDoubleClick(ITreeNode arg)
        {
            var node = arg as NodeViewModel;

            if(node?.Children.Count > 0)
            {
                node.IsExpanded = !node.IsExpanded;
                return true;
            }
            return false;
        }

        private void HandleGluxLoadLate()
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                var project = GlueState.Self.CurrentGlueProject;
                var entities = project.Entities.ToArray();
                var screens = project.Screens.ToArray();

                foreach (var entity in entities)
                {
                    HandleRefreshTreeNodeFor(entity, TreeNodeRefreshType.All);
                }

                foreach (var screen in screens)
                {
                    HandleRefreshTreeNodeFor(screen, TreeNodeRefreshType.All);
                }

                HandleRefreshGlobalContentTreeNode();

                var settings = TreeViewPluginSettingsManager.LoadSettings();
                if(settings != null)
                {
                    TreeViewPluginSettingsManager.ApplySettingsToViewModel(settings, MainViewModel);
                }
            });
        }

        private async void HandleItemsSelected(List<ITreeNode> selectedTreeNodes)
        {
            if(SelectionLogic.IsUpdatingThisSelectionOnGlueEvent )
            {
                var wasPushingSelection = SelectionLogic.IsPushingSelectionOutToGlue;
                var wasSuppressingFocus = SelectionLogic.SuppressFocus;
                SelectionLogic.SuppressFocus = true;
                SelectionLogic.IsPushingSelectionOutToGlue = false;

                MainViewModel.DeselectResursively();

                for (int i = 0; i < selectedTreeNodes.Count; i++)
                {
                    ITreeNode selectedTreeNode = selectedTreeNodes[i];
                    var tag = selectedTreeNode.Tag;
                    var addToSelection = i > 0;

                    if (tag != null)
                    {
                        SelectionLogic.SelectByTag(tag, addToSelection);
                    }
                    else if(selectedTreeNode is NodeViewModel vm)
                    {
                        await SelectionLogic.SelectByTreeNode(vm, addToSelection);
                    }
                    else if(selectedTreeNode != null)
                    {
                        SelectionLogic.SelectByPath(selectedTreeNode.GetRelativeFilePath(), addToSelection);
                    }
                    else
                    {
                        await SelectionLogic.SelectByTreeNode(null, false);
                    }
                }

                SelectionLogic.IsPushingSelectionOutToGlue = wasPushingSelection;
                SelectionLogic.SuppressFocus = wasSuppressingFocus;

            }

            MainViewModel.IsForwardButtonEnabled = TreeNodeStackManager.Self.CanGoForward;
            MainViewModel.IsBackButtonEnabled = TreeNodeStackManager.Self.CanGoBack;
        }

        private async void HandleRefreshGlobalContentTreeNode()
        {
            ITreeNode parentTreeNode = SelectionLogic.CurrentNode?.Parent;
            var parentNodeViewModel = parentTreeNode as NodeViewModel;
            var oldRfs = SelectionLogic.CurrentNode?.Tag as ReferencedFileSave;
            var isGlobalContentRfs =
                SelectionLogic.CurrentNode != null &&
                oldRfs != null &&
                parentTreeNode != null &&
                (parentTreeNode.IsGlobalContentContainerNode() || parentTreeNode.IsChildOfGlobalContent());

            int? indexInParent = null;

            var glueProject = GlueState.Self.CurrentGlueProject;

            if (parentTreeNode != null)
            {
                indexInParent = SelectionLogic.CurrentNode?.Parent.Children.IndexOf(SelectionLogic.CurrentNode);
            }

            var oldSelection = SelectionLogic.CurrentNode;

            MainViewModel.RefreshGlobalContentTreeNodes();


            if (oldRfs != null && !MainViewModel.IsInTreeView(oldSelection) && indexInParent > -1 &&
                glueProject.GlobalFiles.Count > 0)
            {
                var index = indexInParent.Value;
                if(index >= parentNodeViewModel.Children.Count)
                {
                    index = parentNodeViewModel.Children.Count - 1;
                }

                if(index > -1 && MainViewModel.IsInTreeView(parentNodeViewModel))
                {
                    var wasPushingSelection = SelectionLogic.IsPushingSelectionOutToGlue;
                    // If the tag changed, push it back out:
                    SelectionLogic.IsPushingSelectionOutToGlue = true;
                    var newSelection = parentNodeViewModel.Children[index];
                    await SelectionLogic.SelectByTreeNode(newSelection, false);
                    SelectionLogic.IsPushingSelectionOutToGlue = wasPushingSelection;
                }
            }
        }

        private void HandleGluxLoadedEarly()
        {
            pluginTab.Show();
            MainViewModel.AddDirectoryNodes();
            MainViewModel.RefreshGlobalContentTreeNodes();
            MainViewModel.RefreshBookmarks();

        }

        private void HandleUnloadedGlux()
        {
            FillAndSaveTreeViewPluginSettings();


            pluginTab.Hide();
            MainViewModel.Clear();
        }

        private void FillAndSaveTreeViewPluginSettings()
        {
            var settings = TreeViewPluginSettingsManager.CreateSettingsFrom(MainViewModel);

            TreeViewPluginSettingsManager.SaveSettings(settings);
        }

        private void HandleRefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType)
        {
            var oldTag = SelectionLogic.CurrentNode?.Tag;
            var oldNode = SelectionLogic.CurrentNode;
            var currentNode = SelectionLogic.CurrentNode;
            MainViewModel.RefreshTreeNodeFor(element, treeNodeRefreshType);

            if(treeNodeRefreshType == TreeNodeRefreshType.All)
            {
                if(element is ScreenSave)
                {
                    MainViewModel.ScreenRootNode.SortByTextConsideringDirectories(recursive:true);
                }
                else // entity save
                {

                    MainViewModel.EntityRootNode.SortByTextConsideringDirectories(recursive:true);
                }
            }
            if(currentNode?.Tag != null)
            {
                // November 20, 2021
                // When a tree node is
                // refreshed, we want to
                // re-select the same tree
                // node as before. Since the
                // app-level selection should
                // not have changed, we don't want
                // to push this change out to Glue.
                // Doing so causes unexpected side effects
                // such as the state data tab refreshing its
                // current category and recreating its viewmodel
                // in the middle of a view model change.

                var wasPushingSelection = SelectionLogic.IsPushingSelectionOutToGlue;
                // If the tag changed, push it back out:
                SelectionLogic.IsPushingSelectionOutToGlue = oldTag != currentNode?.Tag;
                // todo - need to add currentTreeNodes (plural)
                SelectionLogic.SelectByTag(currentNode.Tag, false);
                SelectionLogic.IsPushingSelectionOutToGlue = wasPushingSelection;

                // This can happen if the last item in a category (like a variable) is removed. If so, push
                // the change out:
                if (SelectionLogic.CurrentNode == null && GlueState.Self.CurrentTreeNode != null)
                {
                    GlueState.Self.CurrentTreeNode = null;
                }
            }
            
        }

        private void HandleRefreshDirectoryTreeNodes()
        {
            var oldTag = SelectionLogic.CurrentNode?.Tag;

            MainViewModel.RefreshDirectoryNodes();
            
            if(oldTag != null && SelectionLogic.CurrentNode?.Tag != oldTag)
            {
                var wasPushingSelection = SelectionLogic.IsPushingSelectionOutToGlue;
                // If the tag changed, push it back out:
                SelectionLogic.IsPushingSelectionOutToGlue = false;
                SelectionLogic.SelectByTag(oldTag, false);
                SelectionLogic.IsPushingSelectionOutToGlue = wasPushingSelection;
            }
        }

        private void HandleFocusOnTreeView()
        {
            this.mainView?.Focus();
        }

        private void HandleCtrlF()
        {
            mainView.FocusSearchBox();
        }

        #region Bookmarks

        public void AddBookmark(ITreeNode treeNode)
        {
            mainView.AddBookmark(treeNode);
            MainViewModel.IsBookmarkListVisible = true;
        }

        #endregion
    }
}
