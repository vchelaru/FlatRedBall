using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Gui;
using FlatRedBall.Input;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;


using Microsoft.Xna.Framework.Graphics;
using AnimationChain = FlatRedBall.Graphics.Animation.AnimationChain;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector2 = Microsoft.Xna.Framework.Vector2;
//using FlatRedBall.Content.Scene;
using FileManager = FlatRedBall.IO.FileManager;
using AnimationChainList = FlatRedBall.Graphics.Animation.AnimationChainList;
using IInstructable = FlatRedBall.Instructions.IInstructable;
using Matrix = Microsoft.Xna.Framework.Matrix;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Utilities;
using FlatRedBall.Graphics.Texture;

namespace FlatRedBall
{
    #region XML docs
    /// <summary>
    /// Delegate for methods which can be assigned to the Sprite for every-frame
    /// custom logic or when a Sprite is removed.
    /// </summary>
    /// <remarks>
    /// <seealso cref="FlatRedBall.Sprite.CustomBehavior"/>
    /// <seealso cref="FlatRedBall.Sprite.Remove"/>
    /// </remarks>
    /// <param name="sprite">The Sprite on which the logic should execute.</param>
    #endregion
    public delegate void SpriteCustomBehavior(Sprite sprite);

    public partial class Sprite : IColorable, ICursorSelectable,
        ITexturable
#if FRB_XNA && !MONOGAME
        , IMouseOver
#endif
    {
        #region Fields

        #region Color/Fade
        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;

        ColorOperation mColorOperation;
        BlendOperation mBlendOperation;

        // This used to only be on MonoDroid and WP7, but we need it on PC for premult alpha when using ColorOperation.Color
//#if WINDOWS_PHONE || MONODROID
        float mRed;
        float mGreen;
        float mBlue;
        float mAlpha;
//#endif
        #endregion

        #region Collision
        AxisAlignedRectangle mCollisionAxisAlignedRectangle;
        Circle mCollisionCircle;
        Polygon mCollisionPolygon;
        Line mCollisionLine;
        #endregion

        #region ICursorSelectable
        protected bool mCursorSelectable;
        #endregion

        #region Texture and pixel size
        internal Texture2D mTexture; // made internal to avoid a getter in tight loops
        internal float mPixelSize;
        bool mFlipHorizontal;
        bool mFlipVertical;
        TextureFilter? mTextureFilter = null;
        #endregion



        #region Internal Drawing members
        internal bool mInCameraView;
        internal bool mAutomaticallyUpdated;
        internal VertexPositionColorTexture[] mVerticesForDrawing;
        internal Vector3 mOldPosition; // used when sorting along forward vector to hold old position
        internal SpriteVertex[] mVertices;
        TextureAddressMode mTextureAddressMode;

        internal bool mOrdered = true;
        #endregion

        #endregion

        #region Properties


        #region IColorable

        #region XML Docs
        /// <summary>
        /// Controls the Sprite's transparency.
        /// </summary>
        /// <remarks>
        /// Fade controls a Sprite's transparency.   A completely opaque Sprite has an
        /// Alpha of 1 while a completely transparent object has an Alpha of 0.
        /// 
        /// Setting the AlphaRate of a completely opaque Sprite to -1 will 
        /// make the sprite disappear in one second.  Invisible Sprites continue
        /// to remain in memory and are managed by the SpriteManager.  The Alpha variable
        /// will automatically regulate itself if the value is set to something outside of the
        /// 0 - 1 range.
        /// </remarks>
        #endregion
        public float Alpha
        {
            get { return mVertices[0].Color.W; }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(0, value);

                mVertices[0].Color.W = value;
                mVertices[1].Color.W = value;
                mVertices[2].Color.W = value;
                mVertices[3].Color.W = value;

#if WINDOWS_PHONE || XNA4
                mAlpha = value;
                UpdateColorsAccordingToAlpha();
#endif
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the rate at which the Alpha property changes.  This is in units per second.  A fully opaque
        /// Sprite (Alpha = 1) will disappear in 1 second if its AlphaRate is set to -1.
        /// </summary>
        /// <remarks>
        /// The AlphaRate changes Alpha as follows:
        /// <para>
        /// Alpha += AlphaRate * TimeManager.SecondDifference;
        /// </para>
        /// This is automatically applied if the Sprite is managed by the SpriteManager(usually the case).
        /// </remarks>
        #endregion
        public float AlphaRate
        {
            get { return mAlphaRate; }
            set
            {
                mAlphaRate = value;
            }
        }

#if WINDOWS_PHONE || XNA4
        private void UpdateColorsAccordingToAlpha()
        {

            float redValue = mRed;
            float greenValue = mGreen;
            float blueValue = mBlue;


#if WINDOWS
            if (ColorOperation == Graphics.ColorOperation.Color)

#else

            if (ColorOperation == Graphics.ColorOperation.Modulate || 
                ColorOperation == Graphics.ColorOperation.Color ||
                ColorOperation == Graphics.ColorOperation.ColorTextureAlpha ||
                Texture == null
                )
#endif
            {
                redValue = mRed * mAlpha;
                greenValue = mGreen * mAlpha;
                blueValue = mBlue * mAlpha;
            }
                
            else
            {
#if WINDOWS
                redValue = mRed;
                greenValue = mGreen;
                blueValue = mBlue;
#else
                redValue = mAlpha;
                greenValue = mAlpha;
                blueValue = mAlpha;
#endif
            }


            if ((Texture == null || ColorOperation == Graphics.ColorOperation.Color) &&
                (mBlendOperation == Graphics.BlendOperation.Modulate || mBlendOperation == Graphics.BlendOperation.Modulate2X)
                )
            {
                float toWhiteInterpolationValue = 1 - mAlpha;


                redValue = redValue + (1 - redValue) * toWhiteInterpolationValue;
                greenValue = greenValue + (1 - greenValue) * toWhiteInterpolationValue;
                blueValue = blueValue + (1 - blueValue) * toWhiteInterpolationValue;

            }

            mVertices[0].Color.X = redValue;
            mVertices[1].Color.X = redValue;
            mVertices[2].Color.X = redValue;
            mVertices[3].Color.X = redValue;

            mVertices[0].Color.Y = greenValue;
            mVertices[1].Color.Y = greenValue;
            mVertices[2].Color.Y = greenValue;
            mVertices[3].Color.Y = greenValue;

            mVertices[0].Color.Z = blueValue;
            mVertices[1].Color.Z = blueValue;
            mVertices[2].Color.Z = blueValue;
            mVertices[3].Color.Z = blueValue;



        }
#endif


        public float Red
        {
            get
            {
#if WINDOWS_PHONE || XNA4
                return mRed;
#else
                return mVertices[0].Color.X;
#endif
            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

#if WINDOWS_PHONE || XNA4
                mRed = value;

                UpdateColorsAccordingToAlpha();

#else
                mVertices[0].Color.X = value;
                mVertices[1].Color.X = value;
                mVertices[2].Color.X = value;
                mVertices[3].Color.X = value;
#endif
            }
        }

        public float Green
        {
            get
            {
#if WINDOWS_PHONE || XNA4
                return mGreen;
#else
                return mVertices[0].Color.Y;
#endif
            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

#if WINDOWS_PHONE || XNA4
                mGreen = value;

                UpdateColorsAccordingToAlpha();

#else
                mVertices[0].Color.Y = value;
                mVertices[1].Color.Y = value;
                mVertices[2].Color.Y = value;
                mVertices[3].Color.Y = value;
#endif
            }
        }

        public float Blue
        {
            get
            {
#if WINDOWS_PHONE || XNA4
                return mBlue;
#else
                return mVertices[0].Color.Z; 
#endif
            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

#if WINDOWS_PHONE || XNA4
                mBlue = value;

                UpdateColorsAccordingToAlpha();
#else
                mVertices[0].Color.Z = value;
                mVertices[1].Color.Z = value;
                mVertices[2].Color.Z = value;
                mVertices[3].Color.Z = value;
#endif
            }
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
            set 
            {
#if DEBUG
                // Check for unsupporte color operations
                if(Debugging.CrossPlatform.ShouldApplyRestrictionsFor(Debugging.Platform.iOS) ||
                    Debugging.CrossPlatform.ShouldApplyRestrictionsFor(Debugging.Platform.Android) ||
                    Debugging.CrossPlatform.ShouldApplyRestrictionsFor(Debugging.Platform.WindowsRt))
                {
                    if(value == Graphics.ColorOperation.Add || value == Graphics.ColorOperation.Subtract ||
                        value == Graphics.ColorOperation.InterpolateColor || value == Graphics.ColorOperation.InverseTexture ||
                        value == Graphics.ColorOperation.Modulate2X || value == Graphics.ColorOperation.Modulate4X ||
                        value == Graphics.ColorOperation.Subtract)
                    {
                        throw new Exception("The color operation " + value + " is not available due to platform restrictions");
                    }
                }

#endif



                mColorOperation = value;
#if WINDOWS_PHONE || WINDOWS_8 || XNA4
                UpdateColorsAccordingToAlpha();
#endif
            }
        }

        public BlendOperation BlendOperation
        {
            get { return mBlendOperation; }
            set 
            { 
                mBlendOperation = value;

#if WINDOWS_PHONE || WINDOWS_8 || XNA4
                UpdateColorsAccordingToAlpha();
#endif
            }
        }


        #endregion

        #region Collision

        [Obsolete("This property will not be available in a future version of FRB.  Use the Entity pattern instead")]
        public Polygon CollisionPolygon
        {
            get { return mCollisionPolygon; }
        }

        [Obsolete("This property will not be available in a future version of FRB.  Use the Entity pattern instead")]
        public Circle CollisionCircle
        {
            get { return mCollisionCircle; }
        }

        [Obsolete("This property will not be available in a future version of FRB.  Use the Entity pattern instead")]
        public AxisAlignedRectangle CollisionAxisAlignedRectangle
        {
            get { return mCollisionAxisAlignedRectangle; }
        }

        [Obsolete("This property will not be available in a future version of FRB.  Use the Entity pattern instead")]
        public Line CollisionLine
        {
            get { return mCollisionLine; }
        }

        #endregion

        #region Texture and PixelSize
        /// <summary>
        /// The texture to be displayed by the Sprite.
        /// </summary>
        [ExportOrder(0)]
        public Texture2D Texture
        {
            get { return mTexture; }
            set 
            {
				if (mTexture != value)
				{
					mTexture = value;

#if XNA4
					if (this.TextureAddressMode != Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp &&
					               FlatRedBallServices.GraphicsDevice.GraphicsProfile == GraphicsProfile.Reach && mTexture != null)
					{
						bool isNotPowerOfTwo = !MathFunctions.IsPowerOfTwo (mTexture.Width) ||
						                                     !MathFunctions.IsPowerOfTwo (mTexture.Height);

						if (isNotPowerOfTwo)
						{
							throw new NotImplementedException (
								"The texture " +
								mTexture.Name +
								" must be power of two if using non-Clamp texture address mode on Reach");
						}
					}
#endif

#if WINDOWS_PHONE || XNA4
					UpdateColorsAccordingToAlpha ();
#endif

					UpdateScale ();            
				}
            }
        }

        AtlasedTexture atlasedTexture;
        public AtlasedTexture AtlasedTexture
        {
            get
            {
                return atlasedTexture;
            }
            set
            {
                atlasedTexture = value;

                // todo - eventually we don't want to modify the original values, just use this in rendering, but
                // I'm doing this to get it implemented quickly:
                Texture = atlasedTexture.Texture;
                LeftTexturePixel = atlasedTexture.SourceRectangle.Left;
                TopTexturePixel = atlasedTexture.SourceRectangle.Top;
                RightTexturePixel = atlasedTexture.SourceRectangle.Right;
                BottomTexturePixel = atlasedTexture.SourceRectangle.Bottom;
            }
        }

        [ExportOrder(1)]
        [Obsolete("Use TextureScale")]
        public float PixelSize
        {
            get { return mPixelSize; }
            set 
            { 
                mPixelSize = value;

                UpdateScale();
            
            }
        }

        public float TextureScale
        {
            get
            {
                return mPixelSize * 2.0f;
            }
            set
            {
                PixelSize = value / 2.0f;
            }
        }


        #region XML Docs
        /// <summary>
        /// Whether to flip the Sprite's texture on the y Axis (left and right switch).
        /// </summary>
        /// <remarks>
        /// This kind of texture rotation can be accomplished by simply rotating 
        /// a Sprite on its yAxis; however, there are times when this
        /// is inconvenient or impossible due to attachment relationships.  There 
        /// is no efficiency consequence for using either method.  If a Sprite
        /// is animated, this value will be overwritten by the AnimationChain being used.
        /// </remarks>
        #endregion
        public bool FlipHorizontal
        {
            get { return mFlipHorizontal; }
            set { mFlipHorizontal = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether to flip the Sprite's texture on the x Axis (top and bottom switch).
        /// </summary>
        /// <remarks>
        /// This kind of texture rotation can be accomplished by simply rotating a 
        /// Sprite on its xAxis; however, there are times when this
        /// is inconvenient or impossible due to attachment relationships.  
        /// There is no efficiency consequence for using either method.  If a Sprite
        /// is animated, this value will be overwritten by the AnimationChain being used.
        /// </remarks>
        #endregion
        public bool FlipVertical
        {
            get { return mFlipVertical; }
            set { mFlipVertical = value; }
        }

        [ExportOrder(2)]
        public float TopTextureCoordinate
        {
            get { return mVertices[0].TextureCoordinate.Y; }
            set
            {
                mVertices[0].TextureCoordinate.Y = value;
                mVertices[1].TextureCoordinate.Y = value;

                UpdateScale();
            }
        }


        [ExportOrder(2)]
        public float TopTexturePixel
        {
            get
            {
                if (Texture != null)
                {
                    return TopTextureCoordinate * Texture.Height;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (Texture != null)
                {
                    TopTextureCoordinate = value / Texture.Height;
                }
                else
                {
                    throw new Exception("You must have a Texture set before setting this value");
                }
            }
        }

        [ExportOrder(2)]
        public float BottomTextureCoordinate
        {
            get { return mVertices[2].TextureCoordinate.Y; }
            set
            {
                mVertices[2].TextureCoordinate.Y = value;
                mVertices[3].TextureCoordinate.Y = value;

                UpdateScale();
            }
        }


        [ExportOrder(2)]
        public float BottomTexturePixel
        {
            get
            {
                if (Texture != null)
                {
                    return BottomTextureCoordinate * Texture.Height;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (Texture != null)
                {
                    BottomTextureCoordinate = value / Texture.Height;
                }
                else
                {
                    throw new Exception("You must have a Texture set before setting this value");
                }
            }
        }

        [ExportOrder(2)]
        public float LeftTextureCoordinate
        {
            get { return mVertices[0].TextureCoordinate.X; }
            set
            {
                mVertices[0].TextureCoordinate.X = value;
                mVertices[3].TextureCoordinate.X = value;

                UpdateScale();
            }
        }


        [ExportOrder(2)]
        public float LeftTexturePixel
        {
            get
            {
                if (Texture != null)
                {
                    return LeftTextureCoordinate * Texture.Width;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (Texture != null)
                {
                    LeftTextureCoordinate = value / Texture.Width;
                }
                else
                {
                    throw new Exception("You must have a Texture set before setting this value");
                }
            }
        }

        [ExportOrder(2)]
        public float RightTextureCoordinate
        {
            get { return mVertices[1].TextureCoordinate.X; }
            set
            {
                mVertices[1].TextureCoordinate.X = value;
                mVertices[2].TextureCoordinate.X = value;

                UpdateScale();
            }
        }

       
        [ExportOrder(2)]
        public float RightTexturePixel
        {
            get
            {
                if (Texture != null)
                {
                    return RightTextureCoordinate * Texture.Width;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (mTexture != null)
                {
                    RightTextureCoordinate = value / (float)mTexture.Width;
                }
                else
                {
                    throw new Exception("You must have a Texture set before setting this value");
                }
            }
        }

        public TextureAddressMode TextureAddressMode
        {
            get { return mTextureAddressMode; }
            set { mTextureAddressMode = value; }
        }

        public TextureFilter? TextureFilter {
            get { return mTextureFilter; }
            set { mTextureFilter = value; }
        }

        #endregion


        #region XML Docs
        /// <summary>
        /// These can be used to change Sprite appearance
        /// on individual vertices.
        /// </summary>
        /// <remarks>
        /// The index begins counting at the top left (index 0)
        /// and increases moving clockwise.
        /// </remarks>
        #endregion
        public SpriteVertex[] Vertices
        {
            get { return mVertices; }
        }

        /// <summary>
        /// Represents the four (4) vertices used to render the Sprite.  This value is set
        /// if the Sprite is either a manuall updated Sprite, or if the SpriteManager's ManualUpdate
        /// method is called on this.
        /// </summary>
        public VertexPositionColorTexture[] VerticesForDrawing
        {
            get { return mVerticesForDrawing; }
        }


        #endregion

        #region Events

        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public SpriteCustomBehavior CustomBehavior;

        public event SpriteCustomBehavior Remove;
        #endregion

        #region Methods

        #region Constructor


        public Sprite()
            : base()
        {
            mVisible = true;

            mScaleX = 1;
            mScaleY = 1;

            mVertices = new SpriteVertex[4];

            mVertices[0] = new SpriteVertex();
            mVertices[1] = new SpriteVertex();
            mVertices[2] = new SpriteVertex();
            mVertices[3] = new SpriteVertex();

            mVertices[0].TextureCoordinate.X = 0;
            mVertices[0].TextureCoordinate.Y = 0;
            mVertices[0].Scale = new Vector2(-1, 1);

            mVertices[1].TextureCoordinate.X = 1;
            mVertices[1].TextureCoordinate.Y = 0;
            mVertices[1].Scale = new Vector2(1, 1);

            mVertices[2].TextureCoordinate.X = 1;
            mVertices[2].TextureCoordinate.Y = 1;
            mVertices[2].Scale = new Vector2(1, -1);

            mVertices[3].TextureCoordinate.X = 0;
            mVertices[3].TextureCoordinate.Y = 1;
            mVertices[3].Scale = new Vector2(-1, -1);

#if WINDOWS_PHONE || MONODROID
            mVertices[0].Color.X = 1;
            mVertices[1].Color.X = 1;
            mVertices[2].Color.X = 1;
            mVertices[3].Color.X = 1;

            mVertices[0].Color.Y = 1;
            mVertices[1].Color.Y = 1;
            mVertices[2].Color.Y = 1;
            mVertices[3].Color.Y = 1;
            
            mVertices[0].Color.Z = 1;
            mVertices[1].Color.Z = 1;
            mVertices[2].Color.Z = 1;
            mVertices[3].Color.Z = 1;

            mVertices[0].Color.W = 1;
            mVertices[1].Color.W = 1;
            mVertices[2].Color.W = 1;
            mVertices[3].Color.W = 1;

#endif
            Alpha = GraphicalEnumerations.MaxColorComponentValue; 

            ColorOperation = ColorOperation.Texture;
            mAnimationChains = new AnimationChainList();
            mCurrentChainIndex = -1;
            mAnimationSpeed = 1;

            mTextureAddressMode = TextureAddressMode.Clamp;

            mCursorSelectable = true;
        
        }

        #endregion

        #region Internal Methods

        internal void OnCustomBehavior()
        {
            if (CustomBehavior != null)
                CustomBehavior(this);
        }

        internal void OnRemove()
        {
            if (Remove != null)
                Remove(this);
        }

        internal void UpdateVertices(Camera camera)
        {
            // Vic says: I tried to optimize this on
            // March 6, 2011 for the windows phone - I
            // couldn't get it to run any faster on the
            // the emulator.  Seems like it's pretty darn
            // optimized.
            mVertices[0].Position.X = (mScaleX * mVertices[0].Scale.X);
            mVertices[1].Position.X = (mScaleX * mVertices[1].Scale.X);
            mVertices[2].Position.X = (mScaleX * mVertices[2].Scale.X);
            mVertices[3].Position.X = (mScaleX * mVertices[3].Scale.X);

            mVertices[0].Position.Y = (mScaleY * mVertices[0].Scale.Y);
            mVertices[1].Position.Y = (mScaleY * mVertices[1].Scale.Y);
            mVertices[2].Position.Y = (mScaleY * mVertices[2].Scale.Y);
            mVertices[3].Position.Y = (mScaleY * mVertices[3].Scale.Y);

            mVertices[0].Position.Z = 0;
            mVertices[1].Position.Z = 0;
            mVertices[2].Position.Z = 0;
            mVertices[3].Position.Z = 0;

            if (this.ListsBelongingTo.Contains(camera.mSpritesToBillBoard))
            {
                Matrix modifiedMatrix =  mRotationMatrix * SpriteManager.Camera.RotationMatrix;

                MathFunctions.TransformVector(ref mVertices[0].Position, ref modifiedMatrix);
                MathFunctions.TransformVector(ref mVertices[1].Position, ref modifiedMatrix);
                MathFunctions.TransformVector(ref mVertices[2].Position, ref modifiedMatrix);
                MathFunctions.TransformVector(ref mVertices[3].Position, ref modifiedMatrix);
            }
            else
            {
                MathFunctions.TransformVector(ref mVertices[0].Position, ref mRotationMatrix);
                MathFunctions.TransformVector(ref mVertices[1].Position, ref mRotationMatrix);
                MathFunctions.TransformVector(ref mVertices[2].Position, ref mRotationMatrix);
                MathFunctions.TransformVector(ref mVertices[3].Position, ref mRotationMatrix);
            }

            mVertices[0].Position += Position;
            mVertices[1].Position += Position;
            mVertices[2].Position += Position;
            mVertices[3].Position += Position;

        }


        #endregion

        #region Public Methods

        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();
            SpriteManager.ManualUpdate(this);
        }

        public void UpdateToCurrentAnimationFrame()
        {
            if (mAnimationChains != null && mAnimationChains.Count > mCurrentChainIndex && mCurrentChainIndex != -1 && 
                mCurrentFrameIndex > -1 &&
                mCurrentFrameIndex < mAnimationChains[mCurrentChainIndex].Count)
            {
                var frame = mAnimationChains[mCurrentChainIndex][mCurrentFrameIndex];
				// Set the property so that any necessary values change:
//				mTexture = mAnimationChains[mCurrentChainIndex][mCurrentFrameIndex].Texture;
                Texture = frame.Texture;
                this.Vertices[0].TextureCoordinate.X = frame.LeftCoordinate;
                this.Vertices[1].TextureCoordinate.X = frame.RightCoordinate;
                this.Vertices[2].TextureCoordinate.X = frame.RightCoordinate;
                this.Vertices[3].TextureCoordinate.X = frame.LeftCoordinate;

                this.Vertices[0].TextureCoordinate.Y = frame.TopCoordinate;
                this.Vertices[1].TextureCoordinate.Y = frame.TopCoordinate;
                this.Vertices[2].TextureCoordinate.Y = frame.BottomCoordinate;
                this.Vertices[3].TextureCoordinate.Y = frame.BottomCoordinate;

                if (mIgnoreAnimationChainTextureFlip == false)
                {
                    mFlipHorizontal = frame.FlipHorizontal;
                    mFlipVertical = frame.FlipVertical;
                }

                if (mUseAnimationRelativePosition)
                {

                    RelativePosition.X = frame.RelativeX;
                    RelativePosition.Y = frame.RelativeY;
                }

                foreach(var instruction in frame.Instructions)
                {
                    instruction.Execute();
                }

                if (mPixelSize > 0 && mTexture != null)
                {
                    mScaleX = mTexture.Width * mPixelSize * (mVertices[1].TextureCoordinate.X - mVertices[0].TextureCoordinate.X);
                    mScaleY = mTexture.Height * mPixelSize * (mVertices[2].TextureCoordinate.Y - mVertices[1].TextureCoordinate.Y);
                }
            }
        }


        public void ClearCollision()
        {
            if (mCollisionAxisAlignedRectangle != null)
            {
                ShapeManager.Remove(mCollisionAxisAlignedRectangle);
                mCollisionAxisAlignedRectangle = null;
            }
            if (mCollisionCircle != null)
            {
                ShapeManager.Remove(mCollisionCircle);
                mCollisionCircle = null;
            }
            if (mCollisionPolygon != null)
            {
                ShapeManager.Remove(mCollisionPolygon);
                mCollisionPolygon = null;
            }
            if (mCollisionLine != null)
            {
                ShapeManager.Remove(mCollisionLine);
                mCollisionLine = null;
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns a clone of this instance.
        /// </summary>
        /// <remarks>
        /// Attachments are not cloned.  The new clone
        /// will not have any parents or children.
        /// </remarks>
        /// <returns>The new clone.</returns>
        #endregion
        public Sprite Clone()
        {
            Sprite sprite = base.Clone<Sprite>();

            sprite.Texture = Texture;

            sprite.FlipHorizontal = FlipHorizontal;
            sprite.FlipVertical = FlipVertical;
            sprite.ColorOperation = mColorOperation;

            // Although the Scale has already been set at this point, set it again so that it will
            // override the PixelSize IF the PixelSize is being overridden by the current scale:
            sprite.ScaleX = ScaleX;
            sprite.ScaleY = ScaleY;

            sprite.TimeCreated = TimeManager.CurrentTime;

            sprite.mVertices = new SpriteVertex[4];
            for (int i = 0; i < 4; i++)
            {
                sprite.mVertices[i] = new SpriteVertex(mVertices[i]);
            }

            sprite.mVerticesForDrawing = new VertexPositionColorTexture[4];

            sprite.mCollisionAxisAlignedRectangle = null;
            sprite.mCollisionCircle = null;
            sprite.mCollisionPolygon = null;
            sprite.mCollisionLine = null;

            sprite.mAnimationChains = new AnimationChainList();
            for (int i = 0; i < mAnimationChains.Count; i++)
            {
                AnimationChain ac = mAnimationChains[i];
                sprite.mAnimationChains.Add(ac);
            }

            if (CustomBehavior != null)
            {
#if XNA4
                throw new NotSupportedException("Sprite custom behavior is not supported in XNA 4");
#else
                sprite.CustomBehavior = CustomBehavior.Clone() as SpriteCustomBehavior;
#endif
            }

            return sprite;
        }

        public Sprite CloneForRig()
        {
            Sprite sprite = new Sprite();

            sprite.Name = Name;

            sprite.RelativePosition = RelativePosition;
            sprite.RelativeRotationX = RelativeRotationX;
            sprite.RelativeRotationY = RelativeRotationY;
            sprite.RelativeRotationZ = RelativeRotationZ;

            sprite.FlipHorizontal = FlipHorizontal;
            sprite.FlipVertical = FlipVertical;

            sprite.Texture = Texture;
            sprite.ColorOperation = mColorOperation;

            // Although the Scale has already been set at this point, set it again so that it will
            // override the PixelSize IF the PixelSize is being overridden by the current scale:
            sprite.ScaleX = ScaleX;
            sprite.ScaleY = ScaleY;

            sprite.TimeCreated = TimeManager.CurrentTime;

            sprite.mVertices = new SpriteVertex[4];
            for (int i = 0; i < 4; i++)
            {
                sprite.mVertices[i] = new SpriteVertex(mVertices[i]);
            }

            sprite.mVerticesForDrawing = new VertexPositionColorTexture[4];

            sprite.mCollisionAxisAlignedRectangle = null;
            sprite.mCollisionCircle = null;
            sprite.mCollisionPolygon = null;
            sprite.mCollisionLine = null;

            sprite.mAnimationChains = new AnimationChainList();
            for (int i = 0; i < mAnimationChains.Count; i++)
            {
                AnimationChain ac = mAnimationChains[i];
                sprite.mAnimationChains.Add(ac);
            }

            if (CustomBehavior != null)
#if XNA4
                throw new NotSupportedException("Sprite custom behavior is not supported in XNA 4");
#else
                sprite.CustomBehavior = CustomBehavior.Clone() as SpriteCustomBehavior;
#endif
            return sprite;
        }


        #region CollideAgainst Methods
        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainst(Sprite sprite)
        {
            #region If this Sprite has an AxisAlignedRectangle
            if (mCollisionAxisAlignedRectangle != null)
            {
                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    return mCollisionAxisAlignedRectangle.CollideAgainst(sprite.mCollisionAxisAlignedRectangle);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    return mCollisionAxisAlignedRectangle.CollideAgainst(sprite.mCollisionCircle);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionAxisAlignedRectangle.CollideAgainst(sprite.mCollisionPolygon);
                }
                else if (sprite.mCollisionLine != null)
                {
                    return mCollisionAxisAlignedRectangle.CollideAgainst(sprite.mCollisionLine);
                }
                else
                    return false;
            }
            #endregion

            #region if the Sprite has a Circle
            else if (mCollisionCircle != null)
            {
                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    return mCollisionCircle.CollideAgainst(sprite.mCollisionAxisAlignedRectangle);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    return mCollisionCircle.CollideAgainst(sprite.mCollisionCircle);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionCircle.CollideAgainst(sprite.mCollisionPolygon);
                }
                else if (sprite.mCollisionLine != null)
                {
                    return mCollisionCircle.CollideAgainst(sprite.mCollisionLine);
                }
                else
                    return false;
            }
            #endregion

            #region the Sprite has a Polygon
            else if (mCollisionPolygon != null)
            {
                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    return mCollisionPolygon.CollideAgainst(sprite.mCollisionAxisAlignedRectangle);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    return mCollisionPolygon.CollideAgainst(sprite.mCollisionCircle);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionPolygon.CollideAgainst(sprite.mCollisionPolygon);
                }
                else if (sprite.mCollisionLine != null)
                {
                    return mCollisionPolygon.CollideAgainst(sprite.mCollisionLine);
                }
                else
                    return false;
            }
            #endregion

            #region the Sprite has a Line
            else if (mCollisionLine != null)
            {
                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    return mCollisionLine.CollideAgainst(sprite.mCollisionAxisAlignedRectangle);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    return mCollisionLine.CollideAgainst(sprite.mCollisionCircle);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionLine.CollideAgainst(sprite.mCollisionPolygon);
                }
                else if (sprite.mCollisionLine != null)
                {
                    return mCollisionLine.CollideAgainst(sprite.mCollisionLine);
                }
                else
                    return false;
            }
            #endregion
            else
            {
                return false;
            }
        }

        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle)
        {
            if (mCollisionAxisAlignedRectangle != null)
            {
                return axisAlignedRectangle.CollideAgainst(mCollisionAxisAlignedRectangle);
            }
            else if (mCollisionCircle != null)
            {
                return axisAlignedRectangle.CollideAgainst(mCollisionCircle);
            }
            else if (mCollisionPolygon != null)
            {
                return mCollisionPolygon.CollideAgainst(axisAlignedRectangle);
            }
            else if (mCollisionLine != null)
            {
                return mCollisionLine.CollideAgainst(axisAlignedRectangle);
            }

            return false;
        }



        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainst(Polygon polygon)
        {
            if (mCollisionAxisAlignedRectangle != null)
            {
                return polygon.CollideAgainst(mCollisionAxisAlignedRectangle);
            }
            else if (mCollisionCircle != null)
            {
                return polygon.CollideAgainst(mCollisionCircle);
            }
            else if (mCollisionPolygon != null)
            {
                return mCollisionPolygon.CollideAgainst(polygon);
            }
            else if (mCollisionLine != null)
            {
                return mCollisionLine.CollideAgainst(polygon);
            }

            return false;
        }


        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainst(Line line)
        {
            if (mCollisionAxisAlignedRectangle != null)
            {
                return line.CollideAgainst(mCollisionAxisAlignedRectangle);
            }
            else if (mCollisionCircle != null)
            {
                return line.CollideAgainst(mCollisionCircle);
            }
            else if (mCollisionPolygon != null)
            {
                return mCollisionPolygon.CollideAgainst(line);
            }
            else if (mCollisionLine != null)
            {
                return mCollisionLine.CollideAgainst(line);
            }

            return false;
        }



        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainstMove(Sprite sprite, float thisMass, float otherMass)
        {

            if (mCollisionAxisAlignedRectangle != null)
            {

                mCollisionAxisAlignedRectangle.UpdateDependencies(
                    TimeManager.CurrentTime);

                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    sprite.mCollisionAxisAlignedRectangle.UpdateDependencies(
                        TimeManager.CurrentTime);

                    return mCollisionAxisAlignedRectangle.CollideAgainstMove(sprite.mCollisionAxisAlignedRectangle, thisMass, otherMass);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    sprite.mCollisionCircle.UpdateDependencies(
                        TimeManager.CurrentTime);

                    return mCollisionAxisAlignedRectangle.CollideAgainstMove(sprite.mCollisionCircle, thisMass, otherMass);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionAxisAlignedRectangle.CollideAgainstMove(sprite.mCollisionPolygon, thisMass, otherMass);
                }

                return false;
            }
            else if (mCollisionCircle != null)
            {
                mCollisionCircle.UpdateDependencies(TimeManager.CurrentTime);

                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    sprite.mCollisionAxisAlignedRectangle.UpdateDependencies(
                        TimeManager.CurrentTime);

                    return mCollisionCircle.CollideAgainstMove(sprite.mCollisionAxisAlignedRectangle, thisMass, otherMass);
                }
                else if (sprite.mCollisionCircle != null)
                {
                    sprite.mCollisionCircle.UpdateDependencies(
                        TimeManager.CurrentTime);


                    return mCollisionCircle.CollideAgainstMove(sprite.mCollisionCircle, thisMass, otherMass);
                }
                else
                    return false;
            }
            else if (mCollisionPolygon != null)
            {
                if (sprite.mCollisionAxisAlignedRectangle != null)
                {
                    return mCollisionPolygon.CollideAgainstMove(sprite.mCollisionAxisAlignedRectangle, thisMass, otherMass);
                }
                else if (sprite.mCollisionPolygon != null)
                {
                    return mCollisionPolygon.CollideAgainstMove(sprite.mCollisionPolygon, thisMass, otherMass);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }



        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
        {
            if (this.mCollisionAxisAlignedRectangle != null)
            {
                return this.mCollisionAxisAlignedRectangle.CollideAgainstMove(polygon, thisMass, otherMass);
            }
            else if (this.mCollisionPolygon != null)
            {
                return this.mCollisionPolygon.CollideAgainstMove(polygon, thisMass, otherMass);
            }

            // polygon vs. circle move collision not currently supported

            return false;
        }
        #endregion



        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead")]
        public void CopyCustomBehaviorFrom(Sprite spriteToCopyFrom)
        {
            if (spriteToCopyFrom.CustomBehavior != null)
            {
                this.CustomBehavior = null;
                CustomBehavior += spriteToCopyFrom.CustomBehavior;
            }
        }





        private void PlatformSpecificInitialization()
        {

            mColorOperation = ColorOperation.Texture;
            mBlendOperation = BlendOperation.Regular;


            // This is needed because SpriteChains may
            // use particle Sprites which can screw up 
            mVertices[0].TextureCoordinate.X = 0;
            mVertices[0].TextureCoordinate.Y = 0;
            mVertices[0].Scale = new Vector2(-1, 1);

            mVertices[1].TextureCoordinate.X = 1;
            mVertices[1].TextureCoordinate.Y = 0;
            mVertices[1].Scale = new Vector2(1, 1);

            mVertices[2].TextureCoordinate.X = 1;
            mVertices[2].TextureCoordinate.Y = 1;
            mVertices[2].Scale = new Vector2(1, -1);

            mVertices[3].TextureCoordinate.X = 0;
            mVertices[3].TextureCoordinate.Y = 1;
            mVertices[3].Scale = new Vector2(-1, -1);

            CustomBehavior = null;
        }


        #region SetCollision Methods
        [Obsolete("This method is obsolete - use the Entity pattern instead.")]
        public void SetCollision(AxisAlignedRectangle rectangle)
        {
            mCollisionCircle = null;
            mCollisionPolygon = null;
            mCollisionLine = null;

            mCollisionAxisAlignedRectangle = rectangle;
            //mCollisionAxisAlignedRectangle.ScaleX = ScaleX;
            //mCollisionAxisAlignedRectangle.ScaleY = ScaleY;

            mCollisionAxisAlignedRectangle.AttachTo(this, false);
            mCollisionAxisAlignedRectangle.ForceUpdateDependencies();
        }

        [Obsolete("This method is obsolete - use the Entity pattern instead.")]
        public void SetCollision(Circle circle)
        {
            mCollisionAxisAlignedRectangle = null;
            mCollisionPolygon = null;
            mCollisionLine = null;

            mCollisionCircle = circle;
            mCollisionCircle.Radius = ScaleX;

            mCollisionCircle.AttachTo(this, false);
            mCollisionCircle.ForceUpdateDependencies();
        }

        [Obsolete("This method is obsolete - use the Entity pattern instead.")]
        public void SetCollision(Polygon polygon)
        {
            mCollisionCircle = null;
            mCollisionAxisAlignedRectangle = null;
            mCollisionLine = null;

            mCollisionPolygon = polygon;

            mCollisionPolygon.AttachTo(this, false);
            mCollisionPolygon.ForceUpdateDependencies();
        }

        [Obsolete("This method is obsolete - use the Entity pattern instead.")]
        public void SetCollision(Line line)
        {
            mCollisionCircle = null;
            mCollisionAxisAlignedRectangle = null;
            mCollisionPolygon = null;

            mCollisionLine = line;

            mCollisionLine.AttachTo(this, false);
            mCollisionLine.ForceUpdateDependencies();
        }

        #endregion


#if FRB_XNA && !MONOGAME
        #region IMouseOver
        bool IMouseOver.IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion
#endif


        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append(base.ToString());
            if (Texture != null)
                sb.Append("\nTexture: ").Append(this.Texture.Name);
            sb.Append("\nVisible: ").Append(Visible.ToString());

            return sb.ToString();
        }

        #endregion


        #region Private Methods

        private void UpdateScale()
        {
            if (mPixelSize > 0 && mTexture != null)
            {
                mScaleX = mTexture.Width * mPixelSize * (mVertices[1].TextureCoordinate.X - mVertices[0].TextureCoordinate.X);
                mScaleY = mTexture.Height * mPixelSize * (mVertices[2].TextureCoordinate.Y - mVertices[1].TextureCoordinate.Y);
            }
        }

        #endregion

        #endregion
    }
}
