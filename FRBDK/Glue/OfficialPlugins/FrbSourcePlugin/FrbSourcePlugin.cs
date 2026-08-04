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
        private AddFrbSourceViewModel ViewModel;

        private ToolStripMenuItem _linkToSourceMenuItem;
        private readonly GlueState _glueState;
        private readonly AddSourceManager _addSourceManager;

        public override string FriendlyName => "FRB Source";

        #endregion

        public FrbSourcePlugin()
        {
            _glueState = GlueState.Self;
            _addSourceManager = new AddSourceManager();
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            _linkToSourceMenuItem.Owner.Items.Remove(_linkToSourceMenuItem);

            this.ReactToLoadedGlux -= HandleGluxLoaded;
            this.ReactToUnloadedGlux -= HandleGluxUnloaded;

            return true;
        }

        public override void StartUp()
        {
            _linkToSourceMenuItem = this.AddMenuItemTo(
                "Link Game to FRB Source", 
                (Action)null, 
                "Project");

            _linkToSourceMenuItem.Enabled = false;

            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
        }

        private void HandleGluxUnloaded()
        {
            _linkToSourceMenuItem.Enabled = false;
            RefreshLinkToSourceItems();
        }

        private void HandleGluxLoaded()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if (mainProject is MonoGameDesktopGlBaseProject
                or FnaDesktopProject
                or AndroidMonoGameNet8Project
                or IosMonoGameNet8Project
                or KniWebProject)
            {
                _linkToSourceMenuItem.Enabled = true;
            }

            RefreshLinkToSourceItems();
        }

        private void RefreshLinkToSourceItems()
        {
            var project = _glueState.CurrentMainProject;
            _linkToSourceMenuItem.DropDownItems.Clear();

            if(project != null)
            {
                void AddItem(VisualStudioProject project)
                {
                    _linkToSourceMenuItem.DropDownItems.Add(
                        project.Name,
                        null,
                        (_, _) => ShowGameToGlueSourceTab(project));

                }
                AddItem(_glueState.CurrentMainProject);

                foreach (var item in _glueState.SyncedProjects)
                {
                    if(item is VisualStudioProject visualStudioProject)
                    {
                        AddItem(visualStudioProject);
                    }
                }
            }
        }

        private void ShowGameToGlueSourceTab(VisualStudioProject project)
        {
            CreateTabIfNecessary();



            // Github for desktop has a standard folder for source files, so let's default to that if it exists

            if (System.IO.Directory.Exists(_addSourceManager.DefaultFrbFilePath))
            {
                ViewModel.FrbRootFolder = _addSourceManager.DefaultFrbFilePath;
            }
            if (System.IO.Directory.Exists(_addSourceManager.DefaultGumFilePath))
            {
                ViewModel.GumRootFolder = _addSourceManager.DefaultGumFilePath;
            }

            var alreadyLinked = project.IsFrbSourceLinked();
            ViewModel.VisualStudioProject = project;
            ViewModel.AlreadyLinkedMessageVisibility = alreadyLinked.ToVisibility();

            Tab.Show();
            Tab.Focus();

        }

        private void CreateTabIfNecessary()
        {
            if (Tab != null) 
                return;

            ViewModel = new AddFrbSourceViewModel();
            
            control = new AddFrbSourceView();
            control.DataContext = ViewModel;
            control.LinkToSourceClicked += async () =>
            {
                GlueCommands.Self.DialogCommands.ShowToast("Adding Source...", TimeSpan.FromSeconds(999));
                await _addSourceManager.HandleLinkToSourceClicked(ViewModel);
                Tab.Hide();
                GlueCommands.Self.DialogCommands.HideToast();

            };
            Tab = CreateTab(control, "Add FRB Source");
        }

        public bool HasFrbAndGumReposInDefaultLocation() =>
            System.IO.Directory.Exists(_addSourceManager.DefaultFrbFilePath) &&
            System.IO.Directory.Exists(_addSourceManager.DefaultGumFilePath);

        public async Task AddFrbSourceToDefaultLocation(VisualStudioProject visualStudioProject)
        {
            await _addSourceManager.LinkToSourceUsingDefaults(visualStudioProject);
        }
    }
}
