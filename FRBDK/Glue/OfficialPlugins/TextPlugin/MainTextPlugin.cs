using FlatRedBall.Glue.Plugins;
using OfficialPlugins.SpritePlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficialPlugins.TextPlugin.Managers;

namespace OfficialPlugins.TextPlugin
{
    [Export(typeof(PluginBase))]
    public class MainTextPlugin : PluginBase
    {
        public override string FriendlyName => "Text Plugin";

        public override void StartUp()
        {
            AssetTypeInfoManager.HandleStartup();
        }

    }
}
