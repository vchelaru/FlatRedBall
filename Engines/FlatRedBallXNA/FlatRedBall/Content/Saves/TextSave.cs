using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.Attributes;

using Microsoft.Xna.Framework.Graphics;


namespace FlatRedBall.Content.Saves
{
    #region XML Docs
    /// <summary>
    /// An XML Serializable class representing the state of a Text.
    /// </summary>
    #endregion
    public class TextSave : TextSaveBase
    {
        #region Fields

#if !MONODROID
        [ExternalInstance]
        internal Texture2D mFontTextureInstance;
#endif

        internal string FontPatternText;

        #endregion

        #region Methods

        public static TextSave FromText(Text text)
        {
            TextSave textSave = new TextSave();
            textSave.X = text.Position.X;
            textSave.Y = text.Position.Y;
            textSave.Z = text.Position.Z;

            textSave.RotationX = text.RotationX;
            textSave.RotationY = text.RotationY;
            textSave.RotationZ = text.RotationZ;

            textSave.RelativeX = text.RelativePosition.X;
            textSave.RelativeY = text.RelativePosition.Y;
            textSave.RelativeZ = text.RelativePosition.Z;

            textSave.RelativeRotationX = text.RelativeRotationX;
            textSave.RelativeRotationY = text.RelativeRotationY;
            textSave.RelativeRotationZ = text.RelativeRotationZ;

            textSave.DisplayText = text.DisplayText;
            textSave.Name = text.Name;

            textSave.MaxWidth = text.MaxWidth;
            textSave.MaxWidthBehavior = text.MaxWidthBehavior;

            textSave.Scale = text.Scale;
            textSave.Spacing = text.Spacing;
            textSave.NewLineDistance = text.NewLineDistance;

            textSave.VerticalAlignment = text.VerticalAlignment;
            textSave.HorizontalAlignment = text.HorizontalAlignment;

            textSave.Visible = text.Visible;

            textSave.CursorSelectable = text.CursorSelectable;

            if (text.Parent != null)
            {
                textSave.Parent = text.Parent.Name;
            }

            if (text.Font != null && text.Font != TextManager.DefaultFont)
            {
                textSave.FontFile = text.Font.mFontFile;

                if (text.Font.mTextures.Length == 1)
                {

                    textSave.FontTexture = text.Font.Texture.Name;
                }
                else
                {
                    textSave.FontTexture = "";
                }
            }

            //spriteSave.Fade = (1 - spriteToCreateSaveFrom.Alpha) * 255.0f;
            //spriteSave.FadeRate = -spriteToCreateSaveFrom.AlphaRate * 255.0f;

            textSave.Red = text.Red * 255.0f;
            textSave.Green = text.Green * 255.0f;
            textSave.Blue = text.Blue * 255.0f;

            //spriteSave.TintRedRate = spriteToCreateSaveFrom.RedRate * 255.0f;
            //spriteSave.TintGreenRate = spriteToCreateSaveFrom.GreenRate * 255.0f;
            //spriteSave.TintBlueRate = spriteToCreateSaveFrom.BlueRate * 255.0f;

            textSave.ColorOperation =
                GraphicalEnumerations.ColorOperationToFlatRedBallMdxString(text.ColorOperation);
            
            return textSave;
        }


        public Text ToText(string contentManagerName)
        {
            Text text = new Text();

            text.X = X;
            text.Y = Y;
            text.Z = Z;

            text.RotationX = RotationX;
            text.RotationY = RotationY;
            text.RotationZ = RotationZ;

            text.RelativePosition.X = RelativeX;
            text.RelativePosition.Y = RelativeY;
            text.RelativePosition.Z = RelativeZ;

            text.RelativeRotationX = RelativeRotationX;
            text.RelativeRotationY = RelativeRotationY;
            text.RelativeRotationZ = RelativeRotationZ;

            text.DisplayText = DisplayText;
            text.Name = Name;

            text.Scale = Scale;
            text.Spacing = Spacing;
            text.NewLineDistance = NewLineDistance;

            text.MaxWidth = MaxWidth;
            text.MaxWidthBehavior = MaxWidthBehavior;

            text.VerticalAlignment = VerticalAlignment;
            text.HorizontalAlignment = HorizontalAlignment;

            text.Visible = Visible;

            text.CursorSelectable = CursorSelectable;


#if !MONODROID
            if (this.mFontTextureInstance != null)
            {
                BitmapFont bitmapFont = new BitmapFont(
                     mFontTextureInstance, FontPatternText);

                text.Font = bitmapFont;
            }
            else
#endif

            if (!string.IsNullOrEmpty(FontFile))
            {

                BitmapFont bitmapFont = null;

                if (!string.IsNullOrEmpty(FontTexture))
                {
                    bitmapFont = new BitmapFont(
                        FontTexture, FontFile, contentManagerName);
                }
                else
                {
                    bitmapFont = new BitmapFont(
                        FontFile, contentManagerName);
                }

                text.Font = bitmapFont;
            }
            else
            {
                text.Font = TextManager.DefaultFont;
            }


            //sprite.Alpha = (255 - Fade) / valueToDivideBy;
            //sprite.AlphaRate = -FadeRate / valueToDivideBy;
            //sprite.BlendOperation = GraphicalEnumerations.TranslateBlendOperation(BlendOperation);

            //sprite.RedRate = TintRedRate / valueToDivideBy;
            //sprite.GreenRate = TintGreenRate / valueToDivideBy;
            //sprite.BlueRate = TintBlueRate / valueToDivideBy;


            var colorOperation = ColorOperation;

#if MONODROID
            if (colorOperation == "SelectArg2")
            {
                colorOperation = "Modulate";
            }
#endif

            GraphicalEnumerations.SetColors(text, Red, Green, Blue, colorOperation);


            return text;
        }

        #endregion
    }
}
