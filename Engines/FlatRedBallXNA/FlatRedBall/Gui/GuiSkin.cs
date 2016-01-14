using System;
using System.Collections.Generic;
using System.Text;
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;

namespace FlatRedBall.Gui
{
    #region Skin classes

    #region WindowSkin
    public class WindowSkin
    {
        public Texture2D Texture;
        public float SpriteBorderWidth = .2f;
        public float TextureBorderWidth = .5f;
        public float Alpha = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
        public SpriteFrame.BorderSides BorderSides = SpriteFrame.BorderSides.All;

        public Texture2D MoveBarTexture;
        public float MoveBarSpriteBorderWidth = .1f;
        public float MoveBarTextureBorderWidth = .5f;
        public SpriteFrame.BorderSides MoveBarBorderSides = SpriteFrame.BorderSides.TopLeftRight;

        public ButtonSkin CloseButtonSkin;

		public void SetValuesFrom(SpriteFrame spriteFrame)
		{
			Texture = spriteFrame.Texture;
			SpriteBorderWidth = spriteFrame.SpriteBorderWidth;
			TextureBorderWidth = spriteFrame.TextureBorderWidth;
			Alpha = spriteFrame.Alpha;
			BorderSides = spriteFrame.Borders;
		}

    }

    #endregion

    #region ButtonSkin
    public class ButtonSkin : WindowSkin
    {
        public BitmapFont Font = TextManager.DefaultFont;
        public float TextSpacing = 1;
        public float TextScale = 1;
    }
    #endregion

    #region TextBoxSkin
    public class TextBoxSkin : WindowSkin
    {
        public BitmapFont Font = TextManager.DefaultFont;
        public float TextSpacing = 1;
        public float TextScale = 1;

		public float TextRed;
		public float TextGreen;
		public float TextBlue;

		public void SetValuesFrom(SpriteFrame baseFrame, Text text)
		{
			base.SetValuesFrom(baseFrame);
			TextSpacing = text.Spacing;
			TextScale = text.Scale;

			TextRed = text.Red;
			TextGreen = text.Green;
			TextBlue = text.Blue;

			Font = text.Font;

		}
    }
    #endregion

    #region HighlightBarSkin

    public class HighlightBarSkin : WindowSkin
    {
        public float HighlightBarHorizontalBuffer = 1;
        public float ScaleY = 1;
        public float HorizontalOffset = 0;
    }

    #endregion

    #region SeparatorSkin

    public class SeparatorSkin : WindowSkin
    {
        public float ScaleY = .35f;
        public float HorizontalOffset = 0;
        public int ExtraSeparators = 1;

    }

    #endregion

    #region ListBoxSkin
    public class ListBoxSkin : WindowSkin
    {
        public BitmapFont Font = TextManager.DefaultFont;
        public float TextSpacing = 1;
        public float TextScale = 1;
        public float DistanceBetweenLines = 2.4f;
        public float FirstItemDistanceFromTop = 2.4f;

        public float Red = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
        public float Green = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
        public float Blue = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

        public float DisabledRed = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue/2.0f;
        public float DisabledGreen = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue / 2.0f;
        public float DisabledBlue = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue / 2.0f;

        public float HorizontalTextOffset = 0;

        public HighlightBarSkin HighlightBarSkin = new HighlightBarSkin();

        public SeparatorSkin SeparatorSkin = new SeparatorSkin();

    }
    #endregion

    #region ScrollBarSkin
    public class ScrollBarSkin : WindowSkin
    {
        public ButtonSkin UpButtonSkin = new ButtonSkin();
        public ButtonSkin UpButtonDownSkin = new ButtonSkin();

        public ButtonSkin DownButtonSkin = new ButtonSkin();
        public ButtonSkin DownButtonDownSkin = new ButtonSkin();

        public WindowSkin PositionBarSkin = new WindowSkin();
    }
    #endregion

    #endregion

    public class GuiSkin
    {
        #region Fields

        internal WindowSkin mWindowSkin = new WindowSkin();
        internal ButtonSkin mButtonSkin = new ButtonSkin();
        internal ButtonSkin mButtonDownSkin = new ButtonSkin();
        internal TextBoxSkin mTextBoxSkin = new TextBoxSkin();
        internal ListBoxSkin mListBoxSkin = new ListBoxSkin();
        internal ScrollBarSkin mScrollBarSkin = new ScrollBarSkin();

        #endregion

        #region Properties

        public ButtonSkin ButtonSkin
        {
            get { return mButtonSkin; }
        }

        public ButtonSkin ButtonDownSkin
        {
            get { return mButtonDownSkin; }
        }

        public ListBoxSkin ListBoxSkin
        {
            get { return mListBoxSkin; }
        }

        public ScrollBarSkin ScrollBarSkin
        {
            get { return mScrollBarSkin; }
        }

        public TextBoxSkin TextBoxSkin
        {
            get { return mTextBoxSkin; }
        }

        public WindowSkin WindowSkin
        {
            get { return mWindowSkin; }
        }

        #endregion

        #region Methods

        // This needs a parameterless constructor so it can be instantiated
        // by the content pipeline.
        public GuiSkin() { }


        public void SetAllTextures(Texture2D textureToSet)
        {
            mWindowSkin.Texture = textureToSet;
            mWindowSkin.MoveBarTexture = textureToSet;

            if (mWindowSkin.CloseButtonSkin != null)
            {
                mWindowSkin.Texture = textureToSet;
            }

            mButtonSkin.Texture = textureToSet;
            mButtonDownSkin.Texture = textureToSet;

            mListBoxSkin.Texture = textureToSet;
            mListBoxSkin.HighlightBarSkin.Texture = textureToSet;
            mListBoxSkin.SeparatorSkin.Texture = textureToSet;
            
            mTextBoxSkin.Texture = textureToSet;

            mScrollBarSkin.Texture = textureToSet;
            mScrollBarSkin.UpButtonSkin.Texture = textureToSet;
            mScrollBarSkin.UpButtonDownSkin.Texture = textureToSet;
            mScrollBarSkin.DownButtonSkin.Texture = textureToSet;
            mScrollBarSkin.DownButtonDownSkin.Texture = textureToSet;
            mScrollBarSkin.PositionBarSkin.Texture = textureToSet;      
        }

        #endregion
    }
}
