using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers;
using NAudioPlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace NAudioPlugin
{
    [Export(typeof(PluginBase))]
    public class MainNAudioPlugin : PluginBase
    {
        public override string FriendlyName => "NAduio Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            base.ShutDown(shutDownReason);
            Managers.AssetTypeInfoManager.RemoveAssetTypes();
            return true;
        }

        public override void StartUp()
        {
            Managers.AssetTypeInfoManager.AddAssetTypes();
            RegisterCodeGenerator(new ElementCodeGenerator());

            AddMenuItemTo("Embed NAudio Classes", HandleEmbedNAudioFiles, "Content");
        }

        private void HandleEmbedNAudioFiles(object sender, EventArgs e)
        {
            var codeItemAdder = new CodeBuildItemAdder();
            codeItemAdder.OutputFolderInProject = "NAudio";
            var thisAssembly = this.GetType().Assembly;

            codeItemAdder.AddFolder("NAudioPlugin/Embedded", thisAssembly);

            codeItemAdder.PerformAddAndSaveTask(thisAssembly);

        }
    }
}
