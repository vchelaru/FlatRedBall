using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using OfficialPlugins.PolygonPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.PolygonPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainPolygonPlugin : PluginBase
    {
        public override string FriendlyName => "Main Polygon Plugin";

        public override void StartUp()
        {
            var ati = AvailableAssetTypes.CommonAtis.Polygon;

            var pointsVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Points");

            this.RegisterCodeGenerator(new PolygonCodeGenerator());

        }
    }
}
