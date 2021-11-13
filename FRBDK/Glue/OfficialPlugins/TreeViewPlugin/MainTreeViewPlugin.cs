using FlatRedBall.Glue.FormHelpers;
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

        PluginTab pluginTab;

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

            pluginTab = CreateTab(mainView, "Explorer (beta)", TabLocation.Left);
            pluginTab.CanClose = false;
            AssignEvents();

        }

        private void AssignEvents()
        {
            ReactToLoadedGluxEarly += HandleGluxLoaded;
            ReactToUnloadedGlux += HandleUnloadedGlux;
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
            RefreshGlobalContentTreeNode += HandleRefreshGlobalContentTreeNode;

            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
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
            var currentNode = SelectionLogic.CurrentNode;
            MainViewModel.RefreshTreeNodeFor(element);
            if(currentNode?.Tag != null)
            {
                SelectionLogic.SelectByTag(currentNode.Tag);
            }
        }


    }
}
