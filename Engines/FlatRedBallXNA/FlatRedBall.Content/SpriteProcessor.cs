using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Content.Scene;
using FlatRedBall.Content.AnimationChain;

using Microsoft.Xna.Framework.Content.Pipeline;

namespace FlatRedBall.Content
{
    [ContentProcessor(DisplayName = "Sprite - FlatRedBall")]
    class SpriteProcessor : ContentProcessor<SpriteSaveContent, SpriteSaveContent>
    {
        #region Fields

        string mDirectory;
        //AnimationChainArrayProcessor mAnimationChainArrayProcessor;

        #endregion

        #region Properties

        public string Directory
        {
            get { return mDirectory; }
            set { mDirectory = value; }
        }

        //public AnimationChainArrayProcessor AchProcessor
        //{
        //    get { return mAnimationChainArrayProcessor; }
        //    set { mAnimationChainArrayProcessor = value; }
        //}

        #endregion

        public SpriteProcessor() : base() { }

        public SpriteProcessor(string directory) : base()
        {
            mDirectory = directory;
        }

        public override SpriteSaveContent Process(SpriteSaveContent input, ContentProcessorContext context)
        {
            if (!string.IsNullOrEmpty(input.Texture))
            {
                input.TextureReference = BitmapTextureProcessor.BuildTexture(
                    Path.Combine(mDirectory, input.Texture),
                    context);
                input.TextureReference.Name = input.Texture;
            }
            else
            {
                input.TextureReference = null;
            }

            //process the animation chain data
            //if (!string.IsNullOrEmpty(input.AnimationChainsFile))
            //{
            //    input.AnimationChainReference =
            //        AnimationChainArrayProcessor.BuildExternalReference(mDirectory + @"\" + input.AnimationChainsFile, context);
            //}
            //else if (input.AnimationChains != null && input.AnimationChains.AnimationChains.Count > 0)
            //{
            //    mAnimationChainArrayProcessor.Process(input.AnimationChains, context);
            //}

            return input;
        }
    }
}
