using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;

namespace OfficialPlugins.PositionedObjectNosPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPositionedObjectNosPlugin : PluginBase
    {
        public override string FriendlyName => "PositionedObject NamedObjectSave Plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AdjustRotationVariableDefinition();
        }

        private void AdjustRotationVariableDefinition()
        {
            foreach(var ati in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if(ati.IsPositionedObject)
                {
                    var variableDefinition = ati.VariableDefinitions.Find(item => item.Name == "RotationZ");
                    if(variableDefinition != null)
                    {
                        variableDefinition.PreferredDisplayer = typeof(AngleSelectorDisplay);
                    }
                }
            }
        }
    }
}
