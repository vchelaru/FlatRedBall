using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
            var findManager = new FindManager(MainViewModel);
            GlueState.Self.Find = findManager;
            mainView = new MainTreeViewControl();

            mainView.DataContext = MainViewModel;

            SelectionLogic.Initialize(MainViewModel, mainView);

            pluginTab = CreateTab(mainView, "Explorer", TabLocation.Left);
            pluginTab.CanClose = false;
            AssignEvents();

        }

        private void AssignEvents()
        {
            ReactToLoadedGluxEarly += HandleGluxLoaded;
            ReactToUnloadedGlux += HandleUnloadedGlux;
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
            RefreshGlobalContentTreeNode += HandleRefreshGlobalContentTreeNode;
            RefreshDirectoryTreeNodes += HandleRefreshDirectoryTreeNodes;
            FocusOnTreeView += HandleFocusOnTreeView;
            ReactToCtrlF += HandleCtrlF;
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private async void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            var tag = selectedTreeNode?.Tag;
            if(SelectionLogic.IsUpdatingThisSelectionOnGlueEvent )
            {
                var wasPushingSelection = SelectionLogic.IsPushingSelectionOutToGlue;
                SelectionLogic.IsPushingSelectionOutToGlue = false;
                if (tag != null)
                {
                    SelectionLogic.SelectByTag(tag);
                }
                else if(selectedTreeNode is NodeViewModel vm)
                {
                    await SelectionLogic.SelectByTreeNode(vm);
                }
                else if(selectedTreeNode != null)
                {
                    SelectionLogic.SelectByPath(selectedTreeNode.GetRelativePath());
                }
                else
                {
                    await SelectionLogic.SelectByTreeNode(null);
                }
                SelectionLogic.IsPushingSelectionOutToGlue = wasPushingSelection;

            }

            MainViewModel.IsForwardButtonEnabled = TreeNodeStackManager.Self.CanGoForward;
            MainViewModel.IsBackButtonEnabled = TreeNodeStackManager.Self.CanGoBack;
        }

        private void HandleRefreshGlobalContentTreeNode()
        {
            MainViewModel.RefreshGlobalContentTreeNodes();
        }

        private void HandleGluxLoaded()
        {
            pluginTab.Show();
            MainViewModel.AddDirectoryNodes();
            MainViewModel.RefreshGlobalContentTreeNodes();
        }

        private void HandleUnloadedGlux()
        {
            pluginTab.Hide();
            MainViewModel.Clear();
        }

        private void HandleRefreshTreeNodeFor(GlueElement element)
        {
            var oldTag = SelectionLogic.CurrentNode?.Tag;
            var oldNode = SelectionLogic.CurrentNode;
            var currentNode = SelectionLogic.CurrentNode;
            MainViewModel.RefreshTreeNodeFor(element);

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
                SelectionLogic.SelectByTag(currentNode.Tag);
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
            MainViewModel.RefreshDirectoryNodes();
            
        }

        private void HandleFocusOnTreeView()
        {
            this.mainView?.Focus();
        }

        private void HandleCtrlF()
        {
            mainView.FocusSearchBox();
        }
    }
}
