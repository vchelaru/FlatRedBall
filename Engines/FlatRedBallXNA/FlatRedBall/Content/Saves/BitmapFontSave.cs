using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.IO;

namespace FlatRedBall.Content.Saves
{
    #region XML Docs
    /// <summary>
    /// Save class storing information for a BitmapFont.  
    /// </summary>
    #endregion
    public class BitmapFontSave
    {
        const string ReservedDefaultName = "<DEFAULT>";

        public string TextureFileName;
        public string FontFileName;

        public static BitmapFontSave FromBitmapFont(BitmapFont bitmapFont)
        {
            BitmapFontSave bitmapFontSave = new BitmapFontSave();

            if (bitmapFont == TextManager.DefaultFont)
            {
                bitmapFontSave.TextureFileName = ReservedDefaultName;
                bitmapFontSave.FontFileName = ReservedDefaultName;
            }
            else
            {
                bitmapFontSave.TextureFileName = bitmapFont.Texture.Name;
                bitmapFontSave.FontFileName = bitmapFont.mFontFile;
            }
            return bitmapFontSave;
        }

        public void MakeRelative(string directory)
        {
            if (TextureFileName != ReservedDefaultName &&
                FileManager.IsRelative(TextureFileName) == false)
            {
                TextureFileName = FileManager.MakeRelative(TextureFileName, directory);
            }

            if (FontFileName != ReservedDefaultName &&
                FileManager.IsRelative(FontFileName) == false)
            {
                FontFileName = FileManager.MakeRelative(FontFileName, directory);
            }
        }

        public BitmapFont ToBitmapFont(string contentManagerName)
        {
            if (TextureFileName == ReservedDefaultName)
            {
                return TextManager.DefaultFont;
            }
            else if (string.IsNullOrEmpty(TextureFileName) == false &&
                string.IsNullOrEmpty(FontFileName) == false)
            {
                return new BitmapFont(
                    TextureFileName, FontFileName, contentManagerName);
            }
            else
            {
                return null;
            }
        }
    }
}
