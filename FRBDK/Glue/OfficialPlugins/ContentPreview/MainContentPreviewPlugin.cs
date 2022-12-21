using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
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
        public override string FriendlyName => "Content Preview Plugin";

        public override Version Version => new Version(1,0);


        PluginTab PngTab;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ReactToItemSelectHandler += HandleTreeViewItemSelected;
        }

        private void HandleTreeViewItemSelected(ITreeNode selectedTreeNode)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;

            PngTab?.Hide();
            /////////////////Early Out///////////////////
            if (file == null)
            {
                return;
            }
            ///////////////End Early Out/////////////////

            var extension = FileManager.GetExtension(file.Name);

            var filePath = GlueCommands.Self.GetAbsoluteFilePath(file);


            switch(extension)
            {
                case "png":
                    var view = PngManager.GetView(filePath);
                    view.TextureFilePath = filePath;
                    var vm = PngManager.ViewModel;
                    vm.ResolutionWidth = view.Texture?.Width ?? 0;
                    vm.ResolutionHeight = view.Texture?.Height ?? 0;
                    view.ResetCamera();
                    
                    if (PngTab == null)
                    {
                        PngTab = CreateTab(view, "PNG Preview", TabLocation.Center);
                    }

                    PngTab.Show();
                    view.GumCanvas.InvalidateVisual();
                    break;
            }
        }

        private void ShowPngPreview()
        {

        }
    }
}
