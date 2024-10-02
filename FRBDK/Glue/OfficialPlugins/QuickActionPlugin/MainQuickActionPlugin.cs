using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPluginsCore.QuickActionPlugin.Managers;
using OfficialPluginsCore.QuickActionPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;

namespace OfficialPluginsCore.QuickActionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainQuickActionPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Quick Action Plugin";

        public override Version Version => new Version(1, 0);

        PluginTab pluginTab;

        MainView mainView;

        ButtonVisibilityManager buttonVisibilityManager;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            CreateUi();

            buttonVisibilityManager = new ButtonVisibilityManager(mainView);

            AssignEvents();

            buttonVisibilityManager.UpdateVisibility();

            ReactToLoadedGlux += () => buttonVisibilityManager.UpdateVisibility();
            ReactToUnloadedGlux += () => buttonVisibilityManager.UpdateVisibility(forceUnloaded:true);
        }

        private void CreateUi()
        {
            mainView = new MainView();
            mainView.AnyButtonClicked += () => buttonVisibilityManager.UpdateVisibility();

            pluginTab = this.CreateTab(mainView, Localization.Texts.QuickActions);
            pluginTab.CanClose = false;
            pluginTab.Show();
        }

        private void AssignEvents()
        {
            this.ReactToItemsSelected += _ =>
                buttonVisibilityManager.UpdateVisibility();

            this.ReactToLoadedGlux += () =>
                buttonVisibilityManager.UpdateVisibility();
        }

    }
}
