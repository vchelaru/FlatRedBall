using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using System.Diagnostics;
using OfficialPlugins.ContentPipelinePlugin;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using EditorObjects.IoC;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.FormHelpers;
using FlatRedBall.Glue.IO;

namespace OfficialPlugins.MonoGameContent
{
    [Export(typeof (PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        ContentPipelineControl control;
        ControlViewModel viewModel;
        AliasCodeGenerator aliasCodeGenerator;

        ContentPipelineController controller;

        IGlueState GlueState => Container.Get<IGlueState>();
        PluginTab tab;

        public override string FriendlyName
        {
            get
            {
                return "MonoGame Content Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                // 1.1.0:
                //  - Fixed bugs bug where adding a new file to an Android primary project would not refresh the csproj
                // 1.2.0:
                //  - Fixed generation of parameters for wavs
                //  - Output is now shown in the output window, including as errors if it's an error.
                // 1.3.0
                //  - If a file changes, the plugin will attempt to rebuild it - even if it's not a RFS
                // 1.4.0
                //  - Added right-click option to rebuild a file using the content pipeline
                return new Version(1, 4, 0);
            }
        }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        #region Constructor/Initialize

        public MainPlugin()
        {
            viewModel = new ContentPipelinePlugin.ControlViewModel();
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        private void HandleViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;

            switch(propertyName)
            {
                case nameof(ControlViewModel.UseContentPipelineOnPngs):
                    // so handling file changes will probably do this but let's force it so we know it happens:
                    aliasCodeGenerator.GenerateFileAliasLogicCode(controller.Settings.UseContentPipelineOnAllPngs);
                    break;
            }
        }

        public override void StartUp()
        {
            this.AddMenuItemTo("Content Pipeline Settings", HandleContentPipelineSettings, "Content");

            CreateController();

            // must come after the controller
            CreateAliasCodeGenerator();

            AssignEvents();
        }

        private void CreateAliasCodeGenerator()
        {
            aliasCodeGenerator = new AliasCodeGenerator();
            aliasCodeGenerator.Initialize(controller);
            CodeWriter.GlobalContentCodeGenerators.Add(aliasCodeGenerator);
        }

        private void CreateController()
        {
            controller = new ContentPipelineController();

        }

        private void AssignEvents()
        {
            this.ReactToFileChange += HandleFileChanged;
            this.ReactToBuiltFileChangeHandler += (fileName) => HandleFileChanged(fileName, FileChangeType.Modified);
            this.ReactToLoadedGluxEarly += HandleLoadedGlux;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
            this.ReactToLoadedSyncedProject += HandleLoadedSyncedProject;
            this.ReactToNewFileHandler += HandleNewFile;
            this.ReactToReferencedFileChangedValueHandler += HandleReferencedFileValueChanged;
            this.ReactToFileRemoved += HandleFileRemoved;
            this.GetIfUsesContentPipeline += HandleGetIfUsesContentPipeline;
            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;
            this.ReactToFileBuildCommand += HandleReactToFileBuildCommand;
        }

        private void HandleReactToFileBuildCommand(ReferencedFileSave rfs)
        {
            HandleRfsChange(rfs);
        }

        #endregion

        private void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            if(rightClickedTreeNode.IsReferencedFile())
            {
                var rfs = rightClickedTreeNode.Tag as ReferencedFileSave;
                var forcePngsToPipeline = controller.Settings.UseContentPipelineOnAllPngs;
                if (BuildLogic.IsBuiltByContentPipeline(rfs, forcePngsToPipeline))
                {
                    menuToModify.Add("Rebuild Content Pipeline File (xnb)", (not, used) =>
                    {
                        var fullFileName = GlueCommands.Self.GetAbsoluteFileName(rfs);
                        BuildLogic.Self.TryAddXnbReferencesAndBuild(fullFileName, GlueState.CurrentMainProject, false, rebuild:true);
                    });
                }

            }
        }

        private void HandleFileRemoved(IElement container, ReferencedFileSave file)
        {
            // Delete the file just in case a new file with the same name is added later. If so, we don't
            // want old XNBs to sit around and cause the incremental built to not build the newly-added file.
            BuildLogic.TryRemoveXnbReferences(GlueState.CurrentMainProject, file, save: false);
            BuildLogic.TryDeleteBuiltXnbFor(GlueState.CurrentMainProject, file, viewModel.UseContentPipelineOnPngs);

            foreach(VisualStudioProject syncedProject in GlueState.SyncedProjects)
            {
                BuildLogic.TryRemoveXnbReferences(syncedProject, file, save: false);
                BuildLogic.TryDeleteBuiltXnbFor(syncedProject, file, viewModel.UseContentPipelineOnPngs);
            }

            TaskManager.Self.Add(Container.Get<IGlueCommands>().ProjectCommands.SaveProjects, "Save projects after removing XNBs");
        }

        private void HandleGluxUnloaded()
        {
            viewModel.IsProjectLoaded = false;
        }

        private bool HandleGetIfUsesContentPipeline(string absoluteFileName)
        {
            if(controller?.Settings != null)
            {
                var extension = FileManager.GetExtension(absoluteFileName);
                return extension == "png" && controller.Settings.UseContentPipelineOnAllPngs;
            }

            return false;
        }

        private void HandleContentPipelineSettings(object sender, EventArgs e)
        {
            if(control == null)
            {
                control = new ContentPipelinePlugin.ContentPipelineControl();
                control.DataContext = viewModel;
                controller.SetViewModel(viewModel);
                control.RefreshClicked += controller.HandleRefreshClicked;

                tab = base.CreateTab(control, "Content Pipeline");
            }
            tab.Show();
        }

        private void HandleReferencedFileValueChanged(string memberName, object oldValue)
        {
            if(memberName == nameof(ReferencedFileSave.UseContentPipeline))
            {
                var rfs = GlueState.CurrentReferencedFileSave;

                HandleRfsChange(rfs);
            }
            else if(memberName == nameof(ReferencedFileSave.RuntimeType))
            {
                // See if the new ATI says "no content pipeline" but the object is using the content pipeline:
                var rfs = GlueState.CurrentReferencedFileSave;
                if(rfs.GetCanUseContentPipeline() == false && rfs.UseContentPipeline)
                {
                    rfs.UseContentPipeline = false;

                    HandleRfsChange(rfs);

                }
            }
        }

        private async void HandleRfsChange(ReferencedFileSave rfs)
        {
            var newFiles = await BuildLogic.Self.UpdateFileMembershipAndBuildReferencedFile(GlueState.CurrentMainProject, rfs, viewModel.UseContentPipelineOnPngs);

            foreach(var file in newFiles)
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(file);
            }

            foreach (VisualStudioProject syncedProject in GlueState.SyncedProjects)
            {
                BuildLogic.Self.UpdateFileMembershipAndBuildReferencedFile(syncedProject, rfs, viewModel.UseContentPipelineOnPngs);
            }
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {

            if(BuildLogic.GetIfNeedsMonoGameFilesBuilt( GlueState.CurrentMainProject ))
            {
                BuildLogic.Self.UpdateFileMembershipAndBuildReferencedFile(GlueState.CurrentMainProject, newFile, viewModel.UseContentPipelineOnPngs);
            }
            foreach(VisualStudioProject project in GlueState.SyncedProjects)
            {
                if(BuildLogic.GetIfNeedsMonoGameFilesBuilt( project ))
                {
                    BuildLogic.Self.UpdateFileMembershipAndBuildReferencedFile(project, newFile, viewModel.UseContentPipelineOnPngs);
                }
            }
        }

        private void HandleLoadedGlux()
        {
            viewModel.IsProjectLoaded = true;
            controller.LoadOrCreateSettings();
            viewModel.UseContentPipelineOnPngs = controller.Settings.UseContentPipelineOnAllPngs;
            aliasCodeGenerator.GenerateFileAliasLogicCode(controller.Settings.UseContentPipelineOnAllPngs);
            BuildLogic.Self.RefreshBuiltFilesFor(GlueState.CurrentMainProject, viewModel.UseContentPipelineOnPngs, controller);
        }

        private void HandleLoadedSyncedProject(ProjectBase project)
        {
            BuildLogic.Self.RefreshBuiltFilesFor((VisualStudioProject)project, viewModel.UseContentPipelineOnPngs, controller);
        }

        private void HandleFileChanged(FilePath filePath, FileChangeType fileChangeType)
        {
            aliasCodeGenerator.GenerateFileAliasLogicCode(controller.Settings.UseContentPipelineOnAllPngs);

            // See if it's a ReferencedFileSave. If so, we might want to look at that for additional properties
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(
                filePath);

            // This could have gotten deleted. If it's a wildcard delete, then do nothing, it will get removed:
            /////////////////////////////////////////Early Out////////////////////////////////////////////////////
            if(rfs?.IsCreatedByWildcard == true && filePath.Exists()== false)
            {
                return;
            }
            ///////////////////////////////////////End Early Out/////////////////////////////////////////////////


            if(rfs != null)
            {
                HandleRfsChange(rfs);
            }
            else
            {
                // We only want to process this file if it's actually referenced by the project, not if it's a floating file
                var allFileNames = Container.Get<IGlueCommands>().FileCommands.GetAllReferencedFileNames();

                var contentFolder = Container.Get<IGlueState>().ContentDirectory;
                var absoluteLowerCase = allFileNames.Select(item => (contentFolder + item).ToLowerInvariant());

                var standardized = filePath.Standardized;

                var isReferenced = absoluteLowerCase.Contains(standardized);

                if(isReferenced)
                {
                    BuildLogic.Self.TryHandleReferencedFile(GlueState.CurrentMainProject, filePath.FullPath, viewModel.UseContentPipelineOnPngs);

                    foreach (VisualStudioProject syncedProject in GlueState.SyncedProjects)
                    {
                        BuildLogic.Self.TryHandleReferencedFile(syncedProject, filePath.FullPath, viewModel.UseContentPipelineOnPngs);
                    }
                }
            }

        }
    }
}
