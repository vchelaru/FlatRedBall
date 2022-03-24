using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.SpritePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainSpritePlugin : PluginBase
    {
        public override string FriendlyName => "Sprite Plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var textureVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Texture");
            if(textureVariable != null)
            {
                textureVariable.PreferredDisplayer = typeof(EditableComboBoxDisplay);
            }
        }
    }
}
