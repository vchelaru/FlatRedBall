using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;
using FlatRedBall.IO;

namespace FlatRedBall.Content.Scene
{
    [ContentProcessor(DisplayName = "Text - FlatRedBall")]
    public class TextProcessor : ContentProcessor<TextSaveContent, TextSaveContent>
    {
        #region Properties

        public string Directory
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public TextProcessor() : base() { }

        public TextProcessor(string directory)
            : base()
        {
            Directory = directory;
        }

        public override TextSaveContent Process(TextSaveContent input, ContentProcessorContext context)
        {
            // it's possible that the Text has no font.  If so, let's just not process anything
            if(string.IsNullOrEmpty(input.FontTexture))
            {
                return input;
            }

            string combinedPath = Path.Combine(Directory, input.FontTexture);

            input.FontTextureReference = BitmapTextureProcessor.BuildTexture(
                combinedPath,
                context);
            input.FontTextureReference.Name = input.FontTexture;

            // See if the .fnt file exists.  If not, write it out.

            if (!string.IsNullOrEmpty(input.FontFile))
            {
                string sourceFontFile = Path.Combine(Directory, input.FontFile);

                input.FontPatternText = FileManager.FromFileText(sourceFontFile);

            }

            return input;
        }

        #endregion
    }
}
