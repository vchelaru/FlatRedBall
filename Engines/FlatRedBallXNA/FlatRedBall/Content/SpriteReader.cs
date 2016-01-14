using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.Scene;

namespace FlatRedBall.Content
{
    #region XML Docs
    /// <summary>
    /// Class used by FlatRedBall to read Sprites out of a .XNB file when used
    /// read in through the content pipeline.  This is used when deserializing Scenes.
    /// </summary>
    #endregion
    public class SpriteReader : ContentTypeReader<Sprite>
    {
        
        protected override Sprite Read(ContentReader input, Sprite existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            SpriteSave spriteSave = null;

            if (ObjectReader.UseReflection)
            {

                spriteSave = ObjectReader.ReadObject<SpriteSave>(input);

            }
            else
            {
                spriteSave = ReadUsingGeneratedCode(input);

                if (spriteSave.mTextureInstance != null)
                {
                    spriteSave.mTextureInstance.Name = spriteSave.Texture;
                }
            }
            existingInstance = spriteSave.ToSprite("");

            

            return existingInstance;
        }

        public static SpriteSave ReadUsingGeneratedCode(ContentReader input)
        {
            SpriteSave newObject = new SpriteSave();

            if (input.ReadBoolean())
                newObject.mTextureInstance = input.ReadExternalReference<Microsoft.Xna.Framework.Graphics.Texture2D>();
            if (input.ReadBoolean())
                newObject.mAnimationChainListInstance = input.ReadExternalReference<FlatRedBall.Graphics.Animation.AnimationChainList>();
            newObject.X = input.ReadSingle();
            newObject.Y = input.ReadSingle();
            newObject.Z = input.ReadSingle();
            newObject.XVelocity = input.ReadSingle();
            newObject.YVelocity = input.ReadSingle();
            newObject.ZVelocity = input.ReadSingle();
            newObject.XAcceleration = input.ReadSingle();
            newObject.YAcceleration = input.ReadSingle();
            newObject.ZAcceleration = input.ReadSingle();
            newObject.RotationX = input.ReadSingle();
            newObject.RotationY = input.ReadSingle();
            newObject.RotationZ = input.ReadSingle();
            newObject.RotationZVelocity = input.ReadSingle();
            newObject.ScaleX = input.ReadSingle();
            newObject.ScaleY = input.ReadSingle();
            newObject.ScaleXVelocity = input.ReadSingle();
            newObject.ScaleYVelocity = input.ReadSingle();
            newObject.RelativeX = input.ReadSingle();
            newObject.RelativeY = input.ReadSingle();
            newObject.RelativeZ = input.ReadSingle();
            newObject.RelativeRotationX = input.ReadSingle();
            newObject.RelativeRotationY = input.ReadSingle();
            newObject.RelativeRotationZ = input.ReadSingle();
            newObject.Fade = input.ReadSingle();
            newObject.FadeRate = input.ReadSingle();
            newObject.TintRed = input.ReadSingle();
            newObject.TintGreen = input.ReadSingle();
            newObject.TintBlue = input.ReadSingle();
            newObject.TintRedRate = input.ReadSingle();
            newObject.TintBlueRate = input.ReadSingle();
            newObject.TintGreenRate = input.ReadSingle();
            newObject.ColorOperation = input.ReadString();
            newObject.BlendOperation = input.ReadString();
            newObject.Name = input.ReadString();
            newObject.Parent = input.ReadString();
            newObject.Texture = input.ReadString();
            newObject.Animate = input.ReadBoolean();
            newObject.CurrentChain = input.ReadInt32();
            newObject.AnimationChainsFile = input.ReadString();
            newObject.Type = input.ReadString();
            newObject.Ordered = input.ReadBoolean();
            newObject.Active = input.ReadBoolean();
            newObject.ConstantPixelSize = input.ReadSingle();
            newObject.Visible = input.ReadBoolean();
            newObject.TopTextureCoordinate = input.ReadSingle();
            newObject.BottomTextureCoordinate = input.ReadSingle();
            newObject.LeftTextureCoordinate = input.ReadSingle();
            newObject.RightTextureCoordinate = input.ReadSingle();
            newObject.FlipHorizontal = input.ReadBoolean();
            newObject.FlipVertical = input.ReadBoolean();
            newObject.TextureAddressMode = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)Enum.ToObject(typeof(Microsoft.Xna.Framework.Graphics.TextureAddressMode), (int)input.ReadInt32());

            /////////////CUSTOM CODE
            if (newObject.mTextureInstance != null)
            {
                newObject.mTextureInstance.Name = newObject.Texture;
            }

            ///////////////END CUSTOM////////////

            return newObject;
        }

    }
}
