using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Errors;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.ContentPreview.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Navigation;
using FileManager = ToolsUtilities.FileManager;

namespace OfficialPluginsCore.AnimationChainPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAnimationChainPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Animation Chain Plugin";

        public override Version Version => new Version(1, 0);

        public static MainAnimationChainPlugin Self { get; private set; }

        #endregion

        public new void RefreshErrors() => base.RefreshErrors();

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            Self = this;
            AssignEvents();
            this.AddErrorReporter(new AnimationChainErrorReporter());

            AchxManager.Initialize(this);
        }

        private void AssignEvents()
        {
            this.ReactToNewFileHandler += HandleNewFile;
            this.ReactToFileChange += HandleFileChanged;
            this.ReactToNamedObjectChangedValue += NamedObjectVariableChangeLogic.HandleNamedObjectChangedValue;
            this.TryHandleTreeNodeDoubleClicked += TryHandleDoubleClick;
            this.ReactToItemSelectHandler += HandleTreeViewItemSelected;
            this.ReactToLoadedGluxEarly += HandleLoadedGluxEarly;
            this.ReactToUnloadedGlux += HandleUnloadedGlux;
        }

        private void HandleLoadedGluxEarly()
        {
            var ati = AssetTypeInfoManager.Self.TryGetAsepriteAti();
            if(ati != null)
            {
                base.AddAssetTypeInfo(ati);
            }
        }

        private void HandleUnloadedGlux()
        {
            base.UnregisterAssetTypeInfos();
        }

        private bool TryHandleDoubleClick(ITreeNode tree)
        {
            if (tree.Tag is ReferencedFileSave asRfs)
            {
                var extension = FileManager.GetExtension(asRfs.Name);

                var filePath = GlueCommands.Self.GetAbsoluteFilePath(asRfs);

                switch (extension)
                {
                    case "achx":
                        // Nah, let's open AnimationEditor for now
                        return false;
                }
            }

            return false;
        }

        private void HandleTreeViewItemSelected(ITreeNode selectedTreeNode)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;

            AchxManager.HideTab();

            /////////////////Early Out///////////////////
            if (file == null)
            {
                return;
            }
            ///////////////End Early Out/////////////////

            var filePath = GlueCommands.Self.GetAbsoluteFilePath(file);

            var extension = filePath.Extension;

            switch (extension)
            {
                case "achx":
                    AchxManager.ShowTab(filePath);
                    break;
            }
        }


        private void HandleFileChanged(FilePath filePath, FileChangeType fileChange)
        {
            if (filePath.Extension == "achx")
            {
                this.RefreshErrors();

                if (AchxManager.AchxFilePath == filePath)
                {
                    AchxManager.ForceRefreshAchx(filePath);
                }
            }
        }

        private void HandleNewFile(ReferencedFileSave newFile, AssetTypeInfo assetTypeInfo)
        {
            var extension = FileManager.GetExtension(newFile.Name);
            if(extension == "achx")
            {
                var file = GlueCommands.Self.GetAbsoluteFilePath(newFile);

                if(file.Exists())
                {
                    // load it and set the project file
                    var achx = AnimationChainListSave.FromFile(file.FullPath);

                    var projectFile = FileManager.MakeRelative(GlueState.Self.GlueProjectFileName.FullPath, file.GetDirectoryContainingThis().FullPath);

                    if(projectFile != achx.ProjectFile)
                    {
                        achx.ProjectFile = projectFile;
                        achx.Save(file.FullPath);
                    }
                }
            }
        }
    }
}
