#if NET6_0_OR_GREATER
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Processors;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using AseSpriteSheet = AsepriteDotNet.SpriteSheet;
using AseTexture = AsepriteDotNet.Texture;
using AseTag = AsepriteDotNet.AnimationTag;
using AseAnimationFrame = AsepriteDotNet.AnimationFrame;
using AseRect = AsepriteDotNet.Common.Rectangle;
using FlatRedBall.Content.AnimationChain;
using AsepriteDotNet;

namespace FlatRedBall.Content.Aseprite
{
    public static class AsepriteFileExtensions
    {
        public static AnimationChainList ToAnimationChainList(this AsepriteFile file)
        {

            //  Generate the Aseprite Spritesheet
            AseSpriteSheet spriteSheet = SpriteSheetProcessor.Process(file);
            AseTexture aseTexture = spriteSheet.TextureAtlas.Texture;

            //  Create the MonoGame Texture2D from the Aseprite Spritesheet
            Texture2D texture = new Texture2D(FlatRedBallServices.GraphicsDevice, aseTexture.Size.Width, aseTexture.Size.Height);
            texture.SetData(aseTexture.Pixels.ToArray());

            //  Create an AnimationChainList based on the Aseprite spritesheet
            AnimationChainList list = new AnimationChainList(spriteSheet.Tags.Length);
            for (int i = 0; i < spriteSheet.Tags.Length; i++)
            {
                AseTag tag = spriteSheet.Tags[i];
                Graphics.Animation.AnimationChain chain = new Graphics.Animation.AnimationChain(tag.Frames.Length);
                for (int j = 0; j < tag.Frames.Length; j++)
                {
                    AseAnimationFrame aseFrame = tag.Frames[j];
                    var frame = new Graphics.Animation.AnimationFrame(texture, (float)aseFrame.Duration.TotalSeconds);

                    AseRect bounds = spriteSheet.TextureAtlas.Regions[aseFrame.FrameIndex].Bounds;
                    frame.TopCoordinate = bounds.Location.Y / (float)texture.Height;
                    frame.LeftCoordinate = bounds.Location.X / (float)texture.Width;
                    frame.BottomCoordinate = frame.TopCoordinate + (bounds.Size.Height / (float)texture.Height);
                    frame.RightCoordinate = frame.LeftCoordinate + (bounds.Size.Width / (float)texture.Width);

                    chain.Add(frame);
                }
                chain.Name = tag.Name;
                list.Add(chain);
            }

            return list;
        }

        public static AnimationChainListSave ToAnimationChainListSave(this AsepriteFile file)
        {
            AseSpriteSheet spriteSheet = SpriteSheetProcessor.Process(file);

            AnimationChainListSave list = new AnimationChainListSave();
            AseTexture aseTexture = spriteSheet.TextureAtlas.Texture;
            var width = aseTexture.Size.Width;
            var height = aseTexture.Size.Height;
            for (int i = 0; i < spriteSheet.Tags.Length; i++)
            {
                AseTag tag = spriteSheet.Tags[i];

                var chain = new AnimationChainSave();

                for (int j = 0; j < tag.Frames.Length; j++)
                {
                    AseAnimationFrame aseFrame = tag.Frames[j];
                    AnimationFrameSave animationFrameSave = new AnimationFrameSave();


                    AseRect bounds = spriteSheet.TextureAtlas.Regions[aseFrame.FrameIndex].Bounds;
                    animationFrameSave.TopCoordinate = bounds.Location.Y / (float)width;
                    animationFrameSave.LeftCoordinate = bounds.Location.X / (float)width;
                    animationFrameSave.BottomCoordinate = animationFrameSave.TopCoordinate + (bounds.Size.Height / (float)height);
                    animationFrameSave.RightCoordinate = animationFrameSave.LeftCoordinate + (bounds.Size.Width / (float)height);
                }

                chain.Name = tag.Name;
                list.AnimationChains.Add(chain);
            }
            return list;
        }

    }
}
#endif