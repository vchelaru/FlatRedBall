using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.ContentPreview.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPreview
{

    [Export(typeof(PluginBase))]
    public class MainContentPreviewPlugin : PluginBase
    {
        #region Plugin Required Methods/Properties
        public override string FriendlyName => "Content Preview Plugin";

        public override Version Version => new Version(1,0);



        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        #endregion

        public override void StartUp()
        {
            AssignEvents();

            PngManager.Initialize(this);
            WavManager.Initialize(this);
        }

        private void AssignEvents()
        {
            this.ReactToItemSelectHandler += HandleTreeViewItemSelected;
            this.ReactToFileChange += HandleFileChange;

            this.TryHandleTreeNodeDoubleClicked += TryHandleDoubleClick;
        }

        private bool TryHandleDoubleClick(ITreeNode tree)
        {
            if(tree.Tag is ReferencedFileSave asRfs)
            {
                var extension = FileManager.GetExtension(asRfs.Name);

                var filePath = GlueCommands.Self.GetAbsoluteFilePath(asRfs);

                switch (extension)
                {
                    case "png":
                        PngManager.HandleStrongSelect();
                        return true;
                    case "wav":
                        WavManager.HandleStrongSelect();
                        return true;
                }
            }

            return false;
        }

        private void HandleTreeViewItemSelected(ITreeNode selectedTreeNode)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;

            WavManager.HideTab();
            PngManager.HideTab();

            /////////////////Early Out///////////////////
            if (file == null)
            {
                return;
            }
            ///////////////End Early Out/////////////////

            var filePath = GlueCommands.Self.GetAbsoluteFilePath(file);

            var extension = filePath.Extension;

            switch(extension)
            {
                case "png":
                    PngManager.ShowTab(filePath);
                    break;
                case "wav":
                    WavManager.ShowTab(filePath);
                    break;
            }
        }


        private void HandleFileChange(FilePath filePath, FileChangeType changeType)
        {
            var extension = filePath.Extension;

            switch (extension)
            {
                case "png":
                    if(PngManager.PngFilePath == filePath)
                    {
                        PngManager.ForceRefreshTexture(filePath);
                    }
                    break;
                case "wav":
                    if(WavManager.WavFilePath == filePath)
                    {
                        WavManager.ForceRefreshWav(filePath);
                    }
                    break;
            }


        }

    }
}
