using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficialPlugins.EffectPlugin.Managers;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace OfficialPlugins.EffectPlugin
{
    [Export(typeof(PluginBase))]
    public class MainEffectPlugin : PluginBase
    {
        public override string FriendlyName => "Effect Plugin"; 

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleLoadGlux;
            this.ReactToUnloadedGlux += HandleUnloadGlux;
        }

        private void HandleLoadGlux()
        {
            var project = GlueState.Self.CurrentGlueProject;

            var vsProject = GlueState.Self.CurrentMainProject;

            var isFna = vsProject is FnaDesktopProject;

            if(isFna)
            {
                this.AddAssetTypeInfo(AssetTypeInfoManager.FxbEffectAssetTypeInfo);
            }
        }

        private void HandleUnloadGlux()
        {
            this.UnregisterAssetTypeInfos();
        }
    }
}
