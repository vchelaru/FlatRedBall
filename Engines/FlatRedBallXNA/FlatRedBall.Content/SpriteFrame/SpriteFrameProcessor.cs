using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.Xna.Framework.Content.Pipeline;

namespace FlatRedBall.Content.SpriteFrame
{
    [ContentProcessor(DisplayName = "SpriteFrame - FlatRedBall")]
    class SpriteFrameProcessor : ContentProcessor<SpriteFrameSaveContent, SpriteFrameSaveContent>
    {
        string mDirectory;
        SpriteProcessor mSpriteProcessor;

        public string Directory
        {
            get { return mDirectory; }
            set { mDirectory = value; }
        }

        public SpriteFrameProcessor() : base() { }

        public SpriteFrameProcessor(string directory, SpriteProcessor spriteProcessor) : base()
        {
            mDirectory = directory;
            mSpriteProcessor = spriteProcessor;
        }

        public override SpriteFrameSaveContent Process(SpriteFrameSaveContent input, ContentProcessorContext context)
        {
            mSpriteProcessor.Process(input.ParentSprite, context);
            return input;
        }
    }
}
