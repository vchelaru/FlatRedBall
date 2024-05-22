using FlatRedBall;
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
using System.IO;
using System.Linq;
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
            this.IsHandlingHotkeys += GetIfIsHandlingHotkeys;
            //this.FillWithReferencedFiles += HandleFillWithReferencedFiles;
            this.FillWithReferencedFiles += HandleFillWithReferencedFilesNew;
        }

        // See HandleFillWithReferencedFilesNew for info on why this isn't used
        private ToolsUtilities.GeneralResponse HandleFillWithReferencedFiles(FilePath path, List<FilePath> list)
        {
            if(path.Extension == "achx")
            {
                if(path.Exists())
                {
                    var acls = AnimationChainListSave.FromFile(path.FullPath);
                    var newReferencedFiles = acls.GetReferencedFiles(RelativeType.Absolute).Select(item => new FilePath(item)).ToList();

                    list.AddRange(newReferencedFiles);

                    return ToolsUtilities.GeneralResponse.SuccessfulResponse;
                }
                else
                {
                    return ToolsUtilities.GeneralResponse.UnsuccessfulWith("File does not exist: " + path.FullPath);
                }
            }
            else
            {
                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
        }

        // Deadvivors has a lot of .achx files and 
        // loading the project can be a bit slow. This
        // method attempts to speed up the .achx file reference
        // tracking by looping through the lines and looking for
        // the <TextureName> XML tag. This seems to be faster than
        // XML loading - my tests loaded a file 1000 times and it went
        // from 0.7 seconds to 0.2 seconds. This is a little less flexible
        // since it assumes TextureName rather than relying on reusable reference
        // tracking, but modern .achx files only use this.
        private ToolsUtilities.GeneralResponse HandleFillWithReferencedFilesNew(FilePath path, List<FilePath> list)
        {
            if (path.Extension == "achx")
            {
                if (path.Exists())
                {
                    var directory = path.GetDirectoryContainingThis();
                    using (StreamReader reader = new StreamReader(path.FullPath))
                    {
                        string line;
                        var textureNameLength = "<TextureName>".Length;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if(line.Contains("<TextureName>"))
                            {
                                var startIndex = line.IndexOf("<TextureName>") + textureNameLength;
                                var endIndex = line.IndexOf("</TextureName>");
                                var textureName = line.Substring(startIndex, endIndex - startIndex);
                                list.Add(directory + textureName);
                            }
                        }
                    }
                    return ToolsUtilities.GeneralResponse.SuccessfulResponse;
                }
                else
                {
                    return ToolsUtilities.GeneralResponse.UnsuccessfulWith("File does not exist: " + path.FullPath);
                }
            }
            else
            {
                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
        }

        private bool GetIfIsHandlingHotkeys()
        {
            return AchxManager.GetIfIsHandlingHotkeys();
        }

        public new void RefreshErrors() => base.RefreshErrors();

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
