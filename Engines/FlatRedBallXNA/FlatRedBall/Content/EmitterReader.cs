using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Particle;

namespace FlatRedBall.Content
{
    public class EmitterReader : ContentTypeReader<EmitterList>
    {
        protected override EmitterList Read(ContentReader input, EmitterList existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            EmitterSaveList esl = ObjectReader.ReadObject<EmitterSaveList>(input);
            esl.Name = input.AssetName;
            return esl.ToEmitterList("");

            //// read how many emitters there are
            //int count = input.ReadInt32();

            //existingInstance = new EmitterList();

            //for (int i = 0; i < count; i++)
            //{
            //    Emitter emitter = new Emitter();
            //    existingInstance.Add(emitter);

            //    emitter.X = input.ReadSingle();
            //    emitter.Y = input.ReadSingle();
            //    emitter.Z = input.ReadSingle();

            //    emitter.ScaleX = input.ReadSingle();
            //    emitter.ScaleY = input.ReadSingle();
            //    emitter.ScaleZ = input.ReadSingle();

            //    emitter.AreaEmission = 
            //        FlatRedBall.Graphics.GraphicalEnumerations.TranslateAreaEmissionType( 
            //            input.ReadString() ); 
            //    emitter.EmissionSettings = input.ReadObject<EmissionSettings>();

            //    input.ReadString(); // ParentSpriteName
            //    emitter.Name = input.ReadString();

            //    emitter.ParticleBlueprint = input.ReadObject<Sprite>();

            //    emitter.RemovalEvent = FlatRedBall.Graphics.Particle.Emitter.TranslateRemovalEvent(input.ReadString());

            //    emitter.SecondFrequency = input.ReadSingle();
            //    emitter.NumberPerEmission = input.ReadInt32();
            //    emitter.TimedEmission = input.ReadBoolean();

            //    emitter.RelativeX = input.ReadSingle();
            //    emitter.RelativeY = input.ReadSingle();
            //    emitter.RelativeZ = input.ReadSingle();

            //    emitter.ParentVelocityChangesEmissionVelocity = input.ReadBoolean();

            //    input.ReadBoolean(); // AssetsRelativeToFile
            //    emitter.SecondsLasting = input.ReadSingle();
            //}

            //return existingInstance;
        }
    }
}
