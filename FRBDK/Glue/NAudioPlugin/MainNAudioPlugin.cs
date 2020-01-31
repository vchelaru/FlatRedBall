using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
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
            Managers.AssetTypeInfoManager.RemoveAssetTypes();
            UnregisterAllCodeGenerators();
            return true;
        }

        public override void StartUp()
        {
            Managers.AssetTypeInfoManager.AddAssetTypes();
            RegisterCodeGenerator(new ElementCodeGenerator());
        }
    }
}
