using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Saves;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Xml.Serialization;

namespace FlatRedBall.Content.Gui
{
    public class GuiSkinSaveContent : GuiSkinSave
    {
        #region WindowSkinSaveContent

        public class WindowSkinSaveContent : WindowSkinSave
        {
            [XmlIgnore]
            public TextureContent TextureContent;

            //public static T FromWindowSkin<T>(WindowSkin windowSkin) where T : WindowSkinSave, new()
            //{
            //    T windowSkinSave = new T();

            //    if (windowSkin.Texture != null)
            //    {
            //        windowSkinSave.Texture = windowSkin.Texture.Name;
            //    }

            //    if (windowSkin.MoveBarTexture != null)
            //    {
            //        windowSkinSave.MoveBarTexture = windowSkin.MoveBarTexture.Name;
            //    }

            //    windowSkinSave.SpriteBorderWidth = windowSkin.SpriteBorderWidth;
            //    windowSkinSave.TextureBorderWidth = windowSkin.TextureBorderWidth;
            //    windowSkinSave.BorderSides = windowSkinSave.BorderSides;

            //    windowSkinSave.MoveBarSpriteBorderWidth = windowSkin.MoveBarSpriteBorderWidth;
            //    windowSkinSave.MoveBarTextureBorderWidth = windowSkin.MoveBarTextureBorderWidth;
            //    windowSkinSave.MoveBarBorderSides = windowSkinSave.MoveBarBorderSides;

            //    return windowSkinSave;
            //}

            //public T ToWindowSkin<T>(string contentManagerName) where T : WindowSkin, new()
            //{
            //    T windowSkin = new T();

            //    if (string.IsNullOrEmpty(Texture) == false)
            //    {
            //        windowSkin.Texture = FlatRedBallServices.Load<Texture2D>(
            //            Texture, contentManagerName);
            //    }
            //    if (string.IsNullOrEmpty(MoveBarTexture) == false)
            //    {
            //        windowSkin.MoveBarTexture = FlatRedBallServices.Load<Texture2D>(
            //            MoveBarTexture, contentManagerName);
            //    }

            //    windowSkin.SpriteBorderWidth = SpriteBorderWidth;
            //    windowSkin.TextureBorderWidth = TextureBorderWidth;
            //    windowSkin.BorderSides = BorderSides;

            //    windowSkin.MoveBarSpriteBorderWidth = MoveBarSpriteBorderWidth;
            //    windowSkin.MoveBarTextureBorderWidth = MoveBarTextureBorderWidth;
            //    windowSkin.MoveBarBorderSides = MoveBarBorderSides;

            //    return windowSkin;
            //}
        }

        #endregion

        #region ButtonSkinSave

        public class ButtonSkinSaveContent : WindowSkinSaveContent
        {
            public BitmapFontSave BitmapFontSave;
            public float TextSpacing = 1;
            public float TextScale = 1;

            public static T FromButtonSkin<T>(ButtonSkin buttonSkin) where T : ButtonSkinSave, new()
            {
                T buttonSkinSave = WindowSkinSave.FromWindowSkin<T>(buttonSkin);

                if (buttonSkin.Font != null)
                {
                    buttonSkinSave.BitmapFontSave = BitmapFontSave.FromBitmapFont(buttonSkin.Font);
                }

                buttonSkinSave.TextSpacing = buttonSkin.TextSpacing;
                buttonSkinSave.TextScale = buttonSkin.TextScale;

                return buttonSkinSave;
            }

            public override void MakeRelative(string directory)
            {
                base.MakeRelative(directory);

                if (BitmapFontSave != null)
                {
                    BitmapFontSave.MakeRelative(directory);
                }
            }

            public T ToButtonSkin<T>(string contentManagerName) where T : ButtonSkin, new()
            {
                T buttonSkin = base.ToWindowSkin<T>(contentManagerName);

                if (BitmapFontSave != null)
                {
                    buttonSkin.Font = BitmapFontSave.ToBitmapFont(contentManagerName);
                }

                buttonSkin.TextScale = TextScale;
                buttonSkin.TextSpacing = TextSpacing;

                return buttonSkin;
            }
        }

        #endregion

        #region TextBoxSkinSave

        public class TextBoxSkinSave : WindowSkinSave
        {
            public BitmapFontSave BitmapFontSave;
            public float TextSpacing = 1;
            public float TextScale = 1;

            public static T FromTextBoxSkin<T>(TextBoxSkin textBoxSkin) where T : TextBoxSkinSave, new()
            {
                T textBoxSkinSave = WindowSkinSave.FromWindowSkin<T>(textBoxSkin);

                if (textBoxSkin.Font != null)
                {
                    textBoxSkinSave.BitmapFontSave = BitmapFontSave.FromBitmapFont(textBoxSkin.Font);
                }
                textBoxSkinSave.TextSpacing = textBoxSkin.TextSpacing;
                textBoxSkinSave.TextScale = textBoxSkin.TextScale;

                return textBoxSkinSave;
            }

            public override void MakeRelative(string directory)
            {
                base.MakeRelative(directory);

                if (BitmapFontSave != null)
                {
                    BitmapFontSave.MakeRelative(directory);
                }
            }

            public T ToTextBoxSkin<T>(string contentManagerName) where T : TextBoxSkin, new()
            {
                T textBoxSkin = base.ToWindowSkin<T>(contentManagerName);

                if (BitmapFontSave != null)
                {
                    textBoxSkin.Font = BitmapFontSave.ToBitmapFont(contentManagerName);
                }

                textBoxSkin.TextScale = TextScale;
                textBoxSkin.TextSpacing = TextSpacing;

                return textBoxSkin;
            }
        }

        #endregion

        #region HighlightBarSkinSave

        public class HighlightBarSkinSave : WindowSkinSave
        {
            public float HighlightBarHorizontalBuffer = 1;
            public float ScaleY = 1;

            public static T FromHighlightBarSkin<T>(HighlightBarSkin highlightBarSkin) where T : HighlightBarSkinSave, new()
            {
                T highlightSkinSave = WindowSkinSave.FromWindowSkin<T>(highlightBarSkin);

                highlightSkinSave.HighlightBarHorizontalBuffer = highlightBarSkin.HighlightBarHorizontalBuffer;
                highlightSkinSave.ScaleY = highlightBarSkin.ScaleY;

                return highlightSkinSave;
            }

            public T ToHighlightBarSkin<T>(string contentManager) where T : HighlightBarSkin, new()
            {
                T highlightBarSkin = base.ToWindowSkin<T>(contentManager);

                highlightBarSkin.HighlightBarHorizontalBuffer = HighlightBarHorizontalBuffer;
                highlightBarSkin.ScaleY = ScaleY;

                return highlightBarSkin;
            }

        }

        #endregion

        #region ListBoxSkinSave

        public class ListBoxSkinSave : WindowSkinSave
        {
            public BitmapFontSave BitmapFontSave;

            public float TextSpacing = 1;
            public float TextScale = 1;
            public float DistanceBetweenLines = 2.4f;
            public float FirstItemDistanceFromTop = 2.4f;

            public float Red;
            public float Green;
            public float Blue;

            public HighlightBarSkinSave HighlightBarSkinSave;

            public static T FromListBoxSkin<T>(ListBoxSkin listBoxSkin) where T : ListBoxSkinSave, new()
            {
                T listBoxSkinSave = WindowSkinSave.FromWindowSkin<T>(listBoxSkin);

                if (listBoxSkin.Font != null)
                {
                    listBoxSkinSave.BitmapFontSave = BitmapFontSave.FromBitmapFont(listBoxSkin.Font);
                }
                listBoxSkinSave.TextSpacing = listBoxSkin.TextSpacing;
                listBoxSkinSave.TextScale = listBoxSkin.TextScale;
                listBoxSkinSave.DistanceBetweenLines = listBoxSkin.DistanceBetweenLines;
                listBoxSkinSave.FirstItemDistanceFromTop = listBoxSkin.FirstItemDistanceFromTop;

                listBoxSkinSave.Red = listBoxSkin.Red;
                listBoxSkinSave.Blue = listBoxSkin.Blue;
                listBoxSkinSave.Green = listBoxSkin.Green;

                listBoxSkinSave.HighlightBarSkinSave =
                    HighlightBarSkinSave.FromHighlightBarSkin<HighlightBarSkinSave>(listBoxSkin.HighlightBarSkin);

                return listBoxSkinSave;
            }

            public override void MakeRelative(string directory)
            {
                base.MakeRelative(directory);

                if (BitmapFontSave != null)
                {
                    BitmapFontSave.MakeRelative(directory);
                }

                HighlightBarSkinSave.MakeRelative(directory);
            }

            public T ToListBoxSkin<T>(string contentManagerName) where T : ListBoxSkin, new()
            {
                T listBoxSkin = base.ToWindowSkin<T>(contentManagerName);

                if (BitmapFontSave != null)
                {
                    listBoxSkin.Font = BitmapFontSave.ToBitmapFont(contentManagerName);
                }

                listBoxSkin.TextScale = TextScale;
                listBoxSkin.TextSpacing = TextSpacing;
                listBoxSkin.DistanceBetweenLines = DistanceBetweenLines;
                listBoxSkin.FirstItemDistanceFromTop = FirstItemDistanceFromTop;

                listBoxSkin.Red = Red;
                listBoxSkin.Blue = Blue;
                listBoxSkin.Green = Green;

                listBoxSkin.HighlightBarSkin =
                    HighlightBarSkinSave.ToHighlightBarSkin<HighlightBarSkin>(contentManagerName);

                return listBoxSkin;
            }
        }

        #endregion

        #region ScrollBarSkinSave

        public class ScrollBarSkinSave : WindowSkinSave
        {
            public ButtonSkinSave UpButtonSkinSave = new ButtonSkinSave();
            public ButtonSkinSave UpButtonDownSkinSave = new ButtonSkinSave();

            public ButtonSkinSave DownButtonSkinSave = new ButtonSkinSave();
            public ButtonSkinSave DownButtonDownSkinSave = new ButtonSkinSave();

            public WindowSkinSave PositionBarSkinSave = new WindowSkinSave();

            public static T FromScrollBarSkin<T>(ScrollBarSkin scrollBarSkin) where T : ScrollBarSkinSave, new()
            {
                T scrollBarSkinSave = WindowSkinSave.FromWindowSkin<T>(scrollBarSkin);

                scrollBarSkinSave.UpButtonSkinSave = ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(scrollBarSkin.UpButtonSkin);
                scrollBarSkinSave.UpButtonDownSkinSave = ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(scrollBarSkin.UpButtonDownSkin);

                scrollBarSkinSave.DownButtonSkinSave = ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(scrollBarSkin.DownButtonSkin);
                scrollBarSkinSave.DownButtonDownSkinSave = ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(scrollBarSkin.DownButtonDownSkin);

                scrollBarSkinSave.PositionBarSkinSave = WindowSkinSave.FromWindowSkin<WindowSkinSave>(scrollBarSkin.PositionBarSkin);

                return scrollBarSkinSave;
            }

            public override void MakeRelative(string directory)
            {
                base.MakeRelative(directory);

                UpButtonSkinSave.MakeRelative(directory);
                UpButtonDownSkinSave.MakeRelative(directory);

                DownButtonSkinSave.MakeRelative(directory);
                DownButtonDownSkinSave.MakeRelative(directory);

                PositionBarSkinSave.MakeRelative(directory);
            }

            public T ToScrollBarSkin<T>(string contentManagerName) where T : ScrollBarSkin, new()
            {
                T scrollBarSkin = base.ToWindowSkin<T>(contentManagerName);

                scrollBarSkin.UpButtonSkin = UpButtonSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);
                scrollBarSkin.UpButtonDownSkin = UpButtonDownSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);

                scrollBarSkin.DownButtonSkin = DownButtonSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);
                scrollBarSkin.DownButtonDownSkin = DownButtonDownSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);

                scrollBarSkin.PositionBarSkin = PositionBarSkinSave.ToWindowSkin<WindowSkin>(contentManagerName);

                return scrollBarSkin;
            }
        }

        #endregion
    }
}
