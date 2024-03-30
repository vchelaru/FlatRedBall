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
                    AnimationFrame frame = new AnimationFrame(texture, (float)aseFrame.Duration.TotalSeconds);

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
    }
}
