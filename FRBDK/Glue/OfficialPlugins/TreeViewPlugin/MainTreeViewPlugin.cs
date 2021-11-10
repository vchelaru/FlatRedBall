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
        public override string FriendlyName => "Tree View Plugin";

        public override Version Version => new Version(1, 0);

        MainTreeViewViewModel MainViewModel = new MainTreeViewViewModel();

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
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
            RefreshGlobalContentTreeNode += HandleRefreshGlobalContentTreeNode;

            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            var tag = selectedTreeNode.Tag;
            if(tag != null)
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

        private void HandleRefreshTreeNodeFor(GlueElement element)
        {
            MainViewModel.RefreshTreeNodeFor(element);
            
        }


    }
}
