using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Content.Scene;
using FlatRedBall.ManagedSpriteGroups;

using SpriteFrame = FlatRedBall.ManagedSpriteGroups.SpriteFrame;
using FlatRedBall.Graphics;
using System.Globalization;

namespace FlatRedBall.Content.SpriteFrame
{
    public class SpriteFrameSave : SpriteFrameSaveBase<SpriteSave>
    {

        #region Methods

        public static SpriteFrameSave FromSpriteFrame(FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame)
        {
            SpriteFrameSave spriteFrameSave = new SpriteFrameSave();

            spriteFrameSave.ParentSprite = 
                SpriteSave.FromSprite(spriteFrame);

            if (spriteFrame.CenterSprite != null && SpriteManager.ZBufferedSprites.Contains(spriteFrame.CenterSprite))
            {
                spriteFrameSave.ParentSprite.Ordered = false;
            }

            spriteFrameSave.BorderSides = (int)spriteFrame.Borders;

            spriteFrameSave.TextureBorderWidth = spriteFrame.TextureBorderWidth;
            spriteFrameSave.SpriteBorderWidth = spriteFrame.SpriteBorderWidth;
            return spriteFrameSave;
        }

        public FlatRedBall.ManagedSpriteGroups.SpriteFrame ToSpriteFrame(string contentManagerName)
        {
            Texture2D textureToUse = null;

#if !MONODROID
            if (ParentSprite.mTextureInstance != null)
            {
                textureToUse = ParentSprite.mTextureInstance;
            }
            else
            {
#endif
                textureToUse =
                    FlatRedBallServices.Load<Texture2D>(ParentSprite.Texture, contentManagerName);

#if !MONODROID
            }
#endif

                FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame =
                new FlatRedBall.ManagedSpriteGroups.SpriteFrame(
                    textureToUse,
                    (FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides)BorderSides);

            spriteFrame.Name = ParentSprite.Name;

            spriteFrame.Position = new Vector3(
                ParentSprite.X, ParentSprite.Y, ParentSprite.Z);

            spriteFrame.RelativePosition = new Vector3(
                ParentSprite.RelativeX, ParentSprite.RelativeY, ParentSprite.RelativeZ);

            spriteFrame.RotationX = ParentSprite.RotationX;
            spriteFrame.RotationY = ParentSprite.RotationY;
            spriteFrame.RotationZ = ParentSprite.RotationZ;

            spriteFrame.RelativeRotationX = ParentSprite.RelativeRotationX;
            spriteFrame.RelativeRotationY = ParentSprite.RelativeRotationY;
            spriteFrame.RelativeRotationZ = ParentSprite.RelativeRotationZ;

            spriteFrame.ScaleX = ParentSprite.ScaleX;
            spriteFrame.ScaleY = ParentSprite.ScaleY;
            
            spriteFrame.TextureBorderWidth = TextureBorderWidth;
            spriteFrame.SpriteBorderWidth = SpriteBorderWidth;

            spriteFrame.Visible = ParentSprite.Visible;


            spriteFrame.Animate = ParentSprite.Animate;
            SpriteSave.SetRuntimeAnimationChain(
                contentManagerName, 
                spriteFrame, 
#if !MONODROID
                ParentSprite.mAnimationChainListInstance,
#else
                null,
#endif
                ParentSprite.CurrentChain, ParentSprite.AnimationChains, ParentSprite.AnimationChainsFile);


			float valueToDivideBy = 255 / GraphicalEnumerations.MaxColorComponentValue;


			spriteFrame.Red = ParentSprite.TintRed / valueToDivideBy;
			spriteFrame.Green = ParentSprite.TintGreen / valueToDivideBy;
			spriteFrame.Blue = ParentSprite.TintBlue / valueToDivideBy;

			spriteFrame.Alpha = (255 - ParentSprite.Fade) / valueToDivideBy;

			spriteFrame.BlendOperation = GraphicalEnumerations.TranslateBlendOperation(ParentSprite.BlendOperation);

			GraphicalEnumerations.SetColors(spriteFrame, ParentSprite.TintRed, ParentSprite.TintGreen, ParentSprite.TintBlue, ParentSprite.ColorOperation);

            if (spriteFrame.CenterSprite != null)
            {
                spriteFrame.CenterSprite.mOrdered = ParentSprite.Ordered;
            }

			return spriteFrame;
        }
        #endregion

        internal static SpriteFrameSave FromXElement(System.Xml.Linq.XElement element)
        {
            SpriteFrameSave sfs = new SpriteFrameSave();

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "BorderSides":
                        sfs.BorderSides = SceneSave.AsInt(subElement);
                        break;
                    case "ParentSprite":
                        sfs.ParentSprite = SpriteSave.FromXElement(subElement);
                        break;
                    case "SpriteBorderWidth":
                        sfs.SpriteBorderWidth = SceneSave.AsFloat(subElement);
                        break;
                    case "TextureBorderWidth":
                        sfs.TextureBorderWidth = SceneSave.AsFloat(subElement);
                        break;
                    default:
                        throw new NotImplementedException();

                        //break;
                }
            }

            return sfs;
        }
    }
}
