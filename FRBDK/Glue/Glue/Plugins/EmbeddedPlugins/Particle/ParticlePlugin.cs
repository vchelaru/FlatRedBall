using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Particle
{
    [Export(typeof(PluginBase))]
    public class ParticlePlugin : EmbeddedPlugin
    {
        ParticleCodeGenerator mParticleCodeGenerator;
        ParticleFileReferenceManager fileReferenceManager;

        public override void StartUp()
        {
            mParticleCodeGenerator = new ParticleCodeGenerator();
            CodeWriter.CodeGenerators.Add(mParticleCodeGenerator);

            fileReferenceManager = new ParticleFileReferenceManager();

            this.CanFileReferenceContent = fileReferenceManager.CanFileReferenceContent;
            this.FillWithReferencedFiles = fileReferenceManager.FillWithReferencedFiles;
        }
    }
}
