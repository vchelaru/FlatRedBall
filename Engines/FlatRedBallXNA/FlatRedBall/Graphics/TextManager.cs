
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using FlatRedBall.Math;
using FlatRedBall.Instructions;

using FlatRedBall.Gui;
using FlatRedBall.Graphics.Texture;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics
{
    #region Enums

        public enum VerticalAlignment
        {
            Top,
            Bottom,
            Center
        }

        public enum HorizontalAlignment
        {
            Left,
            Right,
            Center
        }    

        #endregion
    public static class TextManager
    {
        #region Fields

        // internal so the Renderer has access to the texts for drawing.
        static internal PositionedObjectList<Text> mDrawnTexts;
        static internal PositionedObjectList<Text> mAutomaticallyUpdatedTexts;
        static internal ReadOnlyCollection<Text> mAutomaticallyUpdatedTextsReadOnly;

        internal static List<float> WidthList = new List<float>();

        static BitmapFont mDefaultFont;
        static SpriteFont mDefaultSpriteFont;
        public static SpriteBatch SpriteBatch;

        static BasicEffect mBasicEffect;

        static VertexPositionColorTexture[] v; 
        static internal float mXForVertexBuffer = 0;
        static internal float mYForVertexBuffer = 0;
        static internal float mZForVertexBuffer = 0;
        static internal float mSpacingForVertexBuffer = 0;
        static internal float mScaleForVertexBuffer = 0;
        static internal float mNewLineDistanceForVertexBuffer = 2;
        static internal float mMaxWidthForVertexBuffer = 0;
        static internal float mAlphaForVertexBuffer = 0;
        static internal float mRedForVertexBuffer = 0;
        static internal float mGreenForVertexBuffer = 0;
        static internal float mBlueForVertexBuffer = 0;
        static internal HorizontalAlignment mAlignmentForVertexBuffer = HorizontalAlignment.Left;

        #endregion

        #region Properties
        public static BitmapFont DefaultFont
        {
            get { return mDefaultFont; }
            set { mDefaultFont = value; }
        }

        public static SpriteFont DefaultSpriteFont
        {
            get { return mDefaultSpriteFont; }
            set { mDefaultSpriteFont = value; }
        }

        public static bool FilterTexts
        {
            get;
            set;
        }

        public static ReadOnlyCollection<Text> AutomaticallyUpdatedTexts
        {
            get{ return mAutomaticallyUpdatedTextsReadOnly;}
        }


        public static bool UseNativeTextRendering
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructor/Initialize

        static TextManager()
        {

            mDrawnTexts = new PositionedObjectList<Text>();
            mAutomaticallyUpdatedTexts = new PositionedObjectList<Text>();

            mAutomaticallyUpdatedTextsReadOnly = new ReadOnlyCollection<Text>(mAutomaticallyUpdatedTexts);

            v = new VertexPositionColorTexture[6];

        }

        public static void Initialize(GraphicsDevice graphicsDevice)
        {

#if XNA4 && !UNIT_TESTS
            //UseNativeTextRendering = true;
            mBasicEffect = new BasicEffect(graphicsDevice);


            mBasicEffect.TextureEnabled = true;

#endif


#if FRB_MDX
#elif (SILVERLIGHT || XNA4) && !UNIT_TESTS
            SpriteBatch = new SpriteBatch(graphicsDevice);
#endif
        }
        #endregion

        #region Public Methods

        #region AddText methods

        public static Text AddText(string displayText)
        {
#if DEBUG
            if (DefaultFont == null)
            {
                throw new System.InvalidOperationException("Cannot create Text object because the TextManager's DefaultFont has not been set.");
            }
#endif
            return AddText(displayText, DefaultFont);
        }

        public static Text AddText(string displayText, BitmapFont bitmapFont)
        {
            return AddText(displayText, bitmapFont, null);
        }


        public static Text AddText(string displayText, BitmapFont bitmapFont, string contentManagerName)
        {
            Text text = new Text(bitmapFont);
            text.ContentManager = contentManagerName;
            text.DisplayText = displayText;

            text.AdjustPositionForPixelPerfectDrawing = true;

#if DEBUG
            if (mDrawnTexts.Contains(text))
            {
                throw new InvalidOperationException("Can't add the text to the Drawn Text list because it's already there.  This error is here to prevent you from double-adding Texts.");
            }
            if (mAutomaticallyUpdatedTexts.Contains(text))
            {
                throw new InvalidOperationException("Can't add the text to the Automatically Updated (Managed) Text list because it's already there.  This error is here to prevent you from double-adding Texts.");
            }
#endif

            mDrawnTexts.Add(text);
            mAutomaticallyUpdatedTexts.Add(text);

            text.SetPixelPerfectScale(SpriteManager.Camera);

            return text;
        }

        public static Text AddText(string displayText, Layer layer)
        {
            if (layer == null)
                return AddText(displayText, DefaultFont);
            else
            {
                if (DefaultFont == null)
                {
                    throw new System.InvalidOperationException("Cannot create Text object because the TextManager's DefaultFont has not been set.");
                }

                Text text = new Text(DefaultFont);
                text.DisplayText = displayText;
                if (layer.CameraBelongingTo != null)
                {
                    text.SetPixelPerfectScale(layer.CameraBelongingTo);
                }
                else
                {
                    text.SetPixelPerfectScale(SpriteManager.Camera);
                }
                layer.Add(text);
                // Don't add the text to mDrawnTexts since it's drawn as part of the layer.

#if DEBUG
                if (mAutomaticallyUpdatedTexts.Contains(text))
                {
                    throw new InvalidOperationException("Can't add the text to the Automatically Updated (Managed) Text list because it's already there.  This error is here to prevent you from double-adding Texts.");
                }
#endif

                mAutomaticallyUpdatedTexts.Add(text);
                return text;
            }
        }

        /// <summary>
        /// Creates a new Text with the argument string which will only be drawn in the argument Camera's
        /// destination.
        /// </summary>
        /// <remarks>
        /// The new text will be stored both by the camera and the TextManager's managed invisible Texts array.
        /// The text is automatically attached to the argument Camera so to be moved, it must either
        /// be detached or moved with relative variables.
        /// </remarks>
        /// <param name="s">The string to show.</param>
        /// <param name="cameraToAddTo">The camera that the newly created Text belongs to.</param>
        /// <returns>Reference to the newly created Text.</returns>
        static public Text AddText(string s, Camera cameraToAddTo)
        {
            Text t = new Text();
            t.DisplayText = s;
            t.AttachTo(cameraToAddTo, true);
            cameraToAddTo.Layer.Add(t);
#if DEBUG
            if (mAutomaticallyUpdatedTexts.Contains(t))
            {
                throw new InvalidOperationException("Can't add the text to the Automatically Updated (Managed) Text list because it's already there.  This error is here to prevent you from double-adding Texts.");
            }
#endif
            
            mAutomaticallyUpdatedTexts.Add(t);

            
            return t;
        }

        static public Text AddText(Text textToAdd)
        {
#if DEBUG
            if (mAutomaticallyUpdatedTexts.Contains(textToAdd))
            {
                var message =
                    "Can't add the text to the Automatically Updated (Managed) Text list because it's already there.  This error is here to prevent you from double-adding Texts.";

                throw new InvalidOperationException(message);
            }
#endif
            
            mDrawnTexts.Add(textToAdd);
            mAutomaticallyUpdatedTexts.Add(textToAdd);

            return textToAdd;
        }

        /// <summary>
        /// Adds an already-created Text instance to the argument camera.
        /// </summary>
        /// <remarks>
        /// The argument Text should not already be in the TextManager's memory when this
        /// method is called.
        /// </remarks>
        /// <param name="textToAdd">Reference to the text object to add.</param>
        /// <param name="cameraToAddTo">Reference to the camera to be added to.</param>
        /// <returns>The added text.</returns>
        static public Text AddText(Text textToAdd, Camera cameraToAddTo)
        {
            cameraToAddTo.Layer.Add(textToAdd);

#if DEBUG
            if (mAutomaticallyUpdatedTexts.Contains(textToAdd))
            {
                throw new InvalidOperationException("Can't add the text to the Automatically Updated (Managed) Text list because it's already there.  This error is here to prevent you from double-adding Texts.");
            }
#endif

            mAutomaticallyUpdatedTexts.Add(textToAdd);

            return textToAdd;
        }


        static public Text AddText(Text textToAdd, Layer layerToAddTo)
        {
            if (layerToAddTo == null)
                return AddText(textToAdd);
            else
            {
                if (textToAdd.ListsBelongingTo.Contains(mDrawnTexts))
                {
                    mDrawnTexts.Remove(textToAdd);
                }

                layerToAddTo.Add(textToAdd);

                if (textToAdd.ListsBelongingTo.Contains(mAutomaticallyUpdatedTexts) == false)
                {
                    mAutomaticallyUpdatedTexts.Add(textToAdd);
                }
                // otherwise no need to add it.

                return textToAdd;
            }
        }

        #endregion

        public static void AddToLayer(Text text, Layer layerToAddTo)
        {
            if (layerToAddTo == null)
            {
                AddText(text);
            }
            else
            {
                layerToAddTo.Add(text);
                if (text.ListsBelongingTo.Contains(mDrawnTexts))
                {
                    mDrawnTexts.Remove(text);
                }

                if (text.ListsBelongingTo.Contains(mAutomaticallyUpdatedTexts) == false)
                {
                    // The Text is not updated.  That means
                    // it was instantiated outside of the TextManager.  Add it here as an
                    // automatically updated Text
                    mAutomaticallyUpdatedTexts.Add(text);
                }
            }



        }

        /// <summary>
        /// Adds a text object to the TextManager to be drawn, but which will not have
        /// its every-frame activity called.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="layerToAddTo">The optional layer to add to. If null, the Text will be unlayered.</param>
        /// <remarks>Texts added with AddManuallyUpdated will not be visible unless they have
        /// their UpdateDependencies or ForceUpdateDependencies called after all properties have been assigned.
        /// </remarks>
        public static void AddManuallyUpdated(Text text, Layer layerToAddTo = null)
        {
            if (layerToAddTo == null)
            {
#if DEBUG
                if (mDrawnTexts.Contains(text))
                {
                    throw new InvalidOperationException("This text has already been added to the TextManager");
                }
#endif
                mDrawnTexts.Add(text);
            }
            else
            {
#if DEBUG
                if (layerToAddTo.mTexts.Contains(text))
                {
                    throw new InvalidOperationException("This text has already been added to the layer " + layerToAddTo.Name);
                }
#endif
                layerToAddTo.mTexts.Add(text);
            }
        }

        public static void ConvertToManuallyUpdated(Text text)
        {
            if (mAutomaticallyUpdatedTexts.Contains(text))
            {
                mAutomaticallyUpdatedTexts.Remove(text);
            }

            text.UpdateDependencies(TimeManager.CurrentTime);
        }

        public static void ConvertToAutomaticallyUpdated(Text text)
        {
#if DEBUG
            if (mAutomaticallyUpdatedTexts.Contains(text))
            {
                throw new InvalidOperationException("The text " + text.Name + " is already automatically updated");
            }
#endif
            mAutomaticallyUpdatedTexts.Add(text);
        }

        public static int GetCursorPosition(float relX, string text, float scale, int firstLetterShowing)
        {
            float accumulatedOffset = 0;
            float currentLetterWidth;

            int i;
            for (i = firstLetterShowing; i < text.Length; i++)
            {
#if SILVERLIGHT
                // TODO:  May need to handle this if we're using sprite fonts in XNA
				float currentWidth = scale * mDefaultSpriteFont.MeasureString(
					text.Substring(firstLetterShowing, i - firstLetterShowing)).X / (float)mDefaultSpriteFont.FontSize;

				float widthAfterNextLetter = scale * mDefaultSpriteFont.MeasureString(
					text.Substring(firstLetterShowing, i + 1 - firstLetterShowing)).X / (float)mDefaultSpriteFont.FontSize;

				if (relX < widthAfterNextLetter)
				{
                    return i - firstLetterShowing;
				}
				// else do nothing because we don't have to accumulate here
#else
                currentLetterWidth = mDefaultFont.GetCharacterSpacing((int)text[i]) * scale;

                if (relX < accumulatedOffset + currentLetterWidth / 2.0f)
                {
                    return i - firstLetterShowing;
                }
                else
                {
                    accumulatedOffset += currentLetterWidth;
                }
#endif
            }
            return i - firstLetterShowing;

        }


        static List<float> arrayOfFloats = new List<float>();
        public static List<float> GetLineWidth(string text, float spacing, BitmapFont font)
        {
            arrayOfFloats.Clear();

            int index = 0;
            int nextNewline = 0;
            while (index < text.Length)
            {
                nextNewline = text.IndexOf('\n', index);

                if (nextNewline == -1)
                    nextNewline = text.Length;

                arrayOfFloats.Add(GetWidth(text, spacing, font, index, nextNewline - index));

                index = nextNewline + 1;

            }

            return arrayOfFloats;
        }


        static public int GetNumberOfCharsIn(float maxWidth, string textToWrite, float spacing)
        {
            return GetNumberOfCharsIn(maxWidth, textToWrite, spacing, 0, mDefaultFont, HorizontalAlignment.Left);
        }


        static public int GetNumberOfCharsIn(float maxWidth, string textToWrite, float spacing, int firstLetterShowing)
        {
            return GetNumberOfCharsIn(maxWidth, textToWrite, spacing, firstLetterShowing, mDefaultFont, HorizontalAlignment.Left);
        }


        static public int GetNumberOfCharsIn(float maxWidth, string textToWrite, float spacing, 
            int firstLetterShowing, BitmapFont bitmapFont, HorizontalAlignment alignment)
        {
            if (string.IsNullOrEmpty(textToWrite))
            {
                return 0;
            }

            // GUIMan drawn objects use the default font, so they will pass null.  
            // Make sure that we have a default font in this case.  If not, throw
            // an exception.  
            if (bitmapFont == null)
            {
                if (mDefaultFont == null)
                {
                    throw new ArgumentNullException("No bitmapFont passed as an argument and the TextManager does not have its DefaultFont set");
                }

                bitmapFont = mDefaultFont;

            }


            float stringWidth = 0;

            int i = 0;

            if (alignment == HorizontalAlignment.Left)
            {
                for (i = 0; i + firstLetterShowing < textToWrite.Length; i++)
                {

                    float characterWidth = bitmapFont.GetCharacterSpacing(textToWrite[i + firstLetterShowing]) * spacing;
                    if (stringWidth + characterWidth > maxWidth) break;

                    stringWidth += characterWidth;
                }
            }
            else if (alignment == HorizontalAlignment.Right)
            {
                for (i = 0; firstLetterShowing - i > -1; i++)
                {

                    float characterWidth = bitmapFont.GetCharacterSpacing(textToWrite[firstLetterShowing - i]) * spacing;
                    if (stringWidth + characterWidth > maxWidth) break;

                    stringWidth += characterWidth;
                }
            }
            else
            {
                // currently this starts at the center letter ignoring the variable spacing.  This could be improved.

                int center = textToWrite.Length / 2;

                int movingLeft = center;
                int movingRight = center + 1;

                while (movingLeft > -1 || movingRight < textToWrite.Length)
                {
                    if (movingLeft > -1)
                    {

                        float characterWidth = bitmapFont.GetCharacterSpacing(textToWrite[movingLeft]) * spacing;
                        if(stringWidth + characterWidth > maxWidth) break;
                        stringWidth += characterWidth;

                        movingLeft--;
                        i++;
                    }

                    if (movingRight < textToWrite.Length)
                    {

                        float characterWidth = bitmapFont.GetCharacterSpacing(textToWrite[movingRight]) * spacing;
                        if(stringWidth + characterWidth > maxWidth) break;
                        stringWidth += characterWidth;

                        movingRight++;
                        i++;
                    }
                }
            }
            return i;
        }

        #region XML Docs
        /// <summary>
        /// Returns the width of the rendered text assuming the spacing is 1.
        /// </summary>
        /// <remarks>
        /// If there are newline characters in the string, GetWidth returns the width of the longest line.
        /// Spaces are considered characters as well so "Hello " will be longer than "Hello".
        /// </remarks>
        /// <param name="text">The text to measure.</param>
        /// <returns>The width of the longest line in the text.</returns>
        #endregion
        public static float GetWidth(string text)
        {
            return GetWidth(text, 1, mDefaultFont);
        }


        public static float GetWidth(string text, float spacing)
        {
            return GetWidth(text, spacing, mDefaultFont);
        }

        public static float GetWidth(string text, float spacing, BitmapFont font)
        {
            if (text == null)   return 0;
            else                return GetWidth(text, spacing, font, 0, text.Length, WidthList);
        }

        public static float GetWidth(string text, float spacing, BitmapFont font, int startIndex, int count)
        {
            
            return GetWidth(text, spacing, font, startIndex, count, WidthList);
        }

        public static float GetWidth(string text, float spacing, BitmapFont font, int startIndex, int count, List<float> widthList)
        {
            widthList.Clear();

            if (text == null || text == "")
                return 0;

            if (font == null)
                font = mDefaultFont;
            float width = 0;
            float maxWidth = 0;

            if (text.Length == 1)
            {
                width += font.GetCharacterSpacing(text[0]) * spacing;
            }
            else
            {

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    if (text[i] == '\r' && i < text.Length - 1 && text[i + 1] == '\n')
                    {
                        maxWidth = System.Math.Max(width, maxWidth);
                        widthList.Add(width);
                        width = 0;
                        i++; // move ahead another letter so that we skip past '\r\n'
                        continue;
                    }
                    else if (text[i] == '\n' || text[i] == '\r')
                    {
                        maxWidth = System.Math.Max(width, maxWidth);
                        widthList.Add(width);
                        width = 0;
                        continue;
                    }
                    width += font.GetCharacterSpacing(text[i]) * spacing;
                }
            }
            if (width != 0)
            {
                widthList.Add(width);
            }
            maxWidth = System.Math.Max(width, maxWidth);

            return maxWidth; // +2 * .9f * spacing;

        }


        public static float GetWidth(Text text)
        {
            if (text == null)
            {
                return 0;
            }
            else // using bitmap text
            {
                return GetWidth(text.mAdjustedText, text.Spacing, text.Font);
            }
        }


        public static string InsertNewLines(string text, float spacing, float maxWidth, BitmapFont font)

        {
            // if the text is null or empty, just return the text as is
            if (text == null || text == "") return text;

            int letterOn = 0;
            //float widthSoFar = 0;
            int lineOn = 0;
            //int asciiNumber;

            float textWidth = spacing;

            if (font == null)
            {
                font = DefaultFont;
            }

            bool hasMoreToDo = true;
            while (hasMoreToDo)
            {
                hasMoreToDo = InsertOneNewLine(ref text, ref letterOn, ref lineOn, font, spacing, maxWidth);

            }

            return text;
        }


        internal static bool InsertOneNewLine(ref string text, ref int letterOn, ref int lineOn, BitmapFont font, float spacing, float maxWidth)
        {
            float widthSoFar = 0;

            while (true)
            {
                int asciiNumber = (int)text[letterOn];

                if (asciiNumber == (int)'\n')
                {
                    lineOn++;
                    letterOn++;
                    widthSoFar = 0;

                    if (letterOn == text.Length)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                if (asciiNumber < 33)
                {
                    widthSoFar += font.GetCharacterSpacing(asciiNumber) * spacing;
                    letterOn++;

                    if (letterOn == text.Length)
                    {
                        return false;
                    }
                    continue;
                }
                else
                {
                    widthSoFar += font.GetCharacterSpacing(asciiNumber) * spacing;



                    if (widthSoFar < maxWidth)
                    {
                        letterOn++;
                        if (letterOn == text.Length)
                        {
                            return false;
                        }
                        continue;
                    }
                    else
                    {
                        for (int i = letterOn; i > 0; i--)
                        {
                            if (text[i] == ' ' || text[i] == '/' || text[i] == '\\' || text[i] == '-')
                            {
                                text = text.Insert(i + 1, "\n");
                                lineOn++;
                                letterOn = i + 2;
                                widthSoFar = 0;
                                if (letterOn >= text.Length)
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            /* If the particular word is too long, then just continue until
                             * we find the next space, slash, or end of the text.
                             */
                            else if (text[i] == '\n')
                            {
                                for (int j = letterOn; j < text.Length; j++)
                                {
                                    letterOn = j + 1;
                                    if (text[j] == ' ' || text[j] == '/' || text[j] == '\\')
                                    {
                                        text = text.Insert(j + 1, "\n");
                                        lineOn++;
                                        letterOn = j + 2;
                                        widthSoFar = 0;
                                        break;
                                    }
                                }

                                if (letterOn == text.Length)
                                    return false;

                                break;
                            }
                        }

                        if (widthSoFar != 0)
                        {
                            // December 16, 2010
                            // If a text's width is
                            // smaller than the width
                            // of one character, than letterOn
                            // will be 0.  That causes weird behavior
                            // that is difficult to understand, so we should
                            // make sure it's at least 1.
                            if (text.Length > 1 && letterOn == 0)
                            {
                                letterOn = 1;
                            }

                            text = text.Insert(letterOn, "\n");

                            letterOn++;
                            lineOn++;
                            widthSoFar = 0;
                            return true;

                        }
                    }
                }
            }
            // Unreachable code:
            //return false;
        }


        public static void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            foreach (Text text in mAutomaticallyUpdatedTexts)
            {
                for (int i = 0; i < text.Font.Textures.Length; i++)
                {
                    if (text.Font.Textures[i] == oldTexture)
                    {
                        text.Font.Textures[i] = newTexture;
                    }
                }
            }
        }


        public static void RemoveText(Text textToRemove)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Text objects can only be removed on the primary thread");
            }
#endif
            if (textToRemove == null)
                return;

            textToRemove.ClearRelationships();

            textToRemove.RemoveSelfFromListsBelongingTo();

            //This is to clean up the Sprite from the engine and
            // unloads its texture
            if (textToRemove.RenderOnTexture)
            {
                textToRemove.RenderOnTexture = false;
            }
        }


        public static void RemoveTextOneWay(Text textToRemove)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Text objects can only be removed on the primary thread");
            }
#endif

            if (textToRemove.ListsBelongingTo.Contains(mDrawnTexts))
            {
                mDrawnTexts.Remove(textToRemove);
            }

            if (textToRemove.ListsBelongingTo.Contains(mAutomaticallyUpdatedTexts))
            {
                mAutomaticallyUpdatedTexts.Remove(textToRemove);
            }


            SpriteManager.UnderAllDrawnLayer.Remove(textToRemove);

            foreach (Layer sl in SpriteManager.Layers)
                sl.Remove(textToRemove);

            //This is to clean up the Sprite from the engine and
            // unloads its texture
            if (textToRemove.RenderOnTexture)
            {
                textToRemove.RenderOnTexture = false;
            }



        }

        public static void RemoveText<T>(AttachableList<T> listToRemove) where T : Text
        {
            // backwards loop so we don't miss any Text objects
            for (int i = listToRemove.Count - 1; i > -1; i--)
                RemoveText(listToRemove[i]);
        }


        public static void SetFontTexture(string fontTextureGraphic, string fntFile, string contentManagerName)
        {
            mDefaultFont = new BitmapFont(fontTextureGraphic, fntFile, contentManagerName);
        }



        public static void ShuffleInternalLists()
        {
            mDrawnTexts.Shuffle();
        }


        #endregion

        #region Internal Methods

        #region Draw methods


        static public void Draw(ref string txt)
        {
            if (txt == null || txt == "") return;
            float newlineShift = 0;


            if (txt.Contains("\n"))
            {

                string[] lines = txt.Split('\n');

                // now we need to truncate the strings so that they fit in the width


                for (int lnNum = 0; lnNum < lines.Length; lnNum++)
                {
                    string textToWrite = lines[lnNum];

                    DrawSingleLineString(ref textToWrite, ref newlineShift);

                    if (lnNum != lines.Length - 1)
                        newlineShift -= mNewLineDistanceForVertexBuffer;
                }
            }
            else
            {
                DrawSingleLineString(ref txt, ref newlineShift);
            }

        }

        static private void DrawSingleLineString(ref string textToWrite, ref float newlineShift)
        {
            if (mMaxWidthForVertexBuffer != 0)
                textToWrite = textToWrite.Substring(0, GetNumberOfCharsIn(mMaxWidthForVertexBuffer, textToWrite, mSpacingForVertexBuffer));

            if (textToWrite == "") return;

            int asciiNumber;

            //	If the string is X characters long, then the length of the spaces is
            // X-1.  That is, a 2 letter string will have only 1 space between the letters.

            float stringWidth = GetWidth(textToWrite, mSpacingForVertexBuffer);


            #region Gui text
            float halfLastWidth = 0;

            float halfFirstCharaterWidth = 0;
            if (textToWrite != null && textToWrite != "")
            {
                halfFirstCharaterWidth = mDefaultFont.GetCharacterScaleX(textToWrite[0]) * mScaleForVertexBuffer;
            }


            Color color = new Color(new Vector4(mRedForVertexBuffer, mGreenForVertexBuffer,
                mBlueForVertexBuffer, mAlphaForVertexBuffer));

            // keep the centerX a certain number of pixels from the edge
            
            float leftEdge = (SpriteManager.Camera.X - SpriteManager.Camera.RelativeXEdgeAt(mZForVertexBuffer));

            float unitsFromEdge = mXForVertexBuffer - leftEdge;
            //unitsFromEdge = Text.PixelPerfectOffset + Math.MathFunctions.RoundFloat(unitsFromEdge, 1 / SpriteManager.Camera.PixelsPerUnitAt(mZForVertexBuffer));
            float tempXToUse =  MathFunctions.RoundFloat(leftEdge + unitsFromEdge, 
                GuiManager.XEdge * 2 /(float)SpriteManager.Camera.DestinationRectangle.Width);
            tempXToUse += SpriteManager.Camera.XEdge * .1f / (float)SpriteManager.Camera.DestinationRectangle.Width;

            float bottomEdge = (SpriteManager.Camera.Y - SpriteManager.Camera.RelativeYEdgeAt(mZForVertexBuffer));
            unitsFromEdge =
                mYForVertexBuffer - bottomEdge + newlineShift;
            //unitsFromEdge = Text.PixelPerfectOffset + Math.MathFunctions.RoundFloat(unitsFromEdge, 1 / SpriteManager.Camera.PixelsPerUnitAt(mZForVertexBuffer));
            unitsFromEdge = MathFunctions.RoundFloat(unitsFromEdge,
                                GuiManager.YEdge * 2 / (float)SpriteManager.Camera.DestinationRectangle.Height);
            
            unitsFromEdge += SpriteManager.Camera.YEdge * .1f / (float)SpriteManager.Camera.DestinationRectangle.Height;

            float sx = 0;
            if (mAlignmentForVertexBuffer == HorizontalAlignment.Left)
            { sx = tempXToUse; }
            else if (mAlignmentForVertexBuffer == HorizontalAlignment.Right)
            { sx = tempXToUse - stringWidth; }
            else if (mAlignmentForVertexBuffer == HorizontalAlignment.Center)
            { sx = halfFirstCharaterWidth + tempXToUse - stringWidth / 2.0f; }

#if SILVERLIGHT
            float sy = mYForVertexBuffer;
#else
            float sy = bottomEdge + unitsFromEdge;
#endif

            float tx1 = 0; float tx2 = 0; float ty1 = 0; float ty2 = 0;

            // loop through the letters
            for (int i = 0; i < textToWrite.Length; i++)
            {
                asciiNumber = (int)textToWrite[i];

                if (asciiNumber == ' ' || asciiNumber == '\n')
                {
                    sx += (mDefaultFont.GetCharacterSpacing(asciiNumber) / 2.0f) * mSpacingForVertexBuffer + halfLastWidth;

                    halfLastWidth = (mDefaultFont.GetCharacterSpacing(asciiNumber) / 2.0f) * mSpacingForVertexBuffer;
                    continue;

                }
                else if (halfLastWidth != 0)
                {
                    sx += .5f * mDefaultFont.GetCharacterSpacing(asciiNumber) * mSpacingForVertexBuffer + halfLastWidth;
                }
                mDefaultFont.AssignCharacterTextureCoordinates(asciiNumber, out ty1, out ty2, out tx1, out tx2);

                float halfWidth = .5f * mDefaultFont.GetCharacterSpacing(asciiNumber) * mScaleForVertexBuffer;
                float topCoordinate = sy + mScaleForVertexBuffer * (1 - mDefaultFont.DistanceFromTopOfLine(asciiNumber));
                float bottomCoordinate = topCoordinate - mScaleForVertexBuffer * mDefaultFont.GetCharacterHeight(asciiNumber);

                float ScaleX = mDefaultFont.GetCharacterScaleX(asciiNumber) * mScaleForVertexBuffer;

                #region fill the v vertices

                v[0].Position.X = sx - ScaleX;
                v[0].Position.Y = bottomCoordinate;
                v[0].Position.Z = mZForVertexBuffer;
                v[0].TextureCoordinate.X = tx1;
                v[0].TextureCoordinate.Y = ty2;
                v[0].Color = color;

                v[1].Position.X = sx - ScaleX;
                v[1].Position.Y = topCoordinate;
                v[1].Position.Z = mZForVertexBuffer;
                v[1].TextureCoordinate.X = tx1;
                v[1].TextureCoordinate.Y = ty1;
                v[1].Color = color;

                v[2].Position.X = sx + ScaleX;
                v[2].Position.Y = topCoordinate;
                v[2].Position.Z = mZForVertexBuffer;
                v[2].TextureCoordinate.X = tx2;
                v[2].TextureCoordinate.Y = ty1;
                v[2].Color = color;

                v[3] = v[0];

                v[4] = v[2];

                v[5].Position.X = sx + ScaleX;
                v[5].Position.Y = bottomCoordinate;
                v[5].Position.Z = mZForVertexBuffer;
                v[5].TextureCoordinate.X = tx2;
                v[5].TextureCoordinate.Y = ty2;
                v[5].Color = color;
                #endregion

#if SUPPORTS_FRB_DRAWN_GUI
                GuiManager.WriteVerts(v);
                // since spacing indicates how far apart letters are, we need to advance relative 
                // to spacing, not scale.
                halfLastWidth = .5f * mDefaultFont.GetCharacterSpacing(asciiNumber) * mSpacingForVertexBuffer;
#else
                throw new NotImplementedException();
#endif


            }


            //				if(lnNum != lines.Length - 1)
            //					format.Y += 2 * lines.Length;
            #endregion
        }


        #endregion

        internal static void DrawTexts(IList<Text> texts, int startIndex,
    int numToDraw, Camera camera)
        {
            double currentTime = TimeManager.CurrentTime;
            int count = mAutomaticallyUpdatedTexts.Count;

            Matrix matrix = Matrix.Identity;

           
                //Matrix.Identity;
                //SpriteManager.Camera.GetLookAtMatrix();

            //camera.SetDeviceViewAndProjection(mBasicEffect, false);
            SpriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend, 
                SamplerState.LinearClamp, 
                Renderer.GraphicsDevice.DepthStencilState, 
                Renderer.GraphicsDevice.RasterizerState,
                null,
                matrix

                );

            //SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, Renderer.GraphicsDevice.DepthStencilState, Renderer.GraphicsDevice.RasterizerState, mBasicEffect, matrix);


            //foreach (Text t in mAutomaticallyUpdatedTexts)
            int endIndex = startIndex + numToDraw;

            for (int i = startIndex; i < endIndex; i++)
            {
                Text t = texts[i];

                if (t.AbsoluteVisible)
                {
                    #region Get the SpriteFont to use

                    SpriteFont spriteFont = t.SpriteFont;

                    if (spriteFont == null)
                    {
                        spriteFont = TextManager.DefaultSpriteFont;
                    }


                    #endregion

                    #region Get the offset to be used due to the alignment settings

                    Vector2 stringDimensions = spriteFont.MeasureString(t.DisplayText);

                    float scaleValue = 1;

                    float xOffset = 0;
                    float yOffset = 0;



                    #region Vertical Alignment offset adjustments

                    
                    if (t.VerticalAlignment == VerticalAlignment.Top)
                    {
                        yOffset = 0;
                    }
                    if (t.VerticalAlignment == VerticalAlignment.Center)
                    {
                        yOffset = -.5f * scaleValue * stringDimensions.Y;
                    }
                    else if (t.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        yOffset = -scaleValue * stringDimensions.Y;
                    }
                    #endregion

                    #region Horizontal Alignment offset adjustments

                    if (t.HorizontalAlignment == HorizontalAlignment.Center)
                    {
                        xOffset = -.5f * scaleValue * stringDimensions.X;
                    }
                    else if (t.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        xOffset = -scaleValue * stringDimensions.X;
                    }


                    #endregion





                    #endregion


                    // I dunno, maybe we need to make the rotation negative on FSB too?

                    FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(
                        0, 0,
                        ref xOffset, ref yOffset, -t.RotationZ);

                    Color color = new Color(
                        t.Red, t.Green, t.Blue, t.Alpha);

                    Vector3 positionIn2D = Renderer.GraphicsDevice.Viewport.Project(t.Position, SpriteManager.Camera.Projection,
                        SpriteManager.Camera.View, Matrix.Identity);

                    positionIn2D.X += xOffset;
                    positionIn2D.Y += yOffset;

                    SpriteBatch.DrawString(spriteFont,
                       t.DisplayText,
                       new Vector2(positionIn2D.X, positionIn2D.Y),
                       color,
                       -t.RotationZ,
                       new Vector2(),
                       1,
                       SpriteEffects.None,
                       0);

                }
            }

            SpriteBatch.End();
        }

        internal static float GetRelativeOffset(int chars, int start, string text)
        {

            float relX = 0;
            int asciiNumber;

            for (int i = start; i < chars; i++)
            {
                asciiNumber = (int)text[i];
                relX += mDefaultFont.GetCharacterSpacing(asciiNumber);
            }
            return relX;
        }


        internal static void Pause(InstructionList instructions)
        {
            for(int i = 0; i < mAutomaticallyUpdatedTexts.Count; i++)
            {
                Text text = mAutomaticallyUpdatedTexts[i];

                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(text))
                {
                    text.Pause(instructions);
                }
            }
        }


        internal static void RefreshBitmapFontTextures()
        {
            foreach (Text text in mAutomaticallyUpdatedTexts)
            {
                // No need to pass the ContentManager names because the Texture2Ds should have
                // already been reloaded using the appropriate name.
                for (int i = 0; i < text.Font.mTextures.Length; i++)
                {
                    text.Font.mTextures[i] =
                        FlatRedBallServices.Load<Texture2D>(text.Font.mTextures[i].SourceFile());
                }
            }
        }


        public static void Update()
        {
            for (int i = mAutomaticallyUpdatedTexts.Count - 1; i > -1; i--)
            {
                if (i < mAutomaticallyUpdatedTexts.Count)
                {
                    mAutomaticallyUpdatedTexts[i].ExecuteInstructions(TimeManager.CurrentTime);
                }
            }
            
            for (int i = 0; i < mAutomaticallyUpdatedTexts.Count; i++)
            {
                mAutomaticallyUpdatedTexts[i].TimedActivity(
                    TimeManager.SecondDifference, 
                    TimeManager.SecondDifferenceSquaredDividedByTwo, 
                    TimeManager.LastSecondDifference);
            }
        }


        internal static void UpdateDependencies()
        {
            //Flush();

            double currentTime = TimeManager.CurrentTime;
            int count = mAutomaticallyUpdatedTexts.Count;
            for (int i = 0; i < count; i++)
            {
                mAutomaticallyUpdatedTexts[i].UpdateDependencies(currentTime);
            }
        }

        #endregion

        #region Private Methods

        //private static void Flush()
        //{
        //    mAutomaticallyUpdatedTextBuffer.Flush();
        //    mDrawnTextBuffer.Flush();
        //}

        #endregion

        #endregion

    }
}
