using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;

using FlatRedBall.Utilities;

using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.ManagedSpriteGroups
{
    /// <summary>
    /// Delegate for methods which can be assigned to the SpriteFrame
    /// for every-frame custom logic.
    /// </summary>
    /// <param name="spriteFrame">The SpriteFrame on which the logic should execute.</param>
    public delegate void SpriteFrameCustomBehavior(SpriteFrame spriteFrame);

    #region XML Docs
    /// <summary>
    /// Visible object with static-width borders.
    /// </summary>
    /// <remarks>
    /// SpriteFrames are often used for creating UI because their static-width
    /// borders make single-texture UI entities easy to construct and manage.
    /// <para>
    /// SpriteFrames achieve a static-width border by 
    /// 
    /// </para>
    /// </remarks>
    #endregion
    public class SpriteFrame : PositionedObject, IScalable, IColorable, 
        IAnimationChainAnimatable, ICursorSelectable, IEquatable<SpriteFrame>, IMouseOver, IVisible
    {
        #region Enums

        #region XML Docs
        /// <summary>
        /// Defines sides which can be combined to speicfy borders on SpriteFrames.
        /// </summary>
        /// <remarks>
        /// The most common combinations are BorderSides.All, 
        /// BorderSides.Left | BorderSides.Right, and BorderSides.Top | BorderSides.Bottom.
        /// </remarks>
        #endregion
        public enum BorderSides
        {
            #region XML Docs
            /// <summary>
            /// No border sides - the SpriteFrame will appear similar to a regular Sprite.
            /// </summary>
            #endregion
            None = 0,
            #region XML Docs
            /// <summary>
            /// Include a border on the top of the SpriteFrame.
            /// </summary>
            #endregion
            Top = 1,
            #region XML Docs
            /// <summary>
            /// Include a border on the bottom of the SpriteFrame.
            /// </summary>
            #endregion
            Bottom = 2,
            #region XML Docs
            /// <summary>
            /// Include a border on the top and bottom of the SpriteFrame.  This is equivalent to 
            /// BorderSides.Top | BorderSides.Bottom
            /// </summary>
            #endregion
            TopBottom = 3,
            #region XML Docs
            /// <summary>
            /// Include a border on the left of the SpriteFrame.
            /// </summary>
            #endregion
            Left = 4,
            #region XML Docs
            /// <summary>
            /// Include a border on the right of the SpriteFrame.
            /// </summary>
            #endregion
            Right = 8,
            #region XML Docs
            /// <summary>
            /// Include a border on the left and right of the SpriteFrame.  This is equivalent to 
            /// BorderSides.Right | BorderSides.Left
            /// </summary>
            #endregion
            LeftRight = 12,
            #region XML Docs
            /// <summary>
            /// Include a border on the top, left, and right of the SpriteFrame.  This is 
            /// equivalent to BorderSides.Left | BorderSides.Right | borderSides.Top.
            /// </summary>
            #endregion
            TopLeftRight = 13,
            #region XML Docs
            /// <summary>
            /// Include borders on all sides of the SpriteFrame.  This is equivalent to 
            /// BorderSides.Right | BorderSides.Left | BorderSides.Top | BorderSides.Bottom.
            /// </summary>
            #endregion
            All = 15
        }
        #endregion

        #region Fields

        #region 9 pieces of SpriteFrame

        internal Sprite mCenter;

        internal Sprite mTopLeft;
        internal Sprite mTop;
        internal Sprite mTopRight;
        internal Sprite mRight;
        internal Sprite mBottomRight;
        internal Sprite mBottom;
        internal Sprite mBottomLeft;
        internal Sprite mLeft;
        
        #endregion

        #region Scale variables
        float mScaleX;
        float mScaleY;
        float mScaleXVelocity;
        float mScaleYVelocity;

        float mScaleXAfterManage;
        float mScaleYAfterManage;

        #endregion

        internal Layer mLayerBelongingTo;

        float mTextureBorderWidth;
        float mSpriteBorderWidth;

		float mPixelSize;

        // these are stores separate from the mParentSprite scale so that
        // the update is not performed every frame; that is, if the
        // mParentSprite scale does not match the mSclX or mSclY, then
        // a change has been made this frame, so the borders and center
        // Sprites should be changed.
         
        #endregion


        #region Properties

        #region IColorable

        #region XML Docs
        /// <summary>
        /// Controls the SpriteFrame's transparency.
        /// </summary>
        #endregion
        public float Alpha
        {
            get { return mCenter.Alpha; }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(0, value);

                mCenter.Alpha = value;
                if (mTop != null)
                    mTop.Alpha = value;
                if (mBottom != null)
                    mBottom.Alpha = value;
                if (mLeft != null)
                    mLeft.Alpha = value;
                if (mRight != null)
                    mRight.Alpha = value;
                if (mTopLeft != null)
                    mTopLeft.Alpha = value;
                if (mTopRight != null)
                    mTopRight.Alpha = value;
                if (mBottomLeft != null)
                    mBottomLeft.Alpha = value;
                if (mBottomRight != null)
                    mBottomRight.Alpha = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the alpha component in units per second.
        /// </summary>
        #endregion
        public float AlphaRate
        {
            get { return mCenter.AlphaRate; }
            set
            {
                mCenter.AlphaRate = value;
                if (mTop != null)
                    mTop.AlphaRate = value;
                if (mBottom != null)
                    mBottom.AlphaRate = value;
                if (mLeft != null)
                    mLeft.AlphaRate = value;
                if (mRight != null)
                    mRight.AlphaRate = value;
                if (mTopLeft != null)
                    mTopLeft.AlphaRate = value;
                if (mTopRight != null)
                    mTopRight.AlphaRate = value;
                if (mBottomLeft != null)
                    mBottomLeft.AlphaRate = value;
                if (mBottomRight != null)
                    mBottomRight.AlphaRate = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The red value to use with the ColorOperation.
        /// </summary>
        #endregion
        public float Red
        {
            get { return mCenter.Red; }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mCenter.Red = value;
                if (mTop != null)
                    mTop.Red = value;
                if (mBottom != null)
                    mBottom.Red = value;
                if (mLeft != null)
                    mLeft.Red = value;
                if (mRight != null)
                    mRight.Red = value;
                if (mTopLeft != null)
                    mTopLeft.Red = value;
                if (mTopRight != null)
                    mTopRight.Red = value;
                if (mBottomLeft != null)
                    mBottomLeft.Red = value;
                if (mBottomRight != null)
                    mBottomRight.Red = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The green value to use with the ColorOperation.
        /// </summary>
        #endregion
        public float Green
        {
            get { return mCenter.Green; }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mCenter.Green = value;
                if (mTop != null)
                    mTop.Green = value;
                if (mBottom != null)
                    mBottom.Green = value;
                if (mLeft != null)
                    mLeft.Green = value;
                if (mRight != null)
                    mRight.Green = value;
                if (mTopLeft != null)
                    mTopLeft.Green = value;
                if (mTopRight != null)
                    mTopRight.Green = value;
                if (mBottomLeft != null)
                    mBottomLeft.Green = value;
                if (mBottomRight != null)
                    mBottomRight.Green = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The blue value to use with the color operation.
        /// </summary>
        #endregion
        public float Blue
        {
            get { return mCenter.Blue; }
            set
            {
                value =
                    System.Math.Min(FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);
                value =
                    System.Math.Max(-FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue, value);

                mCenter.Blue = value;
                if (mTop != null)
                    mTop.Blue = value;
                if (mBottom != null)
                    mBottom.Blue = value;
                if (mLeft != null)
                    mLeft.Blue = value;
                if (mRight != null)
                    mRight.Blue = value;
                if (mTopLeft != null)
                    mTopLeft.Blue = value;
                if (mTopRight != null)
                    mTopRight.Blue = value;
                if (mBottomLeft != null)
                    mBottomLeft.Blue = value;
                if (mBottomRight != null)
                    mBottomRight.Blue = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the red component in units per second.
        /// </summary>
        #endregion
        public float RedRate
        {
            get { return mCenter.RedRate; }
            set
            {


                mCenter.RedRate = value;
                if (mTop != null)
                    mTop.RedRate = value;
                if (mBottom != null)
                    mBottom.RedRate = value;
                if (mLeft != null)
                    mLeft.RedRate = value;
                if (mRight != null)
                    mRight.RedRate = value;
                if (mTopLeft != null)
                    mTopLeft.RedRate = value;
                if (mTopRight != null)
                    mTopRight.RedRate = value;
                if (mBottomLeft != null)
                    mBottomLeft.RedRate = value;
                if (mBottomRight != null)
                    mBottomRight.RedRate = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the green component in units per second.
        /// </summary>
        #endregion
        public float GreenRate
        {
            get { return mCenter.GreenRate; }
            set
            {

                mCenter.GreenRate = value;
                if (mTop != null)
                    mTop.GreenRate = value;
                if (mBottom != null)
                    mBottom.GreenRate = value;
                if (mLeft != null)
                    mLeft.GreenRate = value;
                if (mRight != null)
                    mRight.GreenRate = value;
                if (mTopLeft != null)
                    mTopLeft.GreenRate = value;
                if (mTopRight != null)
                    mTopRight.GreenRate = value;
                if (mBottomLeft != null)
                    mBottomLeft.GreenRate = value;
                if (mBottomRight != null)
                    mBottomRight.GreenRate = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the blue component in units per second.
        /// </summary>
        #endregion
        public float BlueRate
        {
            get { return mCenter.BlueRate; }
            set
            {
                mCenter.BlueRate = value;
                if (mTop != null)
                    mTop.BlueRate = value;
                if (mBottom != null)
                    mBottom.BlueRate = value;
                if (mLeft != null)
                    mLeft.BlueRate = value;
                if (mRight != null)
                    mRight.BlueRate = value;
                if (mTopLeft != null)
                    mTopLeft.BlueRate = value;
                if (mTopRight != null)
                    mTopRight.BlueRate = value;
                if (mBottomLeft != null)
                    mBottomLeft.BlueRate = value;
                if (mBottomRight != null)
                    mBottomRight.BlueRate = value;
            }
        }

        /// <summary>
        /// The color operation to perform using the color component values and 
        /// Texture (if available).
        /// </summary>
        public ColorOperation ColorOperation
        {
            get { return mCenter.ColorOperation; }
            set
            {
                mCenter.ColorOperation = value;
                if (mTop != null)
                    mTop.ColorOperation = value;
                if (mBottom != null)
                    mBottom.ColorOperation = value;
                if (mLeft != null)
                    mLeft.ColorOperation = value;
                if (mRight != null)
                    mRight.ColorOperation = value;
                if (mTopLeft != null)
                    mTopLeft.ColorOperation = value;
                if (mTopRight != null)
                    mTopRight.ColorOperation = value;
                if (mBottomLeft != null)
                    mBottomLeft.ColorOperation = value;
                if (mBottomRight != null)
                    mBottomRight.ColorOperation = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The blend operation to perform using the alpha component value and
        /// Texture (if available).
        /// </summary>
        #endregion
        public BlendOperation BlendOperation
        {
            get { return mCenter.BlendOperation; }
            set
            {
                mCenter.BlendOperation = value;
                if (mTop != null)
                    mTop.BlendOperation = value;
                if (mBottom != null)
                    mBottom.BlendOperation = value;
                if (mLeft != null)
                    mLeft.BlendOperation = value;
                if (mRight != null)
                    mRight.BlendOperation = value;
                if (mTopLeft != null)
                    mTopLeft.BlendOperation = value;
                if (mTopRight != null)
                    mTopRight.BlendOperation = value;
                if (mBottomLeft != null)
                    mBottomLeft.BlendOperation = value;
                if (mBottomRight != null)
                    mBottomRight.BlendOperation = value;
            }
        }

        #endregion

        #region IAnimationChainAnimatable


        #region XML Docs
        /// <summary>
        /// Whether animation is currently turned on.
        /// </summary>
        #endregion
        public bool Animate
        {
            // only the center Sprite needs to be animated
            // The borders will have their texture set to the
            // mCenter's Texture in the Manage method.
            get { return mCenter.Animate; }
            set 
            { 
                mCenter.Animate = value; 
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets all animations stored in this.
        /// </summary>
        #endregion
        public AnimationChainList AnimationChains
        {
            get { return mCenter.AnimationChains; }
            set 
            { 
                mCenter.AnimationChains = value;

                // Refresh textures
                Texture = mCenter.Texture;
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets how fast AnimationChains will animate.  Default is 1.  A value
        /// of 2 will result in AnimationChains animating twice as fast.
        /// </summary>
        #endregion
        public float AnimationSpeed
        {
            get{ return mCenter.AnimationSpeed;}
            set { mCenter.AnimationSpeed = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the current AnimationChain.
        /// </summary>
        #endregion
        public AnimationChain CurrentChain
        {
            get{ return mCenter.CurrentChain;}
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the index of the current AnimationChain.
        /// </summary>
        #endregion
        public int CurrentChainIndex
        {
            get { return mCenter.CurrentChainIndex; }
            set { mCenter.CurrentChainIndex = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the current AnimationChain name or sets the current AnimationChain by name.
        /// </summary>
        /// <remarks>
        /// Setting this property will set the search the SpriteFrame for an AnimationChain with a
        /// matching name and set it as the current AnimationChain.
        /// </remarks>
        #endregion
        public string CurrentChainName
        {
            get { return mCenter.CurrentChainName; }
            set 
            {
                mCenter.CurrentChainName = value;
                // Refresh textures
                Texture = mCenter.Texture;

                UpdateTextureCoords();
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the current AnimationFrame index.
        /// </summary>
        #endregion
        public int CurrentFrameIndex
        {
            get { return mCenter.CurrentFrameIndex; }
            set { mCenter.CurrentFrameIndex = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets whether the current AnimationFrame just changed this frame due to animation.
        /// </summary>
        #endregion
        public bool JustChangedFrame
        {
            get { return mCenter.JustChangedFrame; }
        }

        #region XML Docs
        /// <summary>
        /// Gets whether the current AnimationChain just cycled (looped) this frame due to animation.
        /// </summary>
        #endregion
        public bool JustCycled
        {
            get { return mCenter.JustCycled; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the current AnimationFrame's relative position values (RelativeX and RelativeY) are applied
        /// when animating.
        /// </summary>
        #endregion
        public bool UseAnimationRelativePosition
        {
            get { return mCenter.UseAnimationRelativePosition; }
            set { mCenter.UseAnimationRelativePosition = value; }
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// The Layer that this SpriteFrame belongs to.
        /// </summary>
        #endregion
        public Layer LayerBelongingTo
        {
            get
            {
                return mLayerBelongingTo;
            }
            // At one time contained a set as
            // well as a get but this caused problems.
            // The setter required adding contained Sprites
            // to the argument layer.  This caused a double-add
            // if the user set the layer then called AddSpriteFrame.
            // Also, to be consistent with other code the SpriteManager
            // now has an AddToLayer method that adds the SpriteFrame to
            // a layer.
        }

        #region XML Docs
        /// <summary>
        /// The width of the border in texture coordinates.
        /// </summary>
        /// <remarks>
        /// This defines the section of the texture that should not stretch.  Increasing this value will
        /// show more of the texture on the outside border Sprites and less on the inside.
        /// </remarks>
        #endregion
        public float TextureBorderWidth
        {
            get{ return mTextureBorderWidth; }
            set 
			{ 
				mTextureBorderWidth = value; 
				UpdateTextureCoords();
				UpdateSpriteBorderWidthAccordingToPixelSize();
			}
        }

        #region XML Docs
        /// <summary>
        /// The width of the border Sprites in world coordinates.
        /// </summary>
        #endregion
        public float SpriteBorderWidth
        {
            get{ return mSpriteBorderWidth;  }
            set { mSpriteBorderWidth = value; UpdateSpriteScales(); }
        }

        #region XML Docs
        /// <summary>
        /// The borders that the SpriteFrame uses to display.
        /// </summary>
        #endregion
        public BorderSides Borders
        {
            get
            {
                BorderSides valueToReturn = BorderSides.None;

                if (mTop != null) valueToReturn = valueToReturn | BorderSides.Top;
                if (mBottom != null) valueToReturn = valueToReturn | BorderSides.Bottom;
                if (mLeft != null) valueToReturn = valueToReturn | BorderSides.Left;
                if (mRight != null) valueToReturn = valueToReturn | BorderSides.Right;

                return valueToReturn;
            }
            set
            {
                if (value != Borders)
                {
                    RefreshBorders(value);
                    if (SpriteManager.SpriteFrames.Contains(this))
                    {
                        // Readding the SpriteFrame will readd its children
                        // component Sprites
                        SpriteManager.AddSpriteFrame(this);
                    }
                }
            }
        }

        #region Scale

        #region XML Docs
        /// <summary>
        /// The X size of the object.  Measured as the distance from the center of the SpriteFrame 
        /// to its left and right edges in world coordinates.
        /// </summary>
        #endregion
        public float ScaleX
        {
            get { return mScaleX; }
            set 
            { 
                mScaleX = value; 
                UpdateSpriteScales();
                UpdateTextureCoords(); 
            }
        }

        #region XML Docs
        /// <summary>
        /// The Y size of the SpriteFrame.  Measured as the distance from the center of the SpriteFrame 
        /// to its top or bottom edges in world coordinates.
        /// </summary>
        #endregion
        public float ScaleY
        {
            get { return mScaleY; }
            set 
            { 
                mScaleY = value; 
                UpdateSpriteScales();

                UpdateTextureCoords(); 
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the ScaleX property in units per second.  Default 0.
        /// </summary>
        #endregion
        public float ScaleXVelocity
        {
            get { return mScaleXVelocity; }
            set { mScaleXVelocity = value; }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the ScaleY property in units per second.  Default 0.
        /// </summary>
        #endregion
        public float ScaleYVelocity
        {
            get { return mScaleYVelocity; }
            set { mScaleYVelocity = value; }
        }

        public float Left
        {
            get { return Position.X - mScaleX; }
            set { Position.X = value + mScaleX; }
        }

        public float Right
        {
            get { return Position.X + mScaleX; }
            set { Position.X = value - mScaleX; }
        }

        public float Top
        {
            get { return Position.Y + mScaleY; }
            set { Position.Y = value - mScaleY; }
        }

        public float Bottom
        {
            get { return Position.Y - mScaleY; }
            set { Position.Y = value + mScaleY; }
        }


        public float RelativeLeft
        {
            get { return RelativePosition.X - mScaleX; }
            set { RelativePosition.X = value + mScaleX; }
        }

        public float RelativeRight
        {
            get { return RelativePosition.X + mScaleX; }
            set { RelativePosition.X = value - mScaleX; }
        }

        public float RelativeTop
        {
            get { return RelativePosition.Y + mScaleY; }
            set { RelativePosition.Y = value - mScaleY; }
        }

        public float RelativeBottom
        {
            get { return RelativePosition.Y - mScaleY; }
            set { RelativePosition.Y = value + mScaleY; }
        }





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
        /// Whether the SpriteFrame is drawn.
        /// </summary>
        #endregion
        public virtual bool Visible
        {
            get { return mCenter.Visible; }
            set 
            { 
                if(mCenter != null)
                    mCenter.Visible = value;
                if (mTop != null)
                    mTop.Visible = value;
                if (mBottom != null)
                    mBottom.Visible = value;
                if (mLeft != null)
                    mLeft.Visible = value;
                if (mRight != null)
                    mRight.Visible = value;
                if (mTopLeft != null)
                    mTopLeft.Visible = value;
                if (mTopRight != null)
                    mTopRight.Visible = value;
                if (mBottomLeft != null)
                    mBottomLeft.Visible = value;
                if (mBottomRight != null)
                    mBottomRight.Visible = value;
          
            }
        }

        #region XML Docs
        /// <summary>
        /// The texture to be displayed by the SpriteFrame.
        /// </summary>
        #endregion
        public Texture2D Texture
        {
            get { return mCenter.Texture; }
            set
            {
                mCenter.Texture = value;
                if (mTop != null)
                    mTop.Texture = value;
                if (mBottom != null)
                    mBottom.Texture = value;
                if (mLeft != null)
                    mLeft.Texture = value;
                if (mRight != null)
                    mRight.Texture = value;
                if (mTopLeft != null)
                    mTopLeft.Texture = value;
                if (mTopRight != null)
                    mTopRight.Texture = value;
                if (mBottomLeft != null)
                    mBottomLeft.Texture = value;
                if (mBottomRight != null)
                    mBottomRight.Texture = value;

				UpdateSpriteBorderWidthAccordingToPixelSize();
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether the instance can be selected by the Cursor.
        /// </summary>
        #endregion
        public bool CursorSelectable
        {
            get { return mCenter.CursorSelectable; }
            set { mCenter.CursorSelectable = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the SpriteFrame's center Sprite.
        /// </summary>
        /// <remarks>
        /// This can be used to modify the SpriteFrame's appearance.
        /// </remarks>
        #endregion
        public Sprite CenterSprite
        {
            // Vic Asks:  (June 28, 2008) Do we still need this now that the SpriteFrame is a PositionedObject?
            // Any extra information that we'll need can be added as properties at a later time.
            get { return mCenter; }
        }

		public float PixelSize
		{
			get{ return mPixelSize;}
			set
			{
				mPixelSize = value;

				UpdateSpriteBorderWidthAccordingToPixelSize();
			}
		}

        public IEnumerable<Sprite> AllSprites
        {
            get
            {
                if (mCenter != null)
                {
                    yield return mCenter;
                }
                if (mTopLeft != null)
                {
                    yield return mTopLeft;
                }
                if (mTop != null)
                {
                    yield return mTop;
                }
                if (mTopRight != null)
                {
                    yield return mTopRight;
                }

                if (mRight != null)
                {
                    yield return mRight;
                }
                if (mBottomRight != null)
                {
                    yield return mBottomRight;
                }
                if (mBottom != null)
                {
                    yield return mBottom;
                }
                if (mBottomLeft != null)
                {
                    yield return mBottomLeft;
                }
                if (mLeft != null)
                {
                    yield return mLeft;
                }
            }
        }
        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Constructs a new, empty SpriteFrame.
        /// </summary>
        #endregion
        public SpriteFrame() : this(null, BorderSides.All)
        {

        }

        #region XML Docs
        /// <summary>
        /// Constructs a new SpriteFrame using the argument texture and border sides.
        /// </summary>
        /// <remarks>
        /// SpriteFrames are usually created through the SpriteManager's AddSpriteFrame method.
        /// <seealso cref="FlatRedBall.SpriteManager.AddSpriteFrame(Texture2D, BorderSides)"/>
        /// </remarks>
        /// <param name="textureToUse">The texture that the SpriteFrame will display.</param>
        /// <param name="borders">Which sides should be used by the SpriteFrame.</param>
        #endregion
        public SpriteFrame(Texture2D textureToUse,
            BorderSides borders)
        {
            Initialize(textureToUse, borders);
        }


        #region XML Docs
        /// <summary>
        /// Constructs a new SpriteFrame using the argument texture, border sides, and Layer.
        /// </summary>
        /// <param name="textureToUse">The texture that the SpriteFrame will display.</param>
        /// <param name="borders">Which sides should be used by the SpriteFrame.</param>
        /// <param name="layerToAddTo">The Layer that the SpriteFrame will be drawn on.</param>
        #endregion
        public SpriteFrame(Texture2D textureToUse,
            BorderSides borders, Layer layerToAddTo)
        {
            Initialize(textureToUse, borders);

            this.mLayerBelongingTo = layerToAddTo;
        }

        private void Initialize(Texture2D textureToUse, BorderSides borders)
        {
            mTextureBorderWidth = 1 / 8.0f;
            mSpriteBorderWidth = 1 / 8.0f;

            mScaleX = 1;
            mScaleY = 1;

            CreateBorderSprites(textureToUse, borders);

            this.Borders = borders;

            UpdateSpriteScales();
            UpdateTextureCoords();

            Texture = textureToUse;
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Creates a copy of the SpriteFrame instance.
        /// </summary>
        /// <remarks>
        /// The cloned SpriteFrame will not belong to any of the lists that the original does.
        /// Since it will not be added to the SpriteManager it will not be drawn and managed.
        /// To add the SpriteFrame to the engine, call SpriteManager.AddSpriteFrame passing
        /// the newly created SpriteFrame as the argument.
        /// <seealso cref="FlatRedBall.SpriteManager.AddSpriteFrame(SpriteFrame)"/>
        /// </remarks>
        /// <returns>The newly-created SpriteFrame.</returns>
        #endregion
        public SpriteFrame Clone()
        {
            SpriteFrame spriteFrame = (SpriteFrame)MemberwiseClone();

            spriteFrame.mListsBelongingTo = new List<IAttachableRemovable>();
            spriteFrame.mChildren = new AttachableList<PositionedObject>();

            spriteFrame.mCenter = mCenter.Clone();
            spriteFrame.mCenter.AttachTo(spriteFrame, false);

            if (mTopLeft != null)
            {
                spriteFrame.mTopLeft = mTopLeft.Clone();
                spriteFrame.mTopLeft.AttachTo(spriteFrame.mCenter, false);
            }

            if (mTop != null)
            {
                spriteFrame.mTop = mTop.Clone();
                spriteFrame.mTop.AttachTo(spriteFrame.mCenter, false);
            }

            if (mTopRight != null)
            {
                spriteFrame.mTopRight = mTopRight.Clone();
                spriteFrame.mTopRight.AttachTo(spriteFrame.mCenter, false);
            }

            if (mRight != null)
            {
                spriteFrame.mRight = mRight.Clone();
                spriteFrame.mRight.AttachTo(spriteFrame.mCenter, false);
            }
            if (mBottomRight != null)
            {
                spriteFrame.mBottomRight = mBottomRight.Clone();
                spriteFrame.mBottomRight.AttachTo(spriteFrame.mCenter, false);
            }
            if (mBottom != null)
            {
                spriteFrame.mBottom = mBottom.Clone();
                spriteFrame.mBottom.AttachTo(spriteFrame.mCenter, false);
            }

            if (mBottomLeft != null)
            {
                spriteFrame.mBottomLeft = mBottomLeft.Clone();
                spriteFrame.mBottomLeft.AttachTo(spriteFrame.mCenter, false);
            }
            if (mLeft != null)
            {
                spriteFrame.mLeft = mLeft.Clone();
                spriteFrame.mLeft.AttachTo(spriteFrame.mCenter, false);
            }





            spriteFrame.mLayerBelongingTo = null;

            // just to refresh
            spriteFrame.Alpha = spriteFrame.Alpha;
            spriteFrame.AlphaRate = spriteFrame.AlphaRate;
            spriteFrame.Red = spriteFrame.Red;
            spriteFrame.Green = spriteFrame.Green;
            spriteFrame.Blue = spriteFrame.Blue;

            spriteFrame.RedRate = spriteFrame.RedRate;
            spriteFrame.GreenRate = spriteFrame.GreenRate;
            spriteFrame.BlueRate = spriteFrame.BlueRate;

            spriteFrame.ColorOperation = spriteFrame.ColorOperation;
            spriteFrame.BlendOperation = spriteFrame.BlendOperation;

            spriteFrame.mParent = null;

            spriteFrame.mInstructions = new FlatRedBall.Instructions.InstructionList();

            spriteFrame.UpdateSpriteScales();

            return spriteFrame;
        }

        #region XML Docs
        /// <summary>
        /// Returns whether a ray starting at the argument Camera's position and travelling through the
        /// point relative to the Camera specified by the arguments intersects with this instance.
        /// </summary>
        /// <remarks>
        /// This method does not take the camera's rotation into consideration when calculating the ray.
        /// </remarks>
        /// <param name="cameraRelativeX">The X position relative to the argument Camera.</param>
        /// <param name="cameraRelativeY">The Y position relative to the argument Camera.</param>
        /// <param name="cameraRelativeZ">The Z position relative to the argument Camera.</param>
        /// <param name="camera">The camera to use for the intersection test.</param>
        /// <returns>Whether the ray intersects the SpriteFrame.</returns>
        #endregion
        public bool DoesCameraRelativeRayIntersect2D(float cameraRelativeX, float cameraRelativeY, float cameraRelativeZ, Camera camera)
        {
            // Vic Says:  This method only considers the Z rotation of the SpriteFrame.  Should it perhaps also
            // consider X and Y rotation as well?

            float cursorX = 0;
            float cursorY = 0;


            if (cameraRelativeZ < camera.NearClipPlane ||
                cameraRelativeZ > camera.FarClipPlane)
                return false;

            if (camera.Orthogonal == false)
            {
                cursorX = (camera.X) + (cameraRelativeZ / 100.0f) * (cameraRelativeX);
                cursorY = (camera.Y) + (cameraRelativeZ / 100.0f) * (cameraRelativeY);

            }
            else
            {
                // orthoWidth measures left to right side of the screen, but we want orthoScl
                cursorX = camera.X + camera.OrthogonalWidth * (cameraRelativeX) / (2 * camera.XEdge);
                cursorY = camera.Y + camera.OrthogonalHeight * (cameraRelativeY) / (2 * camera.YEdge);
            }


            if (RotationZ != 0.0f)
            {
                MathFunctions.RotatePointAroundPoint(X, Y, ref cursorX, ref cursorY, -RotationZ);
            }


            return cursorX < X + ScaleX &&
                        cursorX > X - ScaleX &&
                        cursorY < Y + ScaleY &&
                        cursorY > Y - ScaleY;

        }

        #region XML Docs
        /// <summary>
        /// Returns whether the argument Sprite is a Sprite used by this instance.
        /// </summary>
        /// <param name="sprite">The Sprite to check.</param>
        /// <returns>Whether the argument Sprite is a component of this.</returns>
        #endregion
        public bool IsSpriteComponentOfThis(Sprite sprite)
        {
            if (sprite != null)
            {
                return sprite == mCenter ||
                    sprite == mTopLeft ||
                    sprite == mTop ||
                    sprite == mTopRight ||
                    sprite == mRight ||
                    sprite == mBottomRight ||
                    sprite == mBottom ||
                    sprite == mBottomLeft ||
                    sprite == mLeft;
            }
            else
            {
                return false;
            }
        }

        #region XML Docs
        /// <summary>
        /// Stops all automatic behavior and stores the necessary instructions to 
        /// resume activity in the argument InstructionList.
        /// </summary>
        /// <param name="instructions">The List to store instructions which are executed to
        /// resume activity.</param>
        #endregion
        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.SpriteFrameUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.SpriteFrameUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        #region XML Docs
        /// <summary>
        /// Returns a string with information about this instance.
        /// </summary>
        /// <returns>The string containing information about this instance.</returns>
        #endregion
        public override string ToString()
        {
            return "Name: " + this.Name + "\n" +
                   " Number of Lists belonging to:" + base.mListsBelongingTo.Count +
                   "\nX: " + X;
        }

        #region XML Docs
        /// <summary>
        /// Sets the contained DynamicSpriteFrame to help identify SpriteFrame membership and side.
        /// </summary>
        /// <remarks>
        /// If the SpriteFrame's name is "spriteFrame1", each side will have its name be
        /// the SpriteFrame's name with a suffix indicating the side that the Sprite represents.
        /// That is, the center Sprite would be named "spriteFrame1_center", the top
        /// would be "spriteFrame1_top", and so on.
        /// 
        /// <para>This method can be used in debugging to help identify whether DynamicSprites belong
        /// to a SpriteFrame, and if so, which side they represent.  Otherwise, this
        /// method has no engine functionality.</para>
        /// </remarks>
        #endregion
        public void UpdateInternalSpriteNames()
        {
            if (mCenter != null)
                mCenter.Name = Name + "_center";
            if (mTop != null)
                mTop.Name = Name + "_top";
            if (mTopLeft != null)
                mTopLeft.Name = Name + "_topLeft";
            if (mTopRight != null)
                mTopRight.Name = Name + "_topRight";
            if (mBottom != null)
                mBottom.Name = Name + "_bottom";
            if (mBottomLeft != null)
                mBottomLeft.Name = Name + "_bottomLeft";
            if (mBottomRight != null)
                mBottomRight.Name = Name + "_bottomRight";
            if (mLeft != null)
                mLeft.Name = Name + "_left";
            if (mRight != null)
                mRight.Name = Name + "_right";

            mChildren.Name = "SpriteFrame " + Name + "'s Children";
        }

        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);



            // We can make this faster by not calling these functions:
            if (mTopLeft != null)
            {
                mTopLeft.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mTopLeft);
            }

            if (mTop != null)
            {
                mTop.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mTop);
            }

            if (mTopRight != null)
            {
                mTopRight.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mTopRight);
            }

            if (mRight != null)
            {
                mRight.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mRight);
            }

            if (mBottomRight != null)
            {
                mBottomRight.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mBottomRight);
            }

            if (mBottom != null)
            {
                mBottom.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mBottom);
            }

            if (mBottomLeft != null)
            {
                mBottomLeft.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mBottomLeft);
            }

            if (mLeft != null)
            {
                mLeft.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mLeft);
            }

            if (mCenter != null)
            {
                mCenter.UpdateDependencies(currentTime);
                SpriteManager.ManualUpdate(mCenter);
            }




        }

        #region IMouseOver
        bool IMouseOver.IsMouseOver(FlatRedBall.Gui.Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(FlatRedBall.Gui.Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion

        #endregion

        #region Internal Methods

        internal void RefreshAttachments()
        {
            if (mCenter != null && mCenter.Parent != this)
            {
                mCenter.AttachTo(this, false);
            }
            if (mTopLeft != null && mTopLeft.Parent != this)
            {
                mTopLeft.AttachTo(this, false);
            }
            if (mTop != null && mTop.Parent != this)
            {
                mTop.AttachTo(this, false);
            }
            if (mTopRight != null && mTopRight.Parent != this)
            {
                mTopRight.AttachTo(this, false);
            }
            if (mRight != null && mRight.Parent != this)
            {
                mRight.AttachTo(this, false);
            }
            if (mBottomRight != null && mBottomRight.Parent != this)
            {
                mBottomRight.AttachTo(this, false);
            }
            if (mBottom != null && mBottom.Parent != this)
            {
                mBottom.AttachTo(this, false);
            }
            if (mBottomLeft != null && mBottomLeft.Parent != this)
            {
                mBottomLeft.AttachTo(this, false);
            }
            if (mLeft != null && mLeft.Parent != this)
            {
                mLeft.AttachTo(this, false);
            }
        }

        #region XML Docs
        /// <summary>
        /// Performs the necessary every-frame management of the SpriteFrame.  This
        /// method is automatically called by the SpriteManager.
        /// </summary>
        #endregion
        internal void Manage()
        {

            mScaleX += TimeManager.SecondDifference * mScaleXVelocity;
            mScaleY += TimeManager.SecondDifference * mScaleYVelocity;

            if (mScaleX != mScaleXAfterManage ||
                mScaleY != mScaleYAfterManage)
            {
                mScaleXAfterManage = mScaleX;
                mScaleYAfterManage = mScaleY;

                this.UpdateSpriteScales();
            }

            // since the mCenter Sprite may be animated, update the texture using the 
            // base Sprite's texture.
            // If nothing's changed, this won't affect anything.
            if (mCenter.JustChangedFrame)
            {
                Texture = mCenter.Texture;
            }
        }

        #endregion

        #region Private Methods

        private void CreateBorderSprites(Texture2D textureToUse, BorderSides borders)
        {
            mCenter = new Sprite();
            mCenter.Texture = textureToUse;
            mCenter.AttachTo(this, false);

            if ((borders & BorderSides.Top) == BorderSides.Top)
            {
                mTop = new Sprite();
                mTop.Texture = textureToUse;
                mTop.AttachTo(mCenter, false);

                if ((borders & BorderSides.Left) == BorderSides.Left)
                {
                    mTopLeft = new Sprite();
                    mTopLeft.Texture = textureToUse;
                    mTopLeft.AttachTo(mCenter, false);
                }

                if ((borders & BorderSides.Right) == BorderSides.Right)
                {
                    mTopRight = new Sprite();
                    mTopRight.Texture = textureToUse;
                    mTopRight.AttachTo(mCenter, false);
                }
            }
            if ((borders & BorderSides.Bottom) == BorderSides.Bottom)
            {
                mBottom = new Sprite();
                mBottom.Texture = textureToUse;
                mBottom.AttachTo(mCenter, false);

                if ((borders & BorderSides.Left) == BorderSides.Left)
                {
                    mBottomLeft = new Sprite();
                    mBottomLeft.Texture = textureToUse;
                    mBottomLeft.AttachTo(mCenter, false);
                }

                if ((borders & BorderSides.Right) == BorderSides.Right)
                {
                    mBottomRight = new Sprite();
                    mBottomRight.Texture = textureToUse;
                    mBottomRight.AttachTo(mCenter, false);
                }
            }
            if ((borders & BorderSides.Left) == BorderSides.Left)
            {
                mLeft = new Sprite();
                mLeft.Texture = textureToUse;
                mLeft.AttachTo(mCenter, false);
            }

            if ((borders & BorderSides.Right) == BorderSides.Right)
            {
                mRight = new Sprite();
                mRight.Texture = textureToUse;
                mRight.AttachTo(mCenter, false);
            }        

            UpdateInternalSpriteNames();
        }

        #region XML Docs
        /// <summary>
        /// Updates the SpriteFrame borders.  This method is called automatically
        /// whenever the Borders property is changed.
        /// </summary>
        /// <param name="borderSides">The new BorderSides to use.</param>
        #endregion
        private void RefreshBorders(BorderSides borderSides)
        {
            Texture2D texture = mCenter.Texture;

            SpriteManager.RemoveSprite(mCenter);

            SpriteManager.RemoveSprite(mTop);
            SpriteManager.RemoveSprite(mBottom);
            SpriteManager.RemoveSprite(mLeft);
            SpriteManager.RemoveSprite(mRight);

            SpriteManager.RemoveSprite(mTopLeft);
            SpriteManager.RemoveSprite(mTopRight);
            SpriteManager.RemoveSprite(mBottomLeft);
            SpriteManager.RemoveSprite(mBottomRight);

            mCenter = null;

            mTop = null;
            mBottom = null;
            mLeft = null;
            mRight = null;

            mTopLeft = null;
            mTopRight = null;
            mBottomLeft = null;
            mBottomRight = null;

            this.CreateBorderSprites(texture, borderSides);

            this.UpdateSpriteScales();
            this.UpdateTextureCoords();
        }


		private void UpdateSpriteBorderWidthAccordingToPixelSize()
		{
			if (Texture != null && mPixelSize > 0)
			{
				bool hasTopOrBottom = (Borders & BorderSides.Top) == BorderSides.Top ||
					(Borders & BorderSides.Bottom) == BorderSides.Bottom;

				bool hasLeftOrRight = (Borders & BorderSides.Left) == BorderSides.Left ||
					(Borders & BorderSides.Right) == BorderSides.Right;

				// We want to use width or height
				// appropriately.  Height is dominant
				// if top or bottom are around.
				if (hasTopOrBottom)
				{
					this.SpriteBorderWidth = 2 * TextureBorderWidth * Texture.Height * mPixelSize;
				}
				else
				{
					this.SpriteBorderWidth = 2 * TextureBorderWidth * Texture.Width * mPixelSize;
				}

				if (!hasTopOrBottom)
				{
					ScaleY = Texture.Height * mPixelSize;
				}
				if (!hasLeftOrRight)
				{
					ScaleX = Texture.Width * mPixelSize;
				}
			}
		}


        private void UpdateSpriteScales()
        {
            float centerSclX = mScaleX;
            float centerSclY = mScaleY;

            mCenter.RelativeX = 0;
            mCenter.RelativeY = 0;

            if (mTop != null)
            {
                mCenter.RelativeY -= SpriteBorderWidth / 2.0f;
                centerSclY -= SpriteBorderWidth / 2.0f;
            }
            if (mBottom != null)
            {
                mCenter.RelativeY += SpriteBorderWidth / 2.0f;

                centerSclY -= SpriteBorderWidth / 2.0f;
            }
            if (mLeft != null)
            {
                mCenter.RelativeX += SpriteBorderWidth / 2.0f;
                centerSclX -= SpriteBorderWidth / 2.0f;

            }
            if (mRight != null)
            {
                mCenter.RelativeX -= SpriteBorderWidth / 2.0f;
                centerSclX -= SpriteBorderWidth / 2.0f;
            }

            // If the center is too small, just treat it as if it's not there
            mCenter.ScaleX = System.Math.Max(0, centerSclX);
            mCenter.ScaleY = System.Math.Max(0, centerSclY);

            float amountToChopY = 0;
            float ratioToChopY = 0;
            if (this.ScaleY < SpriteBorderWidth)
            {
                amountToChopY = SpriteBorderWidth - ScaleY;
                ratioToChopY = amountToChopY / SpriteBorderWidth;
            }
            if (mTop != null)
            {
                mTop.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;
                mTop.ScaleX = mCenter.ScaleX;
                mTop.RelativeY = mCenter.RelativeY + mCenter.ScaleY + (SpriteBorderWidth - amountToChopY) / 2.0f;
            }
            if (mBottom != null)
            {
                mBottom.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;
                mBottom.ScaleX = mCenter.ScaleX;
                mBottom.RelativeY = mCenter.RelativeY - (mCenter.ScaleY + (SpriteBorderWidth - amountToChopY) / 2.0f);
            }


            float amountToChopX = 0;
            float ratioToChopX = 0;
            if (this.ScaleX < SpriteBorderWidth)
            {
                amountToChopX = SpriteBorderWidth - ScaleX;
                ratioToChopX = amountToChopX / SpriteBorderWidth;
            }
            if (mLeft != null)
            {
                mLeft.ScaleX = (SpriteBorderWidth - amountToChopX)  / 2.0f;
                
                mLeft.ScaleY = mCenter.ScaleY;
                mLeft.RelativeX = mCenter.RelativeX -(mCenter.ScaleX + (SpriteBorderWidth - amountToChopX) / 2.0f);
            }
            if (mRight != null)
            {
                mRight.ScaleX = (SpriteBorderWidth - amountToChopX) / 2.0f;
                mRight.ScaleY = mCenter.ScaleY;
                mRight.RelativeX = mCenter.RelativeX + mCenter.ScaleX + (SpriteBorderWidth - amountToChopX) / 2.0f;

            }

            if (mTopLeft != null)
            {
                mTopLeft.ScaleX = (SpriteBorderWidth - amountToChopX) / 2.0f;
                mTopLeft.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;

                mTopLeft.RelativeX = mLeft.RelativeX;
                mTopLeft.RelativeY = mTop.RelativeY;
            }
            if (mTopRight != null)
            {
                mTopRight.ScaleX = (SpriteBorderWidth - amountToChopX) / 2.0f;
                mTopRight.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;

                mTopRight.RelativeX = mRight.RelativeX;
                mTopRight.RelativeY = mTop.RelativeY;
            }
            if (mBottomLeft != null)
            {
                mBottomLeft.ScaleX = (SpriteBorderWidth - amountToChopX) / 2.0f;
                mBottomLeft.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;

                mBottomLeft.RelativeX = mLeft.RelativeX;
                mBottomLeft.RelativeY = mBottom.RelativeY;
            }
            if (mBottomRight != null)
            {
                mBottomRight.ScaleX = (SpriteBorderWidth - amountToChopX) / 2.0f;
                mBottomRight.ScaleY = (SpriteBorderWidth - amountToChopY) / 2.0f;

                mBottomRight.RelativeX = mRight.RelativeX ;
                mBottomRight.RelativeY = mBottom.RelativeY;
            }

 //           center.ScaleX -= .1f;
   //         center.ScaleY -= .1f;

        }
        

        private void UpdateTextureCoords()
        {
            float amountToChopX = 0;
            float ratioToShowX = 1;
            if (this.ScaleX < SpriteBorderWidth)
            {
                amountToChopX = SpriteBorderWidth - ScaleX;

                ratioToShowX = 1 - (amountToChopX / SpriteBorderWidth);
            }

            float amountToChopY = 0;
            float ratioToShowY = 1;
            if (this.ScaleY < SpriteBorderWidth)
            {
                amountToChopY = SpriteBorderWidth - ScaleY;

                ratioToShowY = 1 - (amountToChopY / SpriteBorderWidth);
            }



            if (mTop != null)
            {
                mCenter.Vertices[0].TextureCoordinate.Y = this.TextureBorderWidth;
                mCenter.Vertices[1].TextureCoordinate.Y = this.TextureBorderWidth;

                mTop.Vertices[2].TextureCoordinate.Y = this.TextureBorderWidth * ratioToShowY;
                mTop.Vertices[3].TextureCoordinate.Y = this.TextureBorderWidth * ratioToShowY;

            }

            if (mBottom != null)
            {
                mCenter.Vertices[2].TextureCoordinate.Y = 1-this.TextureBorderWidth;
                mCenter.Vertices[3].TextureCoordinate.Y = 1-this.TextureBorderWidth;

                mBottom.Vertices[0].TextureCoordinate.Y = 1 - this.TextureBorderWidth * ratioToShowY;
                mBottom.Vertices[1].TextureCoordinate.Y = 1 - this.TextureBorderWidth * ratioToShowY;


            }

            if (mLeft != null)
            {
                mCenter.Vertices[0].TextureCoordinate.X = TextureBorderWidth;
                mCenter.Vertices[3].TextureCoordinate.X = TextureBorderWidth;

                mLeft.Vertices[1].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;
                mLeft.Vertices[2].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;

                if (mTop != null)
                {
                    mTopLeft.Vertices[1].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;
                    mTopLeft.Vertices[2].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;
                    mTopLeft.Vertices[2].TextureCoordinate.Y = TextureBorderWidth * ratioToShowY;
                    mTopLeft.Vertices[3].TextureCoordinate.Y = TextureBorderWidth * ratioToShowY;

                    mTop.Vertices[0].TextureCoordinate.X = TextureBorderWidth;
                    mTop.Vertices[3].TextureCoordinate.X = TextureBorderWidth;

                    mLeft.Vertices[0].TextureCoordinate.Y = TextureBorderWidth;
                    mLeft.Vertices[1].TextureCoordinate.Y = TextureBorderWidth;
                }
                if (mBottom != null)
                {
                    mBottomLeft.Vertices[0].TextureCoordinate.Y = 1 - TextureBorderWidth * ratioToShowY;
                    mBottomLeft.Vertices[1].TextureCoordinate.Y = 1 - TextureBorderWidth * ratioToShowY;
                    mBottomLeft.Vertices[1].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;
                    mBottomLeft.Vertices[2].TextureCoordinate.X = TextureBorderWidth * ratioToShowX;

                    mBottom.Vertices[0].TextureCoordinate.X = TextureBorderWidth;
                    mBottom.Vertices[3].TextureCoordinate.X = TextureBorderWidth;

                    mLeft.Vertices[2].TextureCoordinate.Y = 1 - TextureBorderWidth;
                    mLeft.Vertices[3].TextureCoordinate.Y = 1 - TextureBorderWidth;
                }
            }

            if (mRight != null)
            {
                mCenter.Vertices[1].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowY;
                mCenter.Vertices[2].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowY;

                mRight.Vertices[0].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;
                mRight.Vertices[3].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;

                if (mTop != null)
                {
                    mTopRight.Vertices[0].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;
                    mTopRight.Vertices[3].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;
                    mTopRight.Vertices[3].TextureCoordinate.Y = TextureBorderWidth * ratioToShowY;
                    mTopRight.Vertices[2].TextureCoordinate.Y = TextureBorderWidth * ratioToShowY;

                    mTop.Vertices[1].TextureCoordinate.X = 1-TextureBorderWidth;
                    mTop.Vertices[2].TextureCoordinate.X = 1-TextureBorderWidth;

                    mRight.Vertices[0].TextureCoordinate.Y = TextureBorderWidth;
                    mRight.Vertices[1].TextureCoordinate.Y = TextureBorderWidth;
                }
                if (mBottom != null)
                {
                    mBottomRight.Vertices[0].TextureCoordinate.Y = 1 - TextureBorderWidth * ratioToShowY;
                    mBottomRight.Vertices[1].TextureCoordinate.Y = 1 - TextureBorderWidth * ratioToShowY;
                    mBottomRight.Vertices[0].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;
                    mBottomRight.Vertices[3].TextureCoordinate.X = 1 - TextureBorderWidth * ratioToShowX;

                    mBottom.Vertices[1].TextureCoordinate.X = 1-TextureBorderWidth;
                    mBottom.Vertices[2].TextureCoordinate.X = 1-TextureBorderWidth;

                    mRight.Vertices[2].TextureCoordinate.Y = 1 - TextureBorderWidth;
                    mRight.Vertices[3].TextureCoordinate.Y = 1 - TextureBorderWidth;
                }
            }   

        }
        
        #endregion

        #endregion


        #region IEquatable<SpriteFrame> Members

        bool IEquatable<SpriteFrame>.Equals(SpriteFrame other)
        {
            return this == other;
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
