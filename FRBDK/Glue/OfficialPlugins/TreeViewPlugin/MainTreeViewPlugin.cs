using FlatRedBall.Glue.Plugins;
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

        MainTreeViewViewModel MainViewModel = new MainTreeViewViewModel();

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var mainView = new MainTreeViewControl();

            mainView.DataContext = MainViewModel;

            SelectionLogic.Initialize(MainViewModel, mainView);

            AssignEvents();

            CreateAndAddTab(mainView, "Explorer (beta)", TabLocation.Left);
        }

        private void AssignEvents()
        {
            ReactToLoadedGluxEarly += HandleGluxLoaded;
            ReactToUnloadedGlux += HandleUnloadedGlux;
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
            RefreshGlobalContentTreeNode += HandleRefreshGlobalContentTreeNode;

            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            var tag = selectedTreeNode?.Tag;
            if(SelectionLogic.IsUpdatingSelectionOnGlueEvent )
            {

                SelectionLogic.SelectByTag(tag);
            }
        }

        private void HandleRefreshGlobalContentTreeNode()
        {
            MainViewModel.RefreshGlobalContentTreeNodes();
        }

        private void HandleGluxLoaded()
        {
            MainViewModel.AddDirectoryNodes();
            MainViewModel.RefreshGlobalContentTreeNodes();
        }

        private void HandleUnloadedGlux()
        {
            MainViewModel.Clear();
        }

        private void HandleRefreshTreeNodeFor(GlueElement element)
        {
            var currentNode = SelectionLogic.CurrentNode;
            MainViewModel.RefreshTreeNodeFor(element);
            if(currentNode?.Tag != null)
            {
                SelectionLogic.SelectByTag(currentNode.Tag);
            }
        }


    }
}
