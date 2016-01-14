using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.Xna.Framework.Content.Pipeline;

namespace FlatRedBall.Content.SpriteGrid
{
    [ContentProcessor(DisplayName = "SpriteGrid - FlatRedBall")]
    class SpriteGridProcessor : ContentProcessor<SpriteGridSaveContent, SpriteGridSaveContent>
    {
        SpriteProcessor mSpriteProcessor;
        
        public SpriteGridProcessor() : base() { }

        public SpriteGridProcessor(SpriteProcessor spriteProcessor)
        {
            mSpriteProcessor = spriteProcessor;
        }

        public override SpriteGridSaveContent Process(SpriteGridSaveContent input, ContentProcessorContext context)
        {
            string directory = System.IO.Path.GetDirectoryName(input.FileName) + @"\";

            mSpriteProcessor.Process(input.Blueprint, context);

			if (!string.IsNullOrEmpty(input.BaseTexture))
			{
				input.BaseTextureReference = BitmapTextureProcessor.BuildTexture(
							directory + input.BaseTexture,
							context);
			}

            input.GridTextureReferences =
                new ExternalReference<Microsoft.Xna.Framework.Content.Pipeline.Graphics.TextureContent>[input.GridTexturesArray.Length][];

            for (int i = 0; i < input.GridTexturesArray.Length; i++)
            {
                input.GridTextureReferences[i] =
                    new ExternalReference<Microsoft.Xna.Framework.Content.Pipeline.Graphics.TextureContent>[input.GridTexturesArray[i].Length];
                for (int j = 0; j < input.GridTexturesArray[i].Length; j++)
                {
					if (!string.IsNullOrEmpty(input.GridTexturesArray[i][j]))
					{
						input.GridTextureReferences[i][j] = BitmapTextureProcessor.BuildTexture(
							directory + input.GridTexturesArray[i][j],
							context);
					}
                }
            }

            return input;
        }
    }
}
