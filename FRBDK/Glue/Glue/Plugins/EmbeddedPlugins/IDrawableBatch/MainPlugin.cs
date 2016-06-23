using FlatRedBall.Glue.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.IDrawableBatch
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            var generator = new IDrawableBatchCodeGenerator();

            CodeWriter.CodeGenerators.Add(generator);

        }
    }
}
