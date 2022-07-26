using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json.Linq;
using OfficialPlugins.PreviewGenerator.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.PreviewGenerator
{
    [Export(typeof(PluginBase))]
    internal class MainPreviewGeneratorPlugin : PluginBase
    {
        public override string FriendlyName => "Preview Generator";

        public override Version Version => new Version(1, 0);

        PluginTab previewPreviewTab;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var view = new MainPreviewGeneratorControl();
            previewPreviewTab = this.CreateTab(view, "Preview Preview");

            this.ReactToItemSelectHandler = HandleReactToItemSelectHandler;
        }

        private void HandleReactToItemSelectHandler(ITreeNode selectedTreeNode)
        {
            var shouldShow = GlueState.Self.CurrentEntitySave != null;

            if(shouldShow)
            {
                previewPreviewTab.Show();
            }
            else
            {
                previewPreviewTab.Hide();
            }
        }

        protected override Task<string> HandleEventWithReturnImplementation(string eventName, string payload)
        {
            switch(eventName)
            {
                case "PreviewGenerator_SaveImageSourceForSelection":

                    var obj = JObject.Parse(payload);

                    if(obj.ContainsKey("ImageFilePath") && obj.ContainsKey("NamedObjectSave") && obj.ContainsKey("Element") && obj.ContainsKey("State"))
                    {
                        var path = obj.Value<string>("ImageFilePath");
                        var nosId = obj.Value<Guid?>("NamedObjectSave");
                        var eId = obj.Value<Guid?>("Element");
                        var sId = obj.Value<Guid?>("sId");

                        var nos = nosId.HasValue ? PluginStorage.TryRemove(nosId.Value, out var tempNos) ? (NamedObjectSave)tempNos : null : null;
                        var e = eId.HasValue ? PluginStorage.TryRemove(eId.Value, out var tempE) ? (GlueElement)tempE : null : null;
                        var s = sId.HasValue ? PluginStorage.TryRemove(sId.Value, out var tempS) ? (StateSave)tempS : null : null;

                        var image = PreviewGenerator.Managers.PreviewGenerationLogic.GetImageSourceForSelection(nos, e, s);
                        if (image != null)
                        {
                            try
                            {
                                PreviewGenerator.Managers.PreviewSaver.SavePreview(image as BitmapSource, e, s);
                            }
                            catch (Exception ex)
                            {
                                GlueCommands.Self.PrintError(ex.ToString());
                            }
                        }
                    }

                    return Task.FromResult("");
            }

            return Task.FromResult((string)null);
        }
    }
}
