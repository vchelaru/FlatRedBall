#if FRB_MDX || XNA3
#define SUPPORTS_FRB_DRAWN_GUI
#endif

using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Texture2D = FlatRedBall.Texture2D;

using GraphicsDevice = Microsoft.DirectX.Direct3D.Device;
#elif FRB_XNA || SILVERLIGHT
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Microsoft.Xna.Framework.Graphics;
using Ray = Microsoft.Xna.Framework.Ray;
#endif

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#endif

using FlatRedBall.Input;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Animation;
using System.Collections.ObjectModel;
using FlatRedBall.Utilities;
using FlatRedBall.Math;
//using Microsoft.Xna.Framework;


namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
	/// This is the base class for most Gui elements.
	/// </summary>
    /// <remarks>
    /// The WindowClass contains common properties, events, and methods
    /// used by other Gui elements.  Much like most FRB objects, Windows and
    /// window sub-classes should be created through the GuiManager.
    /// 
    /// <para>
    /// Windows can be created and drawn two different ways.  One is the
    /// default Gui.  The default is drawn using the guiTex.png graphic.
    /// The appearance of the default Gui is similar to a regular Windows
    /// Gui.  Most default-drawn Gui elements must be added through a Window
    /// instance rather than the GuiManager.  It is common practice to create
    /// a Window through the GuiManager, then to add the children Gui elements
    /// through the newly-created Window.
    /// </para>
    /// 
    /// <para>To chnage the appearance of the Gui, most Gui elements can
    /// be constructed using SpriteFrames.  SpriteFrame-created GuiElements
    /// can be created either through the GuiManager or a Window instance.</para>
    /// <seealso cref="FlatRedBall.Gui.GuiManager.AddWindow()"/>
    /// <seealso cref="FlatRedBall.ManagedSpriteGroups.SpriteFrame"/>
    /// </remarks>
    #endregion

    public class Window
        
#if !SILVERLIGHT
        : IScalable, IAnimationChainAnimatable, INameable, IWindow, IVisible
#else
        :IWindow, IVisible
#endif
    {
        #region Fields

        #region const fields (used for rendering)

        public const float BorderWidth = .4f;

        const float horizontalLeft = 15/256f;
        const float horizontalBottom = 174.95f/256f;

        const float cornerLeft = 15/256f;
        const float cornerBottom = 178/256f;

        const float horizontalPixelWidth = 4/256f;
        const float horizontalUnitWidth = 4;

        const float cornerPixelWidth = 4/256f;
        const float cornerUnitWidth = 4;

        const float centerLeft = 28.1f/256f;
        const float centerRight = 28.9f/256f;
        const float centerTop = .63719f;
        const float centerBottom = .6953125f;

        #endregion

        #region Static Fields

        #region XML Docs
        /// <summary>
        /// Whether Windows should be repositioned to keep their move bar
        /// accessible to the mouse.  Default true.
        /// </summary>
        #endregion
        public static bool KeepWindowsInScreen = true;

        #region XML Docs
        /// <summary>
        /// The height in world units of the move bar.
        /// </summary>
        #endregion
        public const float MoveBarHeight = 2.8f;

        static int NinePieceNumberOfVertices = 54;
        static int OnePieceNumberOfVertices = 6;

        #endregion

        #region Scale
        #region IScalable fields

        #region XML Docs
        /// <summary>
        /// The X size of the Window.
        /// </summary>
        #endregion
        protected float mScaleX = 1;

        #region XML Docs
        /// <summary>
        /// The Y size of the Window.
        /// </summary>
        #endregion
        protected float mScaleY = 1;

        #region XML Docs
        /// <summary>
        /// The rate of change of mScaleX in units per second.
        /// </summary>
        #endregion
        protected float mScaleXVelocity;

        #region XML Docs
        /// <summary>
        /// The rate of change of mScaleY in units per second
        /// </summary>
        #endregion
        protected float mScaleYVelocity;

        #endregion

        bool mResizable;
        float mMinimumScaleX;
        float mMinimumScaleY;

        float mMaximumScaleX;
        float mMaximumScaleY;

        #endregion

        #region Position

        #region XML Docs
        /// <summary>
        /// The x position of the Window in world units.
        /// </summary>
        #endregion
        protected float mWorldUnitX;

        #region XML Docs
        /// <summary>
        /// The y position of the Window in world units.
        /// </summary>
        #endregion
        protected float mWorldUnitY;

        #region XML Docs
        /// <summary>
        /// The x position of the Window in world units relative to its Parent.
        /// </summary>
        #endregion
        protected float mWorldUnitRelativeX;

        #region XML Docs
        /// <summary>
        /// The y position of the Window in world units relative to its Parent.  Although UI normally considers
        /// down to be positive Y, this value is just like Sprites - positive Y is up.
        /// </summary>
        #endregion
        protected float mWorldUnitRelativeY;

        #endregion

        #region Animation

        AnimationChain mCurrentChain;
        //bool mAnimate;

        #endregion

        protected bool mVisible = true;

        #region XML Docs
        /// <summary>
        /// Whether the Window has a move bar which the user can
        /// click and drag over to reposition the Window.  This is usually
        /// only true for regular Windows - objects inheriting from the
        /// Window class almost always have this set to false.
        /// </summary>
        #endregion
        protected bool mMoveBar;

        #region XML Docs
        /// <summary>
        /// The cursor that this Window uses for UI interaction.
        /// </summary>
        #endregion
        internal protected Cursor mCursor;
		
		internal Button mCloseButton;

        #region XML Docs
        /// <summary>
        /// The Window's name.
        /// </summary>
        #endregion
        protected string mName;

        #region XML Docs
        /// <summary>
        /// Windows that belong to and are positioned relative to this Window.
        /// </summary>
        #endregion
        protected WindowArray mChildren = new WindowArray();
        private ReadOnlyCollection<IWindow> mChildrenReadOnly;


        #region XML Docs
        /// <summary>
        /// Windows that belong to this Window but are not positioned relative to it and do not have to 
        /// be physically contained in this Window for the user to interact with them.
        /// </summary>
        #endregion
        protected WindowArray mFloatingWindows = new WindowArray();
        private ReadOnlyCollection<IWindow> mFloatingWindowsReadOnly;
		
        Window mParentWindow;
        Window mFloatingParentWindow;


        #region XML Docs
        /// <summary>
        /// Used to adjust the alpha of the Windows.  Typically, faded Windows
        /// are disabled.  Only the first 8 bytes are changed.  The RGB components
        /// are not touched.  This field is not exposed to the user - it's used internally.
        /// </summary>
        #endregion

#if FRB_MDX
        protected uint mColor = 0xff000000;
#else
        protected Color mColor = new Color(0, 0, 0, 255);
#endif
        private bool mEnabled;

        internal int mNumberOfVertices;

        #region XML Docs
        /// <summary>
        /// Vertices used in the Draw methods.
        /// </summary>
        /// <remarks>
        /// Derived UI elements do not need to use this unless they are manually drawing new types of objects.
        /// Derived UI elements which simply use other base elements will not need to touch this field.
        /// </remarks>
        #endregion
        protected static VertexPositionColorTexture[] StaticVertices = new VertexPositionColorTexture[6];

        Vector2 mTopLeftValues;
        bool mDrawBorders; // see DrawBorders property for explanation

        #region XML Docs
        /// <summary>
        /// The left coordinate of the texture to use when drawing a non-null BaseTexture
        /// </summary>
        #endregion
        protected float mTextureLeft = 0;

        #region XML Docs
        /// <summary>
        /// The right coordinate of the texture to use when drawing a non-null BaseTexture
        /// </summary>
        #endregion
        protected float mTextureRight = 1;

        #region XML Docs
        /// <summary>
        /// The top coordinate of the texture to use when drawing a non-null BaseTexture
        /// </summary>
        #endregion
        protected float mTextureTop = 0;

        #region XML Docs
        /// <summary>
        /// The bottom coordinate of the texture to use when drawing a non-null BaseTexture
        /// </summary>
        #endregion
        protected float mTextureBottom = 1;

        Texture2D mBaseTexture;

        public float TextureLeft
        {
            get { return mTextureLeft; }
            set { mTextureLeft = value; }
        }

        public float TextureRight
        {
            get { return mTextureRight; }
            set { mTextureRight = value; }
        }
        
        public float TextureTop
        {
            get { return mTextureTop; }
            set { mTextureTop = value; }
        }

        public float TextureBottom
        {
            get { return mTextureBottom; }
            set { mTextureBottom = value; }
        }


        #region SpriteFrame fields
        /*
         * If the Gui is drawn by the SpriteManager, it needs to have SpriteFrame references
         */

        bool mGuiManagerDrawn;

        // Vic says:  Before documenting this it probably needs to be made a property.  Before doing that
        // we need some SpriteFrame examples on FlatRedBall.com to introduce SpriteFrame UI.
        public SpriteFrame SpriteFrame
        {
            get;
            set;
        }

        SpriteFrame mMoveBarSpriteFrame;

        GuiSkin mGuiSkin;
        
        #endregion

        #endregion

        #region Properties


        #region IScalable Properties

        #region XML Docs
        /// <summary>
        /// The X Scale of the Window.
        /// </summary>
        /// <remarks>
        /// Changing the ScaleX raises the Resizing event.
        /// <seealso cref="SetScaleTL(float, float)"/>
        /// </remarks>
        #endregion
        public virtual float ScaleX
		{
			get
            {
                if(GuiManagerDrawn)
                {
                    return mScaleX;
                }
                else
                {
                    return SpriteFrame.ScaleX;
                }
            }
			set
			{
                // encase in an if statement so the callOnResize is only called when there's a change
                if (value != mScaleX)
                {

                    mScaleX = System.Math.Max(value, mMinimumScaleX);
                    mScaleX = System.Math.Min(mScaleX, mMaximumScaleX);

                    if (mCloseButton != null)
                        mCloseButton.SetPositionTL(2 * mScaleX - 1.6f, -1.2f);

                    if (SpriteFrame != null)
                    {
                        SpriteFrame.ScaleX = value;
                    }

                    if (mMoveBarSpriteFrame != null)
                    {
                        mMoveBarSpriteFrame.ScaleX = value;
                    }

                    foreach (Window child in mChildren)
                        child.SetPositionTL(child.mTopLeftValues.X, child.mTopLeftValues.Y);

                    OnResize();
                }
			}
        }

        #region XML Docs
        /// <summary>
        /// Yhe Y Scale of the Window.
        /// </summary>
        /// <remarks>
        /// Changing the ScaleY raises the Resizing event.
        /// <seealso cref="SetScaleTL(float, float)"/>
        /// </remarks>
        #endregion
        public virtual float ScaleY
		{
			get
            {
                if (GuiManagerDrawn)
                {
                    return mScaleY;
                }
                else
                {
                    return SpriteFrame.ScaleY;
                }
            }
			set
			{
                // encase in an if statement so the callOnResize is only called when there's a change
                if (value != mScaleY)
                {
                    mScaleY = System.Math.Max(value, mMinimumScaleY);
                    mScaleY = System.Math.Min(mScaleY, mMaximumScaleY);

                    if (mCloseButton != null)
                        mCloseButton.SetPositionTL(2 * mScaleX - 1.6f, -1.2f);

                    if(KeepWindowsInScreen)
                        KeepInScreen();

                    if (SpriteFrame != null)
                    {
                        SpriteFrame.ScaleY = value;

                    }

                    if (mMoveBarSpriteFrame != null)
                    {
                        mMoveBarSpriteFrame.RelativeY =
                            ScaleY + mMoveBarSpriteFrame.ScaleY;
                    }

                    foreach (Window child in mChildren)
                        child.SetPositionTL(child.mTopLeftValues.X, child.mTopLeftValues.Y);

                    OnResize();
                }
			}
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the ScaleX property in units per second.
        /// </summary>
        #endregion
        public float ScaleXVelocity
        {
            get { return mScaleXVelocity; }
            set { mScaleXVelocity = value; }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the ScaleY property in units per second.
        /// </summary>
        #endregion
        public float ScaleYVelocity
        {
            get { return mScaleYVelocity; }
            set { mScaleYVelocity = value; }
        }
        

        #endregion


        #region IAnimationChainAnimatable Properties

        #region XML Docs
        /// <summary>
        /// Whether the Window uses its AnimationChain.
        /// </summary>
        #endregion
        public bool Animate
        {
            get { return false;}// return mAnimate; }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public AnimationChainList AnimationChains
        {
            get { throw new Exception("The method or operation is not implemented."); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public float AnimationSpeed
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public int CurrentChainIndex
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #region XML Docs
        /// <summary>
        /// NOT IMPLEMENTED:  Will be included at a future time.  Using this will throw an exception.
        /// </summary>
        #endregion
        public string CurrentChainName
        {
            get { throw new Exception("The method or operation is not implemented."); }
            set { throw new Exception("The method or operation is not implemented."); }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public AnimationChain CurrentChain
        {
            get { return null; }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public int CurrentFrameIndex
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public bool JustChangedFrame
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public bool JustCycled
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public bool UseAnimationRelativePosition
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        #region ILayered Properties

        Layer ILayered.Layer
        {
            get
            {
                return null;
            }
        }

        #endregion

        public bool MovesWhenGrabbed
        {
            get;
            set;
        }

        #region Position

        #region XML Docs
        /// <summary>
        /// The screen-relative X position with 0 being the center of the screen.
        /// </summary>
        /// <remarks>
        /// If this has a Parent then this property is essentially read-only.
        /// </remarks>
        #endregion
        public float WorldUnitX
        {
            get { return mWorldUnitX; }
            set { mWorldUnitX = value; }
        }

        #region XML Docs
        /// <summary>
        /// The screen-relative Y position with 0 being the center of the screen.  This proerty
        /// uses up as positive Y.
        /// </summary>
        /// <remarks>
        /// If this has a Parent then this property is essentially read-only.
        /// </remarks>
        #endregion
        public float WorldUnitY
        {
            get { return mWorldUnitY; }
            set { mWorldUnitY = value; }
        }

        #region XML Docs
        /// <summary>
        /// The X position of the Window relative to its Parent.
        /// </summary>
        #endregion
        public float WorldUnitRelativeX
        {
            get 
            {
                if (SpriteFrame == null)
                    return mWorldUnitRelativeX;
                else
                    return SpriteFrame.RelativeX;
            }
            set 
            {
                if (SpriteFrame == null)
                    mWorldUnitRelativeX = value;
                else
                    SpriteFrame.RelativeX = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The Y position of the Window relative to its Parent.  Positive Y is up.
        /// </summary>
        #endregion
        public float WorldUnitRelativeY
        {
            get 
            {
                if (SpriteFrame == null)
                    return mWorldUnitRelativeY; 
                else
                    return SpriteFrame.RelativeY;
            }
            set 
            {
                if (SpriteFrame == null)
                    mWorldUnitRelativeY = value;
                else
                    SpriteFrame.RelativeY = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The X position of the Window.  If the Window does not have a parent this
        /// is the distance between the center of the Window and the left edge of the
        /// screen.  If the Window does have a parent this is the distance between the
        /// center of the Window and the left edge of its Parent.
        /// </summary>
        #endregion
        public float X
		{
			get
			{
                if (SpriteFrame != null)
                {
                    return SpriteFrame.Position.X;
                }
                else
                {
                    // See comment in the Y getter for why this uses unmodified values
                    //if (Parent == null) return (float)(GuiManager.XEdge + mWorldUnitX);
                    if (Parent == null) return GuiManager.UnmodifiedXEdge + mWorldUnitX;
                    else return Parent.ScaleX + mWorldUnitRelativeX;
                }
            }
			set
			{
                if (SpriteFrame != null)
                {
                    SpriteFrame.Position.X = value;
                }
                else
                {
                    SetPositionTL(value, Y);
                }
			}
		}

        #region XML Docs
        /// <summary>
        /// The Y position of the Window.  If the Window does not have a parent this
        /// is the distance between the center of the Window and the top edge of the
        /// screen.  If the Window does have a parent this is the distance between the
        /// center of the Window and the top edge of its Parent.  Positive is down.
        /// </summary>
        #endregion
		public float Y
		{
			get
			{
                if (SpriteFrame != null)
                {
                    return SpriteFrame.Position.Y;
                }
                else
                {
                    // September 28, 2011 (Victor Chelaru)
                    // Before I made change to how coordinates
                    // worked, we would subtract the mWorldUnitY
                    // from GuiManager.YEdge.  However, now the top
                    // will always be the YEdge if we had a default camera.
                    //if (Parent == null) return GuiManager.YEdge - mWorldUnitY;
                    if (Parent == null) return GuiManager.UnmodifiedYEdge - mWorldUnitY;
                    else return Parent.ScaleY - mWorldUnitRelativeY;
                }
			}
			set
			{
                if (SpriteFrame != null)
                {
                    SpriteFrame.Position.Y = value;
                }
                else
                {
                    SetPositionTL(X, value);
                }
			}
        }

        public float Z
        {
            get
            {
                return 0;
            }
        }

        #region XML Docs
        /// <summary>
        /// The Z plane that Windows are drawn when drawn by the GuiManager.  This 
        /// depends on the position of the Camera.
        /// </summary>
        #endregion
        public float AbsoluteWorldUnitZ
        {
            get
            {
                if (SpriteFrame != null)
                {
                    return SpriteFrame.Z;
                }
                else
                {
#if FRB_MDX
                    return GuiManager.Camera.Z + 100;
#else
                    return GuiManager.Camera.Z - 100;
#endif
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets the distance between the left of the screen and the Window's center.  This value
        /// represents this distance regardless of whether the Window has a parent.
        /// </summary>
        #endregion
        public float ScreenRelativeX
        {
            get
            {
                if (mParentWindow == null)
                    return X;
                else
                    return mParentWindow.ScreenRelativeX + mWorldUnitRelativeX;
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets the distance between the top of the screen and the Window's center.  This value
        /// represents this distance regardless of whether the Window has a parent.
        /// </summary>
        #endregion
        public float ScreenRelativeY
        {
            get
            {
                if (mParentWindow == null)
                    return Y;
                else
                    return mParentWindow.ScreenRelativeY - mWorldUnitRelativeY;
            }
        }



        #endregion

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the UI element can be interacted with by the Cursor.
        /// </summary>
        #endregion
        public virtual bool Enabled
        {
            get { return mEnabled; }
            set { mEnabled = value; }
        }

        #region XML Docs
        /// <summary>
        /// The list of Floating Windows which belong to this Window.
        /// </summary>
        #endregion
        public ReadOnlyCollection<IWindow> FloatingChildren
        {
            get { return mFloatingWindowsReadOnly; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the Window uses the default GUI drawn by the GuiManager.  If false then the Window
        /// uses GuiSkins and SpriteFrames.
        /// </summary>
        /// <remarks>
        /// GuiDrawn UI will always appear on top of other FlatRedBall graphics.  If not GUiDrawn
        /// (if using SpriteFrames) then the SpriteFrames will be sorted appropriately with other
        /// FlatRedBall graphical objects.
        /// </remarks>
        #endregion
        public bool GuiManagerDrawn
        {
            get { return mGuiManagerDrawn; }
            set { mGuiManagerDrawn = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the Window has a close button.
        /// </summary>
        /// <remarks>
        /// This is usually only set to true for Windows which do not have a Parent and 
        /// whidh have HasMoveBar set to true.
        /// </remarks>
        #endregion
        public bool HasCloseButton
        {
            get { return mCloseButton != null; }
            set
            {
                if (value == true && mCloseButton == null)
                {
                    // Adding a close button.

                    if (this.mGuiSkin == null)
                    {
                        mCloseButton = new Button(mCursor);
                    }
                    else
                    {
#if SILVERLIGHT
                        throw new NotImplementedException();
#else
                        mCloseButton = new Button(mGuiSkin, mCursor);
#endif
                    }

                    mCloseButton.Name = "Close Button";
                    mChildren.Add(mCloseButton);

                    mCloseButton.Parent = this;
                    mCloseButton.SetPositionTL(2 * mScaleX - 1.6f, -1.2f);
                    //			xButton.SetPositionTL(2*si.ScaleX + 1.6f, -1.2f);
                    mCloseButton.mScaleX = .6f;
                    mCloseButton.mScaleY = .6f;

                    mCloseButton.overlayTL = new Point(.24609375f, .578125f);
                    mCloseButton.overlayTR = new Point(.2890625f, .578125f);
                    mCloseButton.overlayBL = new Point(.24609375f, .62106375f);
                    mCloseButton.overlayBR = new Point(.2890625f, .62106375f);

                    mCloseButton.downOverlayTL = new Point(.2890625f, .62106375f);
                    mCloseButton.downOverlayTR = new Point(.24609375f, .62106375f);
                    mCloseButton.downOverlayBL = new Point(.2890625f, .578125f);
                    mCloseButton.downOverlayBR = new Point(.24609375f, .578125f);
                    mCloseButton.DrawBase = false;
                }
                else if (value == false && mCloseButton != null)
                {
                    GuiManager.RemoveWindow(mCloseButton);
                    mCloseButton = null;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether thie Window has a bar which can be grabbed to move the Window.
        /// </summary>
        /// <remarks>
        /// This is generally not set to true unless the Window does not have a Parent.
        /// </remarks>
        #endregion
        public virtual bool HasMoveBar
        {
            get { return mMoveBar; }
            set 
            { 
                mMoveBar = value;

                #region If not GuiManager Drawn, show the move bar
                if (!GuiManagerDrawn)
                {
                    if (mMoveBar)
                    {
                        if (mMoveBarSpriteFrame == null)
                        {
                            mMoveBarSpriteFrame = new SpriteFrame();
                            SpriteManager.AddSpriteFrame(mMoveBarSpriteFrame);

                            mMoveBarSpriteFrame.AttachTo(SpriteFrame, false);
                            mMoveBarSpriteFrame.RelativeY =
                                SpriteFrame.ScaleY + mMoveBarSpriteFrame.ScaleY;

                            mMoveBarSpriteFrame.ScaleX = SpriteFrame.ScaleX;
                        }
                        else
                        {
                            mMoveBarSpriteFrame.Visible = false;
                        }

                        SetSkin(mGuiSkin);
                    }


                }
                #endregion
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets whether the Window, any of its children, or any of its floating Windows
        /// are receiving input from the Keyboard.
        /// </summary>
        #endregion
        public virtual bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                // We used to only do this based off of whether
                // something was receiving keyboard input - but if you have
                // a combo box open, that's receiving input and shouldn't be updated
                //if (InputManager.ReceivingInput != null)
                {
                    if (InputManager.ReceivingInput == this)
                    {
                        return true;
                    }

                    foreach (Window window in mChildren)
                    {
                        if (window.IsWindowOrChildrenReceivingInput)
                        {
                            return true;
                        }
                    }

                    foreach (Window window in mFloatingWindows)
                    {
                        if (window.IsWindowOrChildrenReceivingInput)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        #region XML Docs
        /// <summary>
        /// The parent of the Window.
        /// </summary>
        #endregion
        public IWindow Parent
        {
            get { return mParentWindow; }

            // Vic asks:  I don't know why the setter is public.  Should this be removed?
            set 
            {

                mParentWindow = value as Window; 
                // if using SpriteFrames, set the relative values
                if (Parent != null && SpriteFrame != null)
                {
                    SpriteFrame.AttachTo(mParentWindow.SpriteFrame, true);
                }
            }
        }


        public Window FloatingParent
        {
            get { return mFloatingParentWindow; }
        }


        public bool AbsoluteVisible
        {
            get
            {
                if (this.GetParentVisibility() == false)
                    return false;

                if (SpriteFrame == null)
                    return mVisible;
                else
                    return SpriteFrame.Visible;
            }
        }


        #region XML Docs
        /// <summary>
        /// Whether the Window is visible.
        /// </summary>
        #endregion
        public virtual bool Visible
		{
			get
			{

				if(this.GetParentVisibility() == false)
					return false;

                if (SpriteFrame == null)
                    return mVisible;
                else
                    return SpriteFrame.Visible;
			}
			set
            {

                bool changedVisibility = value != Visible;

                if (SpriteFrame == null)
                {
                    mVisible = value;
                }
                else
                {
                    SpriteFrame.Visible = value;
                    if (mMoveBarSpriteFrame != null)
                    {
                        mMoveBarSpriteFrame.Visible = value;
                    }

                    foreach (Window window in mChildren)
                        window.Visible = value;
                    foreach (Window window in mFloatingWindows)
                        window.Visible = value;


                }

                if (changedVisibility)
                {
                    if (VisibleChange != null)
                        VisibleChange(this);
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The name of the Window.
        /// </summary>
        #endregion
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        #region XML Docs
        /// <summary>
        /// The Windows which are attached to this instance.
        /// </summary>
        /// <remarks>
        /// All children Windows must be contained within the physical bounds
        /// of their Parent, otherwise the user will not be able to interact with them.
        /// </remarks>
        #endregion
        public ReadOnlyCollection<IWindow> Children
        {
            get { return mChildrenReadOnly; }
        }

        #region XML Docs
        /// <summary>
        /// The minimum ScaleX of the Window.
        /// </summary>
        #endregion
        public float MinimumScaleX
        {
            get { return mMinimumScaleX; }
            set { mMinimumScaleX = value; ScaleX = System.Math.Max(ScaleX, MinimumScaleX); }
        }

        #region XML Docs
        /// <summary>
        /// The minimum ScaleY of the Window.
        /// </summary>
        #endregion
        public float MinimumScaleY
        {
            get { return mMinimumScaleY; }
            set { mMinimumScaleY = value; ScaleY = System.Math.Max(ScaleY, MinimumScaleY); }
        }

        #region XML Docs
        /// <summary>
        /// The maximum ScaleX of the Window.
        /// </summary>
        #endregion
        public float MaximumScaleX
        {
            get { return mMaximumScaleX; }
            set { mMaximumScaleX = value; ScaleX =  System.Math.Min(ScaleX, MaximumScaleX); }
        }

        #region XML Docs
        /// <summary>
        /// The maximum ScaleY of the Window.
        /// </summary>
        #endregion
        public float MaximumScaleY
        {
            get { return mMaximumScaleY; }
            set { mMaximumScaleY = value; ScaleY = System.Math.Min(ScaleY, MaximumScaleY); }
        }

        #region XML Docs
        /// <summary>
        /// Whether the Window can be resized by grabbing its bottom-right corner with the Cursor.
        /// </summary>
        #endregion
        public virtual bool Resizable
        {
            get { return mResizable; }
            set { mResizable = value; }
        }
#if !SILVERLIGHT

        #region XML Docs
        /// <summary>
        /// Whether the borders are drawn on the window.
        /// </summary>
        /// <remarks>
        /// This controls the sides, bottom, top and move bar drawing.
        /// This can be set to false when setting a Window's texture
        /// so that only the texture is shown.  
        /// </remarks>
        #endregion
        public bool DrawBorders
        {
            get { return mDrawBorders; }
            set { mDrawBorders = value; }
        }

        #region XML Docs
        /// <summary>
        /// The texture to use when drawing the Window.  Default of null will
        /// result in the Window using the default texture.
        /// </summary>
        #endregion
        public Texture2D BaseTexture
        {
            get { return mBaseTexture; }
            set { mBaseTexture = value; }
        }

        #region XML Docs
        /// <summary>
        /// The opacity of the Window.  Default is fully opaque (255).
        /// </summary>
        #endregion
        public float Alpha
        {
#if FRB_MDX
            get { return (float)((mColor & 0xff000000) >> 24); }
            set 
            {
                mColor = (uint)(((int)(value)) << 24) +
                    (0x00ffffff & mColor);
            }
#else
            get 
            { 
                return (float)((mColor.PackedValue & 0xff000000) >> 24)/255; 
            }
            set
            {
                mColor.PackedValue = (uint)(((int)(value*255)) << 24) +
                    (0x00ffffff & mColor.PackedValue);
            }
#endif
        }

#endif

        public bool VisibleSettingIgnoringParent
        {
            get { return mVisible; }
        }


        public bool IgnoredByCursor
        {
            get;
            set;
        }

        protected string ConcatenatedTitle
        {
            get 
            {
                float maxWidth = (this.ScaleX - .5f) * 2;

                string titleToReturn = 
                    mName.Substring(0, TextManager.GetNumberOfCharsIn(maxWidth, mName, GuiManager.TextSpacing));

                return titleToReturn; 
            
            }
        }
       


        #endregion	

		#region Events

        #region XML Docs
        /// <summary>
        /// Raised when the user has pushed and released the primary (left) mouse button
        /// on the Window.
        /// </summary>
        #endregion
        public event GuiMessage Click;

        #region XML Docs
        /// <summary>
        /// Raised when the user has pushed and released the secondary (right) mouse button
        /// on the Window.
        /// </summary>
        #endregion
        public event GuiMessage SecondaryClick;

        #region XML Docs
        /// <summary>
        /// Raised when the user has pushed the primary (left) mouse button on the Window.
        /// </summary>
        #endregion
        public event GuiMessage Push;

        #region XML Docs
        /// <summary>
        /// Raised when the Window has been closed.
        /// </summary>
        #endregion
        public event GuiMessage Closing;

        #region XML Docs
        /// <summary>
        /// Raised when the cursor is over the window whether it has moved or not.
        /// </summary>
        #endregion
        public event GuiMessage CursorOver;

        #region XML Docs
        /// <summary>
        /// Raised when the user has double-clicked on the Window with the primary (left) mouse button.
        /// </summary>
        #endregion
        public event GuiMessage DoubleClick;

        #region XML Docs
        /// <summary>
        /// Raised after the user has clicked on this Window giving it focus, then on a different
        /// Window causing this Window to lose focus.
        /// </summary>
        #endregion
        public event GuiMessage LosingFocus;

        #region XML Docs
        /// <summary>
        /// Raised when the Cursor has grabbed and is moving a Window.  Most UI elements
        /// are not by default grabbable.
        /// </summary>
        /// <remarks>
        /// The ScrollBar has its position bar grabbable.
        /// </remarks>
        #endregion
        public event GuiMessage Dragging;

        #region XML Docs
        /// <summary>
        /// Continually raised while the user is resizing the Window, or when ScaleX or ScaleY are
        /// changed.  This is not raised once, but once each frame during resizing with the Cursor.
        /// </summary>
        #endregion
        public event GuiMessage Resizing;


        public event GuiMessage ResizeEnd;

        #region XML Docs
        /// <summary>
        /// Raised when the cursor first moves over the Window.
        /// </summary>
        #endregion
        public event GuiMessage RollingOn;

        #region XML Docs
        /// <summary>
        /// Raised when the cursor leaves the Window.
        /// </summary>
        #endregion
        public event GuiMessage RollingOff;

        public event GuiMessage RollOver;

        #region XML Docs
        /// <summary>
        /// Raised when the Window's Visible property changes.
        /// </summary>
        #endregion
        public event GuiMessage VisibleChange;

        #region XML Docs
        /// <summary>
        /// Raised when the cursor is over the window and has moved since last frame.
        /// </summary>
        #endregion
        public event GuiMessage RollingOver;


        public event GuiMessage MouseWheelScroll;

		#endregion

		#region delegate and delegate calling methods

        public virtual void CallClick()
        {
            if (Click != null)
                Click(this);
        }

        #region XML Docs
        /// <summary>
        /// Raises the Click event.
        /// </summary>
        #endregion
        public virtual void OnClick()
		{
			if(Click != null)
				Click(this);
        }

        #region XML Docs
        /// <summary>
	    /// Raises the Dragging event.
        /// </summary>
        #endregion
        public void OnDragging()
		{
			if(Dragging != null)
				Dragging(this);
        }

        #region XML Docs
        /// <summary>
        /// Raises the LosingFocus event.
        /// </summary>
        #endregion
        public void OnLosingFocus()
		{
			if(LosingFocus != null)
				LosingFocus(this);
        }

        #region XML Docs
        /// <summary>
		/// Raises the Push event.
        /// </summary>
        #endregion
        public void OnPush()
		{
			if(Push != null)
				Push(this);
        }

        #region XML Docs
        /// <summary>
        /// Raises the Resizing event.
        /// </summary>
        #endregion
        public void OnResize()
        {
            if (Resizing != null)
                Resizing(this);
        }


        public void OnResizeEnd()
        {
            if (mCursor.ResizingWindow && ResizeEnd != null)
            {
                ResizeEnd(this);
            }
        }

		#endregion

		#region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates a new Window using the default graphics.
        /// </summary>
        /// <remarks>
        /// To have the Window drawn by the engine, it must be added
        /// to the GuiManager through the AddWindow button.  It is more
        /// common to create Windows using the no-argument GuiManager's
        /// AddWindow method.
        /// 
        /// <para>The following code creates a window, places it at the
        /// center of the screen, scales it, and adds the move bar.
        /// This method of creating a Window is more common than calling
        /// the constructor.</para>
        /// <code>
        /// Window newWindow = GuiManager.AddWindow();
        /// newWindow.SetPositionTL(SpriteManager.Camera.XEdge, SpriteManager.Camera.YEdge);
        /// newWindow.ScaleX = 10;
        /// newWindow.ScaleY = 10;
        /// newWindow.HasMoveBar = true;
        /// </code>
        /// </remarks>
        /// <param name="cursor">Reference to the cursor to use.</param>
        #endregion
        public Window(Cursor cursor)
		{
            MovesWhenGrabbed = true;
            mName = this.GetType().Name;

            mMaximumScaleX = mMaximumScaleY = float.PositiveInfinity;
            Enabled = true;
            this.mCursor = cursor;

            Resizable = false;

            mChildrenReadOnly = new ReadOnlyCollection<IWindow>(mChildren);

            mFloatingWindowsReadOnly = new ReadOnlyCollection<IWindow>(mFloatingWindows);
			//moveBar = false;
			//parentWindow = null;
			//xButton = null;

			mNumberOfVertices = 54; // 9 quads, 6 vertices per quad

            mGuiManagerDrawn = true;
            mDrawBorders = true;

		}

        internal Window(string texture, Cursor cursor, string contentManagerName)
        {
            MovesWhenGrabbed = true;
            mName = this.GetType().Name;

            mMaximumScaleX = mMaximumScaleY = float.PositiveInfinity;
            Enabled = true;
            this.mCursor = cursor;

            Resizable = false;

            mName = "";

            mGuiManagerDrawn = false;

            SpriteFrame = new SpriteFrame( 
                FlatRedBallServices.Load<Texture2D>(texture, contentManagerName), SpriteFrame.BorderSides.All);

            mDrawBorders = true;

            mChildrenReadOnly = new ReadOnlyCollection<IWindow>(mChildren);

            mFloatingWindowsReadOnly = new ReadOnlyCollection<IWindow>(mFloatingWindows);

        }

        #region XML Docs
        /// <summary>
        /// Creates a new Window using the argument GuiSkin to set its appearance.  This will
        /// create a SpriteFrame as the visible representation for the Window.
        /// </summary>
        /// <param name="guiSkin">The GuiSkin to use to set the Window's visibile representation properties.</param>
        /// <param name="cursor">The cursor that will interact with the Window.</param>
        #endregion
        public Window(GuiSkin guiSkin, Cursor cursor)
        {
            MovesWhenGrabbed = true;
            mName = this.GetType().Name;

            mGuiSkin = guiSkin;
            mMaximumScaleX = mMaximumScaleY = float.PositiveInfinity;
            Enabled = true;
            this.mCursor = cursor;

            Resizable = false;

            mChildrenReadOnly = new ReadOnlyCollection<IWindow>(mChildren);

            mFloatingWindowsReadOnly = new ReadOnlyCollection<IWindow>(mFloatingWindows);

            mDrawBorders = true;

            SetSkin(guiSkin);

        }

		#endregion


        #region Public Methods

        public virtual void Activity(Camera camera)
        {
            if (SpriteFrame != null)
                SpriteFrame.Manage();

            foreach (Window w in this.mChildren)
                w.Activity(camera);

            foreach (Window w in mFloatingWindows)
                w.Activity(camera);

        }

        #region adding methods

        #region XML Docs
        /// <summary>
        /// Adds the arguemnt Window to this Window's children List.
        /// </summary>
        /// <param name="windowToAdd">The Window to add.</param>
        #endregion
        public virtual void AddWindow(IWindow windowToAdd)
		{
			mChildren.Add( windowToAdd);
			windowToAdd.Parent = this;
        }

        #region XML Docs
        /// <summary>
        /// Adds the arguent Window to this Window's floating Windows List.
        /// </summary>
        /// <param name="windowToAdd">The Window to add.</param>
        #endregion
        public void AddFloatingWindow(Window windowToAdd)
        {
            mFloatingWindows.Add(windowToAdd);
            windowToAdd.mFloatingParentWindow = this;
        }

        #endregion
#if !SILVERLIGHT

        public virtual void AddToLayer(Layer layerToAddTo)
        {
            if (GuiManagerDrawn)
            {
                throw new InvalidOperationException(
                    "Can't add this element to a Layer since it is drawn by the GuiManager.");
            }

            SpriteManager.AddToLayer(SpriteFrame, layerToAddTo);

            foreach (Window window in mChildren)
            {
                window.AddToLayer(layerToAddTo);
            }
        }
#endif
        public void BringToFront(IWindow childWindow)
        {
            if (this.mChildren.Contains(childWindow))
            {
                mChildren.Remove(childWindow);
                mChildren.Add(childWindow);
            }
        }

        public void CallRollOn()
        {
            if (RollingOn != null)
                RollingOn(this);
        }


        public void CallRollOff()
        {
            if (RollingOff != null)
                RollingOff(this);
        }

        public void CallRollOver()
        {
            if(RollOver != null)
            {
                RollOver(this);
            }
        }

        #region XML Docs
        /// <summary>
        /// Clears all of the Window's events.
        /// </summary>
        /// <remarks>
        /// This is usually only called by the engine when a window is being destroyed.
        /// </remarks>
        #endregion
        public virtual void ClearEvents()
        {
            Click = null;
            SecondaryClick = null;
            Push = null;
            Closing = null;
            DoubleClick = null;
            LosingFocus = null;
            Dragging = null;
            Resizing = null;
        }


        #region XML Docs
        /// <summary>
        /// Sets the Visible to false and calls the Closing event.  This does not remove
        /// the window from the GuiManager unless the Window was created by the ObjectDisplayManager
        /// or any IObjectDisplayer provided as part of FlatRedBall.
        /// </summary>
        #endregion
        public void CloseWindow()
        {
            // Use the property so that events get raised properly.
            Visible = false;
            if (Closing != null)
                Closing(this);
        }

        #region XML Docs
        /// <summary>
        /// Returns the number of visible children and floating Windows.  This method
        /// does not recursively check children's children.
        /// </summary>
        /// <returns>The number of visible children and floating Windows.</returns>
        #endregion
        public int GetVisibleChildrenCount()
        {
            int visibleCount = 0;
            for (int i = 0; i < mChildren.Count; i++)
                if (mChildren[i].Visible == true)
                    visibleCount++;

            for (int i = 0; i < mFloatingWindows.Count; i++)
                if (mFloatingWindows[i].Visible == true)
                    visibleCount++;

            return visibleCount;

        }


        #region XML Docs
        /// <summary>
        /// Returns which window the argument coordinates are currently over.  Windows
        /// tested are this, children, and floating Windows.
        /// </summary>
        /// <param name="cameraRelativeX">The Camera-relative X position at 100 units away.  This matches the UI coordinate system.</param>
        /// <param name="cameraRelativeY">The Camera-relative Y position at 100 units away.  This matches the UI coordinate system.</param>
        /// <returns>The window that the point is over if any, else null.</returns>
        #endregion
        public Window GetWindowOver(float cameraRelativeX, float cameraRelativeY)
        {
            Window windowOver;


            // first see if the point is over any windows.  
            foreach (Window w in mFloatingWindows)
            {
                windowOver = w.GetWindowOver(cameraRelativeX, cameraRelativeY);

                if (windowOver != null)
                    return windowOver;
            }

            if (IsPointOnWindow(cameraRelativeX, cameraRelativeY))
            {
                foreach (Window w in mChildren)
                {
                    windowOver = w.GetWindowOver(cameraRelativeX, cameraRelativeY);

                    if (windowOver != null)
                        return windowOver;
                }

                // the point isn't on any children windows so just return this window
                return this;
            }

            // If execution reaches this code then the point is not on any floating Windows,
            // children Windows, or no this Window itself - so return null;
            return null;
        }

        #region XML Docs
        /// <summary>
        /// Searches through children and floating windows for a Window with 
        /// its Name matching the argument name.  Returns null if none are found.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The Window with the matching Name or null if none are found.</returns>
        #endregion
        public Window FindByName(string name)
        {

            Window foundWindow = mChildren.FindByName(name) as Window;

            if (foundWindow == null)
                foundWindow = mFloatingWindows.FindByName(name) as Window;

            return foundWindow;
        }

        #region XML Docs
        /// <summary>
        /// Returns the child or floating Window with the matching name, or null if none are found.
        /// </summary>
        /// <param name="name">The name to earch for.</param>
        /// <returns>The window with the matching name.</returns>
        #endregion
        public Window FindWithNameContaining(string name)
        {
            Window foundWindow = mChildren.FindWithNameContaining(name) as Window;

            if (foundWindow == null)
                foundWindow = mFloatingWindows.FindWithNameContaining(name) as Window;

            return foundWindow;


        }


        public bool GetParentVisibility()
        {
            if ((SpriteFrame == null && mVisible == false) || (SpriteFrame != null && SpriteFrame.AbsoluteVisible == false))
            {
                return false;
            }

            if (Parent != null && Parent.GetParentVisibility() == false)
            {
                return false;
            }

            if (mFloatingParentWindow != null && mFloatingParentWindow.GetParentVisibility() == false)
            {
                return false;
            }

            return true;
        }

        public bool IsCursorOnMoveBar(Cursor cursor)
        {
            return HasMoveBar == true && cursor.YForUI > mWorldUnitY + mScaleY - .4f;
        }

        static Vector3 mThrowAwayIsPointOnWindowVector = new Vector3();
        #region XML Docs
        /// <summary>
        /// Returns whether the argument coordinate is on this instance.  Units 
        /// are at 100 units away from the camera (similar to how the cursor 
        /// is positioned);
        /// </summary>
        /// <param name="cameraRelativeX">The X coordinate relative to the center of the screen at 100 units away.</param>
        /// <param name="cameraRelativeY">The Y coordinate relative to the center of the screen at 100 units away.</param>
        /// <returns>Whether the point is on the Window.</returns>
        #endregion        

        public virtual bool IsPointOnWindow(float cameraRelativeX, float cameraRelativeY)
        {
            // the CollapseWindow overrides this method since the logic is slightly different
            // when it is collapsed.


            if (GuiManagerDrawn == false)
            {
                Vector3 rayDirection = new Vector3(
                    SpriteManager.Camera.X + cameraRelativeX,
                    SpriteManager.Camera.Y + cameraRelativeY,
                    SpriteManager.Camera.Z + 100);
#if !SILVERLIGHT
                Ray ray = new Ray(SpriteManager.Camera.Position, rayDirection);
#else 
                Ray ray = new Ray();
#endif

                // assumes only using the default camera
                return FlatRedBall.Math.MathFunctions.IsOn3D<SpriteFrame>
                    (SpriteFrame, true, ray, GuiManager.Camera,
                    ref mThrowAwayIsPointOnWindowVector);

            }
            else
            {
                if (HasMoveBar == false)
                {
                    return cameraRelativeX < mWorldUnitX + mScaleX &&
                            cameraRelativeX > mWorldUnitX - mScaleX &&
                            cameraRelativeY < mWorldUnitY + mScaleY &&
                            cameraRelativeY > mWorldUnitY - mScaleY;
                }
                else
                {
                    return
                        cameraRelativeX < mWorldUnitX + mScaleX &&
                        cameraRelativeX > mWorldUnitX - mScaleX &&
                        cameraRelativeY < mWorldUnitY + mScaleY + 2.8f &&
                        cameraRelativeY > mWorldUnitY - mScaleY;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Repositions the Window if the Window has a move bar so that it can be accessed by the Cursor.
        /// </summary>
        #endregion
        public void KeepInScreen()
        {
            // If the camera's YEdge is 0 then the window is minimized.  Don't worry about keeping
            // things in screen in this situation.

            const float minimumYEdgeForResize = .64f;  // weird value, I know.  Determined through tests.
            const float minimumXEdge = 3f;

            if (Parent == null)
            {
                if (GuiManager.YEdge > minimumYEdgeForResize)
                {
                    float top = this.Y - this.ScaleY;

                    // see if the window's above the screen
                    if (HasMoveBar && top < 1)
                        Y = ScaleY + 1.01f;

                    top = this.Y - this.ScaleY;

                    // Commented this out...
                    // see if the window's below the screen
                    //if (HasMoveBar && mWorldUnitY + mScaleY < -GuiManager.YEdge + 0)
                    //    Y = 2 * GuiManager.YEdge - .01f + ScaleY;
                    // And used this because it seems more accurate:
                    if (HasMoveBar && top > 2 * GuiManager.YEdge -1)
                        Y = ScaleY + 2 * GuiManager.YEdge - 1.01f;
                }
                if (this.X - this.ScaleX > GuiManager.XEdge * 2)
                {
                    this.X = 2 * GuiManager.XEdge - minimumXEdge + this.ScaleX;
                }
                if (this.X + this.ScaleX < 0)
                {
                    this.X = minimumXEdge - this.ScaleX;
                }
            }
        }

        public bool OverlapsWindow(IWindow windowToTestOverlappingAgainst)
        {
            float thisExtraY = 0;
            float otherExtraY = 0;

            if (this.HasMoveBar)
                thisExtraY = Window.MoveBarHeight;
            if (windowToTestOverlappingAgainst is Window && ((Window)windowToTestOverlappingAgainst).HasMoveBar)
                otherExtraY = Window.MoveBarHeight;

            return
                this.X + this.ScaleX > windowToTestOverlappingAgainst.X - windowToTestOverlappingAgainst.ScaleX &&
                this.X - this.ScaleX < windowToTestOverlappingAgainst.X + windowToTestOverlappingAgainst.ScaleX &&
                this.Y + this.ScaleY > windowToTestOverlappingAgainst.Y - windowToTestOverlappingAgainst.ScaleY - otherExtraY &&
                this.Y - this.ScaleY - thisExtraY < windowToTestOverlappingAgainst.Y + windowToTestOverlappingAgainst.ScaleY;


        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Window from the internal WindowArray belonging to this.  Also removes the SpriteFrame from the
        /// SpriteManager if the window is not drawn by the GuiManager.
        /// </summary>
        /// <param name="windowToRemove">Reference to the Window to remove.</param>
        #endregion
        public virtual void RemoveWindow(IWindow windowToRemove)
        {
            if (windowToRemove == null)
            {
                return;
            }

            if (mChildren.Contains(windowToRemove))
            {
                this.mChildren.Remove(windowToRemove);
            }

            if (mFloatingWindows.Contains(windowToRemove))
            {
                if (windowToRemove is Window)
                {
                    ((Window)windowToRemove).mFloatingParentWindow = null;
                }

                mFloatingWindows.Remove(windowToRemove);
            }

            if (windowToRemove.SpriteFrame != null)
            {
                SpriteManager.RemoveSpriteFrame(windowToRemove.SpriteFrame);
            }
        }

        #region XML Docs
        /// <summary>
        /// Currently not implemented.
        /// </summary>
        /// <param name="chainToSet"></param>
        #endregion
        public void SetAnimationChain(AnimationChain chainToSet)
        {
            if (SpriteFrame != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                mCurrentChain = chainToSet;
            }

        }


        #region XML Docs
        /// <summary>
        /// Sets the screen relative or Parent-relative position.
        /// </summary>
        /// <remarks>
        /// Calling this method is the same as setting the X and Y properties.
        /// </remarks>
        /// <param name="x">The X value.  Positive X is to the right.</param>
        /// <param name="y">The Y value.  Positive Y is down.</param>
        #endregion
        public void SetPositionTL(float x, float y)
        {
            if (Parent == null)
            {
                if (SpriteFrame == null)
                {
                    mWorldUnitX = -GuiManager.UnmodifiedXEdge + x;
                    mWorldUnitY = GuiManager.UnmodifiedYEdge - y;
                }
                else
                {
                    // we don't know the actual position of the SpriteFrame so we need to take the
                    // z value into consideration
                    float cameraDistance = SpriteFrame.Z - GuiManager.Camera.Z;

                    mWorldUnitX = SpriteFrame.X = -GuiManager.Camera.XEdge * cameraDistance / 100 + x;
                    mWorldUnitY = SpriteFrame.Y = GuiManager.Camera.YEdge * cameraDistance / 100 - y;


                }

                if (KeepWindowsInScreen)
                    KeepInScreen();
            }

            else
            {
                if (SpriteFrame == null)
                {
                    mTopLeftValues = new Vector2(x, y);
                    mWorldUnitRelativeX = -Parent.ScaleX + x;
                    mWorldUnitRelativeY = Parent.ScaleY - y;
                }
                else
                {
                    SpriteFrame.RelativeX = -Parent.SpriteFrame.ScaleX + x;
                    SpriteFrame.RelativeY = Parent.SpriteFrame.ScaleY - y;
                }
            }

            this.UpdateDependencies();

        }



        #region XML Docs
        /// <summary>
        /// Resizes the Window and keeps the top left position static.  This method keeps the top left of all
        /// children Windows in the same relative position to the top left of
        /// this Window after the resize.
        /// </summary>
        /// <param name="newScaleX">The new ScaleX value.</param>
        /// <param name="newScaleY">The new ScaleY value.</param>
        #endregion
        public virtual void SetScaleTL(float newScaleX, float newScaleY)
        {
            SetScaleTL(newScaleX, newScaleY, true);
        }

        #region XML Docs
        /// <summary>
        /// Resizes the Window and can keep the top left position static.  This method
        /// keeps the top left of all children Windows in the same relative position to
        /// the top left of this Window after the resize.
        /// </summary>
        /// <param name="newScaleX">The new ScaleX value.</param>
        /// <param name="newScaleY">The new ScaleY value.</param>
        /// <param name="keepTopLeftStatic">Whether the top left of this Window should be in the same position after the scale values are changed.</param>
        #endregion
        public virtual void SetScaleTL(float newScaleX, float newScaleY, bool keepTopLeftStatic)
        {
            bool oldKeepInScreen = KeepWindowsInScreen;
            KeepWindowsInScreen = false;

            // Since the new scales are used to reposition
            // the window, make sure they fall within the min
            // and max scales allowed for the window.
            newScaleX = System.Math.Min(newScaleX, mMaximumScaleX);
            newScaleX = System.Math.Max(newScaleX, mMinimumScaleX);

            newScaleY = System.Math.Min(newScaleY, mMaximumScaleY);
            newScaleY = System.Math.Max(newScaleY, mMinimumScaleY);

            if (keepTopLeftStatic)
            {
                X += newScaleX - ScaleX;
                Y += newScaleY - ScaleY;
            }
            ScaleX = newScaleX;
            ScaleY = newScaleY;



            // Not sure why I was repositioning the floating windows.  
            // It screws up the PropertyGrid floating windows.  If this
            // needs to be added back in, I'll probably have to add a property
            // that controls whether a floating window is moved when the parent
            // is resized.
            /*
            foreach (Window w in mFloatingWindows)
                w.SetPositionTL(w.mTopLeftValues.X, w.mTopLeftValues.Y);
            */

            KeepWindowsInScreen = oldKeepInScreen;
            if (KeepWindowsInScreen)
                KeepInScreen();
        }

        #region XML Docs
        /// <summary>
        /// Sets the GuiSkin for the window.  This refreshes the appearance of the Window as well.
        /// </summary>
        /// <param name="guiSkin">The GuiSkin to set.</param>
        #endregion
        public virtual void SetSkin(GuiSkin guiSkin)
        {
            SetFromWindowSkin(guiSkin.WindowSkin);
        }
#if !SILVERLIGHT
        #region XML Docs
        /// <summary>
        /// Sets the texture coordinates for the Window.  Only valid if the Window's BaseTexture is non-null.
        /// </summary>
        /// <param name="top">The top texture coordinate.</param>
        /// <param name="bottom">The bottom texture coordinate.</param>
        /// <param name="left">The left texture coordinate.</param>
        /// <param name="right">The right texture coordinate.</param>
        #endregion
        public virtual void SetTextureCoordinates(float top, float bottom, float left, float right)
        {
            mTextureTop = top;
            mTextureBottom = bottom;
            mTextureLeft = left;
            mTextureRight = right;
        }

        #region XML Docs
        /// <summary>
        /// Returns a string containing basic information about the Window.
        /// </summary>
        /// <returns>The string with the Window's information.</returns>
        #endregion
        public override string ToString()
        {
            return this.mName +
                "\n x =    " + this.X.ToString() +
                "\n y =	   " + this.Y.ToString() +
                "\n ScaleX = " + this.ScaleX.ToString() +
                "\n ScaleY = " + this.ScaleY.ToString();
        }
#endif

        #region XML Docs
        /// <summary>
        /// Updates the Window's position according to its Parent's position. 
        /// Also performs updates on all children.
        /// </summary>
        /// <remarks>
        /// The GuiManager automatically calls this on all Windows so it's unlikely that
        /// this needs to be called outside of the engine.  This method does not update the
        /// Window's Parents like the UpdateDependencies method does for PositionedObjects because
        /// the GuiManager only keeps reference to the top parents of Windows.  Therefore, the GuiManager
        /// only loops through the top parents and it's their responsibility to update their children's positions.
        /// </remarks>
        #endregion
        public void UpdateDependencies()
        {
            if (Parent != null)
            {
				if (SpriteFrame == null)
				{
					mWorldUnitX = mWorldUnitRelativeX + Parent.WorldUnitX;
					mWorldUnitY = mWorldUnitRelativeY + Parent.WorldUnitY;
				}
				else
				{
					mWorldUnitX = SpriteFrame.X;
					mWorldUnitY = SpriteFrame.Y;
				}
                // else handled by SpriteFrame attachment behavior
            }
			else if (SpriteFrame != null)
			{
				mWorldUnitX = SpriteFrame.X;
				mWorldUnitY = SpriteFrame.Y;
			}

            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].UpdateDependencies();

            for (int i = 0; i < mFloatingWindows.Count; i++)
                mFloatingWindows[i].UpdateDependencies();
        }

		public bool HasCursorOver(Cursor cursor) 
		{
#if SILVERLIGHT
            return false;
#else
            if (SpriteFrame != null)
            {
                return cursor.IsOn3D(SpriteFrame);
            }
            else
            {
                return IsPointOnWindow(cursor.XForUI, cursor.YForUI);
            }
#endif
		}

        #endregion

        #region Protected Methods

        #region XML Docs
        /// <summary>
        /// Sets the SpriteFrame's properties according to the argument WindowSkin.
        /// </summary>
        /// <remarks>
        /// This allows Window-inheriting UI elements to pass any WindowSkin and control
        /// their visibility.  For example, a Button may pass a different WindowSkin than a
        /// TextBox to this method.
        /// </remarks>
        /// <param name="windowSkin">The WindowSKin to use for setting the SpriteFrame's properties.</param>
        #endregion
        protected internal void SetFromWindowSkin(WindowSkin windowSkin)
        {
            if (SpriteFrame == null)
            {
                mGuiManagerDrawn = false;
                
                this.SpriteFrame = new SpriteFrame();
                SpriteManager.AddSpriteFrame(SpriteFrame);

                SpriteFrame.ScaleX = mScaleX;
                SpriteFrame.ScaleY = mScaleY;

            }

            this.SpriteFrame.Texture = windowSkin.Texture;
            this.SpriteFrame.SpriteBorderWidth = windowSkin.SpriteBorderWidth;
            this.SpriteFrame.TextureBorderWidth = windowSkin.TextureBorderWidth;
            this.SpriteFrame.Borders = windowSkin.BorderSides;
            this.SpriteFrame.Alpha = windowSkin.Alpha;

            if (mMoveBarSpriteFrame != null)
            {
                this.mMoveBarSpriteFrame.Texture = windowSkin.MoveBarTexture;
                this.mMoveBarSpriteFrame.SpriteBorderWidth = windowSkin.MoveBarSpriteBorderWidth;
                this.mMoveBarSpriteFrame.TextureBorderWidth = windowSkin.MoveBarTextureBorderWidth;
                this.mMoveBarSpriteFrame.Borders = windowSkin.MoveBarBorderSides;
            }
        }

        #endregion

        #region Internal Methods

        
#if !SILVERLIGHT


        internal void AnimateSelf()
        {
            if (this.Animate)
            {
#if FRB_MDX
                SpriteManager.AnimateWindow(this);
#else
                throw new NotImplementedException("Window animation not implemented in FRB_XNA");
#endif
            }

            for (int i = 0; i < mChildren.Count; i++)
            {
                Window asWindow = mChildren[i] as Window;

                if (asWindow.Visible)
                {
                    asWindow.AnimateSelf();
                }
            }

            for (int i = 0; i < mFloatingWindows.Count; i++)
            {
                Window asWindow = mFloatingWindows[i] as Window;

                if (asWindow.Visible)
                {
                    asWindow.AnimateSelf();
                }
            }


        }

#endif

        #region XML Docs
        /// <summary>
        /// Removes self from all Parent Windows, destroys all Children windows, and 
        /// clears events if keepEvents is false.
        /// </summary>
        /// <remarks>
        /// This method is called by GuiManager's RemoveWindow method.  The GuiManager's RemoveWindow
        /// method removes the window from the GuiManager if it is referenced there.  This method removes
        /// the window from any parents that it belongs to.  The two methods together will clear all engine
        /// references to the Window.
        /// </remarks>
        /// <param name="keepEvents">Whether to keep all events.</param>
        #endregion
        internal protected virtual void Destroy(bool keepEvents)
        {
            if (this.SpriteFrame != null)
            {
                SpriteManager.RemoveSpriteFrame(SpriteFrame);
            }

            if (mMoveBarSpriteFrame != null)
            {
                SpriteManager.RemoveSpriteFrame(mMoveBarSpriteFrame);
            }

            if (keepEvents == false)
                ClearEvents();

            while (mChildren.Count != 0)
            {
                ((Window)mChildren[mChildren.Count - 1]).Destroy(keepEvents);
            }
            while (mFloatingWindows.Count != 0)
            {
                ((Window)mFloatingWindows[mFloatingWindows.Count - 1]).Destroy(keepEvents);
            }

            if (mParentWindow != null)
            {
                mParentWindow.RemoveWindow(this);
            }

            if (mFloatingParentWindow != null)
            {
                mFloatingParentWindow.RemoveWindow(this);
            }



            if (InputManager.ReceivingInput == this)
                InputManager.ReceivingInput = null;
        }


        internal virtual void Destroy()
        {
            Destroy(false);

        }


        internal virtual void DrawSelfAndChildren(Camera camera)
        {
#if !SILVERLIGHT
            if (Visible == false)
                return;

            float xToUse = (mWorldUnitX);
            float yToUse = (mWorldUnitY);

            StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = camera.Z + 
                FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;

            StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;

            #region draw the main window

            #region Textured Center
            if (mBaseTexture != null)
            {
                GuiManager.AddTextureSwitch(mBaseTexture);

                float borders = 0;

                if (mDrawBorders)
                {
                    borders = BorderWidth;
                }

                StaticVertices[0].Position.X = xToUse - ScaleX + borders;
                StaticVertices[0].Position.Y = yToUse - ScaleY + borders;
                StaticVertices[0].TextureCoordinate.X = mTextureLeft;
                StaticVertices[0].TextureCoordinate.Y = mTextureBottom;


                StaticVertices[1].Position.X = xToUse - ScaleX + borders;
                StaticVertices[1].Position.Y = yToUse + ScaleY - borders;
                StaticVertices[1].TextureCoordinate.X = mTextureLeft;
                StaticVertices[1].TextureCoordinate.Y = mTextureTop;

                StaticVertices[2].Position.X = xToUse + ScaleX - borders;
                StaticVertices[2].Position.Y = yToUse + ScaleY - borders;
                StaticVertices[2].TextureCoordinate.X = mTextureRight;
                StaticVertices[2].TextureCoordinate.Y = mTextureTop;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - borders;
                StaticVertices[5].Position.Y = yToUse - ScaleY + borders;
                StaticVertices[5].TextureCoordinate.X = mTextureRight;
                StaticVertices[5].TextureCoordinate.Y = mTextureBottom;

                GuiManager.WriteVerts(StaticVertices);
            }
            #endregion

            #region else, untextured center
            else
            {

                StaticVertices[0].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[0].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[0].TextureCoordinate.X = centerLeft;
                StaticVertices[0].TextureCoordinate.Y = centerBottom;


                StaticVertices[1].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[1].Position.Y = yToUse + ScaleY - .4f;
                StaticVertices[1].TextureCoordinate.X = centerLeft;
                StaticVertices[1].TextureCoordinate.Y = centerTop;

                StaticVertices[2].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[2].Position.Y = yToUse + ScaleY - .4f;
                StaticVertices[2].TextureCoordinate.X = centerRight;
                StaticVertices[2].TextureCoordinate.Y = centerTop;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[5].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[5].TextureCoordinate.X = centerRight;
                StaticVertices[5].TextureCoordinate.Y = centerBottom;

                GuiManager.WriteVerts(StaticVertices);

            }
            #endregion

            if (mDrawBorders)
            {

                #region RightBorder
                StaticVertices[0].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[0].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[0].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[0].TextureCoordinate.Y = horizontalBottom;

                StaticVertices[1].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[1].Position.Y = yToUse + ScaleY - BorderWidth;
                StaticVertices[1].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[1].TextureCoordinate.Y = horizontalBottom - horizontalPixelWidth;


                StaticVertices[2].Position.X = xToUse + ScaleX;
                StaticVertices[2].Position.Y = yToUse + ScaleY - BorderWidth;
                StaticVertices[2].TextureCoordinate.X = horizontalLeft;
                StaticVertices[2].TextureCoordinate.Y = horizontalBottom - horizontalPixelWidth;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX;
                StaticVertices[5].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[5].TextureCoordinate.X = horizontalLeft;
                StaticVertices[5].TextureCoordinate.Y = horizontalBottom;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Bottom Right of the Window
                StaticVertices[0].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                StaticVertices[0].TextureCoordinate.Y = cornerBottom;

                StaticVertices[1].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[1].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[1].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                StaticVertices[1].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                StaticVertices[2].Position.X = xToUse + ScaleX;
                StaticVertices[2].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[2].TextureCoordinate.X = cornerLeft;
                StaticVertices[2].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = cornerLeft;
                StaticVertices[5].TextureCoordinate.Y = cornerBottom;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Bottom Border

                StaticVertices[0].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = horizontalLeft;
                StaticVertices[0].TextureCoordinate.Y = horizontalBottom;

                StaticVertices[1].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[1].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[1].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[1].TextureCoordinate.Y = horizontalBottom;

                StaticVertices[2].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[2].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[2].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[2].TextureCoordinate.Y = horizontalBottom - .001f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - BorderWidth;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = horizontalLeft;
                StaticVertices[5].TextureCoordinate.Y = horizontalBottom - .001f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region LeftBorder
                StaticVertices[0].Position.X = xToUse - ScaleX;
                StaticVertices[0].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[0].TextureCoordinate.X = horizontalLeft;
                StaticVertices[0].TextureCoordinate.Y = horizontalBottom;

                StaticVertices[1].Position.X = xToUse - ScaleX;
                StaticVertices[1].Position.Y = yToUse + ScaleY - BorderWidth;
                StaticVertices[1].TextureCoordinate.X = horizontalLeft;
                StaticVertices[1].TextureCoordinate.Y = horizontalBottom - .001f;

                StaticVertices[2].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[2].Position.Y = yToUse + ScaleY - BorderWidth;
                StaticVertices[2].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[2].TextureCoordinate.Y = horizontalBottom - .001f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[5].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[5].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                StaticVertices[5].TextureCoordinate.Y = horizontalBottom;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Bottom Left of the Window

                StaticVertices[0].Position.X = xToUse - ScaleX;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = cornerLeft;
                StaticVertices[0].TextureCoordinate.Y = cornerBottom;

                StaticVertices[1].Position.X = xToUse - ScaleX;
                StaticVertices[1].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[1].TextureCoordinate.X = cornerLeft;
                StaticVertices[1].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                StaticVertices[2].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[2].Position.Y = yToUse - ScaleY + BorderWidth;
                StaticVertices[2].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                StaticVertices[2].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];


                StaticVertices[5].Position.X = xToUse - ScaleX + BorderWidth;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                StaticVertices[5].TextureCoordinate.Y = cornerBottom;

                GuiManager.WriteVerts(StaticVertices);

                #endregion


                #region draw the move bar

                if (HasMoveBar)
                {
                    #region Left Side
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - .4f;
                    StaticVertices[0].TextureCoordinate.X = 0;
                    StaticVertices[0].TextureCoordinate.Y = .6953125f;


                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[1].TextureCoordinate.X = 0;
                    StaticVertices[1].TextureCoordinate.Y = .570313f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + .6f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[2].TextureCoordinate.X = .0234375f;
                    StaticVertices[2].TextureCoordinate.Y = .570313f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + .6f;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - .4f;
                    StaticVertices[5].TextureCoordinate.X = .0234375f;
                    StaticVertices[5].TextureCoordinate.Y = .6953125f;

                    GuiManager.WriteVerts(StaticVertices);

                    #endregion

                    #region Title Bar Center
                    StaticVertices[0].Position.X = xToUse - ScaleX + .59f;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[0].TextureCoordinate.X = .01953125f;
                    StaticVertices[0].TextureCoordinate.Y = .6953125f;


                    StaticVertices[1].Position.X = xToUse - ScaleX + .59f;
                    StaticVertices[1].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[1].TextureCoordinate.X = .01953125f;
                    StaticVertices[1].TextureCoordinate.Y = .570313f;

                    StaticVertices[2].Position.X = xToUse + ScaleX - .59f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[2].TextureCoordinate.X = .0234375f;
                    StaticVertices[2].TextureCoordinate.Y = .570313f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX - .59f;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[5].TextureCoordinate.X = .0234375f;
                    StaticVertices[5].TextureCoordinate.Y = .6953125f;

                    GuiManager.WriteVerts(StaticVertices);

                    #endregion

                    #region Right Side
                    StaticVertices[0].Position.X = xToUse + ScaleX - .6f;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - .4f;
                    StaticVertices[0].TextureCoordinate.X = .0234375f;
                    StaticVertices[0].TextureCoordinate.Y = .6953125f;


                    StaticVertices[1].Position.X = xToUse + ScaleX - .6f;
                    StaticVertices[1].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[1].TextureCoordinate.X = .0234375f;
                    StaticVertices[1].TextureCoordinate.Y = .570313f;


                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse + ScaleY + 2.8f;
                    StaticVertices[2].TextureCoordinate.X = 0;
                    StaticVertices[2].TextureCoordinate.Y = .570313f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - .4f;
                    StaticVertices[5].TextureCoordinate.X = 0;
                    StaticVertices[5].TextureCoordinate.Y = .6953125f;

                    GuiManager.WriteVerts(StaticVertices);

                    #endregion

                    #region title bar text
                    if (mName != "")
                    {
                        TextManager.mXForVertexBuffer = xToUse - ScaleX + 1.3f;
                        TextManager.mYForVertexBuffer = yToUse + ScaleY + 1.1f;

    #if FRB_MDX
                        TextManager.mZForVertexBuffer = camera.Z + 100; ;
    #else
                        TextManager.mZForVertexBuffer = camera.Z - 100; ;
    #endif
                        TextManager.mRedForVertexBuffer = GraphicalEnumerations.MaxColorComponentValue;
                        TextManager.mGreenForVertexBuffer = GraphicalEnumerations.MaxColorComponentValue;
                        TextManager.mBlueForVertexBuffer = GraphicalEnumerations.MaxColorComponentValue;
                        TextManager.mAlphaForVertexBuffer = GraphicalEnumerations.MaxColorComponentValue;
                        TextManager.mMaxWidthForVertexBuffer = float.PositiveInfinity;

                        TextManager.mAlignmentForVertexBuffer = HorizontalAlignment.Left;

                        TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
                        TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;

                        string title = ConcatenatedTitle;

                        TextManager.Draw(ref title);
                    }
                    #endregion

                }
                else
                {

                    #region Top Left of the Window
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[0].TextureCoordinate.X = cornerLeft;
                    StaticVertices[0].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = cornerLeft;
                    StaticVertices[1].TextureCoordinate.Y = cornerBottom;

                    StaticVertices[2].Position.X = xToUse - ScaleX + BorderWidth;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                    StaticVertices[2].TextureCoordinate.Y = cornerBottom;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + BorderWidth;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[5].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                    StaticVertices[5].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                    GuiManager.WriteVerts(StaticVertices);

                    #endregion

                    #region Top Border

                    StaticVertices[0].Position.X = xToUse - ScaleX + BorderWidth;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[0].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                    StaticVertices[0].TextureCoordinate.Y = horizontalBottom;

                    StaticVertices[1].Position.X = xToUse - ScaleX + BorderWidth;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = horizontalLeft;
                    StaticVertices[1].TextureCoordinate.Y = horizontalBottom;

                    StaticVertices[2].Position.X = xToUse + ScaleX - BorderWidth;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = horizontalLeft;
                    StaticVertices[2].TextureCoordinate.Y = horizontalBottom - .004f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX - BorderWidth;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[5].TextureCoordinate.X = horizontalLeft + horizontalPixelWidth;
                    StaticVertices[5].TextureCoordinate.Y = horizontalBottom - .004f;

                    GuiManager.WriteVerts(StaticVertices);

                    #endregion

                    #region Top Right of the Window
                    StaticVertices[0].Position.X = xToUse + ScaleX - BorderWidth;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[0].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                    StaticVertices[0].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                    StaticVertices[1].Position.X = xToUse + ScaleX - BorderWidth;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = cornerLeft + cornerPixelWidth;
                    StaticVertices[1].TextureCoordinate.Y = cornerBottom;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = cornerLeft;
                    StaticVertices[2].TextureCoordinate.Y = cornerBottom;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - BorderWidth;
                    StaticVertices[5].TextureCoordinate.X = cornerLeft;
                    StaticVertices[5].TextureCoordinate.Y = cornerBottom - cornerPixelWidth;

                    GuiManager.WriteVerts(StaticVertices);
                    #endregion

                }
                #endregion
            }
            #endregion

            #region Draw all children
            for (int i = 0; i < mChildren.Count; i++)
            {
                Window asWindow = mChildren[i] as Window;

                if (asWindow != null && asWindow.Visible)
                {
                    asWindow.DrawSelfAndChildren(camera);
                }
            }
            #endregion

#endif
        }


#if !SILVERLIGHT
        internal virtual void DrawFloatingWindows(Camera camera)
        {
            for (int i = 0; i < mFloatingWindows.Count; i++)
            {
                Window asWindow = mFloatingWindows[i] as Window;

                if (asWindow != null && asWindow.Visible)
                {
                    asWindow.DrawSelfAndChildren(camera);
                    asWindow.DrawFloatingWindows(camera);
                }
            }            
            
            
            for (int i = 0; i < mChildren.Count; i++)
            {
                Window asWindow = mChildren[i] as Window;

                if (asWindow.Visible)
                    asWindow.DrawFloatingWindows(camera);
            }
            

        }

#endif


        internal virtual int GetNumberOfVerticesToDraw()
        {
#if !SILVERLIGHT 
            int numberOfVertices = 0;

            if (mDrawBorders)
                numberOfVertices += NinePieceNumberOfVertices;
            else
                numberOfVertices += OnePieceNumberOfVertices;

            numberOfVertices = DrawMoveBar(numberOfVertices);


            foreach (Window w in this.mChildren)
                if (w.Visible)
                    numberOfVertices += w.GetNumberOfVerticesToDraw();


            return numberOfVertices;
#else
            return 0;
#endif
        }

#if !SILVERLIGHT

        private int DrawMoveBar(int numberOfVertices)
        {
            if (HasMoveBar)
            {
                string title = ConcatenatedTitle;
                for (int letter = 0; letter < title.Length; letter++)
                {
                    if (title[letter] != ' ')
                    {
                        numberOfVertices += 6;
                    }
                }
            }
            return numberOfVertices;
        }
#endif


        internal int GetNumberOfVerticesToDrawFloating()
        {
        
#if !SILVERLIGHT
            int numberOfVertices = 0;

            foreach (Window w in this.mFloatingWindows)
            {
                if (w.Visible)
                {
                    numberOfVertices += w.GetNumberOfVerticesToDraw();
                    numberOfVertices += w.GetNumberOfVerticesToDrawFloating();
                }
            }

            foreach (Window w in this.mChildren)
                if (w.Visible)
                    numberOfVertices += w.GetNumberOfVerticesToDrawFloating();

            return numberOfVertices;
#else 
            return 0;
#endif
        }


        #region XML Docs
        /// <summary>
        /// Calls events for clicking, pushing, dragging, and any other window events.
        /// </summary>
        /// <remarks>
        /// This method is called either by this instance's parent Window or the GuiManager.
        /// </remarks>
        /// <param name="cursor">The Cursor to use for collision.</param>
        #endregion
        public virtual void TestCollision(Cursor cursor)
        {
            TestCollisionBase(cursor);
        }

        #region XML Docs
        /// <summary>
        /// Provides acces to base TestCollision functionality for objects which derive from the
        /// Window class.
        /// </summary>
        /// <remarks>
        /// Sometimes a Window is separated by one or more layers of inheritance from the
        /// base Window class.  In these cases the Window may want to call the base TestCollision
        /// method without going through the immediate class it inherits from.
        /// 
        /// For example, the Button class inherits from the Window class.  The ToggleButtonClass inherits
        /// from the Button class.  Since the Button class needs to perform different functionality than the
        /// Button class when the user interacts with it with the Cursor it needs to be able to "step over" the
        /// Button's TestCollision call.
        /// </remarks>
        /// <param name="cursor">The Cursor to use for interaction with this element.</param>
        #endregion
        protected void TestCollisionBase(Cursor cursor)
        {
            if (IgnoredByCursor)
                return;

            cursor.WindowOver = this;

            #region See if the cursor is over the bottom-right and set isOverBottomRight to true if so

            bool isOverBottomRight = false;


            if (Resizable)
            {
                #region SpriteFrame used for drawing
                if (SpriteFrame != null)
                {
                    float bottom = SpriteFrame.Y - SpriteFrame.ScaleY;
                    float right = SpriteFrame.X + SpriteFrame.ScaleX;

                    float maximumDistance = 20/SpriteManager.Camera.PixelsPerUnitAt(
                        SpriteFrame.Z) ;

                    

                    if (System.Math.Abs(cursor.WorldXAt(SpriteFrame.Z) - right) < maximumDistance &&
                        System.Math.Abs(cursor.WorldYAt(SpriteFrame.Z) - bottom) < maximumDistance)
                    {
                        isOverBottomRight = true;
                    }
                }
                #endregion

                #region Default GuiManager rendering
                else
                {
                    if (mWorldUnitX + mScaleX - (cursor.XForUI + cursor.tipXOffset) < 1 &&
                       (cursor.YForUI + cursor.tipYOffset) - (mWorldUnitY - mScaleY) < 1)
                    {
                        // the actual resizing of the window is performed in the Cursor.Control method
                        isOverBottomRight = true;
                    }
                }
                #endregion
            }
            #endregion

            #region loop through all of the floating windows and exit the method if on a floating window

            for (int i = mFloatingWindows.Count - 1; i > -1; i--)
            {
                if (mFloatingWindows[i].Visible == false || !mFloatingWindows[i].Enabled)
                    continue;

                if (mFloatingWindows[i].GuiManagerDrawn)
                {
                    if (cursor.IsOnWindowOrFloatingChildren(mFloatingWindows[i]))
                    {
                        mFloatingWindows[i].TestCollision(cursor);
                    }
                }
                else if (cursor.IsOn(mFloatingWindows[i].SpriteFrame.mCenter))
                {
                    mFloatingWindows[i].TestCollision(cursor);
                }
                // as soon as we enter this method the windowResult is set to this.
                // The following if statement should be triggered only if the cursor
                // is over one of the floating windows.
                if (cursor.WindowOver != this)
                {
                    if (cursor.WindowOver == mCloseButton && cursor.PrimaryClick)
                    {
                        cursor.WindowClosing = this;
                        CloseWindow();

                    }
                    return;
                }
            }
            #endregion

            #region loop through all children Windows and exit method if we are on a child
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Visible == false || !mChildren[i].Enabled)
                    continue;

                bool isOnWindow = false;

                if (mChildren[i].GuiManagerDrawn)
                {
                    if (cursor.IsOnWindowOrFloatingChildren(mChildren[i]))
                    {
                        mChildren[i].TestCollision(cursor);
                        isOnWindow = true;
                    }
                }
                else if (cursor.IsOn(mChildren[i].SpriteFrame))
                {
                    mChildren[i].TestCollision(cursor);
                    isOnWindow = true;
                }
                if (isOnWindow)
                {
                    if (cursor.WindowOver == mCloseButton && cursor.PrimaryClick)
                    {
                        cursor.WindowClosing = this;
                        CloseWindow();

                    }
                    return;
                }
            }
            #endregion

            // if we got here, that means we are not on any children or floating Windows

            // as long as the window's enabled, then the roll on works
            cursor.LastWindowOver = this;

            #region If the cursor's primaryPush is true
            if (cursor.PrimaryPush)
            {
                cursor.mSidesGrabbed = Sides.None;
                cursor.WindowPushed = this;

                if (IsCursorOnMoveBar(cursor))
                { // grab the window
                    cursor.GrabWindow(this);
                }

                // Some activities should occur on a Window push but not when resizing.  Therefore,
                // determine which side is being grabbed before the onPush is called so the user
                // can check the cursor's ResizingWindow property and get an accurate value.
                if (isOverBottomRight)
                {
                    cursor.mSidesGrabbed = Sides.BottomRight;
                }

                if (Push != null)
                    Push(this);


            }
            #endregion

            #region If the cursor's primaryClick is true
            if (cursor.PrimaryClick) // both pushing and clicking can occur in one frame because of buffered input
            {
                if (cursor.WindowPushed == this)
                {
                    OnClick();

                    if (cursor.PrimaryDoubleClick && DoubleClick != null)
                        DoubleClick(this);
                }
            }
            #endregion

            #region If the cursor's secondaryPush is true
            if (cursor.SecondaryPush)
            {
                cursor.mWindowSecondaryPushed = this;
            }
            #endregion

#if SUPPORTS_FRB_DRAWN_GUI
            if (cursor.MiddlePush)
            {
                cursor.WindowMiddleButtonPushed = this;
            }
#endif
            #region If the cursor's secondaryClick is true

            if (cursor.SecondaryClick)
            {
                if (cursor.mWindowSecondaryPushed == this)
                {
                    if (SecondaryClick != null)
                        SecondaryClick(this);
                    cursor.mWindowSecondaryPushed = null;
                }

            }

            #endregion

#if SUPPORTS_FRB_DRAWN_GUI
            if (cursor.MiddleClick)
            {
                if (cursor.WindowMiddleButtonPushed == this)
                {

                    // raise a middle click event
                    cursor.WindowMiddleButtonPushed = null;
                }
            }
#endif

            #region If the Mouse wheel is spinning

            if (cursor.ZVelocity != 0 && MouseWheelScroll != null)
            {
                MouseWheelScroll(this);
            }	

            #endregion

            #region Raise Dragging if the cursor is down over the Window, the window was originally pushed, and the cursor is moving
            if (cursor.PrimaryDown == true && 
                cursor.WindowPushed == this && (cursor.XVelocity != 0 || cursor.YVelocity != 0) && this.Dragging != null)
                Dragging(this);

            #endregion

#if !XBOX360 && !SILVERLIGHT && ! WINDOWS_PHONE && !MONODROID
            if (isOverBottomRight)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
            }
#endif

            if (CursorOver != null)
            {
                CursorOver(this);
            }

            if (cursor.XVelocity != 0 || cursor.YVelocity != 0)
            {
                if (RollingOver != null)
                    RollingOver(this);
            }
        }

        #endregion

        #region Private Methods


        #endregion

        #endregion

        #region IVisible Members

        bool IVisible.Visible
        {
            get
            {
                return this.Visible;
            }
            set
            {
                this.Visible = value;
            }
        }

        IVisible IVisible.Parent
        {
            get { return this.Parent as IVisible; }
        }

        bool IVisible.AbsoluteVisible
        {
            get 
            {
                IVisible parentAsIVisible = this.Parent as IVisible;

                if (IgnoresParentVisibility || parentAsIVisible == null)
                {
                    return this.Visible;
                }
                else
                {
                    return this.Visible && parentAsIVisible.AbsoluteVisible;
                }
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }

        #endregion
    }
}

