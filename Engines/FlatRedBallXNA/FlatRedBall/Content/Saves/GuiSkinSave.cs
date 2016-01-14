using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.IO;
using FlatRedBall.Graphics;
using System.Xml.Serialization;


#if FRB_XNA
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Content.Saves
{
    #region WindowSkinSave

    public class WindowSkinSave
    {
        public string Texture;
        public float SpriteBorderWidth = .2f;
        public float TextureBorderWidth = .5f;
        public float Alpha = 255;

        public FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides BorderSides = 
            FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides.All;

        public string MoveBarTexture;
        public float MoveBarSpriteBorderWidth = .1f;
        public float MoveBarTextureBorderWidth = .5f;
        public FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides MoveBarBorderSides = 
            FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides.TopLeftRight;


        public static T FromWindowSkin<T>(WindowSkin windowSkin) where T : WindowSkinSave, new()
        {
            T windowSkinSave = new T();

            if (windowSkin.Texture != null)
            {
                windowSkinSave.Texture = windowSkin.Texture.Name;
            }

            if (windowSkin.MoveBarTexture != null)
            {
                windowSkinSave.MoveBarTexture = windowSkin.MoveBarTexture.Name;
            }

            windowSkinSave.SpriteBorderWidth = windowSkin.SpriteBorderWidth;
            windowSkinSave.TextureBorderWidth = windowSkin.TextureBorderWidth;
            windowSkinSave.BorderSides = windowSkin.BorderSides;
            windowSkinSave.Alpha = windowSkin.Alpha;

            windowSkinSave.MoveBarSpriteBorderWidth = windowSkin.MoveBarSpriteBorderWidth;
            windowSkinSave.MoveBarTextureBorderWidth = windowSkin.MoveBarTextureBorderWidth;
            windowSkinSave.MoveBarBorderSides = windowSkin.MoveBarBorderSides;

            return windowSkinSave;
        }

        public virtual void MakeRelative(string directory)
        {
            if (!string.IsNullOrEmpty(Texture) &&
                FileManager.IsRelative(Texture) == false)
            {
                Texture = FileManager.MakeRelative(Texture, directory);
            }

            if (!string.IsNullOrEmpty(MoveBarTexture) &&
                    FileManager.IsRelative(MoveBarTexture) == false)
            {
                MoveBarTexture = FileManager.MakeRelative(MoveBarTexture, directory);
            }
        }

        public T ToWindowSkin<T>(string contentManagerName) where T : WindowSkin, new()
        {
            T windowSkin = new T();

            if (string.IsNullOrEmpty(Texture) == false)
            {
                windowSkin.Texture = FlatRedBallServices.Load<Texture2D>(
                    Texture, contentManagerName);
            }
            if (string.IsNullOrEmpty(MoveBarTexture) == false)
            {
                windowSkin.MoveBarTexture = FlatRedBallServices.Load<Texture2D>(
                    MoveBarTexture, contentManagerName);
            }

            windowSkin.SpriteBorderWidth = SpriteBorderWidth;
            windowSkin.TextureBorderWidth = TextureBorderWidth;
            windowSkin.BorderSides = BorderSides;
            windowSkin.Alpha = Alpha;

            windowSkin.MoveBarSpriteBorderWidth = MoveBarSpriteBorderWidth;
            windowSkin.MoveBarTextureBorderWidth = MoveBarTextureBorderWidth;
            windowSkin.MoveBarBorderSides = MoveBarBorderSides;

            return windowSkin;
        }
    }

    #endregion

    #region ButtonSkinSave

    public class ButtonSkinSave : WindowSkinSave
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
        public float HorizontalOffset;

        public static T FromHighlightBarSkin<T>(HighlightBarSkin highlightBarSkin) where T : HighlightBarSkinSave, new()
        {
            T highlightSkinSave = WindowSkinSave.FromWindowSkin<T>(highlightBarSkin);

            highlightSkinSave.HighlightBarHorizontalBuffer = highlightBarSkin.HighlightBarHorizontalBuffer;
            highlightSkinSave.ScaleY = highlightBarSkin.ScaleY;
            highlightSkinSave.HorizontalOffset = highlightBarSkin.HorizontalOffset;

            return highlightSkinSave;
        }

        public T ToHighlightBarSkin<T>(string contentManager) where T : HighlightBarSkin, new()
        {
            T highlightBarSkin = base.ToWindowSkin<T>(contentManager);

            highlightBarSkin.HighlightBarHorizontalBuffer = HighlightBarHorizontalBuffer;
            highlightBarSkin.ScaleY = ScaleY;
            highlightBarSkin.HorizontalOffset = HorizontalOffset;

            return highlightBarSkin;
        }

    }

    #endregion

    #region SeparatorSkinSave

    public class SeparatorSkinSave : WindowSkinSave
    {
        public float ScaleY = 1;
        public float HorizontalOffset = 1;
        public int ExtraSeparators = 1;

        public static T FromSeparatorSkin<T>(SeparatorSkin separatorSkin) where T : SeparatorSkinSave, new()
        {
            T separatorSkinSave = WindowSkinSave.FromWindowSkin<T>(separatorSkin);

            separatorSkinSave.ScaleY = separatorSkin.ScaleY;
            separatorSkinSave.HorizontalOffset = separatorSkin.HorizontalOffset;
            separatorSkinSave.ExtraSeparators = separatorSkin.ExtraSeparators;
            return separatorSkinSave;
        }

        public T ToSeparatorSkin<T>(string contentManager) where T : SeparatorSkin, new()
        {
            T separatorSkin = base.ToWindowSkin<T>(contentManager);

            separatorSkin.ScaleY = ScaleY;
            separatorSkin.HorizontalOffset = HorizontalOffset;
            separatorSkin.ExtraSeparators = ExtraSeparators;
            return separatorSkin;

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

        public float HorizontalTextOffset = 0;

        public HighlightBarSkinSave HighlightBarSkinSave;

        public SeparatorSkinSave SeparatorSkinSave;

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

            listBoxSkinSave.HorizontalTextOffset = listBoxSkin.HorizontalTextOffset;

            listBoxSkinSave.HighlightBarSkinSave =
                HighlightBarSkinSave.FromHighlightBarSkin<HighlightBarSkinSave>(listBoxSkin.HighlightBarSkin);

            listBoxSkinSave.SeparatorSkinSave = 
                SeparatorSkinSave.FromSeparatorSkin<SeparatorSkinSave>(listBoxSkin.SeparatorSkin);

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
            SeparatorSkinSave.MakeRelative(directory);
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

            listBoxSkin.HorizontalTextOffset = HorizontalTextOffset;

            listBoxSkin.HighlightBarSkin = 
                HighlightBarSkinSave.ToHighlightBarSkin<HighlightBarSkin>(contentManagerName);

            if (SeparatorSkinSave != null)
            {
                listBoxSkin.SeparatorSkin =
                    SeparatorSkinSave.ToSeparatorSkin<SeparatorSkin>(contentManagerName);
            }

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

    public class GuiSkinSave
    {
        #region Fields

        public WindowSkinSave WindowSkinSave;
        public ButtonSkinSave ButtonSkinSave;
        public ButtonSkinSave ButtonDownSkinSave;
        public TextBoxSkinSave TextBoxSkinSave;
        public ListBoxSkinSave ListBoxSkinSave;
        public ScrollBarSkinSave ScrollBarSkinSave;

        [XmlIgnore]
        public string FileName;

        #endregion

        #region Methods

        #region Public Static Methods
        public static GuiSkinSave FromGuiSkin(GuiSkin guiSkin)
        {
            GuiSkinSave guiSkinSave = new GuiSkinSave();

            guiSkinSave.WindowSkinSave =
                WindowSkinSave.FromWindowSkin<WindowSkinSave>(guiSkin.WindowSkin);

            guiSkinSave.ButtonSkinSave =
                ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(guiSkin.ButtonSkin);

            guiSkinSave.ButtonDownSkinSave =
                ButtonSkinSave.FromButtonSkin<ButtonSkinSave>(guiSkin.ButtonDownSkin);

            guiSkinSave.TextBoxSkinSave =
                TextBoxSkinSave.FromTextBoxSkin<TextBoxSkinSave>(guiSkin.TextBoxSkin);

            guiSkinSave.ListBoxSkinSave =
                ListBoxSkinSave.FromListBoxSkin<ListBoxSkinSave>(guiSkin.ListBoxSkin);

            guiSkinSave.ScrollBarSkinSave =
                ScrollBarSkinSave.FromScrollBarSkin<ScrollBarSkinSave>(guiSkin.ScrollBarSkin);


            return guiSkinSave;
        }

        public static GuiSkinSave FromFile(string fileName)
        {
            GuiSkinSave loadedSkinSave = FileManager.XmlDeserialize<GuiSkinSave>(fileName);
            loadedSkinSave.FileName = fileName;

            return loadedSkinSave;
        }
        #endregion

        #region Public Methods

        public void Save(string fileName)
        {
            MakeRelative(FileManager.GetDirectory(fileName));

            FileManager.XmlSerialize(this, fileName);
        }

        public GuiSkin ToGuiSkin(string contentManagerName)
        {
            GuiSkin guiSkin = new GuiSkin();

            string oldRelative = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(this.FileName);
            {
                // set the values here
                guiSkin.mWindowSkin = WindowSkinSave.ToWindowSkin<WindowSkin>(contentManagerName);
                guiSkin.mButtonSkin = ButtonSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);
                guiSkin.mButtonDownSkin = ButtonDownSkinSave.ToButtonSkin<ButtonSkin>(contentManagerName);
                guiSkin.mTextBoxSkin = TextBoxSkinSave.ToTextBoxSkin<TextBoxSkin>(contentManagerName);
                guiSkin.mListBoxSkin = ListBoxSkinSave.ToListBoxSkin<ListBoxSkin>(contentManagerName);
                guiSkin.mScrollBarSkin = ScrollBarSkinSave.ToScrollBarSkin<ScrollBarSkin>(contentManagerName);
            }
            FileManager.RelativeDirectory = oldRelative;


            return guiSkin;
        }

        #endregion

        #region Private Methods

        private void MakeRelative(string directory)
        {
            WindowSkinSave.MakeRelative(directory);
            ButtonSkinSave.MakeRelative(directory);
            ButtonDownSkinSave.MakeRelative(directory);
            TextBoxSkinSave.MakeRelative(directory);
            ListBoxSkinSave.MakeRelative(directory);
            ScrollBarSkinSave.MakeRelative(directory);
        }

        #endregion

        #endregion
    }
}
