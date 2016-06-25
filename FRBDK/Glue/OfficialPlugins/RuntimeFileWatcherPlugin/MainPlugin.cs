using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.RuntimeFileWatcherPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            var generator = new FileSystemWatcherCodeGenerator();
            CodeWriter.GlobalContentCodeGenerators.Add(generator);
        }
    }
}
