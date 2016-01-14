using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;

namespace FlatRedBall.Content.AnimationChain
{
    [ContentProcessor(DisplayName = "AnimationChain - FlatRedBall")]
    public class AnimationChainArrayProcessor : ContentProcessor<AnimationChainListSaveContent, AnimationChainListSaveContent>
    {
        #region Properties

        [DefaultValue(false)]
        [DisplayName("Resize to Power of 2")]
        [Description("Whether the textures referenced by the AnimationChain will be resized to a power of two.  This can improve compatability with some hardware, but can result in your texture looking blurry.")]
        public bool ResizeToPowerOfTwo
        {
            get;
            set;
        }

        [DefaultValue(TextureProcessorOutputFormat.Color)]
        [DisplayName("Texture format")]
        [Description("The format of the resulting texture.  This can be used to make textures take less space on the graphics card, but some compression methods are lossy")]
        public TextureProcessorOutputFormat TextureProcessorOutputFormat
        {
            get;
            set;
        }

		[DefaultValue(true)]
		[DisplayName("Generate Mipmaps")]
		[Description("If enabled, a full mipmap chain is generated from source textures.  Existing mipmaps are not replaced")]
		public bool GenerateMipmaps
		{
			get;
			set;
		}
        #endregion

		public AnimationChainArrayProcessor()
            : base()
        {
            GenerateMipmaps = true;

        }

        /// <summary>
        /// look at all of the textures in each frame and convert them into external references
        /// </summary>
        public override AnimationChainListSaveContent Process(AnimationChainListSaveContent input, ContentProcessorContext context)
        {
            string directory = System.IO.Path.GetDirectoryName(input.FileName) + @"\";

            BitmapTextureProcessor.ResizeToPowerOfTwo = ResizeToPowerOfTwo;
            BitmapTextureProcessor.TextureProcessorOutputFormat = TextureProcessorOutputFormat;
			BitmapTextureProcessor.GenerateMipmaps = GenerateMipmaps; // This doesn't seem to be working......why?

            #region Loop through all AnimationChains and process them.

            for (int i = 0; i < input.AnimationChains.Count; i++)
            {
                AnimationChainSaveContent ach = input.AnimationChains[i];
                List<AnimationFrameSaveContent> newFrames = new List<AnimationFrameSaveContent>(ach.Frames.Count);

                for (int j = 0; j < ach.Frames.Count; j++)
                {
                    AnimationFrameSaveContent frameSave = ach.Frames[j];// new AnimationFrameSaveContent(ach.Frames[j]);

                    frameSave.TextureReference = BitmapTextureProcessor.BuildTexture(
                        directory + frameSave.TextureName,
                        context);

                    newFrames.Add(frameSave);
                }
                ach.Frames = newFrames;
            }

            #endregion

            return input;
        }

#if !FRB_MDX
        /// <summary>
        /// Builds an External Reference for use in the content pipeline
        /// </summary>
        public static ExternalReference<AnimationChainListSaveContent> BuildExternalReference(string path, ContentProcessorContext context)
        {
            return context.BuildAsset<AnimationChainListSaveContent, AnimationChainListSaveContent>(
                new ExternalReference<AnimationChainListSaveContent>(path),
                typeof(AnimationChainArrayProcessor).Name,
				context.Parameters,
				typeof(AnimationChainArrayImporter).Name,
				null);
        }
#endif

    }
}
