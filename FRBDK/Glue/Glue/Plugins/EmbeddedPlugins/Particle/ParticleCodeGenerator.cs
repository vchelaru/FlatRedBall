using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Particle
{
    public class ParticleCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeGeneration.CodeBuilder.ICodeBlock GenerateActivity(CodeGeneration.CodeBuilder.ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            // We used to call TimedEmit here
            // but we only want to call it if the
            // NOS is non-null. That means we are going
            // to call the TimedEmit generating code from
            // within the NamedObjectSaveCodeGenerator's GenerateActivity
            return codeBlock;
        }

        public static void GenerateTimedEmit(ICodeBlock codeBlock, NamedObjectSave nos)
        {

            if (!nos.IsDisabled && nos.AddToManagers && nos.IsEmitter() && nos.GenerateTimedEmit)
            {

                var timedEmitBlock = codeBlock;

                if(nos.SetByDerived)
                {
                    // this may be null
                    timedEmitBlock = timedEmitBlock.If($"{nos.InstanceName} != null");
                }
                timedEmitBlock.Line(nos.InstanceName + ".TimedEmit();");
            }
        }

        public static void GenerateTimedEmit(ICodeBlock codeBlock, ReferencedFileSave rfs, GlueElement element)
        {
            if (rfs.LoadedAtRuntime && !rfs.LoadedOnlyWhenReferenced && (element is ScreenSave || rfs.IsSharedStatic == false)
                && !string.IsNullOrEmpty(rfs.Name) && FileManager.GetExtension(rfs.Name) == "emix")
            {
                codeBlock.Line(rfs.GetInstanceName() + ".TimedEmit();");
            }
        }
    }



    static class NosParticleExtensionMethods
    {
        public static bool IsEmitter(this NamedObjectSave nos)
        {
            return (nos.SourceType == SourceType.FlatRedBallType && (nos.SourceClassType == "Emitter" || nos.SourceClassType == "FlatRedBall.Graphics.Particle.Emitter")) ||
                (nos.SourceType == SourceType.File && !string.IsNullOrEmpty(nos.SourceFile) && FileManager.GetExtension(nos.SourceFile) == "emix");

        }


    }
}
