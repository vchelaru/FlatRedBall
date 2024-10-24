using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using FlatRedBall.Utilities;
using FlatRedBall.IO;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Math;
using System.Collections;

namespace FlatRedBall.Graphics
{
    public class BitmapFont : IEquatable<BitmapFont>
    {
        #region Fields

        internal Texture2D[] mTextures;

        BitmapCharacterInfo[] mCharacterInfo;

        int mLineHeightInPixels;

        internal string mFontFile;
        internal string[] mTextureNames = new string[1];

        #endregion

        #region Properties

        public Texture2D Texture
        {
            get { return mTextures[0]; }
            set
            {
                mTextures[0] = value;

                mTextureNames[0] = mTextures[0].Name;
            }
        }

        public Texture2D[] Textures
        {
            get { return mTextures; }
        }

        public string FontFile
        {
            get { return mFontFile; }
        }

        public string TextureName
        {
            get { return mTextureNames[0]; }
        }

        public int LineHeightInPixels
        {
            get { return mLineHeightInPixels; }
        }

        #endregion

        #region Methods

        public BitmapFont(string fontFile, string contentManagerName)
        {
            if (FlatRedBall.IO.FileManager.IsRelative(fontFile))
            {
                fontFile = FlatRedBall.IO.FileManager.RelativeDirectory + fontFile;
            }

            string fontContents = FileManager.FromFileText(fontFile);
            mFontFile = FileManager.Standardize(fontFile);

            string[] texturesToLoad = GetSourceTextures(fontContents);

            mTextures = new Texture2D[texturesToLoad.Length];


            string directory = FileManager.GetDirectory(fontFile);
            string oldRelative = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = directory;

            for (int i = 0; i < mTextures.Length; i++)
            {
				#if IOS || ANDROID
				string fileName = texturesToLoad[i].ToLowerInvariant();
				#else
				string fileName = texturesToLoad[i];
				#endif
					mTextures[i] = FlatRedBallServices.Load<Texture2D>(fileName, contentManagerName);
            }

            FileManager.RelativeDirectory = oldRelative;

            SetFontPattern(fontContents);
        }

        public BitmapFont(string textureFile, string fontFile, string contentManagerName)
        {
            mTextures = new Texture2D[1];
            mTextures[0] = FlatRedBallServices.Load<Texture2D>(textureFile, contentManagerName);

            mTextureNames[0] = mTextures[0].Name;

            //if (FlatRedBall.IO.FileManager.IsRelative(fontFile))
            //    fontFile = FlatRedBall.IO.FileManager.MakeAbsolute(fontFile);

            //FlatRedBall.IO.FileManager.ThrowExceptionIfFileDoesntExist(fontFile);

            SetFontPatternFromFile(fontFile);
        }

        public BitmapFont(Texture2D fontTextureGraphic, string fontPattern)
        {
            // the font could be an extended character set - let's say for Chinese
            // default it to 256, but search for the largest number.
            mTextures = new Texture2D[1];
            mTextures[0] = fontTextureGraphic;

            //mTextureName = mTexture.Name;

            SetFontPattern(fontPattern);
        }


        #region Public Methods

        public void AssignCharacterTextureCoordinates(int asciiNumber, out float tVTop, out float tVBottom,
            out float tULeft, out float tURight)
        {
            BitmapCharacterInfo characterInfo = null;

            if (asciiNumber < mCharacterInfo.Length)
            {
                characterInfo = mCharacterInfo[asciiNumber];
            }
            else
            {
                // Just return the coordinates for the space character
                characterInfo = mCharacterInfo[' '];
            }

            tVTop = characterInfo.TVTop;
            tVBottom = characterInfo.TVBottom;
            tULeft = characterInfo.TULeft;
            tURight = characterInfo.TURight;

        }

        public float DistanceFromTopOfLine(int asciiNumber)
        {
            BitmapCharacterInfo characterInfo = null;

            if (asciiNumber < mCharacterInfo.Length)
            {
                characterInfo = mCharacterInfo[asciiNumber];
            }
            else
            {
                characterInfo = mCharacterInfo[' '];
            }

            return characterInfo.DistanceFromTopOfLine;
        }

        public BitmapCharacterInfo GetCharacterInfo(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber];
            }
            else
            {
                return mCharacterInfo[' '];
            }
        }

        public BitmapCharacterInfo GetCharacterInfo(char character)
        {
            int asciiNumber = (int)character;
            return GetCharacterInfo(asciiNumber);
        }

        public float GetCharacterHeight(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].ScaleY * 2;
            }
            else
            {
                return mCharacterInfo[' '].ScaleY * 2;
            }
        }

        public float GetCharacterScaleX(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].ScaleX;
            }
            else
            {
                return mCharacterInfo[' '].ScaleX;

            }
        }

        public float GetCharacterSpacing(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].Spacing;
            }
            else
            {
                return mCharacterInfo[' '].Spacing;
            }
        }

        public float GetCharacterXOffset(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].XOffset;
            }
            else
            {
                return mCharacterInfo[' '].XOffset;
            }
        }

        public float GetCharacterWidth(char character)
        {
            return GetCharacterScaleX(character) * 2;
        }

        public float GetCharacterWidth(int asciiNumber)
        {
            return GetCharacterScaleX(asciiNumber) * 2;
        }

        public static string[] GetSourceTextures(string fontPattern)
        {
            List<string> texturesToLoad = new List<string>();

            int currentIndexIntoFile = fontPattern.IndexOf("page id=");

            while (currentIndexIntoFile != -1)
            {
                // Right now we'll assume that the pages come in order and they're sequential
                // If this isn' the case then the logic may need to be modified to support this
                // instead of just returning a string[].
                int page = StringFunctions.GetIntAfter("page id=", fontPattern, currentIndexIntoFile);

                int openingQuotesIndex = fontPattern.IndexOf('"', currentIndexIntoFile);

                int closingQuotes = fontPattern.IndexOf('"', openingQuotesIndex + 1);

                string textureName = fontPattern.Substring(openingQuotesIndex + 1, closingQuotes - openingQuotesIndex - 1);
                texturesToLoad.Add(textureName);

                currentIndexIntoFile = fontPattern.IndexOf("page id=", closingQuotes);
            }
            return texturesToLoad.ToArray();
        }

        public void SetFontPattern(string fontPattern)
        {
            #region Identify the size of the character array to create

            int sizeOfArray = 256;
            // now loop through the file and look for numbers after "char id="

            // Vic says:  This used to
            // go through the entire file
            // to find the last character index.
            // I think they're ordered by character
            // index, so we can just find the last one
            // and save some time.
            int index = fontPattern.LastIndexOf("char id=", fontPattern.Length, StringComparison.Ordinal);
            if (index != -1)
            {
                int ID = StringFunctions.GetIntAfter("char id=", fontPattern, index);

                sizeOfArray = System.Math.Max(sizeOfArray, ID + 1);



            }
            else
            {
                // index is -1, but let's try a regular IndexOf:
                int forwardIndexOf = fontPattern.IndexOf("char id=");
                if(forwardIndexOf != -1 && index == -1)
                {
                    throw new Exception("How is this possible? LastIndexOf \"char id=\" is returning a value of -1, while IndexOf for the same string is returning an index value)");

                }
                else
                {
                    string message = "Could not find the last index of the string \"char id=\" in the font pattern. " + 
                        "This means that the font file has no characters. Font files must have at least one defined character";
                    throw new Exception(message);
                }
            }
            #endregion



            mCharacterInfo = new BitmapCharacterInfo[sizeOfArray];
            mLineHeightInPixels =
                StringFunctions.GetIntAfter(
                "lineHeight=", fontPattern);

            // This font may not reference any textures at all - if it doesn't have any
            // characters set in the .bmfc.  I don't think we should crash here if so:
            if (mTextures.Length != 0)
            {

                BitmapCharacterInfo space = FillBitmapCharacterInfo(' ', fontPattern,
                   mTextures[0].Width, mTextures[0].Height, mLineHeightInPixels, 0);

                for (int i = 0; i < sizeOfArray; i++)
                {
                    mCharacterInfo[i] = space;
                }

                // Make the tab character be equivalent to 4 spaces:
                mCharacterInfo['t'].ScaleX = space.ScaleX * 4;
                mCharacterInfo['t'].Spacing = space.Spacing * 4;

                index = fontPattern.IndexOf("char id=", 0, StringComparison.Ordinal);
                while (index != -1)
                {

                    int ID = StringFunctions.GetIntAfter("char id=", fontPattern, index);

                    if (ID == -1)
                    {
                        // The bitmap font may have something like this as the first character:
                        // char id=-1   x=149   y=84    width=10    height=18    xoffset=1     yoffset=7     xadvance=12    page=0  chnl=15
                        // We don't use that, but we don't want to crash on it, so continue onward.
                        int indexOfID = fontPattern.IndexOf("char id=", index, StringComparison.Ordinal);
                        index = indexOfID + ID.ToString().Length;

                        continue;
                    }
                    else
                    {
#if DEBUG
                        if(ID >= mCharacterInfo.Length)
                        {
                            string message = $"Error trying to access character with int {ID} which is character {(char)ID}";

                            message += $"This is happening in the font string at index {index} which has the following characters:";

                            int startIndex = System.Math.Max(0, index);
                            int endIndex = System.Math.Min(fontPattern.Length - 1, index + 100);

                            message += fontPattern.Substring(startIndex, endIndex - startIndex);

                            throw new IndexOutOfRangeException(message);
                        }
#endif


                        mCharacterInfo[ID] = FillBitmapCharacterInfo(ID, fontPattern, mTextures[0].Width,
                            mTextures[0].Height, mLineHeightInPixels, index);

                        int indexOfID = fontPattern.IndexOf("char id=", index, StringComparison.Ordinal);
                        if (indexOfID != -1)
                        {
                            index = indexOfID + ID.ToString().Length;
                        }
                        else
                            index = -1;
                    }
                }

                #region Get Kearning Info

                index = fontPattern.IndexOf("kerning ", 0, StringComparison.Ordinal);

                if (index != -1)
                {

                    index = fontPattern.IndexOf("first=", index, StringComparison.Ordinal);

                    while (index != -1)
                    {
                        int ID = StringFunctions.GetIntAfter("first=", fontPattern, index);
                        int secondCharacter = StringFunctions.GetIntAfter("second=", fontPattern, index);
                        int kearningAmount = StringFunctions.GetIntAfter("amount=", fontPattern, index);

                        if(mCharacterInfo[ID].SecondLetterKearning == null)
                        {
                            mCharacterInfo[ID].SecondLetterKearning = new Dictionary<int, int>();
                        }
                        mCharacterInfo[ID].SecondLetterKearning.Add(secondCharacter, kearningAmount);

                        index = fontPattern.IndexOf("first=", index + 1, StringComparison.Ordinal);
                    }
                }

                #endregion
            }
            //mCharacterInfo[32].ScaleX = .23f;
        }

        public void SetFontPatternFromFile(string fntFileName)
        {
            // standardize before doing anything else
            fntFileName = FileManager.Standardize(fntFileName);
            

            if (FlatRedBall.IO.FileManager.IsRelative(fntFileName))
            {
                fntFileName = FlatRedBall.IO.FileManager.RelativeDirectory + fntFileName;
            }

            mFontFile = fntFileName;

            FileManager.ThrowExceptionIfFileDoesntExist(mFontFile);

            //System.IO.StreamReader sr = new System.IO.StreamReader(mFontFile);
            string fontPattern = FileManager.FromFileText(mFontFile);
            //sr.Close();

            SetFontPattern(fontPattern);
        }

        public Texture2D RenderToTexture2D(string whatToRender, float red, float green, float blue, float alpha)
        {
            return RenderToTexture2D(whatToRender, HorizontalAlignment.Left, red, green, blue, alpha);
        }

        public Texture2D RenderToTexture2D(string whatToRender, HorizontalAlignment horizontalAlignment, float red, float green, float blue, float alpha)
        {
            string[] lines = whatToRender.Split('\n');

            return RenderToTexture2D(lines, horizontalAlignment, true, red, green, blue, alpha);
        }

        public Texture2D RenderToTexture2D(string whatToRender)
        {
            string[] lines = whatToRender.Split('\n');

            return RenderToTexture2D(lines, HorizontalAlignment.Left, false, 0, 0, 0, 0);
        }

        public Texture2D RenderToTexture2D(string whatToRender, HorizontalAlignment horizontalAlignment)
        {
            string[] lines = whatToRender.Split('\n');

            return RenderToTexture2D(lines, horizontalAlignment, false, 0, 0, 0, 0);
        }

        private Texture2D RenderToTexture2D(IEnumerable lines, HorizontalAlignment horizontalAlignment, bool changeColor, float red, float green, float blue, float alpha)
        {

            ImageData sourceImageData = ImageData.FromTexture2D(this.Texture);

            if (changeColor)
                sourceImageData.ApplyColorOperation(ColorOperation.Add, red, green, blue, alpha);

            Point point = new Point();

            int maxWidthSoFar = 0;
            int requiredWidth = 0;
            int requiredHeight = 0;

            List<int> widths = new List<int>();

            foreach (string line in lines)
            {
                requiredHeight += LineHeightInPixels;
                requiredWidth = 0;

                for (int i = 0; i < line.Length; i++)
                {
                    char character = line[i];
                    BitmapCharacterInfo characterInfo = GetCharacterInfo(character);
                    bool isLast = i == line.Length - 1;

                    if (isLast)
                    {
                        requiredWidth += characterInfo.GetPixelWidth(Texture) + characterInfo.GetPixelXOffset(LineHeightInPixels);
                    }
                    else
                    {
                        requiredWidth += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);
                    }
                }
                widths.Add(requiredWidth);
                maxWidthSoFar = System.Math.Max(requiredWidth, maxWidthSoFar);
            }

            ImageData imageData = new ImageData(maxWidthSoFar, requiredHeight);

            int lineNumber = 0;

            foreach (string line in lines)
            {
                point.X = 0;
                if (horizontalAlignment == HorizontalAlignment.Right)
                {
                    point.X = maxWidthSoFar - widths[lineNumber];
                }
                else if (horizontalAlignment == HorizontalAlignment.Center)
                {
                    point.X = (maxWidthSoFar - widths[lineNumber]) / 2;
                }

                foreach (char c in line)
                {

                    BitmapCharacterInfo characterInfo = GetCharacterInfo(c);

                    int sourceLeft = characterInfo.GetPixelLeft(Texture);
                    int sourceTop = characterInfo.GetPixelTop(Texture);
                    int sourceWidth = characterInfo.GetPixelRight(Texture) - sourceLeft;
                    int sourceHeight = characterInfo.GetPixelBottom(Texture) - sourceTop;

                    int distanceFromTop = characterInfo.GetPixelDistanceFromTop(LineHeightInPixels);

                    // There could be some offset for this character
                    int xOffset = characterInfo.GetPixelXOffset(LineHeightInPixels);
                    point.X += xOffset;

                    point.Y = lineNumber * LineHeightInPixels + distanceFromTop;

                    Rectangle sourceRectangle = new Rectangle(
                        sourceLeft, sourceTop, sourceWidth, sourceHeight);

                    imageData.Blit(sourceImageData, sourceRectangle, point);
                    point.X -= xOffset;
                    point.X += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);

                }
                point.X = 0;
                lineNumber++;
            }

            // We don't want
            // to generate mipmaps
            // because text is usually
            // rendered pixel-perfect.
            const bool generateMipmaps = false;
            return imageData.ToTexture2D(generateMipmaps, FlatRedBallServices.GraphicsDevice);
        }

        #endregion

        #region Private Methods

        private BitmapCharacterInfo FillBitmapCharacterInfo(int characterID, string fontString, int textureWidth,
            int textureHeight, int lineHeightInPixels, int startingIndex)
        {
            // Example:
            // char id=101  x=158   y=85    width=5     height=7     xoffset=1     yoffset=6     xadvance=7     page=0  chnl=0 
            BitmapCharacterInfo characterInfoToReturn = new BitmapCharacterInfo();

            int indexOfID = fontString.IndexOf("char id=" + characterID, startingIndex);

            if (indexOfID != -1)
            {
                var x = StringFunctions.GetIntAfter("x=", fontString, ref indexOfID);
                var y = StringFunctions.GetIntAfter("y=", fontString, ref indexOfID);
                var width = StringFunctions.GetIntAfter("width=", fontString, ref indexOfID);
                var height = StringFunctions.GetIntAfter("height=", fontString, ref indexOfID);
                var xOffset = StringFunctions.GetIntAfter("xoffset=", fontString, ref indexOfID);
                var yOffset = StringFunctions.GetIntAfter("yoffset=", fontString, ref indexOfID);
                var xAdvance = StringFunctions.GetIntAfter("xadvance=", fontString, ref indexOfID);
                var page = StringFunctions.GetIntAfter("page=", fontString, ref indexOfID);

                var textureWidthF = (float)textureWidth;
                var textureHeightF = (float)textureHeight;

                characterInfoToReturn.TULeft = x / textureWidthF;
                characterInfoToReturn.TVTop = y / textureHeightF;

                characterInfoToReturn.TURight = characterInfoToReturn.TULeft + width / textureWidthF;
                characterInfoToReturn.TVBottom = characterInfoToReturn.TVTop + height / textureHeightF;

                var lineHeightInPixelsF = (float)lineHeightInPixels;

                characterInfoToReturn.DistanceFromTopOfLine = // 1 sclY means 2 height
                    2 * yOffset / lineHeightInPixelsF;

                characterInfoToReturn.ScaleX = width / lineHeightInPixelsF;

                characterInfoToReturn.ScaleY = height / lineHeightInPixelsF;

                characterInfoToReturn.Spacing = 2 * xAdvance / lineHeightInPixelsF;

                characterInfoToReturn.XOffset = 2 * xOffset / lineHeightInPixelsF;

                characterInfoToReturn.PageNumber = page;
            }

            return characterInfoToReturn;
        }

        #endregion

        #endregion

        #region IEquatable<BitmapFont> Members

        bool IEquatable<BitmapFont>.Equals(BitmapFont other)
        {
            return this == other;
        }

        #endregion
    }
}
