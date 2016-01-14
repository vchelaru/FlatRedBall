using System;
using System.Collections.Generic;

using System.IO;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.SpriteGrid;
using FlatRedBall.Content.SpriteFrame;

using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;
using FlatRedBall.IO;
using System.ComponentModel;
using System.Windows.Forms;
//using FlatRedBall.Content.Model.Animation;
using System.Diagnostics;
namespace FlatRedBall.Content
{
    [ContentProcessor(DisplayName = "Sprite Scene - FlatRedBall")]
    public class SceneFileProcessor : ContentProcessor<SpriteEditorSceneContent, SpriteEditorSceneContent>
    {
        #region Fields
        #endregion


        #region Properties

        [DefaultValue(false)]
        [DisplayName("Resize to Power of 2")]
        [Description("Whether the textures referenced by the Scene will be resized to a power of two.  This can improve compatability with some hardware, but can result in your texture looking blurry.")]
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

        #region Methods

        public SceneFileProcessor()
            : base()
        {
            GenerateMipmaps = true;

        }

        public override SpriteEditorSceneContent Process(SpriteEditorSceneContent input, ContentProcessorContext context)
        {

            //Debugger.Launch();

            #region Define the processors
            
            SpriteProcessor spriteProcessor = new SpriteProcessor(input.ScenePath);

            SpriteGridProcessor spriteGridProcessor = new SpriteGridProcessor(spriteProcessor);

            SpriteFrameProcessor spriteFrameProcessor =
                new SpriteFrameProcessor(input.ScenePath, spriteProcessor);

            TextProcessor textProcessor = new TextProcessor(input.ScenePath);

            BitmapTextureProcessor.ResizeToPowerOfTwo = ResizeToPowerOfTwo;
            BitmapTextureProcessor.TextureProcessorOutputFormat = TextureProcessorOutputFormat;
            BitmapTextureProcessor.GenerateMipmaps = GenerateMipmaps; // This doesn't seem to be working......why?

			context.Parameters.Add("ResizeToPowerOfTwo", ResizeToPowerOfTwo);
			//NM: The TextureProcessorOutputFormat seems to already have been set, shoudl we check
			//that and if not set it or is it always set at this point?
			//context.Parameters.Add("TextureProcessorOutputFormat", TextureProcessorOutputFormat);
			context.Parameters.Add("GenerateMipmaps", GenerateMipmaps);

            #endregion
           

            foreach (SpriteSaveContent s in input.SpriteList)
            {
                spriteProcessor.Process(s, context);
            }

            
            foreach (SpriteGridSaveContent sgs in input.SpriteGridList)
            {
                sgs.mFileName = input.ScenePath + @"\test.test"; // the filename is not important - only the directory
                spriteGridProcessor.Process(sgs, context);
            }

            foreach (SpriteFrameSaveContent spriteFrameSave in input.SpriteFrameSaveList)
            {
                spriteFrameProcessor.Process(spriteFrameSave, context);
            }

            foreach (TextSaveContent textSaveContent in input.TextSaveList)
            {
                textProcessor.Process(textSaveContent, context);
            // eventually will have to process TextSaves for font loading.
            }

            if (context.TargetPlatform == TargetPlatform.Xbox360)
            {
                input.ScenePath = FileManager.MakeRelative(input.ScenePath, FileManager.MakeRelative(FileManager.CurrentDirectory + "../"));
            }
            

            return input;
        }
        
        public static ExternalReference<SpriteEditorSceneContent> CreateAndBuildExternalReference(
            string name, ContentProcessorContext context)
        {
            ExternalReference<SpriteEditorSceneContent> returnObject =
                                new ExternalReference<FlatRedBall.Content.SpriteEditorSceneContent>(name);

            return context.BuildAsset<SpriteEditorSceneContent, SpriteEditorSceneContent>(
                returnObject, typeof(SceneFileProcessor).Name);
        }


        #endregion
    }
}
