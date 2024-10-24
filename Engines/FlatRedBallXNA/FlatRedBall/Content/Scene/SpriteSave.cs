using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Attributes;
using FlatRedBall.Gui;

using FlatRedBall.Graphics.Texture;
using FlatRedBall.Input;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using System.Globalization;
using System.Xml.Linq;

namespace FlatRedBall.Content.Scene
{
#if !UWP

    [Serializable]
#endif
    public class SpriteSave : SpriteSaveBase
    {
        private static bool AsBool(System.Xml.Linq.XElement subElement)
        {
            return bool.Parse(subElement.Value);
        }

        private static float AsFloat(System.Xml.Linq.XElement subElement)
        {
            if (subElement.Value == "INF")
            {
                return float.PositiveInfinity;
            }
            else
            {
                return float.Parse(subElement.Value, CultureInfo.InvariantCulture);
            }
        }

        private static int AsInt(System.Xml.Linq.XElement subElement)
        {
            return int.Parse(subElement.Value, CultureInfo.InvariantCulture);
        }

        #region Fields

        [ExternalInstance]
        internal Texture2D mTextureInstance;

        [ExternalInstance]
        internal AnimationChainList mAnimationChainListInstance;


        [XmlIgnore]
        public AnimationChainListSave AnimationChains;

        #endregion

        #region Methods

        public SpriteSave()
        {
            // Don't put any defaults here!
        }

        public static SpriteSave FromXElement(XElement element)
        {
            SpriteSave spriteSave = new SpriteSave();

            foreach (var item in element.Attributes())
            {
                switch (item.Name.LocalName)
                {
                    case "TextureAddressMode":
                        spriteSave.TextureAddressModeAsString = item.Value;
                        break;
                    default:
                        throw new NotImplementedException("Unknown SpriteSave attribute: " + item.Name.LocalName);

                }
            }
            
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "Active":
                        spriteSave.Active = AsBool(subElement);
                        break;
                    case "Animate":
                        spriteSave.Animate = AsBool(subElement);
                        break;
                    case "AnimationChainsFile":
                        spriteSave.AnimationChainsFile = subElement.Value;
                        break;
                    case "BlendOperation":
                        spriteSave.BlendOperation = subElement.Value;
                        break;
                    case "BottomTextureCoordinate":
                        spriteSave.BottomTextureCoordinate = AsFloat(subElement);
                        break;

                    case "ColorOperation":
                        spriteSave.ColorOperation = subElement.Value;
                        break;

                    case "ConstantPixelSize":
                        spriteSave.ConstantPixelSize = AsFloat(subElement);
                        break;
                    case "CurrentChain":
                        spriteSave.CurrentChain = AsInt(subElement);
                        break;

                    case "Fade":
                        spriteSave.Fade = AsFloat(subElement);
                        break;
                    case "FadeRate":
                        spriteSave.FadeRate = AsFloat(subElement);
                        break;
                    case "FlipHorizontal":
                        spriteSave.FlipHorizontal = AsBool(subElement);
                        break;

                    case "FlipVertical":
                        spriteSave.FlipVertical = AsBool(subElement);
                        break;
                    case "LeftTextureCoordinate":
                        spriteSave.LeftTextureCoordinate = AsFloat(subElement);
                        break;
                    case "Name":
                        spriteSave.Name = subElement.Value;
                        break;
                    case "Ordered":
                        spriteSave.Ordered = AsBool(subElement);
                        break;
                    case "Parent":
                        spriteSave.Parent = subElement.Value;
                        break;
                    case "RightTextureCoordinate":
                        spriteSave.RightTextureCoordinate = AsFloat(subElement);
                        break;
                    case "RelativeX":
                        spriteSave.RelativeX = AsFloat(subElement);
                        break;
                    case "RelativeY":
                        spriteSave.RelativeY = AsFloat(subElement);
                        break;
                    case "RelativeZ":
                        spriteSave.RelativeZ = AsFloat(subElement);
                        break;


                    case "RelativeRotationX":
                        spriteSave.RelativeRotationX = AsFloat(subElement);
                        break;
                    case "RelativeRotationY":
                        spriteSave.RelativeRotationY = AsFloat(subElement);
                        break;
                    case "RelativeRotationZ":
                        spriteSave.RelativeRotationZ = AsFloat(subElement);
                        break;
                    

                    case "RotationX":
                        spriteSave.RotationX = AsFloat(subElement);
                        break;
                    case "RotationY":
                        spriteSave.RotationY = AsFloat(subElement);
                        break;
                    case "RotationZ":
                        spriteSave.RotationZ = AsFloat(subElement);
                        break;
                    case "RotationZVelocity":
                        spriteSave.RotationZVelocity = AsFloat(subElement);
                        break;
                    case "ScaleX":
                        spriteSave.ScaleX = AsFloat(subElement);
                        break;
                    case "ScaleY":
                        spriteSave.ScaleY = AsFloat(subElement);
                        break;
                    case "ScaleXVelocity":
                        spriteSave.ScaleXVelocity = AsFloat(subElement);
                        break;
                    case "ScaleYVelocity":
                        spriteSave.ScaleYVelocity = AsFloat(subElement);
                        break;
                    case "TextureAddressMode":
                        spriteSave.TextureAddressMode = (TextureAddressMode)Enum.Parse(typeof(TextureAddressMode),  subElement.Value, true);
                        break;
                    case "Texture":
                        spriteSave.Texture = subElement.Value;
                        break;
                    case "TintRed":
                        spriteSave.TintRed = AsFloat(subElement);
                        break;
                    case "TintGreen":
                        spriteSave.TintGreen = AsFloat(subElement);
                        break;
                    case "TintBlue":
                        spriteSave.TintBlue = AsFloat(subElement);
                        break;
                    case "TintRedRate":
                        spriteSave.TintRedRate = AsFloat(subElement);
                        break;
                    case "TintGreenRate":
                        spriteSave.TintGreenRate = AsFloat(subElement);
                        break;
                    case "TintBlueRate":
                        spriteSave.TintBlueRate = AsFloat(subElement);
                        break;

                    case "TopTextureCoordinate":
                        spriteSave.TopTextureCoordinate = AsFloat(subElement);
                        break;
                    case "Visible":
                        spriteSave.Visible = AsBool(subElement);
                        break;
                    
                    case "XAcceleration":
                        spriteSave.XAcceleration = AsFloat(subElement);
                        break;
                    case "YAcceleration":
                        spriteSave.YAcceleration = AsFloat(subElement);
                        break;
                    case "ZAcceleration":
                        spriteSave.ZAcceleration = AsFloat(subElement);
                        break;
                    
                    case "XVelocity":
                        spriteSave.XVelocity = AsFloat(subElement);
                        break;
                    case "YVelocity":
                        spriteSave.YVelocity = AsFloat(subElement);
                        break;
                    case "ZVelocity":
                        spriteSave.ZVelocity = AsFloat(subElement);
                        break;
                    case "X":
                        spriteSave.X = AsFloat(subElement);
                        break;
                    case "Y":
                        spriteSave.Y = AsFloat(subElement);
                        break;
                    case "Z":
                        spriteSave.Z = AsFloat(subElement);
                        break;
                    default:
                        throw new NotImplementedException();

                        //break;
                }
            }


            return spriteSave;
        }

        public static SpriteSave FromSprite<T>(T spriteToCreateSaveFrom) where T : PositionedObject, IColorable,
            ICursorSelectable, IReadOnlyScalable, IAnimationChainAnimatable
        {
            SpriteSave spriteSave = new SpriteSave();

            spriteSave.SetFrom<T>(spriteToCreateSaveFrom);

            return spriteSave;
        }

        public void SetFrom<T>(T spriteToCreateSaveFrom) where T : PositionedObject, IColorable, ICursorSelectable, IReadOnlyScalable, IAnimationChainAnimatable
        {
            X = spriteToCreateSaveFrom.X;
            Y = spriteToCreateSaveFrom.Y;

            // Coordinates will be inverted depending on the CoordinateSystem
            // when the scene is loaded.
            Z = spriteToCreateSaveFrom.Z;

            RotationX = spriteToCreateSaveFrom.RotationX;
            RotationY = spriteToCreateSaveFrom.RotationY;
            RotationZ = spriteToCreateSaveFrom.RotationZ;
            RotationZVelocity = spriteToCreateSaveFrom.RotationZVelocity;

            ScaleX = spriteToCreateSaveFrom.ScaleX;
            ScaleY = spriteToCreateSaveFrom.ScaleY;

            if (spriteToCreateSaveFrom is IScalable)
            {

                ScaleXVelocity = ((IScalable)(spriteToCreateSaveFrom)).ScaleXVelocity;
                ScaleYVelocity = ((IScalable)(spriteToCreateSaveFrom)).ScaleYVelocity;
            }

            XVelocity = spriteToCreateSaveFrom.XVelocity;
            YVelocity = spriteToCreateSaveFrom.YVelocity;
            ZVelocity = spriteToCreateSaveFrom.ZVelocity;

            XAcceleration = spriteToCreateSaveFrom.XAcceleration;
            YAcceleration = spriteToCreateSaveFrom.YAcceleration;
            ZAcceleration = spriteToCreateSaveFrom.ZAcceleration;

            RelativeX = spriteToCreateSaveFrom.RelativeX;
            RelativeY = spriteToCreateSaveFrom.RelativeY;
            RelativeZ = spriteToCreateSaveFrom.RelativeZ;

            RelativeRotationX = spriteToCreateSaveFrom.RelativeRotationX;
            RelativeRotationY = spriteToCreateSaveFrom.RelativeRotationY;
            RelativeRotationZ = spriteToCreateSaveFrom.RelativeRotationZ;

            if (spriteToCreateSaveFrom.Name != null)
                Name = spriteToCreateSaveFrom.Name;
            Animate = spriteToCreateSaveFrom.Animate;

            if (spriteToCreateSaveFrom.CurrentChain == null)
            {
                CurrentChain = -1;
            }
            else
            {
                CurrentChain = spriteToCreateSaveFrom.CurrentChain.IndexInLoadedAchx;

                // It's possible that the chain is just a GIF.  In that case, it would have a -1 index
                // in the loaded Achx.  But we still want this thing to play, so we'll just assume an index
                // of 0
                if (CurrentChain == -1)
                {
                    CurrentChain = 0;
                }
            }

            if (spriteToCreateSaveFrom.AnimationChains != null)
            {
                if (string.IsNullOrEmpty(spriteToCreateSaveFrom.AnimationChains.Name) && spriteToCreateSaveFrom.AnimationChains.Count != 0)
                {
                    if (!string.IsNullOrEmpty(spriteToCreateSaveFrom.AnimationChains[0].ParentAchxFileName))
                    {

                        AnimationChainsFile = spriteToCreateSaveFrom.AnimationChains[0].ParentAchxFileName;
                    }
                    else
                    {
                        AnimationChainsFile = spriteToCreateSaveFrom.AnimationChains[0].ParentGifFileName;

                    }
                }
                else
                {
                    AnimationChainsFile = spriteToCreateSaveFrom.AnimationChains.Name;
                }

            }



            if (spriteToCreateSaveFrom.Parent != null)
            {
                Parent = spriteToCreateSaveFrom.Parent.Name;
            }

            BlendOperation =
                GraphicalEnumerations.BlendOperationToFlatRedBallMdxString(spriteToCreateSaveFrom.BlendOperation);

            Fade = (1 - spriteToCreateSaveFrom.Alpha) * 255.0f;
            FadeRate = -spriteToCreateSaveFrom.AlphaRate * 255.0f;

            TintRed = spriteToCreateSaveFrom.Red * 255.0f;
            TintGreen = spriteToCreateSaveFrom.Green * 255.0f;
            TintBlue = spriteToCreateSaveFrom.Blue * 255.0f;

            TintRedRate = spriteToCreateSaveFrom.RedRate * 255.0f;
            TintGreenRate = spriteToCreateSaveFrom.GreenRate * 255.0f;
            TintBlueRate = spriteToCreateSaveFrom.BlueRate * 255.0f;

            ColorOperation =
                GraphicalEnumerations.ColorOperationToFlatRedBallMdxString(spriteToCreateSaveFrom.ColorOperation);

            if (spriteToCreateSaveFrom is Sprite)
            {
                Sprite asSprite = spriteToCreateSaveFrom as Sprite;
                Visible = asSprite.Visible;

                if (asSprite.Texture != null)
                {
                    // Vic says:  On November 11, 2009 I found an interesting
                    // bug.  I was working on the TileEditor which created a Scene
                    // that contained animated Sprites.  These Sprites used an AnimationChain
                    // that was created by loading a .GIF.  That means that each individual texture
                    // in the AnimationChain is not its own file, but rather a Texture created from the
                    // GIF.  This means that the Name of the Texture is not a valid Name.

                    // But this makes me think - if a Sprite has an AnimationChain and it's currently Animated,
                    // why do we even need to save off the Texture?  Instead, let's just not save it at all.  This
                    // will save a little bit of space and could also prevent some loading-related bugs.
                    if (string.IsNullOrEmpty(AnimationChainsFile) || (asSprite.CurrentChain == null || !asSprite.Animate))
                    {
                        Texture = asSprite.Texture.SourceFile();
                    }
                }

                ConstantPixelSize = asSprite.PixelSize;

                TopTextureCoordinate = asSprite.TopTextureCoordinate;
                BottomTextureCoordinate = asSprite.BottomTextureCoordinate;
                LeftTextureCoordinate = asSprite.LeftTextureCoordinate;
                RightTextureCoordinate = asSprite.RightTextureCoordinate;

                FlipHorizontal = asSprite.FlipHorizontal;
                FlipVertical = asSprite.FlipVertical;

                TextureAddressMode = asSprite.TextureAddressMode;

                // If the Sprite is not part of the ZBufferedSprites, then it's ordered.
                Ordered = !asSprite.ListsBelongingTo.Contains(SpriteManager.mZBufferedSprites);
            }
            else if (spriteToCreateSaveFrom is FlatRedBall.ManagedSpriteGroups.SpriteFrame)
            {
                FlatRedBall.ManagedSpriteGroups.SpriteFrame asSpriteFrame = spriteToCreateSaveFrom as
                    FlatRedBall.ManagedSpriteGroups.SpriteFrame;

                Visible = asSpriteFrame.Visible;

                Texture = asSpriteFrame.Texture.Name;

            }


        }

        public Sprite ToSprite(string contentManagerName)
        {
            return ToSprite<Sprite>(contentManagerName);
        }

        public T ToSprite<T>(string contentManagerName) where T : Sprite, new()
        {
            T sprite = new T();

            SetSprite(contentManagerName, sprite);

            return sprite;
        }

        public void SetSprite(string contentManagerName, Sprite sprite) 
        {
            SetSprite(contentManagerName, sprite, true);
        }

        public void SetSprite(string contentManagerName, Sprite sprite, bool setTextures) 
        {
            // Set Texture and PixelSize BEFORE setting Scale so that the Scale
            // overrides it if it's different.
            if (setTextures)
            {
                if (mTextureInstance != null)
                {
                    sprite.Texture = mTextureInstance;
                }

                else
                    // Sprites can have NULL textures as of April 18, 2009    
                    if (!string.IsNullOrEmpty(Texture))
                    {
                        sprite.Texture = FlatRedBallServices.Load<Texture2D>(Texture, contentManagerName);
                    }
            }
            sprite.X = X;
            sprite.Y = Y;
            sprite.Z = Z;

            sprite.RotationX = RotationX;
            sprite.RotationY = RotationY;
            sprite.RotationZ = RotationZ;
            sprite.RotationZVelocity = RotationZVelocity;


            sprite.PixelSize = ConstantPixelSize;
            sprite.TopTextureCoordinate = TopTextureCoordinate;
            sprite.BottomTextureCoordinate = BottomTextureCoordinate;
            sprite.LeftTextureCoordinate = LeftTextureCoordinate;
            sprite.RightTextureCoordinate = RightTextureCoordinate;
            // End of stuff to set before setting Scale

            sprite.ScaleX = ScaleX;
            sprite.ScaleXVelocity = ScaleXVelocity;
            sprite.ScaleY = ScaleY;
            sprite.ScaleYVelocity = ScaleYVelocity;

            sprite.XVelocity = XVelocity;
            sprite.YVelocity = YVelocity;
            sprite.ZVelocity = ZVelocity;

            sprite.XAcceleration = XAcceleration;
            sprite.YAcceleration = YAcceleration;
            sprite.ZAcceleration = ZAcceleration;

            sprite.RelativeX = RelativeX;
            sprite.RelativeY = RelativeY;
            sprite.RelativeZ = RelativeZ;

            sprite.RelativeRotationX = RelativeRotationX;
            sprite.RelativeRotationY = RelativeRotationY;
            sprite.RelativeRotationZ = RelativeRotationZ;

            sprite.Name = Name;
            sprite.Animate = Animate;

            if (setTextures)
            {
                SetRuntimeAnimationChain(contentManagerName, sprite,

 mAnimationChainListInstance,

 CurrentChain,
                    AnimationChains, AnimationChainsFile

                    );
            }

            float valueToDivideBy = 255 / GraphicalEnumerations.MaxColorComponentValue;

            sprite.Alpha = (255 - Fade) / valueToDivideBy;
            sprite.AlphaRate = -FadeRate / valueToDivideBy;
            sprite.BlendOperation = GraphicalEnumerations.TranslateBlendOperation(BlendOperation);

            sprite.RedRate = TintRedRate / valueToDivideBy;
            sprite.GreenRate = TintGreenRate / valueToDivideBy;
            sprite.BlueRate = TintBlueRate / valueToDivideBy;

            GraphicalEnumerations.SetColors(sprite, TintRed, TintGreen, TintBlue, ColorOperation);

            // If the Texture is null, we may want to use "Color" instead of "ColorTextureAlpha"

            if (sprite.Texture == null && sprite.ColorOperation == FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha)
            {
                sprite.ColorOperation = FlatRedBall.Graphics.ColorOperation.Color;
            }

            sprite.mOrdered = Ordered;

            sprite.Visible = Visible;

            sprite.TextureAddressMode = TextureAddressMode;

            sprite.FlipHorizontal = FlipHorizontal;
            sprite.FlipVertical = FlipVertical;
        }

        //public void SetSprite(TextureAtlas textureAtlas, Sprite sprite)
        //{
        //    SetSprite("", sprite);

        //    if (!string.IsNullOrEmpty(this.Texture))
        //    {
        //        var entry = textureAtlas.GetEntryFor(this.Texture);

        //        if (entry != null)
        //        {
        //            float left;
        //            float right;
        //            float top;
        //            float bottom;


        //            entry.FullToReduced(sprite.LeftTextureCoordinate, sprite.RightTextureCoordinate,
        //                sprite.TopTextureCoordinate, sprite.BottomTextureCoordinate,
        //                out left, out right, out top, out bottom);

        //            sprite.LeftTextureCoordinate = left;
        //            sprite.RightTextureCoordinate = right;
        //            sprite.TopTextureCoordinate = top;
        //            sprite.BottomTextureCoordinate = bottom;
                    
        //            sprite.Texture = textureAtlas.Texture;

        //        }
        //    }

        //    if (!string.IsNullOrEmpty(this.AnimationChainsFile))
        //    {

        //        if (string.IsNullOrEmpty(this.AnimationChainsFile) == false)
        //        {
        //            //AnimationChains = FlatRedBall.Content.AnimationChain.AnimationChainListSave.FromFile(AnimationChainsFile);
        //            AnimationChainListSave acls = AnimationChainListSave.FromFile(this.AnimationChainsFile);
        //            sprite.AnimationChains = acls.ToAnimationChainList(textureAtlas);
        //        }

        //        if (CurrentChain != -1)
        //        {
        //            // Now using the CurrentChainName property so it works with IAnimationChainAnimatable
        //            sprite.CurrentChainName = sprite.AnimationChains[CurrentChain].Name;
        //            //sprite.SetAnimationChain();
        //        }
        //    }


        //}

        internal static void SetRuntimeAnimationChain(string contentManagerName, IAnimationChainAnimatable sprite,
            AnimationChainList animationChainListInstance, int currentChain, AnimationChainListSave animationChains,
            string animationChainsFile
            )
        {
            if (animationChainListInstance != null)
            {
                if (animationChainListInstance != null)
                {
                    sprite.AnimationChains = animationChainListInstance; ;

                }

                if (currentChain != -1)
                {
                    // Now using the CurrentChainName property so it works with IAnimationChainAnimatable
                    sprite.CurrentChainName = sprite.AnimationChains[currentChain].Name;
                    //sprite.SetAnimationChain(sprite.AnimationChains[CurrentChain]);
                }
            }
            else if (animationChains != null || string.IsNullOrEmpty(animationChainsFile) == false)
            {

                if (animationChains != null && animationChains.FileName != null && animationChains.FileName != "")
                {
                    // load the AnimationChainArray here
                    //                    AnimationChains = new AnimationChainList(saveToSetFrom.animationChains.Name);
                    sprite.AnimationChains = animationChains.ToAnimationChainList(contentManagerName);
                    sprite.AnimationChains.Name = FlatRedBall.IO.FileManager.Standardize(animationChains.FileName);
                }
                else if (string.IsNullOrEmpty(animationChainsFile) == false)
                {
                    //AnimationChains = FlatRedBall.Content.AnimationChain.AnimationChainListSave.FromFile(AnimationChainsFile);
                    sprite.AnimationChains = FlatRedBallServices.Load<AnimationChainList>(
                        animationChainsFile, contentManagerName);
                }

                if (currentChain != -1)
                {
                    // Now using the CurrentChainName property so it works with IAnimationChainAnimatable
                    sprite.CurrentChainName = sprite.AnimationChains[currentChain].Name;
                    //sprite.SetAnimationChain();
                }
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ") " + Texture;
        }

        public void MakeRelative()
        {
            if (!string.IsNullOrEmpty(Texture))
            {
                Texture = FileManager.MakeRelative(Texture);
            }

            if (string.IsNullOrEmpty(AnimationChainsFile) == false)
            {
                AnimationChainsFile = FileManager.MakeRelative(AnimationChainsFile);
            }
        }

        public List<string> GetReferencedFiles()
        {
            List<string> referencedFiles = new List<string>();
            GetReferencedFiles(referencedFiles);
            return referencedFiles;
        }


        public void GetReferencedFiles(List<string> referencedFiles)
        {

            if (!string.IsNullOrEmpty(Texture) && !referencedFiles.Contains(Texture))
            {
                referencedFiles.Add(Texture);
            }

            if (!string.IsNullOrEmpty(AnimationChainsFile) && !referencedFiles.Contains(AnimationChainsFile))
            {
                referencedFiles.Add(AnimationChainsFile);
            }
        }

        #endregion
    }
}
