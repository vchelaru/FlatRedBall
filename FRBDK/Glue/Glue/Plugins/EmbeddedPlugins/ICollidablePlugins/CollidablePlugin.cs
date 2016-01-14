using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.ICollidablePlugins
{
    [Export(typeof(PluginBase))]
    public class CollidablePlugin : EmbeddedPlugin
    {
        CollidableCodeGenerator mGenerator;
        public override void StartUp()
        {
            // add the code generator:

            mGenerator = new CollidableCodeGenerator();
            CodeWriter.CodeGenerators.Add(mGenerator);
        }
    }
}
