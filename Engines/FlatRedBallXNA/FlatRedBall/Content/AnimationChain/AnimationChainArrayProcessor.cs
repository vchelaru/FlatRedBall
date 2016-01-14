using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.Graphics.Animation;

namespace FlatRedBall.Content.AnimationChain
{
    [ContentProcessor(DisplayName = "AnimationChain - FlatRedBall")]
    public class AnimationChainArrayProcessor : ContentProcessor<AnimationChainListSave, AnimationChainListSave>
    {
        /// <summary>
        /// look at all of the textures in each frame and convert them into external references
        /// </summary>
        public override AnimationChainListSave Process(AnimationChainListSave input, ContentProcessorContext context)
        {
            string directory = System.IO.Path.GetDirectoryName(input.FileName) + @"\";

            for (int i = 0; i < input.AnimationChains.Count; i++)
            {
                AnimationChainSave ach = input.AnimationChains[i];
                List<AnimationFrame> newFrames = new List<AnimationFrame>(ach.Frames.Count);

                for (int j = 0; j < ach.Frames.Count; j++)
                {
                    AnimationFrameSave frameSave = new AnimationFrameSave(ach.Frames[j]);
                    frameSave.TextureReference = BitmapTextureProcessor.BuildTexture(
                        directory + frameSave.TextureName,
                        context);

                    newFrames.Add(frameSave);
                }
                ach.Frames = newFrames;
            }
            return input;
        }

#if !XBOX360 && !FRB_MDX
        /// <summary>
        /// Builds an External Reference for use in the content pipeline
        /// </summary>
        public static ExternalReference<AnimationChainListSave> BuildExternalReference(string path, ContentProcessorContext context)
        {
            return context.BuildAsset<AnimationChainListSave, AnimationChainListSave>(
                new ExternalReference<AnimationChainListSave>(path),
                typeof(AnimationChainArrayProcessor).Name);
        }
#endif

    }
}
