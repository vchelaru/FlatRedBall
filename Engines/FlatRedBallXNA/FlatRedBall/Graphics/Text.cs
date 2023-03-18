using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Gui;
using Point = Microsoft.Xna.Framework.Point;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Math.Geometry;

using StringFunctions = FlatRedBall.Utilities.StringFunctions;
using FlatRedBall.Input;
using FlatRedBall.Math;

namespace FlatRedBall.Graphics
{
    #region MaxWidthBehavior enum

    public enum MaxWidthBehavior
    {
        Chop,
        Wrap
    }

    #endregion

    public enum FontType
    {
        BitmapFont,
        SpriteFont,
        BitmapFontOnTexture
    }

    public partial class Text : PositionedObject, IColorable, IReadOnlyScalable,
        ICursorSelectable, IEquatable<Text>, IVisible, IMouseOver
    {
        #region Fields

        #region Color and Blend

        float mAlpha;
        float mRed;
        float mGreen;
        float mBlue;

        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;
#if FRB_MDX
        Microsoft.DirectX.Direct3D.TextureOperation mColorOperation = 
            Microsoft.DirectX.Direct3D.TextureOperation.Modulate;
#else
        ColorOperation mColorOperation = ColorOperation.Modulate;
#endif
        BlendOperation mBlendOperation;
        #endregion

        #region Scale and Spacing

        float mTextureScale;

        float mSpacing;
        float mScale;

        float mWidth;
        float mHeight;

        float mSpacingVelocity;
        float mScaleVelocity;

        float mNewLineDistance;

        MaxWidthBehavior mMaxWidthBehavior;
        #endregion

        #region Fields used in drawing
        internal VertexPositionColorTexture[] mVertexArray;
        int mVertexCount;

        internal List<Point> mInternalTextureSwitches = new List<Point>();

        // January 6, 2012
        // This used to default
        // to false because it wasn't
        // working properly.  However, it's
        // since been fixed and tweaked a lot
        // on a number of commercial games.  Now
        // I think it should default to true.
        //bool mAdjustPositionForPixelPerfectDrawing = false;
        bool mAdjustPositionForPixelPerfectDrawing = true;

        internal Vector3 mOldPosition; // used when sorting along forward vector to hold old position
        #endregion

        #region Alignment
        VerticalAlignment mVerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment mHorizontalAlignment = HorizontalAlignment.Left;
        #endregion

        private FontType FontType;

        string mText;
        internal string mAdjustedText;

        bool mVisible;

        float mMaxWidth;

        BitmapFont mFont;

        bool mCursorSelectable = true;

        private bool _renderOnTexture;

        private Sprite mPreRenderedSprite;
        private string _texturesText;

        #endregion

        #region Properties

        public float TextureScale
        {
            get
            {
                return mTextureScale;
            }
            set
            {
                mTextureScale = value;


                Scale = mTextureScale * .5f *
                    mFont.LineHeightInPixels;
                Spacing = Scale;


                if (AdjustPositionForPixelPerfectDrawing)
                {
                    NewLineDistance = (float)System.Math.Round(Scale * 1.5f);
                }
                else
                {
                    NewLineDistance = Scale * 1.5f;
                }

                UpdateDisplayedText();
                UpdateDimensions();

            }
        }

        public bool AdjustPositionForPixelPerfectDrawing
        {
            get { return mAdjustPositionForPixelPerfectDrawing; }
            set { mAdjustPositionForPixelPerfectDrawing = value; }

        }


        public Camera CameraToAdjustPixelPerfectTo
        {
            get;
            set;
        }

        public Layer LayerToAdjustPixelPerfectTo
        {
            get;
            set;
        }


        public bool CursorSelectable
        {
            get { return mCursorSelectable; }
            set { mCursorSelectable = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the string that the Text object is to display.
        /// </summary>
        #endregion
        public string DisplayText
        {
            get
            {
                return mText;
            }
            set
            {
                mText = value;
                UpdateDisplayedText();
                UpdateDimensions();
                // Are the next two lines needed?  I think the updates are performed automatically
                /*
                if (font == null)
                    UpdateVertices();
                 * */
            }
        }

        /// <summary>
        /// Returns the strin that the Text object will render.  This considers 
        /// the MaxWidth and MaxWidthBehavior variables, meaning it may contain newlines.
        /// </summary>
        public string AdjustedText
        {
            get
            {
                return mAdjustedText;
            }
        }

        public bool RenderOnTexture
        {
            get { return _renderOnTexture; }
            set
            {
                _renderOnTexture = value;
                if (_renderOnTexture)
                {
                    switch (FontType)
                    {
                        case FontType.BitmapFont:
                            FontType = FontType.BitmapFontOnTexture;
                            UpdatePreRenderedTextureAndSprite();
                            break;
                    }
                }
                else
                {
                    RemoveAndDisposePreRenderedObjects();
                }
                UpdateDisplayedText();
            }
        }

        public BitmapFont Font
        {
            get { return mFont; }
            set 
            { 
                mFont = value; 
                FontType = RenderOnTexture ? FontType.BitmapFontOnTexture : FontType.BitmapFont;
                
                UpdateDisplayedText(); 
                UpdateDimensions();
            }
        }


        public HorizontalAlignment HorizontalAlignment
        {
            get { return mHorizontalAlignment; }
            set { mHorizontalAlignment = value; }
        }

        #region XML Docs
        /// <summary>
        /// Returns the center of the text object.
        /// </summary>
        /// <remarks>
        /// If the text is centered (the format.alignment equals TextManager.Alignment.CENTER),
        /// this will simply return the x value of the text.  Otherwise, this property
        /// calculates the center of the text based on the contained string, the format, whether
        /// the text uses 3D fonts, and the x position of the text.
        /// </remarks>
        #endregion
        public float HorizontalCenter
        {
            get
            {
                if (mHorizontalAlignment == HorizontalAlignment.Center)
                {
                    return X;
                }
                else if (mHorizontalAlignment == HorizontalAlignment.Left)
                {
                    return X + this.mWidth / 2.0f;
                }
                else // (mHorizontalAlignment == HorizontalAlignment.Right)
                {
                    return X - mWidth / 2.0f;
                }
            }
        }


        #region IColorable
        public float Alpha
        {
            get { return mAlpha; }
            set
            {
                value =
                    System.Math.Min(GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(0, value);

                mAlpha = value;

                if (RenderOnTexture)
                    UpdateDisplayedText();
            }
        }

        public float Red
        {
            get { return mRed; }
            set
            {
                mRed = value;
                if(RenderOnTexture)
                    UpdateDisplayedText();
            }
        }

        public float Green
        {
            get { return mGreen; }
            set
            {
                mGreen = value;
                if (RenderOnTexture)
                    UpdateDisplayedText();
            }
        }

        public float Blue
        {
            get { return mBlue; }
            set
            {
                mBlue = value;
                if (RenderOnTexture)
                    UpdateDisplayedText();
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the alpha component in units per second.
        /// </summary>
        #endregion
        public float AlphaRate
        {
            get { return mAlphaRate; }
            set { mAlphaRate = value; }
        }

        public float RedRate
        {
            get { return mRedRate; }
            set
            {
                mRedRate = value;
            }
        }

        public float GreenRate
        {
            get { return mGreenRate; }
            set
            {
                mGreenRate = value;
            }
        }

        public float BlueRate
        {
            get { return mBlueRate; }
            set
            {
                mBlueRate = value;
            }
        }

#if FRB_MDX
        public Microsoft.DirectX.Direct3D.TextureOperation ColorOperation
#else
        public ColorOperation ColorOperation
#endif
        {
            get { return mColorOperation; }
            set { mColorOperation = value; }
        }

        public BlendOperation BlendOperation
        {
            get { return mBlendOperation; }
            set { mBlendOperation = value; }
        }
        #endregion


        public float NewLineDistance
        {
            get { return mNewLineDistance; }
            set
            {
#if DEBUG
                if(float.IsPositiveInfinity(value))
                {
                    throw new Exception("Value cannot be positive infinity");
                }
                if (float.IsNegativeInfinity(value))
                {
                    throw new Exception("Value cannot be negative infinity");
                }
                if (float.IsNaN(value))
                {
                    throw new Exception("Value is not a valid float");
                }
#endif
                mNewLineDistance = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the number of lines in the text object.
        /// </summary>
        /// <remarks>
        /// This currently reports the number of lines only when the text
        /// object is using a bitmap font.  3D text does not use the newline
        /// character.
        /// </remarks>
        #endregion
        public int NumberOfLines
        {
            get
            {
                if (mAdjustedText == null)
                {
                    return 1;
                }

                int i = 1;
                for (int j = 0; j < mAdjustedText.Length; j++)
                {
                    if (mAdjustedText[j] == '\n')
                        i++;
                }
                return i;
            }
        }

        public MaxWidthBehavior MaxWidthBehavior
        {
            get { return mMaxWidthBehavior; }
            set
            {
                mMaxWidthBehavior = value;
                UpdateDisplayedText();
                UpdateDimensions();

            }
        }


        #region XML Docs
        /// <summary>
        /// The maximum width of the text in world units.  This modifies the DisplayedText property.
        /// </summary>
        #endregion
        public float MaxWidth
        {
            get { return mMaxWidth; }
            set
            {
                // It doesn't make sense to have a MaxWidth less than 0.
                mMaxWidth = System.Math.Max(value, 0);
                UpdateDisplayedText();
                UpdateDimensions();

            }
        }


        public int MaxCharacters
        {
            get;
            set;
        }


        public float Spacing
        {
            get { return mSpacing; }
            set
            {
#if DEBUG
                if (float.IsPositiveInfinity(value))
                {
                    throw new Exception("Value cannot be positive infinity");
                }
                if (float.IsNegativeInfinity(value))
                {
                    throw new Exception("Value cannot be negative infinity");
                }
                if (float.IsNaN(value))
                {
                    throw new Exception("Value is not a valid float");
                }
#endif
                mSpacing = value;
                UpdateDisplayedText();
                UpdateDimensions();

            }
        }


        public float Scale
        {
            get { return mScale; }
            set
            {
#if DEBUG
                if (float.IsPositiveInfinity(value))
                {
                    throw new Exception("Value cannot be positive infinity");
                }
                if (float.IsNegativeInfinity(value))
                {
                    throw new Exception("Value cannot be negative infinity");
                }
                if (float.IsNaN(value))
                {
                    throw new Exception("Value is not a valid float");
                }
#endif
                mScale = value;
                UpdateDisplayedText();
                UpdateDimensions();

            }
        }


        public float SpacingVelocity
        {
            get { return mSpacingVelocity; }
            set { mSpacingVelocity = value; }
        }


        public float ScaleVelocity
        {
            get { return mScaleVelocity; }
            set { mScaleVelocity = value; }
        }

        #region XML Docs
        /// <summary>
        /// Returns the distance from the center of the Text object to the edge;
        /// </summary>
        #endregion
        [Obsolete("Use Width/2.0f instead")]
        public float ScaleX
        {
            get
            {
                return mWidth / 2.0f;
            }
        }

        public float Width
        {
            get
            {
                return mWidth;
            }
        }

        [Obsolete("Use Height/2.0f instead")]
        public float ScaleY
        {
            get
            {
                return mHeight / 2.0f;
            }
        }

        public float Height
        {
            get
            {
                return mHeight;
            }
        }

        private SpriteFont mSpriteFont;

        public SpriteFont SpriteFont
        {
            get { return mSpriteFont; }
            set
            {
                mSpriteFont = value;
                FontType = FontType.SpriteFont;
                UpdateDisplayedText();
                UpdateDimensions();

            }
        }


        public VerticalAlignment VerticalAlignment
        {
            get { return mVerticalAlignment; }
            set { mVerticalAlignment = value; }
        }

        #region XML Docs
        /// <summary>
        /// Returns the vertical center of the text.
        /// </summary>
        /// <remarks>
        /// Since the y value of text marks either the top or bottom of the Text
        /// depending on vertical alignment, this value can be useful for finding
        /// the Text's center.
        /// </remarks>
        #endregion
        public float VerticalCenter
        {
            get
            {
                if (this.mVerticalAlignment == VerticalAlignment.Top)
                {
                    return this.Y - this.Height / 2.0f;
                }
                else if (this.mVerticalAlignment == VerticalAlignment.Center)
                {
                    return this.Y;
                }
                else
                {
                    return this.Y + this.Height / 2.0f;

                }
            }
        }


        internal VertexPositionColorTexture[] VertexArray
        {
            get { return mVertexArray; }
        }


        public int VertexCount
        {
            get
            {
                return mVertexCount;
            }
        }


        public virtual bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }

        #endregion

        #region Methods

        #region Constructor

        public Text()
            : this(TextManager.DefaultFont)

        {
            MaxCharacters = int.MaxValue;
        }


        public Text(BitmapFont font)
            : base()
        {
            MaxCharacters = int.MaxValue;


            CameraToAdjustPixelPerfectTo = SpriteManager.Camera;

            mFont = font;

            mText = "";
            mAdjustedText = "";

            mVisible = true;

            mColorOperation = ColorOperation.ColorTextureAlpha;

            mBlendOperation = BlendOperation.Regular;
            mNewLineDistance = 1.4f;

            mScale = 1;
            mSpacing = 1;


            mRed = 1;
            mGreen = 1;
            mBlue = 1;
            mAlpha = 1;

            mMaxWidth = float.PositiveInfinity;
        }

        #endregion

        #region Public Static

        public static PositionedObjectList<Text> GetTextsInPolygon(string text, float spacing, float startingY, float newLineDistance, BitmapFont font, Polygon polygon)
        {
            // This currently allows newLineDistance to be a decimal - this could cause rendering issues.  Do we want to restrict it?

            PositionedObjectList<Text> texts = new PositionedObjectList<Text>();

            polygon.ForceUpdateDependencies();

            Vector3 position = new Vector3(polygon.X, startingY, 0);

            List<float> xValues = new List<float>();
            bool shouldContinue = true;

            float currentY = startingY;

            int letterOn = 0;
            int lineOn = 0;

            while (shouldContinue)
            //if (!polygon.IsPointInside(ref position))
            {
                shouldContinue = false;

                Segment segment = new Segment(new Vector3(position.X, currentY, position.Z), new Vector3(position.X - 100000, currentY, position.Z));


                FlatRedBall.Math.Geometry.Point intersectionPoint;

                bool hasIntersected = false;

                if (polygon.Intersects(segment, out intersectionPoint))
                {
                    hasIntersected = true;
                }
                else
                {
                    segment = new Segment(new Vector3(position.X, currentY, position.Z), new Vector3(position.X + 100000, currentY, position.Z));

                    if (polygon.Intersects(segment, out intersectionPoint))
                    {
                        hasIntersected = true;

                    }
                }

                float startingX = (float)intersectionPoint.X;
                if (hasIntersected)
                {

                    intersectionPoint.X += spacing;

                    segment = new Segment(new Vector3(position.X, currentY, position.Z), new Vector3(position.X + 100000, currentY, position.Z));

                    if (polygon.Intersects(segment, out intersectionPoint))
                    {
                        float width = (float)intersectionPoint.X - startingX;
                        shouldContinue = TextManager.InsertOneNewLine(ref text, ref letterOn, ref lineOn, font, spacing, width);
                        currentY -= newLineDistance;
                    }




                }
                else
                {
                    startingX = polygon.Position.X;
                }
                xValues.Add(startingX);

            }

            char[] separator = new char[] { '\n' };

            string[] splitText = text.Split(separator);

            for (int i = 0; i < splitText.Length; i++)
            {
                string s = splitText[i];

                Text newText = new Text(font);

                // The xValues define the start of each line.  It's possible that
                // a text will go beyond the end of a polygon, but it could still have
                // newlines naturally (as opposed to newlines inserted by the algorithm above).
                // If that's the case, that means we will have newlines beyond the 
                if (i >= xValues.Count)
                {
                    newText.X = xValues[xValues.Count - 1];
                }
                else
                {
                    newText.X = xValues[i];
                }
                newText.Y = startingY - i * newLineDistance;

                newText.Scale = spacing;
                newText.Spacing = spacing;

                newText.DisplayText = s;
                texts.Add(newText);

            }

            return texts;
        }


        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Creates a new Text object.
        /// </summary>
        /// <returns>
        /// Reference to the new Text object.  The object will not 
        /// be in the TextManager's memory.
        /// </returns>
        #endregion
        public Text Clone()
        {
            Text newText = base.Clone<Text>();

            newText.mVertexArray = new VertexPositionColorTexture[0];

            newText.mInternalTextureSwitches = new List<Point>();

            newText.mRed = mRed;
            newText.mGreen = mGreen;
            newText.Blue = mBlue;

            newText.mColorOperation = mColorOperation;

            newText.mAlpha = mAlpha;
            newText.mBlendOperation = mBlendOperation;

            newText.mHorizontalAlignment = mHorizontalAlignment;
            newText.mVerticalAlignment = mVerticalAlignment;

            newText.mSpacing = mSpacing;
            newText.mScale = mScale;

            newText.DisplayText = DisplayText;

            newText.MaxWidth = MaxWidth;
            newText.MaxWidthBehavior = MaxWidthBehavior;

            newText.AdjustPositionForPixelPerfectDrawing = AdjustPositionForPixelPerfectDrawing;

            return newText;
        }

        public override void ForceUpdateDependencies()
        {
            base.ForceUpdateDependencies();

            UpdateInternalRenderingVariables();

        }

        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();
            UpdateInternalRenderingVariables();

        }

        public void InsertNewLines(float maxWidth)
        {
            mAdjustedText = TextManager.InsertNewLines(mAdjustedText, mSpacing, maxWidth, Font);

        }


        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.TextUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.TextUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }


        /// <summary>
        /// Set the RGB properties of this all at once. 0,0,0 is Black, 1,1,1 is White.
        /// </summary>
        /// <param name="red">0 to 1</param>
        /// <param name="green">0 to 1</param>
        /// <param name="blue">0 to 1</param>
        public void SetColor(float red, float green, float blue)
        {
#if DEBUG
            if(red > 1)
            {
                throw new ArgumentException("Red must be less than 1");
            }
            if (green > 1)
            {
                throw new ArgumentException("Green must be less than 1");
            }
            if (blue > 1)
            {
                throw new ArgumentException("Blue must be less than 1");
            }
#endif

            mRed = red;
            mGreen = green;
            mBlue = blue;

            if (RenderOnTexture)
                UpdateDisplayedText();

        }

        public void SetColor(Microsoft.Xna.Framework.Color color)
        {
            SetColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }

        /// <summary>
        /// Sets the Scale and Spacing such that the Text is drawn pixel-perfect at its given Z position.
        /// </summary>
        /// <param name="camera">Reference to the camera to use when calculating the Scale and Spacing.</param>
        public void SetPixelPerfectScale(Camera camera, float multiple = 1)
        {
            int lineHeight = 0;

            if (this.SpriteFont != null)
            {
                lineHeight = SpriteFont.LineSpacing;
            }
            else
            {
                if (mFont != null)
                {
                    lineHeight = mFont.LineHeightInPixels;
                }
                else
                {
                    return;
                }
            }

            var pixelsPerUnit = camera.PixelsPerUnitAt(ref this.Position);

#if DEBUG
            if(pixelsPerUnit == 0 || float.IsPositiveInfinity(pixelsPerUnit) || float.IsNegativeInfinity(pixelsPerUnit))
            {
                throw new Exception(
                    $"Could not set pixel perfect scale because of invalid pixelsPerUnit:{pixelsPerUnit}. camera position:{camera.Position}, text position:{this.Position}");
            }
#endif

            Scale = .5f *
                mFont.LineHeightInPixels / pixelsPerUnit;
            Spacing = Scale;


            if (AdjustPositionForPixelPerfectDrawing)
            {

                // If we are using a 3D camera, then the line height might be something like
                // 1.4.  We don't want to round it if not ortho
                if (camera.Orthogonal)
                {
                    NewLineDistance = (float)System.Math.Round(Scale * 1.5f);
                }
                else
                {
                    NewLineDistance = (float)Scale * 1.5f;
                }
            }
            else
            {
                NewLineDistance = Scale * 1.5f;
            }

            Scale *= multiple;
            Spacing *= multiple;
            NewLineDistance *= multiple;

            UpdateDisplayedText();
            UpdateDimensions();

        }

        /// <summary>
        /// Sets the Scale and Spacing such that the Text is drawn pixel-perfect at the given Z position.
        /// This method obeys the Layer's overridden field of view if it uses one.
        /// </summary>
        /// <param name="layer"></param>
        public void SetPixelPerfectScale(Layer layer)
        {
            if (mFont == null)
            {
                throw new NullReferenceException("The Text object must have a non-null font to be scaled");
            }

            if (layer != null && layer.LayerCameraSettings != null)
            {
                LayerCameraSettings lcs = layer.LayerCameraSettings;

                // Any camera can be used here since the fieldOfView is being overridden:
                if (layer.CameraBelongingTo == null)
                {
                    Scale = .5f * mFont.LineHeightInPixels /
                        SpriteManager.Camera.PixelsPerUnitAt(ref this.Position, lcs.FieldOfView, lcs.Orthogonal, lcs.OrthogonalHeight);
                }
                else
                {
                    Scale = .5f * mFont.LineHeightInPixels /
                        layer.CameraBelongingTo.PixelsPerUnitAt(ref this.Position, lcs.FieldOfView, lcs.Orthogonal, lcs.OrthogonalHeight);

                }
            }
            else if (layer != null && layer.CameraBelongingTo != null)
            {

                Scale = .5f * mFont.LineHeightInPixels / layer.CameraBelongingTo.PixelsPerUnitAt(ref this.Position);
            }
            else
            {
                var camera = Camera.Main;
                bool destinationRectangleHasArea = camera.DestinationRectangle.Width != 0 && camera.DestinationRectangle.Height != 0;
                if(destinationRectangleHasArea)
                {
                    var pixelsPerUnit = camera.PixelsPerUnitAt(ref this.Position);
                    Scale = .5f * mFont.LineHeightInPixels / pixelsPerUnit;
                }
            }

            Spacing = Scale;
            if (AdjustPositionForPixelPerfectDrawing)
            {
                NewLineDistance = (float)System.Math.Round(Scale * 1.5f);
            }
            else
            {
                NewLineDistance = Scale * 1.5f;
            }
        }


        public override void TimedActivity(float secondDifference, double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            Alpha += AlphaRate * secondDifference;
            Red += RedRate * secondDifference;
            Green += GreenRate * secondDifference;
            Blue += BlueRate * secondDifference;

            mScale += mScaleVelocity * secondDifference;
            mSpacing += mSpacingVelocity * secondDifference;
        }


        public override string ToString()
        {
            return this.mAdjustedText;
        }


        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);

            UpdateInternalRenderingVariables();
        }

        public void UpdateInternalRenderingVariables()
        {
            // for thread safety
            string text = mAdjustedText;

            if (FontType == FontType.BitmapFontOnTexture)
            {
                if (!AbsoluteVisible)
                {
                    mPreRenderedSprite.Visible = false;
                    mVertexArray = null;
                }
                else
                {
                    var changed = false;

                    switch (HorizontalAlignment)
                    {
                        case HorizontalAlignment.Right:
                            if (mPreRenderedSprite.X != X - mPreRenderedSprite.ScaleX)
                            {
                                mPreRenderedSprite.X = X - mPreRenderedSprite.ScaleX;
                                changed = true;
                            }

                            break;
                        case HorizontalAlignment.Left:
                            if (mPreRenderedSprite.X != X + mPreRenderedSprite.ScaleX)
                            {
                                mPreRenderedSprite.X = X + mPreRenderedSprite.ScaleX;
                                changed = true;
                            }

                            break;
                        case HorizontalAlignment.Center:
                            if (X != mPreRenderedSprite.X)
                            {
                                mPreRenderedSprite.X = X;
                                changed = true;
                            }

                            break;
                    }

                    switch (VerticalAlignment)
                    {
                        case VerticalAlignment.Bottom:
                            if (mPreRenderedSprite.Y != Y + mPreRenderedSprite.ScaleY)
                            {
                                mPreRenderedSprite.Y = Y + mPreRenderedSprite.ScaleY;
                                changed = true;
                            }

                            break;
                        case VerticalAlignment.Top:
                            if (mPreRenderedSprite.Y != Y - mPreRenderedSprite.ScaleY)
                            {
                                mPreRenderedSprite.Y = Y - mPreRenderedSprite.ScaleY;
                                changed = true;
                            }

                            break;
                        case VerticalAlignment.Center:
                            if (Y != mPreRenderedSprite.Y)
                            {
                                mPreRenderedSprite.Y = Y;
                                changed = true;
                            }

                            break;
                    }

                    if (changed)
                        SpriteManager.ManualUpdate(mPreRenderedSprite);
                }
            }
            else
            {

                // 4/12/2011
                // I'm not sure
                // why, but we were
                // updating the vertex
                // array on Texts even when
                // they were invisible.  This
                // seems wasteful for me, so I 
                // wrapped it in this if statement.
                // I'm putting this comment here just
                // in case this causes issues that I haven't
                // thought of.
                if (this.AbsoluteVisible)
                {
                    mVertexArray = FillVertexArray(
                        mVertexArray,
                        this,
                        ref mVertexCount,
                        mSpacing,
                        mScale,
                        mFont,
                        ref Position,
                        mHorizontalAlignment,
                        mVerticalAlignment,
                        mRed,
                        mGreen,
                        mBlue,
                        mAlpha,
                        mNewLineDistance,
                        ref mRotationMatrix,
                        mAdjustPositionForPixelPerfectDrawing,
                        CameraToAdjustPixelPerfectTo,
                        mInternalTextureSwitches,
                        MaxCharacters);

                }
            }
        }


        #endregion

        #region Protected Methods



        #endregion

        #region Internal Methods

        static List<float> mNudgeAmounts = new List<float>();
        static List<float> widthListForDrawing = new List<float>();



        internal static VertexPositionColorTexture[] FillVertexArray(VertexPositionColorTexture[] vertexArray,
            Text textInstance, ref int vertexCount, float mSpacing, float mScale, BitmapFont mFont, ref Vector3 Position,
            HorizontalAlignment mHorizontalAlignment, VerticalAlignment mVerticalAlignment,
            float mRed, float mGreen, float mBlue, float mAlpha, float mNewLineDistance, ref Matrix mRotationMatrix,
            bool adjustPositionForPixelPerfectDrawing, Camera camera, List<Point> internalTextureSwitches, int maxCharacterCount)
        {
            string text = textInstance.mAdjustedText;
                //        public TransformedColoredTextured[] GetVertArray(string text, 
                //          TextFormat format, Matrix rotationMatrix, BitmapFont bitmapFont)
                internalTextureSwitches.Clear();

                #region ///////////////EARLY OUT!!!!!!!!!////////////////////
                if (text == null || text.Length == 0 || mFont == null || mScale == 0)
                {
                    vertexArray = null;
                    vertexCount = 0;
                    return null;
                }
                #endregion

                #region create the variables that this method will use
                float tx1 = 0;
                float tx2 = 0;
                float ty1 = 0;
                float ty2 = 0;
                int asciiNumber;
                float sx = 0;
                float sy = 0;

                int vertOn = 0;

                float centerX, centerY, centerZ;
                #endregion

                #region get the string width which is used if the alignment is center or right

                float stringWidth =
                    TextManager.GetWidth(text, mSpacing, mFont, 0, text.Length, widthListForDrawing);

                //List<float> lineWidths = TextManager.GetLineWidth(text, mSpacing, mFont);
                float halfFirstCharaterWidth = 0;
                if (text != null && text != "")
                {
                    halfFirstCharaterWidth = mFont.GetCharacterScaleX(text[0]) * mScale;
                }
                #endregion

                // the array should only be recreated if the length if changing.  


                if (vertexArray == null || StringFunctions.CharacterCountWithoutWhitespace(text) * 6 > vertexArray.Length)
                    vertexArray = new VertexPositionColorTexture[StringFunctions.CharacterCountWithoutWhitespace(text) * 6];

                centerX = Position.X;
                centerY = Position.Y;
                centerZ = Position.Z;
                float nudgeOnYAxis = 0;

                ApplyNudges(mScale, mFont, ref Position, mHorizontalAlignment, mVerticalAlignment, ref mNewLineDistance, adjustPositionForPixelPerfectDrawing, camera, ref centerX, ref centerY, ref centerZ, ref nudgeOnYAxis);

                #region Set sx according to the alignment (LEFT, RIGHT, CENTER)
                if (mHorizontalAlignment == HorizontalAlignment.Left)
                {
                    sx = centerX;
                }
                else if (mHorizontalAlignment == HorizontalAlignment.Right && widthListForDrawing.Count > 0)
                {
                    sx = centerX - widthListForDrawing[0];
                }
                else if (mHorizontalAlignment == HorizontalAlignment.Center && widthListForDrawing.Count > 0)
                {
                    sx = centerX - widthListForDrawing[0] / 2.0f; ;
                }
                else
                {
                    // If we get here, that means that we probably have a bad FNT file, but 
                    // I'm not certain that this should never happen, so we're going to tolerate
                    // it for now.
                    sx = centerX;
                }
                #endregion

                #region Set sy according to the horizontal alignment


                switch (mVerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        {
                            int numberOfLines = widthListForDrawing.Count;

                            float height = (numberOfLines - 1) * mNewLineDistance;


                            sy = centerY + height + mScale;

                            //sy = centerY + mScale;

                            break;
                        }
                    case VerticalAlignment.Center:
                        {
                            int numberOfLines = widthListForDrawing.Count;

                            float height = (numberOfLines - 1) * mNewLineDistance;

                            sy = centerY + height / 2.0f;

                            break;
                        }
                    case VerticalAlignment.Top:
                        {
                            //int numberOfLines = lineWidths.Count;

                            //float height = (numberOfLines - 1) * mNewLineDistance;


                            //sy = centerY + height;





                            sy = centerY - mScale;

                            break;
                        }
                }

                #endregion


                #region Get the drawing color


                

#if (XNA4 && !MONOGAME)
                // Colors in XNA4 are premult
                //Color color = new Color(new Vector4(mRed * mAlpha, mGreen * mAlpha, mBlue * mAlpha, mAlpha));
                Color color = Color.FromNonPremultiplied((int)(mRed * 255), (int)(mGreen * 255), (int)(mBlue * 255), (int)(mAlpha * 255));
#elif MONOGAME
            // Seems like the color values are used no matter what in monogame, 
            // so we need to simply use the Alpha values if we're using ColorOperation.Texture or ColorOperation.None
            Color color;
            if (textInstance.ColorOperation == ColorOperation.Texture)
            {
                color = new Color(new Vector4(mAlpha, mAlpha, mAlpha, mAlpha));
            }
            else
            {
                color = new Color(new Vector4(mRed * mAlpha, mGreen * mAlpha, mBlue * mAlpha, mAlpha));
            }
#endif

                #endregion

                int lineNumberOn = 0;

                float lastXAdvance = 0;


                float relativeX = 0;
                float relativeY = 0;

                float topCoordinate;
                float bottomCoordinate;

                float width;

                Vector3 position = Vector3.Zero;

            char letterAsChar;

                int lastPageNumber = 0;
                int stringLength = text.Length;
                for (int letterOn = 0; letterOn < stringLength; letterOn++)
                {
                    // used to eliminate bounds checks
                    letterAsChar = text[letterOn];


                    asciiNumber = (int)letterAsChar;

                    BitmapCharacterInfo characterInfo = mFont.GetCharacterInfo(asciiNumber);

                    #region If the character is '\n' (newline)

                    if (letterAsChar == '\n')
                    {
                        if (letterOn == text.Length - 1)
                            continue;

                        lineNumberOn++;

                        sy -= mNewLineDistance;

                        if (adjustPositionForPixelPerfectDrawing && lineNumberOn > mNudgeAmounts.Count)
                        {
                            centerX = Position.X + camera.RotationMatrix.M11 * mNudgeAmounts[lineNumberOn] + camera.RotationMatrix.M21 * nudgeOnYAxis;
                        }

                        if (mHorizontalAlignment == HorizontalAlignment.Center)
                        {
                            sx = centerX - widthListForDrawing[lineNumberOn] / 2.0f;
                        }
                        else if (mHorizontalAlignment == HorizontalAlignment.Right)
                        {
                            sx = centerX - widthListForDrawing[lineNumberOn];
                        }
                        else
                        {
                            sx = centerX;

                        }

                        lastXAdvance = 0;

                        continue;
                    }
                    #endregion

                    #region If it's whitespace

                    // July 14, 2014
                    // We used to consider
                    // any letters with a char
                    // value of < 32 to be whitespace,
                    // but this screws stuff up if we want
                    // to support glyphs.  So now we just check
                    // if the Char.IsWhiteSpace
                    //else if (letterAsChar < 32 || Char.IsWhiteSpace(letterAsChar))
                    else if (Char.IsWhiteSpace(letterAsChar))
                    {
                        sx += .5f * mFont.GetCharacterSpacing(asciiNumber) * mSpacing + lastXAdvance;

                        lastXAdvance = .5f * mFont.GetCharacterSpacing(asciiNumber) * mSpacing;

                        continue;
                    }

                    #endregion

                    #region Else, it's regular

                    else
                    {
                        sx += lastXAdvance;// this will be zero when writing the first letter or after a newline
                    }

                    #endregion


                    int pageNumber = characterInfo.PageNumber;

                    if (pageNumber != lastPageNumber)
                    {
                        // divide by 3 so we get the triangle number
                        internalTextureSwitches.Add(new Point(vertOn / 3, pageNumber));

                        lastPageNumber = pageNumber;
                    }

                    lastXAdvance = mFont.GetCharacterSpacing(asciiNumber) * mSpacing;


                    bool shouldRenderLetter = letterOn < maxCharacterCount;
                    if (shouldRenderLetter)
                    {
                        mFont.AssignCharacterTextureCoordinates(asciiNumber, out ty1, out ty2, out tx1, out tx2);

                        topCoordinate = sy + mScale * (1 - mFont.DistanceFromTopOfLine(asciiNumber));
                        bottomCoordinate = topCoordinate - mScale * mFont.GetCharacterHeight(asciiNumber);

                        width = mFont.GetCharacterScaleX(asciiNumber) * mScale * 2;
                        float sxWithOffset = sx + mFont.GetCharacterXOffset(asciiNumber) * mScale;

                        relativeX = sxWithOffset - centerX;
                        relativeY = bottomCoordinate - centerY;

                        position.X =
                            relativeX * mRotationMatrix.M11 +
                            relativeY * mRotationMatrix.M21 +
                            centerX;

                        position.Y =
                            relativeX * mRotationMatrix.M12 +
                            relativeY * mRotationMatrix.M22 +
                            centerY;

                        position.Z =
                            relativeX * mRotationMatrix.M13 +
                            relativeY * mRotationMatrix.M23 +
                            centerZ;


                        vertexArray[vertOn].Position = position;
                        vertexArray[vertOn].TextureCoordinate.X = tx1;
                        vertexArray[vertOn].TextureCoordinate.Y = ty2;
                        vertexArray[vertOn].Color = color;

                        vertOn++;

                        relativeX = sxWithOffset - centerX;
                        relativeY = topCoordinate - centerY;

                        position.X =
                            relativeX * mRotationMatrix.M11 +
                            relativeY * mRotationMatrix.M21 +
                            centerX;

                        position.Y =
                            relativeX * mRotationMatrix.M12 +
                            relativeY * mRotationMatrix.M22 +
                            centerY;

                        position.Z =
                            relativeX * mRotationMatrix.M13 +
                            relativeY * mRotationMatrix.M23 +
                            centerZ;
                        vertexArray[vertOn].Position = position;

                        vertexArray[vertOn].TextureCoordinate.X = tx1;
                        vertexArray[vertOn].TextureCoordinate.Y = ty1;
                        vertexArray[vertOn].Color = color;

                        vertOn++;

                        relativeX = sxWithOffset + width - centerX;
                        relativeY = topCoordinate - centerY;

                        position.X =
                            relativeX * mRotationMatrix.M11 +
                            relativeY * mRotationMatrix.M21 +
                            centerX;

                        position.Y =
                            relativeX * mRotationMatrix.M12 +
                            relativeY * mRotationMatrix.M22 +
                            centerY;

                        position.Z =
                            relativeX * mRotationMatrix.M13 +
                            relativeY * mRotationMatrix.M23 +
                            centerZ;
                        vertexArray[vertOn].Position = position;

                        vertexArray[vertOn].TextureCoordinate.X = tx2;
                        vertexArray[vertOn].TextureCoordinate.Y = ty1;

                        vertexArray[vertOn].Color = color;
                        vertOn++;

                        vertexArray[vertOn] = vertexArray[vertOn - 3];
                        vertOn++;


                        vertexArray[vertOn] = vertexArray[vertOn - 2];
                        vertOn++;

                        relativeX = sxWithOffset + width - centerX;
                        relativeY = bottomCoordinate - centerY;

                        position.X =
                            relativeX * mRotationMatrix.M11 +
                            relativeY * mRotationMatrix.M21 +
                            centerX;

                        position.Y =
                            relativeX * mRotationMatrix.M12 +
                            relativeY * mRotationMatrix.M22 +
                            centerY;

                        position.Z =
                            relativeX * mRotationMatrix.M13 +
                            relativeY * mRotationMatrix.M23 +
                            centerZ;
                        vertexArray[vertOn].Position = position;

                        vertexArray[vertOn].TextureCoordinate.X = tx2;
                        vertexArray[vertOn].TextureCoordinate.Y = ty2;

                        vertexArray[vertOn].Color = color;

                        vertOn++;
                    }
                    //   halfLastWidth = .5f * mFont.GetCharacterSpacing(asciiNumber) * mSpacing;
                }

                vertexCount = vertOn;
            return vertexArray;
        }

        private static void ApplyNudges(float mScale, BitmapFont mFont, ref Vector3 Position, HorizontalAlignment mHorizontalAlignment, VerticalAlignment mVerticalAlignment, ref float mNewLineDistance, bool adjustPositionForPixelPerfectDrawing, Camera camera, ref float centerX, ref float centerY, ref float centerZ, ref float nudgeOnYAxis)
        {
            if (adjustPositionForPixelPerfectDrawing)
            {
                mNewLineDistance = (float)System.Math.Round(mNewLineDistance);
                mNudgeAmounts.Clear();

                #region Calculate the position of the Text in camera space

                Matrix inverseRotationMatrix = camera.RotationMatrix;

#if FRB_MDX
                inverseRotationMatrix = Matrix.Invert(inverseRotationMatrix);
#else
                Matrix.Invert(ref inverseRotationMatrix, out inverseRotationMatrix);

#endif
                Vector3 rotatedPosition = Position - camera.Position;

                FlatRedBall.Math.MathFunctions.TransformVector(ref rotatedPosition, ref inverseRotationMatrix);

                #endregion

                #region Calculate the pixels per unit and the edges of the Camera

                float pixelsPerUnit = 1;

                float difference = mScale * 2 - mFont.LineHeightInPixels;
                const float epsilon = .001f;

                float relativeXEdgeAt = camera.DestinationRectangle.Width / 2.0f;
                float relativeYEdgeAt = camera.DestinationRectangle.Height / 2.0f;

                if (System.Math.Abs(difference) > epsilon)
                {
                    pixelsPerUnit = camera.PixelsPerUnitAt(Position.Z);
                    relativeXEdgeAt = camera.RelativeXEdgeAt(rotatedPosition.Z + camera.Z);
                    relativeYEdgeAt = camera.RelativeYEdgeAt(rotatedPosition.Z + camera.Z);


                }

                #endregion

                // .5 screwed up on some machines.  
                // Maybe that brings us too close to a pixel?
                // Trying .3...
                float PixelPerfectOffset = .3f / pixelsPerUnit;

                float distanceFromEdge;
                float unitsFromEdge;

                for (int i = 0; i < widthListForDrawing.Count; i++)
                {
                    float rotatedPositionX = rotatedPosition.X;

                    if (mHorizontalAlignment == HorizontalAlignment.Center)
                    {
                        rotatedPositionX += widthListForDrawing[i] / 2.0f;
                    }

                    // keep the centerX a certain number of pixels from the edge
                    distanceFromEdge =
                        rotatedPositionX - (-relativeXEdgeAt);
                    unitsFromEdge = FlatRedBall.Math.MathFunctions.RoundFloat(distanceFromEdge, 1 / pixelsPerUnit, PixelPerfectOffset);
                    float nudgeOnXAxis = unitsFromEdge - distanceFromEdge;

                    if (camera.ShiftsHalfUnitForRendering)
                    {
                        nudgeOnXAxis += .5f;
                    }

                    mNudgeAmounts.Add(nudgeOnXAxis);
                }

                #region Calculate nudgeOnYAxis
                double heightDifference = System.Math.Abs(mScale - System.Math.Round(mScale));

                if (mVerticalAlignment == VerticalAlignment.Center)
                {
                    rotatedPosition.Y += mScale;
                }

                // keep the centerY a certain number of pixels from the edge
                distanceFromEdge =
                    rotatedPosition.Y - (relativeYEdgeAt);
                unitsFromEdge = FlatRedBall.Math.MathFunctions.RoundFloat(distanceFromEdge, 1 / pixelsPerUnit, PixelPerfectOffset);
                //PixelPerfectOffset + Math.MathFunctions.RoundFloat(distanceFromEdge, 1 / pixelsPerUnit);
                nudgeOnYAxis = unitsFromEdge - distanceFromEdge;


                if (camera.ShiftsHalfUnitForRendering)
                {
                    nudgeOnYAxis += .5f;
                }

                #endregion

                // If the font has a scale of 0, there are no mNudgeAmounts
				// Hm, not sure we need this anymore now that we have a check
				// above.  I'll leave it in to be safe but this could be removed
				// in the future if I ever have time to test the removal.
                if (mNudgeAmounts.Count > 0)
                {
                    centerX = Position.X + camera.RotationMatrix.M11 * mNudgeAmounts[0] + camera.RotationMatrix.M21 * nudgeOnYAxis;
                    centerY = Position.Y + camera.RotationMatrix.M12 * mNudgeAmounts[0] + camera.RotationMatrix.M22 * nudgeOnYAxis;
                    centerZ = Position.Z + camera.RotationMatrix.M13 * mNudgeAmounts[0] + camera.RotationMatrix.M23 * nudgeOnYAxis;
                }
            }
        }



        #endregion

        #region Private Methods

        private void UpdateDimensions()
        {

            this.mWidth = TextManager.GetWidth(this);
            
            if (AdjustPositionForPixelPerfectDrawing)
            {
                this.mHeight = (NumberOfLines - 1) * (float)System.Math.Round(mNewLineDistance) + 2 * mScale;
            }
            else
            {
                this.mHeight = (NumberOfLines - 1) * mNewLineDistance + 2 * mScale;
            }

        }

        #region XML Docs
        /// <summary>
        /// Updates the displayed text according to the MaxWidth.
        /// </summary>
        #endregion
        private void UpdateDisplayedText()
        {

            mAdjustedText = mText;
            int numberOfCharacters = 0;

            switch (FontType)
            {
                case FontType.BitmapFontOnTexture:
                    if (_texturesText != mText)
                    {
                        UpdatePreRenderedTextureAndSprite();
                    }

                    break;

                case FontType.BitmapFont:
                    if (mHorizontalAlignment == HorizontalAlignment.Left || mHorizontalAlignment == HorizontalAlignment.Center)
                    {
                        numberOfCharacters = TextManager.GetNumberOfCharsIn(
                         mMaxWidth, mAdjustedText, mSpacing, 0, mFont, mHorizontalAlignment);
                    }
                    else if (mHorizontalAlignment == HorizontalAlignment.Right)
                    {
                        if (mAdjustedText == null)
                        {
                            numberOfCharacters = 0;
                        }
                        else
                        {
                            numberOfCharacters = TextManager.GetNumberOfCharsIn(
                             mMaxWidth, mAdjustedText, mSpacing, mAdjustedText.Length - 1, mFont, mHorizontalAlignment);
                        }
                    }
                    break;
#if SILVERLIGHT
                case FontType.SpriteFont:
                    if (mHorizontalAlignment == HorizontalAlignment.Left || mHorizontalAlignment == HorizontalAlignment.Center)
                    {
                        numberOfCharacters = TextManager.GetNumberOfCharsIn(
                         mMaxWidth, mAdjustedText, mSpacing, 0, SpriteFont, mHorizontalAlignment);
                    }
                    else if (mHorizontalAlignment == HorizontalAlignment.Right)
                    {
                        numberOfCharacters = TextManager.GetNumberOfCharsIn(
                         mMaxWidth, mAdjustedText, mSpacing, mAdjustedText.Length - 1, SpriteFont, mHorizontalAlignment);
                    }
                    break;
#endif
            }
            if (mAdjustedText != null && numberOfCharacters < mAdjustedText.Length)
            {
                if (MaxWidthBehavior == MaxWidthBehavior.Chop)
                {
                    mAdjustedText = mText.Substring(0, numberOfCharacters);
                }
                else
                {
                    mAdjustedText = mText;
                    InsertNewLines(MaxWidth);
                }
            }
        }

        #endregion

        #endregion


        #region IEquatable<Text> Members

        bool IEquatable<Text>.Equals(Text other)
        {
            return this == other;
        }

        #endregion

        #region IMouseOver Implementation

        public bool IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }

        #endregion


        IVisible IVisible.Parent
        {
            get
            {
                return this.mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return Visible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }
    }
}
