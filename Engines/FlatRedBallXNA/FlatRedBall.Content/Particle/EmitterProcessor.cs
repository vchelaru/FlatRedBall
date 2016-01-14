using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.Graphics.Particle;

namespace FlatRedBall.Content.Particle
{
    [ContentProcessor(DisplayName = "EmitterList - FlatRedBall")]
    class EmitterProcessor : ContentProcessor<EmitterSaveContentList, EmitterSaveContentList>
    {
        public override EmitterSaveContentList Process(EmitterSaveContentList input, ContentProcessorContext context)
        {

            if (input.emitters.Count != 0)
            {
                string directory = System.IO.Path.GetDirectoryName(input.emitters[0].FileName) + @"\";
                SpriteProcessor spriteProcessor = new SpriteProcessor(directory);

                for (int i = 0; i < input.emitters.Count; i++)
                {
                    // 04/2015 Justin Johnson: This is a known break as a result of updates to
                    // the emitter save system. Particle blueprints were retired and the
                    // new ParticleBlueprint is a dummy object, not a real Sprite
                    //spriteProcessor.Process(input.emitters[i].ParticleBlueprint, context);
                }
            }
            return input;
        }
    }
}
