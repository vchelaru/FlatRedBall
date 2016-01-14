using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

using FlatRedBall.Content.Scene;
using FlatRedBall.Graphics.Particle;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.Particle
{
    [ContentTypeWriter]
    class EmitterWriter : ContentTypeWriter<EmitterSaveContentList>
    {
        protected override void Write(ContentWriter output, EmitterSaveContentList value)
        {
                ObjectWriter.WriteObject<EmitterSaveContentList>(output, value);

            // write out how many emitters there are so the reader can create a loop
            //output.Write(value.Count);

            //for (int i = 0; i < value.Count; i++)
            //{

            //    output.Write(value[i].X);
            //    output.Write(value[i].Y);
            //    output.Write(value[i].Z);

            //    output.Write(value[i].ScaleX);
            //    output.Write(value[i].ScaleY);
            //    output.Write(value[i].ScaleZ);

            //    output.Write(value[i].AreaEmissionType);

            //    output.WriteObject<EmissionSettingsSave>(value[i].EmissionSettings);

            //    // output doesn't like if you write null, so convert null to ""
            //    if (value[i].ParentSpriteName == null)
            //        output.Write("");
            //    else
            //        output.Write(value[i].ParentSpriteName);
            //    output.Write(value[i].Name);

            //    output.WriteObject<SpriteSave>(value[i].ParticleBlueprint);
            //    output.Write(value[i].RemovalEvent);

            //    output.Write(value[i].SecondFrequency);
            //    output.Write(value[i].NumberPerEmission);
            //    output.Write(value[i].TimedEmission);

            //    output.Write(value[i].RelativeX);
            //    output.Write(value[i].RelativeY);
            //    output.Write(value[i].RelativeZ);

            //    output.Write(value[i].ParentVelocityChangesEmissionVelocity);

            //    output.Write(value[i].AssetsRelativeToFile);
            //    output.Write(value[i].SecondsLasting);
            //}
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(EmitterReader).AssemblyQualifiedName;
        }
    }
}
