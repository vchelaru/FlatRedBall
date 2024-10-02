using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;

namespace OfficialPlugins.AxisAlignedRectanglePlugin
{
    [Export(typeof(PluginBase))]
    public class MainAxisAlignedRectanglePlugin : PluginBase
    {
        public override void StartUp()
        {
            AdjustAssetTypeInfo();
        }

        private void AdjustAssetTypeInfo()
        {
            var ati = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "XVelocity",
                Type = "float",
                IsVariableVisibleInEditor = (_, _) => false
            });
            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "YVelocity",
                Type = "float",
                IsVariableVisibleInEditor = (_, _) => false
            });
            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "ZVelocity",
                Type = "float",
                IsVariableVisibleInEditor = (_, _) => false
            });

            // No one would ever set this, but just in case it's reset:


        }
    }
}
