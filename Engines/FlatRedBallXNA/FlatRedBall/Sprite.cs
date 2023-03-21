#if WINDOWS
#define USE_CUSTOM_SHADER
#endif
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
using System.Threading.Tasks;

namespace FlatRedBall
{
    /// <summary>
    /// Delegate for methods which can be assigned to the Sprite for every-frame
    /// custom logic or when a Sprite is removed.
    /// </summary>
    /// <remarks>
    /// <seealso cref="FlatRedBall.Sprite.CustomBehavior"/>
    /// <seealso cref="FlatRedBall.Sprite.Remove"/>
    /// </remarks>
    /// <param name="sprite">The Sprite on which the logic should execute.</param>
    public delegate void SpriteCustomBehavior(Sprite sprite);

    /// <summary>
    /// A visual element which can display an entire Texture2D, part of a Texture2D, or solid color.
    /// </summary>
    /// <remarks>
    /// Sprites are one of the most common objects used in FlatRedBall. They are typically used to visualize entities (player, enemies, bullets, power-ups).
    /// Sprites can also be used for particles and other standalone visuals which are not tied to entities. Sprites support a variety of features including
    /// velocity, acceleration, drag, rotation, attachment, AnimationChain animations, color operations, and scaling.
    /// </remarks>
    public partial class Sprite : PositionedObject, IAnimationChainAnimatable, IScalable, IVisible, IAnimatable, IColorable, ICursorSelectable,
        ITexturable
#if FRB_XNA && !MONOGAME
        , IMouseOver
#endif
    {
        #region Fields

        /// <summary>
        /// Whether the Sprite should be rotated to always face the camera.
        /// </summary>
        public bool IsBillboarded;

        #region Color/Fade
        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;

        internal ColorOperation mColorOperation;
        internal BlendOperation mBlendOperation;

        // This used to only be on MonoDroid and WP7, but we need it on PC for premult alpha when using ColorOperation.Color
        // internal to skip the property and speed things up a little
        internal float mRed;
        internal float mGreen;
        internal float mBlue;
        internal float mAlpha;
        #endregion

        #region ICursorSelectable
        protected bool mCursorSelectable;
        #endregion

        #region Texture and pixel size
        internal Texture2D mTexture; // made internal to avoid a getter in tight loops
        internal float mPixelSize;
        bool mFlipHorizontal;
        bool mFlipVertical;
        #endregion

        #region Scale

        float mScaleX;
        float mScaleY;
        float mScaleXVelocity;
        float mScaleYVelocity;

        #endregion

        #region Animation

        // September 21, 2011 (Victor Chelaru)
        // This used to default to false but I think
        // it's best for it to default to true.  The reason
        // is that if it defaults to false, someone might assign
        // an AnimationChain and not know why...and wonder what is
        // going on.  But if we default it to true then it'll make sense
        // why the Sprite is animating when its AnimationChain is assigned.
        bool mAnimate = true;

        // December 1, 2011 (Victor Chelaru)
        // This used to be false but now the typical
        // setup is to have a Sprite attached to an Entity.
        // Users expect that relative values will apply.
        bool mUseAnimationRelativePosition = true;
        AnimationChainList mAnimationChains;
        int mCurrentChainIndex;

        float mAnimationSpeed;

        internal bool mJustCycled;
        internal bool mJustChangedFrame;

        int mCurrentFrameIndex;
        double mTimeIntoAnimation;

        bool mIgnoreAnimationChainTextureFlip;

        /// <summary>
        /// Whether the Sprite code will tolerate (not throw exceptions)
        /// CurrentChainName assignments which do not exist in the code. Normally
        /// this is false. This is set to true in edit mode.
        /// </summary>
        public static bool TolerateMissingAnimations = false;

        #endregion

        #region Particle Fields
        
        /// <summary>
        /// Used by particles to flag a particular Sprite as empty.
        /// </summary>
        /// <remarks>
        /// The SpriteManager is responsible for particle recycling and uses this value to indicate
        /// whether a Sprite is currently used.  This should not be manually modified.
        /// </remarks>
        internal bool mEmpty;
        internal int mParticleIndex;
        double mTimeCreated;
        #endregion

        #region Internal Drawing members
        internal bool mInCameraView;
        internal bool mAutomaticallyUpdated;
        internal VertexPositionColorTexture[] mVerticesForDrawing;
        internal Vector3 mOldPosition; // used when sorting along forward vector to hold old position
        internal SpriteVertex[] mVertices;

        internal bool mOrdered = true;
        #endregion

        bool mVisible;

        #endregion

        #region Properties


        #region IColorable

        /// <summary>
        /// Controls the Sprite's transparency.
        /// </summary>
        /// <remarks>
        /// Alpha controls a Sprite's transparency.   A completely opaque Sprite has an
        /// Alpha of 1 while a completely transparent object has an Alpha of 0.
        /// 
        /// Setting the AlphaRate of a completely opaque Sprite to -1 will 
        /// make the sprite disappear in one second.  Invisible Sprites continue
        /// to remain in memory and are managed by the SpriteManager.  The Alpha variable
        /// will automatically regulate itself if the value is set to something outside of the
        /// 0 - 1 range.
        /// </remarks>
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

                mAlpha = value;
                UpdateColorsAccordingToAlpha();
            }
        }

        /// <summary>
        /// Sets the rate at which the Alpha property changes.  This is in units per second.  A fully opaque
        /// Sprite (Alpha = 1) will disappear in 1 second if its AlphaRate is set to -1.
        /// </summary>
        /// <remarks>
        /// The AlphaRate changes Alpha as follows:
        /// <para>
        /// Alpha += AlphaRate * TimeManager.SecondDifference;
        /// </para>
        /// This is automatically applied if the Sprite is managed by the SpriteManager (usually the case).
        /// </remarks>
        public float AlphaRate
        {
            get { return mAlphaRate; }
            set
            {
                mAlphaRate = value;
            }
        }

        private void UpdateColorsAccordingToAlpha()
        {

            float redValue = mRed;
            float greenValue = mGreen;
            float blueValue = mBlue;


#if USE_CUSTOM_SHADER
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
#if USE_CUSTOM_SHADER
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

        /// <summary>
        /// The red component of the color value to be used with the color operation. This value should be between 0 and 1.
        /// </summary>
        public float Red
        {
            get
            {
                return mRed;

            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mRed = value;

                UpdateColorsAccordingToAlpha();

            }
        }

        /// <summary>
        /// The green component of the color value to be used with the color operation. This value should be between 0 and 1.
        /// </summary>
        public float Green
        {
            get
            {
                return mGreen;
            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mGreen = value;

                UpdateColorsAccordingToAlpha();
            }
        }

        /// <summary>
        /// The blue component of the color value to be used with the color operation. This value should be between 0 and 1.
        /// </summary>
        public float Blue
        {
            get
            {
                return mBlue;
            }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mBlue = value;

                UpdateColorsAccordingToAlpha();
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


        public ColorOperation ColorOperation
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
                        value == Graphics.ColorOperation.Modulate2X || value == Graphics.ColorOperation.Modulate4X)
                    {
                        throw new Exception("The color operation " + value + " is not available due to platform restrictions");
                    }
                }

#endif



                mColorOperation = value;

                UpdateColorsAccordingToAlpha();
            }
        }

        public BlendOperation BlendOperation
        {
            get { return mBlendOperation; }
            set 
            { 
                mBlendOperation = value;

                UpdateColorsAccordingToAlpha();
            }
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

					UpdateColorsAccordingToAlpha ();

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

        /// <summary>
        /// The relationship between the displayed portion of the Sprite's texture and 
        /// its Width/Height. If this value is less than or equal to 0, then Width and Height
        /// values are not set according to the displayed portion of the Sprite's texture. Otherwise,
        /// the displayed portion of the texture are multiplied by this value to determine the Sprite's
        /// Width and Height.
        /// </summary>
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

        /// <summary>
        /// The top coordinate in texture coordinates on the sprite. Default is 0.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom side of the texture.
        /// </summary>
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

        /// <summary>
        /// The top pixel displayed on the sprite. Default is 0.
        /// This value is in pixel coordiantes, so it typically ranges from 0 to the height of the referenced texture.
        /// </summary>
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

        /// <summary>
        /// The bottom coordinate in texture coordinates on the sprite. Default is 1.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom side of the texture.
        /// </summary>
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

        public TextureAddressMode TextureAddressMode;

        /// <summary>
        /// Sets the forced texture filter for this sprite. 
        /// By default this is null, and will use the GraphicsOptions TextureFilter.
        /// </summary>
        public TextureFilter? TextureFilter;

        #endregion


        /// <summary>
        /// These can be used to change Sprite appearance
        /// on individual vertices.
        /// </summary>
        /// <remarks>
        /// The index begins counting at the top left (index 0)
        /// and increases moving clockwise.
        /// </remarks>
        public SpriteVertex[] Vertices => mVertices;

        /// <summary>
        /// Represents the four (4) vertices used to render the Sprite.  This value is set
        /// if the Sprite is either a manuall updated Sprite, or if the SpriteManager's ManualUpdate
        /// method is called on this.
        /// </summary>
        public VertexPositionColorTexture[] VerticesForDrawing
        {
            get { return mVerticesForDrawing; }
        }

        #region Animation

        public bool Animate
        {
            get => mAnimate;
            set => mAnimate = value;
        }

        [ExportOrder(4)]
        public AnimationChainList AnimationChains
        {
            get { return mAnimationChains; }
            set
            {
#if DEBUG
                if (value == null)
                {
                    throw new InvalidOperationException("AnimationChains cannot be set to null. To clear the animations, assign the value to a new AnimationChainList.");
                }
#endif
                mAnimationChains = value;
                // If the user sets a new set of chains, then the CurrentChain may not have enough frames, so reset it
                if (mCurrentFrameIndex >= CurrentChain?.Count)
                {
                    mCurrentFrameIndex = 0;
                }
                UpdateTimeBasedOffOfAnimationFrame();
                UpdateToCurrentAnimationFrame();
            }
        }

        /// <summary>
        /// Gets or sets the speed multiplier when playing animations. A value of 1 makes animations play at normal speed.
        /// A value of 2 will play animations twice as fast.
        /// </summary>
        public float AnimationSpeed
        {
            get => mAnimationSpeed;
            set => mAnimationSpeed = value;
        }

        /// <summary>
        /// Gets the current animationChain - retrieved through the CurrentChainIndex property. Setting this sets the CurrentChainName since rather than
        /// a direct reference. Therefore, this chain must be contained in the AnimationChains list.
        /// </summary>
        public AnimationChain CurrentChain
        {
            set
            {
#if DEBUG
                if (AnimationChains.Contains(value) == false)
                {
                    string message = $"The AnimationChains list {AnimationChains.Name} does not contain the assigned AnimationChain {value?.Name}, so it cannot be set";
                    throw new InvalidOperationException(message);
                }
#endif
                CurrentChainName = value?.Name;
            }
            get
            {
                if (mCurrentChainIndex != -1 && mAnimationChains.Count > 0 && mCurrentChainIndex < mAnimationChains.Count)
                {
                    return mAnimationChains[mCurrentChainIndex];
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets and sets the index of the current AnimationChain in the Sprite's AnimationChains list.
        /// </summary>
        public int CurrentChainIndex
        {
            get => mCurrentChainIndex;
            set
            {
                bool wasChangd = mCurrentChainIndex != value;

                mCurrentChainIndex = value;

                if (wasChangd)
                {
                    // June 20, 2017
                    // Not sure why we
                    // reset the mTimeIntoAnimation
                    // here. Instead, we should be setting
                    // it to preserve the frame. That is, we
                    // should either reset both the frame and
                    // mTimeIntoAnimation to 0, or we should preserve
                    // both. I'm going to preserve both because that seems
                    // to be the intended behavior of the engine, historically:
                    //mTimeIntoAnimation = 0;
                    UpdateToCurrentAnimationFrame();
                    UpdateTimeBasedOffOfAnimationFrame();
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the current animation chain on the sprite. Setting this value is the recommended way to switch between animations.
        /// Setting this value will search the Sprite's AnimationChains for an animation with a matching name. If the argument differs from the current
        /// animation, the animation is set and played from the beginning. If the current animation matches the assigned value, no changes are made.
        /// </summary>
        public string CurrentChainName
        {
            get
            {
                if (mAnimationChains != null && mAnimationChains.Count != 0 && mCurrentChainIndex != -1 && mAnimationChains != null)
                {
                    return mAnimationChains[mCurrentChainIndex].Name;
                }
                else
                {
                    return null;
                }
            }
            set
            {
#if DEBUG
                if (mAnimationChains == null)
                {
                    throw new NullReferenceException("AnimationChains is null - this must be assigned first before setting the CurrentChainName?");
                }
#endif

                if (mCurrentChainIndex == -1 ||
                    mCurrentChainIndex >= mAnimationChains.Count ||
                    mAnimationChains[mCurrentChainIndex].Name != value)
                {
                    bool wasAnimationSet = false;

                    // If the animation is null, let's not do anything.  
                    // This could get set to null through Glue.
                    if (string.IsNullOrEmpty(value))
                    {
                        wasAnimationSet = true;
                        mTimeIntoAnimation = 0;
                        mCurrentChainIndex = -1;
                    }
                    else
                    {
#if DEBUG
                        if (mAnimationChains == null)
                        {
                            throw new NullReferenceException("AnimationChains haven't been set yet.  Do this first before setting CurrentChainName");
                        }
#endif


                        for (int i = mAnimationChains.Count - 1; i > -1; i--)
                        {
                            if (mAnimationChains[i].Name == value)
                            {
                                mTimeIntoAnimation = 0;
                                mCurrentFrameIndex = 0;
                                wasAnimationSet = true;
                                mCurrentChainIndex = i;

                                UpdateToCurrentAnimationFrame();

                                break;
                            }
                        }
                    }

                    if (!wasAnimationSet && !TolerateMissingAnimations)
                    {
                        string error = "There is no animation named " + value;

                        if (mAnimationChains?.Name != null)
                        {
                            error += $" in AnimationChain {mAnimationChains?.Name}";
                        }

                        if (mAnimationChains.Count == 0)
                        {
                            error += "\nThis sprite has no animations";
                        }
                        else
                        {
                            error += "\nAvailable animations are:";
                            for (int i = 0; i < mAnimationChains.Count; i++)
                            {
                                error += "\n" + mAnimationChains[i].Name;
                            }
                        }
                        throw new InvalidOperationException(error);
                    }
                }
            }

        }

        /// <summary>
        /// The frame currently displayed by the Sprite. Normally this frame will advance automatically as a Sprite plays an animation. Manually
        /// setting this value updates the Sprite's texture coordiantes and offsets.
        /// </summary>
        public int CurrentFrameIndex
        {
            get { return mCurrentFrameIndex; }
            set
            {
                if (mCurrentFrameIndex != value)
                {
                    mCurrentFrameIndex = value;
                    UpdateTimeBasedOffOfAnimationFrame();
                    UpdateToCurrentAnimationFrame();
                }
            }
        }

        /// <summary>
        /// The current AnimationFrame displayed by this Sprite, or null if no AnimationFrame is being displayed
        /// </summary>
        public AnimationFrame CurrentFrame
        {
            get
            {
                var currentChain = CurrentChain;
                if (currentChain != null && mCurrentFrameIndex > -1 && mCurrentFrameIndex < currentChain.Count)
                {
                    return currentChain[mCurrentFrameIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// Whether this Sprite just changed the AnimationFrame it is displaying due to internal animation activity this frame.
        /// </summary>
        public bool JustChangedFrame
        {
            get { return mJustChangedFrame; }
        }

        /// <summary>
        /// Whether this Sprite just cycled its animation (set its CurrentFrameIndex to 0) due to internal animation activity this frame.
        /// </summary>
        public bool JustCycled
        {
            get { return mJustCycled; }
        }

        public bool UseAnimationRelativePosition
        {
            get { return mUseAnimationRelativePosition; }
            set { mUseAnimationRelativePosition = value; }
        }

        [Obsolete("Use UseAnimationTextureFlip instead")]
        public bool IgnoreAnimationChainTextureFlip
        {
            get { return mIgnoreAnimationChainTextureFlip; }
            set { mIgnoreAnimationChainTextureFlip = value; }
        }

        public bool UseAnimationTextureFlip
        {
            get => !mIgnoreAnimationChainTextureFlip;
            set => mIgnoreAnimationChainTextureFlip = !value;
        }

        public double TimeIntoAnimation
        {
            get
            {
                return mTimeIntoAnimation;
            }
            set
            {
                mTimeIntoAnimation = value;

                UpdateFrameBasedOffOfTimeIntoAnimation();

                UpdateToCurrentAnimationFrame();

            }
        }
        //public double TimeUntilNextFrame
        //{
        //    get { return mTimeUntilNextFrame; }
        //    set { mTimeUntilNextFrame = value; }
        //}

        #endregion

        #region IScalable

        /// <summary>
        /// The distance from the center of the Sprite to its edge, which is equal to Width / 2.
        /// </summary>
        public float ScaleX
        {
            get { return mScaleX; }
            set { mScaleX = value; }
        }

        /// <summary>
        /// The distance from the center of the Sprite to its edge, which is equal to Height / 2. 
        /// </summary>
        public float ScaleY
        {
            get { return mScaleY; }
            set { mScaleY = value; }
        }

        public float ScaleXVelocity
        {
            get { return mScaleXVelocity; }
            set { mScaleXVelocity = value; }
        }

        public float ScaleYVelocity
        {
            get { return mScaleYVelocity; }
            set { mScaleYVelocity = value; }
        }

        /// <summary>
        /// Returns the left X of the Sprite, assuming no rotation.
        /// </summary>
        /// <remarks>
        /// If the sprite is rotated, this property will not return the correct value.
        /// </remarks>
        public float Left
        {
            get { return Position.X - mScaleX; }
            set { Position.X = value + mScaleX; }
        }

        /// <summary>
        /// Returns the right X of the sprite, assuming no rotation.
        /// </summary>
        /// <remarks>
        /// If the sprite is rotated, this property will not return the correct value.
        /// </remarks>
        public float Right
        {
            get { return Position.X + mScaleX; }
            set { Position.X = value - mScaleX; }
        }

        /// <summary>
        /// Returns the top Y of the sprite, assuming no rotation.
        /// </summary>
        /// <remarks>
        /// If the sprite is rotated, this property will not return the correct value.
        /// </remarks>
        public float Top
        {
            get { return Position.Y + mScaleY; }
            set { Position.Y = value - mScaleY; }
        }

        /// <summary>
        /// Returns the bottom Y of the sprite, assuming no rotation.
        /// </summary>
        /// <remarks>
        /// If the sprite is rotated, this property will not return the correct value.
        /// </remarks>
        public float Bottom
        {
            get { return Position.Y - mScaleY; }
            set { Position.Y = value + mScaleY; }
        }

        /// <summary>
        /// Returns the left edge of the Sprite relative its Parent.  This value uses RelativeX and Width/2 to calculate this value.
        /// </summary>
        public float RelativeLeft
        {
            get { return RelativePosition.X - mScaleX; }
            set { RelativePosition.X = value + mScaleX; }
        }

        /// <summary>
        /// Returns the right edge of the Sprite relative its Parent.  This value uses RelativeX and Width/2 to calculate this value.
        /// </summary>
        public float RelativeRight
        {
            get { return RelativePosition.X + mScaleX; }
            set { RelativePosition.X = value - mScaleX; }
        }

        /// <summary>
        /// Returns the top edge of the Sprite relative its Parent.  This value uses RelativeY and Height/2 to calculate this value.
        /// </summary>
        public float RelativeTop
        {
            get { return RelativePosition.Y + mScaleY; }
            set { RelativePosition.Y = value - mScaleY; }
        }

        /// <summary>
        /// Returns the bottom edge of the Sprite relative its Parent.  This value uses RelativeY and Height/2 to calculate this value.
        /// </summary>
        public float RelativeBottom
        {
            get { return RelativePosition.Y - mScaleY; }
            set { RelativePosition.Y = value + mScaleY; }
        }

        /// <summary>
        /// The Sprite's Width in absolute world coordinates. This value may be set according
        /// to the currently displayed texture and texture coordinates if TextureScale is greater than 0.
        /// </summary>
        public float Width
        {
            get
            {
                return ScaleX * 2;
            }
            set
            {
                ScaleX = value / 2.0f;
            }
        }

        /// <summary>
        /// The Sprite's Height in absolute world coordinates. This value may be set according
        /// to the currently displayed texture and texture coordinates if TextureScale is greater than 0.
        /// </summary>
        public float Height
        {
            get
            {
                return ScaleY * 2;
            }
            set
            {
                ScaleY = value / 2.0f;
            }
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// The time returned by the TimeManager when the Sprite was created.
        /// </summary>
        /// <remarks>
        /// This value is automatically set when the Sprite
        /// is added through the SpriteManager.  If a Sprite is created manually (either as a
        /// Sprite or a class inheriting from the Sprite class) this value should be set manually
        /// if it is to be used later.
        /// </remarks>
        #endregion
        public double TimeCreated
        {
            get { return mTimeCreated; }
            set { mTimeCreated = value; }
        }

        #region XML Docs
        /// <summary>
        /// Controls the visibility of the Sprite
        /// </summary>
        /// <remarks>
        /// This variable controls the visiblity of the Sprite.  Sprites are visible
        /// by default.  Setting Visible to false will make the sprite invisible, but
        /// the Sprite will continue to behave regularly; custom behavior, movement, attachment,
        /// and animation are still executed, and collision is possible.
        /// </remarks>
        #endregion
        public virtual bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
            }
        }

        #region ICursorSelectable
        public bool CursorSelectable
        {
            get { return mCursorSelectable; }
            set { mCursorSelectable = value; }
        }
        #endregion

        #endregion

        #region Events

        // Vic says - I tried removing this but it is used by particles so we'd need a new list for particles to be removed when out of screen.
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

            TimeCreated = TimeManager.CurrentTime;

#if MONODROID
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

            TextureAddressMode = TextureAddressMode.Clamp;

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

        internal void UpdateVertices()
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

            if (IsBillboarded)
            {
                Matrix modifiedMatrix =  mRotationMatrix * Camera.Main.RotationMatrix;

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

        #region Initialize

        public override void Initialize()
        {
            Initialize(true);
        }

        public override void Initialize(bool initializeListsBelongingTo)
        {
            base.Initialize(initializeListsBelongingTo);

            mTexture = null;
            mScaleX = 1;
            mScaleY = 1;
            mScaleXVelocity = 0;
            mScaleYVelocity = 0;

            mVisible = true;
            mEmpty = true;


            Remove = null;

            mAnimate = false;

            Alpha = 1;
            Red = 0;
            Green = 0;
            Blue = 0;

            AlphaRate = 0;
            RedRate = 0;
            GreenRate = 0;
            BlueRate = 0;

            mCurrentChainIndex = 0;
            mCurrentFrameIndex = 0;

            mAnimationSpeed = 1;
            mJustChangedFrame = false;
            mJustCycled = false;
            mInstructions.Clear();


            PlatformSpecificInitialization();
            //           constantPixelSize = -1;
        }

        #endregion

        #region Public Methods




        /// <summary>
        /// Sets the ScaleY so that the ScaleX/ScaleY ratio is the same as the source image used for the Sprite's texture.
        /// </summary>
        public void SetScaleYRatioToX()
        {
            float widthToUse = mTexture.Width * (RightTextureCoordinate - LeftTextureCoordinate);
            float heightToUse = mTexture.Height * (BottomTextureCoordinate - TopTextureCoordinate);

            if (widthToUse != 0)
            {
                ScaleY = ScaleX * heightToUse / widthToUse;
            }
        }

        /// <summary>
        /// Sets the ScaleY so that the ScaleX/ScaleY ratio is the same as the source image used for the Sprite's texture.
        /// </summary>
        public virtual void SetScaleXRatioToY()
        {
            float widthToUse = mTexture.Width * (RightTextureCoordinate - LeftTextureCoordinate);
            float heightToUse = mTexture.Height * (BottomTextureCoordinate - TopTextureCoordinate);

            if (heightToUse != 0)
            {
                ScaleX = ScaleY * widthToUse / (float)heightToUse;
            }
        }

        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.SpriteUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.SpriteUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        /// <summary>
        /// Applies all velocities, rates, accelerations for real and relative values.
        /// If the Sprite is part of the SpriteManager (which is common) then this is automatically
        /// called.
        /// </summary>
        /// <param name="secondDifference">The number of seocnds that have passed since last frame.</param>
        /// <param name="secondDifferenceSquaredDividedByTwo">Precalculated (secondDifference * secondDifference)/2.0f for applying acceleration.</param>
        /// <param name="secondsPassedLastFrame">The number of seconds that passed last frame for calculating "real" values.</param>
        public override void TimedActivity(float secondDifference, double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            if (mAlphaRate != 0.0f)
                Alpha += mAlphaRate * secondDifference;
            if (mRedRate != 0.0f)
                Red += mRedRate * secondDifference;
            if (mGreenRate != 0.0f)
                Green += mGreenRate * secondDifference;
            if (mBlueRate != 0.0f)
                Blue += mBlueRate * secondDifference;

            mScaleX += mScaleXVelocity * secondDifference;
            mScaleY += mScaleYVelocity * secondDifference;

        }


        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();
            SpriteManager.ManualUpdate(this);
        }

        /// <summary>
        /// Updates the sprite according to its current AnimationChain and AnimationFrame. Specifically this updates
        ///  - Texture
        ///  - Texture coordiantes
        ///  - Flip Horizontal (if IgnoreAnimationChainTextureFlip is true)
        ///  - Relative X and Y (if UseAnimationRelativePosition is true
        ///  - Executes AnimationFrame instructions
        ///  - Adjusts the size of the sprite if its TextureScale
        /// </summary>
        /// <remarks>
        /// This method is automatically called for sprites which are automatically updated (default) and which are using
        /// an AnimationChain. This can be manually called after assigning the AnimationFrame and AnimationChain.
        /// </remarks>
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

                UpdateScale();
                
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

        [Obsolete("Do not use this!  This will go away.  Use the Entity pattern instead", error:true)]
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

        #region IVisible implementation



        IVisible IVisible.Parent
        {
            get
            {
                return mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return mVisible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }

        #endregion

        #region Animation

        public async Task PlayAnimationsAsync(params string[] animations)
        {
            foreach(var animation in animations)
            {
                CurrentChainName = animation;
                await TimeManager.DelaySeconds(CurrentChain.TotalLength);
            }
        }

        /// <summary>
        /// Performs the every-frame logic for updating the current AnimationFrame index.  If the
        /// Sprite is part of the SpriteManager then this is automatically called.
        /// </summary>
        /// <param name="currentTime">The number of seconds that have passed since the game has started running.</param>
        public void AnimateSelf(double currentTime)
        {
            mJustChangedFrame = false;
            mJustCycled = false;
            if (mAnimate == false || mCurrentChainIndex == -1 || mAnimationChains.Count == 0 || mCurrentChainIndex >= mAnimationChains.Count || mAnimationChains[mCurrentChainIndex].Count == 0) return;

            int frameBefore = mCurrentFrameIndex;

            // June 10, 2011
            // A negative animation speed should cause the animation to play in reverse
            //Removed the System.Math.Abs on the mAnimationSpeed variable to restore the correct behaviour.
            //double modifiedTimePassed = TimeManager.SecondDifference * System.Math.Abs(mAnimationSpeed);
            double modifiedTimePassed = TimeManager.SecondDifference * mAnimationSpeed;

            mTimeIntoAnimation += modifiedTimePassed;

            AnimationChain animationChain = mAnimationChains[mCurrentChainIndex];

            mTimeIntoAnimation = MathFunctions.Loop(mTimeIntoAnimation, animationChain.TotalLength, out mJustCycled);

            UpdateFrameBasedOffOfTimeIntoAnimation();

            if (mCurrentFrameIndex != frameBefore)
            {
                UpdateToCurrentAnimationFrame();
                mJustChangedFrame = true;
            }
        }

        /// <summary>
        /// Clears all references to AnimationChains and sets the Animate property to false.
        /// </summary>
        public void ClearAnimationChains()
        {
            mAnimate = false;
            mCurrentChainIndex = -1;

            mAnimationChains.Clear();
        }

        /// <summary>
        /// Removes the AnimationChain from the Sprite's internal AnimationChain List.
        /// </summary>
        /// <remarks>
        /// If the chainToRemove is also the CurrentChain, the animate field 
        /// is set to false.
        /// </remarks>
        /// <param name="chainToRemove">The AnimationChain to remove.</param>
        public void RemoveAnimationChain(AnimationChain chainToRemove)
        {
            int index = mAnimationChains.IndexOf(chainToRemove);

            if (mAnimationChains.Contains(chainToRemove))
            {
                mAnimationChains.Remove(chainToRemove);
            }
            if (index == mCurrentChainIndex)
            {
                mCurrentChainIndex = -1;
                mAnimate = false;
            }
        }

        /// <summary>
        /// Sets the argument chainToSet as the animationChain. If the argument chainToSet is not
        /// part of the Sprite's internal list of AnimationChains, it is added.
        /// </summary>
        /// <remarks>
        /// This differs from FlatRedBall MDX - this method on FlatRedBall MDX does not add the argument
        /// AnimationChain to the Sprite's internal list.
        /// <para>
        /// This does not set any animation-related properties, but it does set the current
        /// texture to the current frame's texture.  Therefore, it is still necessary to set Animate to true.
        /// </para>
        /// </remarks>
        /// <param name="chainToSet">The AnimationChain to set as the current AnimationChain.  This is
        /// added to the internal AnimationChains property if it is not already there.</param>
        public void SetAnimationChain(AnimationChain chainToSet)
        {
            if (chainToSet != null)
            {
                int index = mAnimationChains.IndexOf(chainToSet);
                if (index != -1)
                    mCurrentChainIndex = index;
                else
                {
                    mAnimationChains.Add(chainToSet);
                    mCurrentChainIndex = mAnimationChains.Count - 1;
                }

                mTimeIntoAnimation = 0;
                mCurrentFrameIndex = 0;
                UpdateToCurrentAnimationFrame();
            }
        }


        public void SetAnimationChain(AnimationChain chainToSet, double timeIntoAnimation)
        {
            if (chainToSet != null)
            {
                mCurrentFrameIndex = 0;
                SetAnimationChain(chainToSet);

                mTimeIntoAnimation = timeIntoAnimation;

                UpdateFrameBasedOffOfTimeIntoAnimation();
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the current AnimationChain by name and keeps the CurrentFrame the same.
        /// </summary>
        /// <remarks>
        /// This method assumes that the Sprite contains a reference to an AnimationChain with the name matching chainToSet.  Passing a
        /// name that is not found in the Sprite's AnimationChainArray will not cause any changes.
        /// 
        /// <para>This method will keep the CurrentFrame property the same (unless it exceeds the bounds of the new AnimationChain).  In the 
        /// case that the CurrentFrame is greater than the bounds of the new AnimationChain, the animation will cycle back to the beginning.
        /// The animate field is not changed to true if it is false.</para>
        /// <seealso cref="FRB.Sprite.AnimationChains"/>
        /// </remarks>
        /// <param name="chainToSet">The name of the AnimationChain to set as current.</param>
        #endregion
        [Obsolete("Use the CurrentChainName Property instead of this method")]
        public void SetAnimationChain(string chainToSet)
        {
            CurrentChainName = chainToSet;
        }

        public string GetAnimationInformation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Animate: ").AppendLine(mAnimate.ToString());
            if (CurrentChain != null)
                stringBuilder.Append("CurrentChain: ").AppendLine(CurrentChain.Name);
            else
                stringBuilder.Append("CurrentChain: <null>");

            stringBuilder.Append("CurrentFrameIndex: ").AppendLine(CurrentFrameIndex.ToString());

            stringBuilder.Append("Time Into Animation: ").AppendLine(mTimeIntoAnimation.ToString());

            return stringBuilder.ToString();


        }


        void UpdateFrameBasedOffOfTimeIntoAnimation()
        {
            if (double.IsPositiveInfinity(mTimeIntoAnimation))
            {
                mTimeIntoAnimation = 0;
            }
            double timeIntoAnimation = mTimeIntoAnimation;

            // Not sure how this can happen, but we want to make sure the engine doesn't freeze if it does

            if (timeIntoAnimation < 0)
            {
                throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
            }
            else if (CurrentChain != null && CurrentChain.Count > 1)
            {
                int frameIndex = 0;
                while (timeIntoAnimation >= 0)
                {
                    double frameTime = CurrentChain[frameIndex].FrameLength;

                    if (timeIntoAnimation < frameTime)
                    {
                        mCurrentFrameIndex = frameIndex;

                        break;
                    }
                    else
                    {
                        timeIntoAnimation -= frameTime;

                        frameIndex = (frameIndex + 1) % CurrentChain.Count;
                    }
                }
            }
        }

        void UpdateTimeBasedOffOfAnimationFrame()
        {
            int animationFrame = mCurrentFrameIndex;

            if (animationFrame < 0)
            {
                throw new ArgumentException("The animationFrame argument must be 0 or positive");
            }
            else if (CurrentChain != null && CurrentChain.Count > 1)
            {
                mTimeIntoAnimation = 0.0f;
                //update to the correct time for this frame
                for (int x = 0; x < mCurrentFrameIndex && x < CurrentChain.Count; x++)
                {
                    mTimeIntoAnimation += CurrentChain[x].FrameLength;
                }
            }
        }
        #endregion

        #region IAnimatable implementation


        void IAnimatable.PlayAnimation(string animationName)
        {
            this.CurrentChainName = animationName;
            Animate = true;
        }

        bool IAnimatable.HasAnimation(string animationName)
        {
            if (AnimationChains == null)
            {
                return false;
            }

            for (int i = 0; i < AnimationChains.Count; i++)
            {
                if (AnimationChains[i].Name == animationName)
                {
                    return true;
                }
            }
            return false;
        }

        bool IAnimatable.IsPlayingAnimation(string animationName)
        {
            return this.Animate && CurrentChainName == animationName;
        }

        bool IAnimatable.DidAnimationFinishOrLoop => JustCycled;

        #endregion
    }
}
