using System;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.IO;
using System.Collections.Generic;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using System.Linq;
using OfficialPlugins.FrbSourcePlugin.Views;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using FlatRedBall.Glue.MVVM;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.FrbSourcePlugin.Managers;
using System.Threading.Tasks;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    #region FrbOrGum enum

    public enum FrbOrGum
    {
        Frb,
        Gum
    }

    #endregion

    #region ProjectReference Class

    public class ProjectReference
    {
        public FrbOrGum ProjectRootType;
        public string RelativeProjectFilePath;
        public Guid ProjectTypeId;
        public Guid ProjectId;
        public string ProjectName;
        public List<VSSolution.SharedProject> SharedProjects;
        public List<string> ProjectConfigurations;
        public List<string> SolutionConfigurations;

        public override string ToString()
        {
            return ProjectName;
        }
    }

    #endregion

    [Export(typeof(PluginBase))]
    public class FrbSourcePlugin : PluginBase
    {
        #region Fields/Properties

        private PluginTab Tab;
        private AddFrbSourceView control;

        private ToolStripMenuItem miLinkSource;

        public override string FriendlyName => "FRB Source";

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            miLinkSource.Owner.Items.Remove(miLinkSource);

            this.ReactToLoadedGlux -= HandleGluxLoaded;
            this.ReactToUnloadedGlux -= HandleGluxUnloaded;

            return true;
        }

        public override void StartUp()
        {
            miLinkSource = this.AddMenuItemTo(
                Localization.Texts.LinkGameToFrbSource, 
                Localization.MenuIds.LinkGameToFrbSourceId, 
                ShowGameToGlueSourceTab, 
                Localization.MenuIds.ProjectId);

            miLinkSource.Enabled = false;

            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
        }

        private void HandleGluxUnloaded()
        {
            miLinkSource.Enabled = false;
        }

        private void HandleGluxLoaded()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if (mainProject is DesktopGlProject or FnaDesktopProject or AndroidProject or IosMonogameProject or Xna4Project)
            {
                miLinkSource.Enabled = true;
            }
        }

        private void ShowGameToGlueSourceTab()
        {
            CreateTabIfNecessary();

            Tab.Show();
            Tab.Focus();
        }

        private void CreateTabIfNecessary()
        {
            if (Tab != null) 
                return;
            
            control = new AddFrbSourceView();
            control.LinkToSourceClicked += async () =>
            {
                await AddSourceManager.HandleLinkToSourceClicked(control.ViewModel);
                Tab.Hide();
            };
            Tab = CreateTab(control, Localization.Texts.AddFrbSource);
        }

        public bool HasFrbAndGumReposInDefaultLocation() => 
            System.IO.Directory.Exists(AddSourceManager.DefaultFrbFilePath) &&
            System.IO.Directory.Exists(AddSourceManager.DefaultGumFilePath);

        public async Task AddFrbSourceToDefaultLocation()
        {
            await AddSourceManager.LinkToSourceUsingDefaults();
        }
    }
}
