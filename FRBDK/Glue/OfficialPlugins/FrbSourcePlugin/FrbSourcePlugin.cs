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
    }

    #endregion

    [Export(typeof(PluginBase))]
    public class FrbSourcePlugin : PluginBase
    {
        private PluginTab Tab;
        private AddFrbSourceView control;

        private ToolStripMenuItem miLinkSource;

        public override string FriendlyName => "FRB Source";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            miLinkSource.Owner.Items.Remove(miLinkSource);

            this.ReactToLoadedGlux -= HandleGluxLoaded;
            this.ReactToUnloadedGlux -= HandleGluxUnloaded;

            return true;
        }

        public override void StartUp()
        {
            miLinkSource = this.AddMenuItemTo("Link Game to FRB Source", (_, _) => ShowGameToGlueSourceTab(), "Project");

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
            if (GlueState.Self.CurrentMainProject is DesktopGlProject)
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
            if(Tab == null)
            {
                control = new AddFrbSourceView();
                control.LinkToSourceClicked += () =>
                {
                    AddSourceManager.HandleLinkToSourceClicked(control.ViewModel);
                    Tab.Hide();
                };
                    Tab = CreateTab(control, "Add FRB Source");
            }
        }

    }
}
