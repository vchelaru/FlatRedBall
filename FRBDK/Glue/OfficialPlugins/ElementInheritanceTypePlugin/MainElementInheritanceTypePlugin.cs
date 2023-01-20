using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.ElementInheritanceTypePlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ElementInheritanceTypePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainElementInheritanceTypePlugin : PluginBase
    {
        public override string FriendlyName => "Element Inheritance Type Plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.RegisterCodeGenerator(new ElementInheritanceTypeCodeGenerator());
        }
    }
}
