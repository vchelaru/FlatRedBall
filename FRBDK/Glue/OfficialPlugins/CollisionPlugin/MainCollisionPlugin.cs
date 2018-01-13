using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainCollisionPlugin : PluginBase
    {
        public override string FriendlyName => "Collision Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            UnregisterAllCodeGenerators();
            return true;
        }

        public override void StartUp()
        {
            var collisionCodeGenerator = new CollisionCodeGenerator();

            RegisterCodeGenerator(collisionCodeGenerator);
        }
    }
}
