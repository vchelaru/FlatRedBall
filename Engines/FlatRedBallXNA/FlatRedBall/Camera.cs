
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


using Microsoft.Xna.Framework.Content;

using FlatRedBall.Math;
using FlatRedBall.Graphics;
using System.Diagnostics;
using FlatRedBall.Math.Geometry;


namespace FlatRedBall
{
    public partial class Camera : PositionedObject
    {
        #region Enums
        public enum CoordinateRelativity
        {
            RelativeToWorld,
            RelativeToCamera
        }

        public enum SplitScreenViewport
        {
            FullScreen,
            TopHalf,
            BottomHalf,
            LeftHalf,
            RightHalf,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,

            LeftThird,
            MiddleThird,
            RightThird
        }

        #endregion

        #region Fields

        /// <summary>
        /// A Vector3 representing the "Up" orientation. The camera will adjust its rotation so that this vector
        /// is up. This enables 3D games (such as first person shooters) to rotate the camera yet still have a natural-feeling
        /// up vector. 
        /// </summary>
        public Vector3 UpVector = new Vector3(0, 1, 0);

        static int sCreatedCount = 0;

        float mFarClipPlane;
        float mNearClipPlane;

        Matrix mView;
        Matrix mViewRelative;
        Matrix mProjection;
        Matrix mViewProjection;

        float mMinimumX;
        float mMinimumY;
        float mMaximumX;
        float mMaximumY;

        float mBaseZ;
        float mBaseMinimumX;
        float mBaseMinimumY;
        float mBaseMaximumX;
        float mBaseMaximumY;

        bool mOrthogonal;
        float mOrthogonalWidth;
        float mOrthogonalHeight;

#if SUPPORTS_POST_PROCESSING
        ShadowMap mShadow = null;
        
        internal PostProcessingEffectCollection mPostProcessing;

        #region XML Docs
        /// <summary>
        /// Defines the rendering order for this camera
        /// </summary>
        #endregion
        public List<RenderMode> RenderOrder;



        #region XML Docs
        /// <summary>
        /// List of rendered textures (during render pass)
        /// </summary>
        #endregion
        internal Dictionary<int, RenderTargetTexture> mRenderTargetTextures;

        #region XML Docs
        /// <summary>
        /// The final render for this camera (after post-processing)
        /// </summary>
        #endregion
        internal RenderTargetTexture mRenderTargetTexture;

        #region XML Docs
        /// <summary>
        /// Whether or not this camera should be drawn to the screen
        /// </summary>
        #endregion
        public bool DrawToScreen = true;
        bool mClearsTargetDefaultRenderMode = true;
#endif

        BoundingFrustum mBoundingFrustum;

        CameraCullMode mCameraCullMode;
        CameraModelCullMode mCameraModelCullMode;

        #region Viewport settings



        //internal int mTargetWidth;
        //internal int mTargetHeight;

        public Color BackgroundColor =
            new Color(0,0,0,0);

        SplitScreenViewport splitScreenViewport;
        public SplitScreenViewport CurrentSplitScreenViewport => splitScreenViewport;

        bool mUsesSplitScreenViewport = false;

        #endregion

        List<Layer> mLayers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;

        //internal SpriteList mSpritesToBillBoard = new SpriteList();

        string mContentManager;

        /// <summary>
        /// Whether or not lighting is enabled for this camera
        /// </summary>
        internal bool mLightingEnabled = false;

        float mYEdge;
        float mXEdge;

        float mTopDestination;
        float mBottomDestination;
        float mLeftDestination;
        float mRightDestination;

        internal Rectangle mDestinationRectangle;

        float mTopDestinationVelocity;
        float mBottomDestinationVelocity;
        float mLeftDestinationVelocity;
        float mRightDestinationVelocity;


        float mFieldOfView;
        float mAspectRatio;

        bool mDrawsWorld = true;
        bool mDrawsCameraLayer = true;
        bool mDrawsShapes = true;

        #endregion

        #region Properties

        /// <summary>
        /// Sets whether the Camera will prevent viewports from being larger than the resolution. This value defaults to true.
        /// </summary>
        /// <remarks>
        /// The purpose of this value is to prevent cameras from attempting to draw outside of the window's client bounds. A camera
        /// which has a viewport larger than the window client bounds will throw an exception. However, cameras (and layers) which render
        /// to a render target which is larger than the current window should be able to render to the full render target even if it is larger
        /// than the current window. Therefore, this value should be set to false if rendering to large render targets.
        /// </remarks>
        public bool ShouldRestrictViewportToResolution
        {
            get;
            set;
        }

        public Matrix View
        {
            get { return mView; }// GetLookAtMatrix(false); }
        }

        public Matrix Projection
        {
            get { return mProjection; }// GetProjectionMatrix(); }
        }

        public BoundingFrustum BoundingFrustum
        {
            get { return mBoundingFrustum; }
        }

        public CameraCullMode CameraCullMode
        {
            get { return mCameraCullMode; }
            set { mCameraCullMode = value; }
        }

        public CameraModelCullMode CameraModelCullMode
        {
            get { return mCameraModelCullMode; }
            set { mCameraModelCullMode = value; }
        }

        /// <summary>
        /// The Y field of view of the camera in radians.  Field of view represents the 
        /// Y angle from the bottom of the screen to the top.
        /// </summary>
        /// <remarks>
        /// This modifies the xEdge and yEdge properties.  Default value is (float)Math.PI / 4.0f;
        /// </remarks>
        public virtual float FieldOfView
        {
            get { return mFieldOfView; }
            set
            {
#if DEBUG
                if (value >= (float)System.Math.PI)
                {
                    throw new ArgumentException("FieldOfView must be smaller than PI.");
                }
                if (value <= 0)
                {
                    throw new ArgumentException("FieldOfView must be greater than 0.");

                }
#endif

                mFieldOfView = value;
                mYEdge = (float)(100 * System.Math.Tan(mFieldOfView / 2.0));
                mXEdge = mYEdge * mAspectRatio;

#if !SILVERLIGHT
                UpdateViewProjectionMatrix();
#endif

            }
        }

        /// <summary>
        /// A Camera-specific layer.  Objects on this layer will not appear
        /// in any other cameras.
        /// </summary>
        /// <remarks>
        /// This instance is automatically created when the Camera is instantiated.
        /// </remarks>
        public Layer Layer
        {
            get { return mLayers[0]; }
        }


        public ReadOnlyCollection<Layer> Layers
        {
            get { return mLayersReadOnly; }
        }

        /// <summary>
        /// The Minimum camera X (center). This is applied prior to rendering and will override attachment.
        /// </summary>
        public float MinimumX
        {
            get { return mMinimumX; }
            set { mMinimumX = value; }
        }
        
        /// <summary>
        /// The Minimum camera Y (center). This is applied prior to rendering and will override attachment.
        /// </summary>
        public float MinimumY
        {
            get { return mMinimumY; }
            set { mMinimumY = value; }
        }

        /// <summary>
        /// The maximum Camera X (center). This is applied prior to rendering and will override attachment.
        /// </summary>
        public float MaximumX
        {
            get { return mMaximumX; }
            set { mMaximumX = value; }
        }

        /// <summary>
        /// The maximum Camera Y (center). This is applied prior to rendering and will override attachment.
        /// </summary>
        public float MaximumY
        {
            get { return mMaximumY; }
            set { mMaximumY = value; }
        }


        public float NearClipPlane
        {
            get { return mNearClipPlane; }
            set { mNearClipPlane = value; }
        }


        public float FarClipPlane
        {
            get { return mFarClipPlane; }
            set { mFarClipPlane = value; }
        }



        public float XEdge
        {
            get { return mXEdge; }
        }


        public float YEdge
        {
            get { return mYEdge; }
        }

        public float TopDestinationVelocity
        {
            get { return mTopDestinationVelocity; }
            set { mTopDestinationVelocity = value;  }
        }
        public float BottomDestinationVelocity
        {
            get { return mBottomDestinationVelocity; }
            set { mBottomDestinationVelocity = value;  }
        }
        public float LeftDestinationVelocity
        {
            get { return mLeftDestinationVelocity; }
            set { mLeftDestinationVelocity = value;  }
        }
        public float RightDestinationVelocity
        {
            get { return mRightDestinationVelocity; }
            set { mRightDestinationVelocity = value;  }
        }

        /// <summary>
        /// The absolute X value of the right edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteRightXEdge
        {
            get => AbsoluteRightXEdgeAt(0);
            set
            {
                if (this.Orthogonal)
                {
                    X = value - OrthogonalWidth / 2.0f;
                }
                else
                {
                    throw new NotImplementedException("Setting the AbsoluteRightXEdge is not supported for perspective cameras.")
                }
            }
        }


        /// <summary>
        /// Returns the right-most visible X value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the right-edge.</param>
        /// <returns>The furthest-right visible X value at the given absolute Z.</returns>
        public float AbsoluteRightXEdgeAt(float absoluteZ)
        {
            return Position.X + RelativeXEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute X value of the left edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteLeftXEdge
        {
            get => AbsoluteLeftXEdgeAt(0);
            set
            {
                if (this.Orthogonal)
                {
                    X = value + OrthogonalWidth / 2.0f;
                }
                else
                {
                    throw new NotImplementedException("Setting the AbsoluteLeftXEdge is not supported for perspective cameras.");
                }
            }
        }

        /// <summary>
        /// Returns the left-most visible X value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determing the left-edge.</param>
        /// <returns>The furthest-left visible X value at the given absolute Z.</returns>
        public float AbsoluteLeftXEdgeAt(float absoluteZ)
        {
            return Position.X - RelativeXEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute Y value of the top edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteTopYEdge
        {
            get => AbsoluteTopYEdgeAt(0);
            set
            {
                if (this.Orthogonal)
                {
                    Y = value - OrthogonalHeight / 2.0f;
                }
                else
                {
                    throw new NotImplementedException("Setting the AbsoluteTopYEdge is not supported for perspective cameras.");
                }
            }
        }

        /// <summary>
        /// Returns the top-most visible Y value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the top-edge.</param>
        /// <returns>The furthest-top visible Y value at the given absolute Z.</returns>
        public float AbsoluteTopYEdgeAt(float absoluteZ)
        {
            return Position.Y + RelativeYEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute Y value of the bottom edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteBottomYEdge
        {
            get => AbsoluteBottomYEdgeAt(0);
            set
            {
                if (this.Orthogonal)
                {
                    Y = value + OrthogonalHeight / 2.0f;
                }
                else
                {
                    throw new NotImplementedException("Setting the AbsoluteBottomYEdge is not supported for perspective cameras.");
                }
            }
        }

        /// <summary>
        /// Returns the bottom-most visible Y value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the bottom-edge.</param>
        /// <returns>The furthest-bottom visible Y value at the given absolute Z.</returns>
        public float AbsoluteBottomYEdgeAt(float absoluteZ)
        {
            return Position.Y - RelativeYEdgeAt(absoluteZ);
        }

        /// <summary>
        /// The number of horizontal units shown by the camera when the camera has Orthogonal = true
        /// </summary>
        /// <remarks>
        /// Orthogonal values will not have any impact on rendering if Orthogonal is false.
        /// </remarks>
        public float OrthogonalWidth
        {
            get { return mOrthogonalWidth; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new Exception("OrthogonalWidth must be positive");
                }
#endif
                mOrthogonalWidth = value;
            }
        }

        /// <summary>
        /// The number of vertical units shown by the camera when the camera has Orthogonal = true 
        /// </summary>
        /// <remarks>
        /// Orthogonal values will not have any impact on rendering if Orthogonal is false.
        /// </remarks>
        public float OrthogonalHeight
        {
            get { return mOrthogonalHeight; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new Exception("OrthogonalHeight must be positive");
                }
#endif
                mOrthogonalHeight = value;
            }
        }

        public static Camera Main
        {
            get
            {
                return SpriteManager.Camera;
            }
        }

        /// <summary>
        /// Gets and sets the top side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.  Destination uses an inverted Y (positive points down).
        /// </summary>
        public virtual float TopDestination
        {
            get { return mTopDestination; }
            set
            {
                mTopDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        /// <summary>
        /// Gets and sets the bottom side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.   Destination uses an inverted Y (positive points down).
        /// </summary>
        public virtual float BottomDestination
        {
            get { return mBottomDestination; }
            set
            {
                mBottomDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        /// <summary>
        /// Gets and sets the left side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.
        /// </summary>
        public virtual float LeftDestination
        {
            get { return mLeftDestination; }
            set
            {
                mLeftDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        /// <summary>
        /// Gets and sets the right side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.
        /// </summary>
        public virtual float RightDestination
        {
            get { return mRightDestination; }
            set
            {
                mRightDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        /// <summary>
        /// Represents the top left justified area the Camera will draw over.
        /// </summary>
        /// <remarks>
        /// This represents the area in pixel coordinates that the camera will display relative
        /// to the top left of the owning Control.  If the Control is resized, the camera should modify
        /// its DestinationRectangle to match the new area.
        /// 
        /// <para>
        /// Multiple cameras with different DestinationRectangles can be used to display split screen
        /// or picture-in-picture.
        /// </para>
        /// </remarks>
        public virtual Rectangle DestinationRectangle
        {
            get
            {
                return mDestinationRectangle;
            }
            set
            {
                mUsesSplitScreenViewport = false;

                mDestinationRectangle = value;

                mTopDestination = mDestinationRectangle.Top;
                mBottomDestination = mDestinationRectangle.Bottom;
                mLeftDestination = mDestinationRectangle.Left;
                mRightDestination = mDestinationRectangle.Right;

                FixAspectRatioYConstant();
            }
        }

        public bool ClearsDepthBuffer
        {
            get;
            set;
        }

        /// <summary>
        /// The width/height of the view of the camera
        /// </summary>
        /// <remarks>
        /// This determines the ratio of the width to height of the camera.  By default, the aspect ratio is 4/3,
        /// but this should be changed for widescreen monitors or in situations using multiple cameras.  For example, if
        /// a game is in split screen with a vertical split, then each camera will show the same height, but half the width.
        /// The aspect ratio should be 2/3.
        /// </remarks>
        public float AspectRatio
        {
            get { return mAspectRatio; }
            set
            {
                mAspectRatio = value;
                mXEdge = mYEdge * AspectRatio;
                // The user may expect AspectRatio to work when in 2D mode
                if (mOrthogonal)
                {
                    mOrthogonalWidth = mOrthogonalHeight * mAspectRatio;
                }
            }
        }


        /// <summary>
        /// Returns whether the camera is using an orthogonal perspective. If true, the camera is a "2D" camera.
        /// </summary>
        public bool Orthogonal
        {
            get => mOrthogonal;
            set => mOrthogonal = value;
        }


        /// <summary>
        /// Whether the camera draws its layers.
        /// </summary>
        public bool DrawsCameraLayer
        {
            get { return mDrawsCameraLayer; }
            set { mDrawsCameraLayer = value; }
        }

        /// <summary>
        /// Whether the Camera draws world objects (objects not on the Camera's Layer). This is true by default.
        /// This is usually set to false for cameras used in render targets which only draw layers.
        /// </summary>
        public bool DrawsWorld
        {
            get { return mDrawsWorld; }
            set { mDrawsWorld = value; }
        }

        /// <summary>
        /// Whether the Camera draws shapes
        /// </summary>
        public bool DrawsShapes
        {
            get { return mDrawsShapes; }
            set { mDrawsShapes = value; }
        }

        /// <summary>
        /// Whether this camera draws its contents to the screen. By default this is true.
        /// </summary>
        public bool DrawsToScreen
        {
            get;
            set;
        }

        #endregion

        public bool ShiftsHalfUnitForRendering
        {
            get
            {
                // This causes all kinds of jitteryness when attached to an object, so we should make sure
                // the camera is not attached to anything:

                return Parent == null &&
                    mOrthogonal && (this.mOrthogonalWidth / (float)this.DestinationRectangle.Width == 1);
            }



        }

        #region Constructor

        /// <summary>
        /// Creates a new camera instance. This camera will not be drawn by the engine until it is added
        /// through the SpriteManager.
        /// </summary>
        public Camera () : this(null)
        {

        }


        public Camera(string contentManagerName) : this(
            contentManagerName,
            FlatRedBallServices.ClientWidth,
            FlatRedBallServices.ClientHeight)
        {
            SetSplitScreenViewport(SplitScreenViewport.FullScreen);

            ShouldRestrictViewportToResolution = true;
        }

        public Camera(string contentManagerName, int width, int height)
            : base()
        {
            ShouldRestrictViewportToResolution = true;

            ClearsDepthBuffer = true;

            mContentManager = contentManagerName;

            DrawsToScreen = true;

            ClearMinimumsAndMaximums();

            // set the borders to float.NaN so they are not applied.
            ClearBorders();

            mNearClipPlane = 1;
            mFarClipPlane = 1000;

            mOrthogonal = false;
            mOrthogonalWidth = width;
            mOrthogonalHeight = height;

            mAspectRatio = width / (float)height;

            // Be sure to instantiate the frustum before setting the FieldOfView
            // since setting the FieldOfView sets the frustum:
            mBoundingFrustum = new BoundingFrustum(Matrix.Identity);

            // use the Property rather than base value so that the mXEdge and mYEdge are set
            FieldOfView = (float)System.Math.PI / 4.0f;


            // Set up the default viewport
            DestinationRectangle = new Rectangle(
                0, 0, width, height);

            mCameraCullMode = CameraCullMode.UnrotatedDownZ;

            Layer layer = new Layer();
            layer.mCameraBelongingTo = this;
            layer.Name = "Camera Layer";

            mLayers.Add(layer);

            mLayersReadOnly = new ReadOnlyCollection<Layer>(mLayers);

            Position.Z = -MathFunctions.ForwardVector3.Z * 40;

#if SUPPORTS_POST_PROCESSING
            mPostProcessing = new PostProcessingEffectCollection();

            if (Renderer.UseRenderTargets)
            {
                mPostProcessing.InitializeEffects();
            }
            // Create the render order collection
            RenderOrder = new List<RenderMode>();
            RenderOrder.Add(RenderMode.Default);

            RefreshTexture();
#endif




        }

        #endregion

        #region Methods

        public Layer AddLayer()
        {
            Layer layer = new Layer();
            mLayers.Add(layer);
            layer.mCameraBelongingTo = this;
            return layer;
        }

        #region XML Docs
        /// <summary>
        /// Adds a layer to the Camera.  This method does not remove layers that already 
        /// exist in the SpriteManager.
        /// </summary>
        /// <param name="layerToAdd">The layer to add.</param>
        #endregion
        public void AddLayer(Layer layerToAdd)
        {
            if (layerToAdd.mCameraBelongingTo != null)
            {
                throw new System.InvalidOperationException("The argument layer already belongs to a Camera.");
            }
            else
            {
                // The layer doesn't belong to a camera so it can be added here.
                mLayers.Add(layerToAdd);
                layerToAdd.mCameraBelongingTo = this;
            }
        }

        public void MoveToBack(Layer layer)
        {
            mLayers.Remove(layer);
            mLayers.Insert(0, layer);
        }

        public void MoveToFront(Layer layer)
        {
            // Last layers appear on top (front)
            mLayers.Remove(layer);
            mLayers.Add(layer);
        }

        #region XmlDocs
        /// <summary>
        /// Supplied sprites are billboarded using the camera's RotationMatrix.
        /// Only the main Camera can billboard sprites.
        /// </summary>
        #endregion
        public void AddSpriteToBillboard(Sprite sprite)
        {
            // This only works on the main camera. Multi-camera games must implement their
            // own solutions:
#if DEBUG
            if(this != Camera.Main)
            {
                throw new InvalidOperationException("Sprites can only be billboarded on the main camera");
            }

#endif
            //this.mSpritesToBillBoard.Add(sprite);
            sprite.IsBillboarded = true;
        }


        #region XML Docs
        /// <summary>
        /// Removes all visibility borders.
        /// <seealso cref="FlatRedBall.Camera.SetBordersAtZ"/>
        /// </summary>
        #endregion
        public void ClearBorders()
        {
            mBaseZ = float.NaN;
            mBaseMinimumX = float.NaN;
            mBaseMinimumY = float.NaN;
            mBaseMaximumX = float.NaN;
            mBaseMaximumY = float.NaN;
        }


        public void ClearMinimumsAndMaximums()
        {
            mMinimumX = float.NegativeInfinity;
            mMinimumY = float.NegativeInfinity;
            mMaximumX = float.PositiveInfinity;
            mMaximumY = float.PositiveInfinity;
        }




        public void FixDestinationRectangleHeightConstant()
        {
            mDestinationRectangle.Width = (int)(mAspectRatio * mDestinationRectangle.Height);
        }


        public void FixDestinationRectangleWidthConstant()
        {
            mDestinationRectangle.Height = (int)(mDestinationRectangle.Width / mAspectRatio);
        }

        public override void ForceUpdateDependencies()
        {
            base.ForceUpdateDependencies();

            // This will be done both in TimedUpdate as well as here
            X = System.Math.Min(X, mMaximumX);
            X = System.Math.Max(X, mMinimumX);
            Y = System.Math.Min(Y, mMaximumY);
            Y = System.Math.Max(Y, mMinimumY);

            if (!double.IsNaN(this.mBaseMaximumX))
            {
                CalculateMaxAndMins();
            }

            // This should happen AFTER mins and maxes are set
            UpdateViewProjectionMatrix(true);
        }

        public Matrix GetLookAtMatrix()
        {
            return GetLookAtMatrix(false);
        }

        public Matrix GetLookAtMatrix(bool relativeToCamera)
        {
            Vector3 positionVector;

            if (relativeToCamera)
            {
                positionVector = new Vector3(0, 0, 0);

                return Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
            }
            else
            {
                positionVector = Position;

                Vector3 cameraTarget = positionVector + mRotationMatrix.Forward;

                // FRB has historically had a lot of
                // jagged edges and rendering issues which
                // I think are caused by floating point inaccuracies.
                // Unfortunately these tend to happen most when objects
                // are positioned at whole numbers, which is very common.
                // Going to shift the camera by .5 units if the Camera is viewing
                // in 2D mode to try to reduce this
                if (ShiftsHalfUnitForRendering)
                {
                    // This doesn't seem to fix
                    // problems when the camera can
                    // smooth scroll.  I'm going to move
                    // the camera to multiples of 5.
                    //positionVector.X += .5f;
                    //positionVector.Y -= .5f;
                    //cameraTarget.X += .5f;
                    //cameraTarget.Y -= .5f;

                    // This also causes rendering issues, I think I'm going to do it in update dependencies
                    //positionVector.X = (int)positionVector.X + .5f;
                    //positionVector.Y = (int)positionVector.Y - .5f;
                    //cameraTarget.X = (int)cameraTarget.X + .5f;
                    //cameraTarget.Y = (int)cameraTarget.Y - .5f;
                
                    // Math this is complicated.  Without this
                    // stuff that is attached to the Camera tends
                    // to not be rendered properly :(  So putting it back in
                    positionVector.X += .5f;
                    positionVector.Y -= .5f;
                    cameraTarget.X += .5f;
                    cameraTarget.Y -= .5f;

                }
                Matrix returnMatrix;
                Matrix.CreateLookAt(
                    ref positionVector, // Position of the camera eye
                    ref cameraTarget,  // Point that the eye is looking at
                    ref UpVector,
                    out returnMatrix);

                return returnMatrix;
            }

        }


        public Matrix GetProjectionMatrix()
        {
            if (mOrthogonal == false)
            {
                //return Matrix.CreatePerspectiveFieldOfView(
                //    mFieldOfView * 1.066667f,
                //    mAspectRatio,
                //    mNearClipPlane,
                //    mFarClipPlane);
                return Matrix.CreatePerspectiveFieldOfView(
                    mFieldOfView,
                    mAspectRatio,
                    mNearClipPlane,
                    mFarClipPlane);
            }
            else
            {
                return Matrix.CreateOrthographic(
                    mOrthogonalWidth,
                    mOrthogonalHeight,
                    mNearClipPlane,
                    mFarClipPlane);
            }
        }


        public float GetRequiredAspectRatio(float desiredYEdge, float zValue)
        {
            float modifiedDesiredYEdge = 100 * desiredYEdge / (Position.Z - zValue);
            return (float)System.Math.Atan(modifiedDesiredYEdge / 100) * 2;
        }

        /// <summary>
        /// Returns the viewport for the Graphics Device, optionally restricted to the resolution.
        /// </summary>
        public Viewport GetViewport()
        {
            // Viewport is a struct, so we can start with the current viewport
            Viewport viewport = Renderer.GraphicsDevice.Viewport;
            viewport.X = DestinationRectangle.X;
            viewport.Y = DestinationRectangle.Y;
            viewport.Width = DestinationRectangle.Width;
            viewport.Height = DestinationRectangle.Height;

            if (ShouldRestrictViewportToResolution)
            {
                RestrictViewportToResolution(ref viewport);
            }

            return viewport;
        }

        public static void RestrictViewportToResolution(ref Viewport viewport)
        {
            // Renders can happen before the graphics device is reset
            // If so, we want to make sure we aren't rendering outside of the bounds:
            int maxWidthAllowed = Renderer.GraphicsDevice.PresentationParameters.BackBufferWidth - viewport.X;
            int maxHeightAllowed = Renderer.GraphicsDevice.PresentationParameters.BackBufferHeight - viewport.Y;

            viewport.Width = System.Math.Min(viewport.Width, maxWidthAllowed);
            viewport.Height = System.Math.Min(viewport.Height, maxHeightAllowed);
        }

        public Viewport GetViewport(LayerCameraSettings lcs, RenderTarget2D renderTarget = null)
        {
            Viewport viewport = Renderer.GraphicsDevice.Viewport;

            bool explicitlySetsDestination = lcs != null &&
                lcs.TopDestination >= 0 &&
                lcs.BottomDestination >= 0 &&
                lcs.LeftDestination >= 0 &&
                lcs.RightDestination >= 0;

            if(explicitlySetsDestination)
            {
                viewport.X = (int)lcs.LeftDestination;
                viewport.Y = (int)lcs.TopDestination;
                viewport.Width = (int)(lcs.RightDestination - lcs.LeftDestination);
                viewport.Height = (int)(lcs.BottomDestination - lcs.TopDestination);
            }
            else
            {
                // borrow the size from whatever it's tied to, which could be a camera or a render target
                if(renderTarget != null)
                {
                    viewport.X = 0;
                    viewport.Y = 0;
                    viewport.Width = renderTarget.Width;
                    viewport.Height = renderTarget.Height;
                }
                else
                {
                    viewport.X = DestinationRectangle.X;
                    viewport.Y = DestinationRectangle.Y;
                    viewport.Width = DestinationRectangle.Width;
                    viewport.Height = DestinationRectangle.Height;
                }
            }
            // Debug should *NEVER* be more tolerant of bad settings:
            //#if DEBUG


            if (ShouldRestrictViewportToResolution)
            {
                int destinationWidth;
                int destinationHeight;

                if(renderTarget == null)
                {
                    destinationWidth = FlatRedBallServices.GraphicsOptions.ResolutionWidth;
                    destinationHeight = FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                }
                else
                {
                    destinationWidth = renderTarget.Width;
                    destinationHeight = renderTarget.Height;
                }

                // September 24, 2012
                // There is currently a 
                // bug if the user starts
                // on a Screen with a 2D layer
                // in portrait mode.  The game tells
                // Windows 8 that it wants to be in portrait
                // mode.  When it does so, the game flips, but
                // it takes a second. FRB may still render during
                // that time and this code gets called.  The resolutions
                // don't match up and we have issues.  Not sure how to fix
                // this (yet).
                // Update August 25, 2013
                // This code can throw an exception when resizing a window.
                // But the user didn't do anything wrong - the engine just hasn't
                // gotten a chance to reset the device yet.  So we'll tolerate it:
                if (viewport.Y + viewport.Height > destinationHeight)
                {

                    //throw new Exception("The pixel height resolution of the display is " +
                    //    FlatRedBallServices.GraphicsOptions.ResolutionHeight +
                    //    " but the LayerCameraSettings' BottomDestination is " + (viewport.Y + viewport.Height));
                    int amountToSubtract = (viewport.Y + viewport.Height) - destinationHeight;
                    viewport.Height -= amountToSubtract;
                }

                if (viewport.X + viewport.Width > destinationWidth)
                {
                    //throw new Exception("The pixel width resolution of the display is " +
                    //    FlatRedBallServices.GraphicsOptions.ResolutionWidth +
                    //    " but the LayerCameraSettings' RightDestination is " + (viewport.X + viewport.Width));
                    int amountToSubtract = (viewport.X + viewport.Width) - destinationWidth;
                    viewport.Width -= amountToSubtract;
                }
                //#endif

            }
            return viewport;

        }
        

        #region Is object in view

        #endregion


        #region XML Docs
        /// <summary>
        /// Moves a Sprite so that it remains fully in the camera's view.
        /// </summary>
        /// <remarks>
        /// This method does not consider Sprite rotation, negative scale, or situations 
        /// when the camera is not looking down the Z axis.
        /// </remarks>
        /// <param name="sprite">The Sprite to keep in view.</param>
        #endregion
        public void KeepSpriteInScreen(Sprite sprite)
        {
            // If the Sprite has a parent, then we need to force update so that its
            // position is what it will be when it's drawn.  Be sure to do this before
            // storing off the oldPosition
            if (sprite.Parent != null)
            {
                sprite.ForceUpdateDependencies();
            }

            Vector3 oldPosition = sprite.Position;
            float edgeCoef = (Z - sprite.Z) / 100.0f;

            if (sprite.X - sprite.ScaleX < X - edgeCoef * XEdge)
                sprite.X = X - edgeCoef * XEdge + sprite.ScaleX;
            if (sprite.X + sprite.ScaleX > X + edgeCoef * XEdge)
                sprite.X = X + edgeCoef * XEdge - sprite.ScaleX;


            if (sprite.Y - sprite.ScaleY < Y - edgeCoef * YEdge)
                sprite.Y = Y - edgeCoef * YEdge + sprite.ScaleY;
            if (sprite.Y + sprite.ScaleY > Y + edgeCoef * YEdge)
                sprite.Y = Y + edgeCoef * YEdge - sprite.ScaleY;

            if (sprite.Parent != null)
            {
                Vector3 shiftAmount = sprite.Position - oldPosition;

                sprite.TopParent.Position += shiftAmount;
            }
        }


        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.CameraUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.CameraUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);

            // TODO:  Need to pause the lights, but currently we don't know what type the lights are
        }


        public void PositionRandomlyInView(IPositionable positionable)
        {
            PositionRandomlyInView(positionable, Z, Z);
        }

        /// <summary>
        /// Positiones the argument positionable randomly in camera between the argument bounds.
        /// </summary>
        /// <remarks>
        /// Assumes the camera is viewing down the Z plane - it is unrotated.
        /// </remarks>
        /// <param name="positionable">The object to reposition.</param>
        /// <param name="minimumDistanceFromCamera">The closest possible distance from the camera.</param>
        /// <param name="maximumDistanceFromCamera">The furthest possible distance from the camera.</param>
        public void PositionRandomlyInView(IPositionable positionable, float minimumDistanceFromCamera, float maximumDistanceFromCamera)
        {
            // First get the distance from the camera.
            float distanceFromCamera = minimumDistanceFromCamera + 
                (float)(FlatRedBallServices.Random.NextDouble() * (maximumDistanceFromCamera - minimumDistanceFromCamera));

            positionable.Z = Z + (distanceFromCamera * FlatRedBall.Math.MathFunctions.ForwardVector3.Z);

            positionable.X = X - RelativeXEdgeAt(positionable.Z) + 
                (float)( FlatRedBallServices.Random.NextDouble() * 2.0f * RelativeXEdgeAt(positionable.Z) );

            positionable.Y = Y - RelativeYEdgeAt(positionable.Z) +
                (float)(FlatRedBallServices.Random.NextDouble() * 2.0f * RelativeYEdgeAt(positionable.Z));
            
        }
        

        public void RefreshTexture()
        {

#if SUPPORTS_POST_PROCESSING
            if (mRenderTargetTexture != null && mRenderTargetTexture.IsDisposed == false)
            {
                throw new InvalidOperationException("The old RenderTargetTexture must first be disposed");
            }

            // Create the render textures collection
            mRenderTargetTextures = new Dictionary<int, RenderTargetTexture>();
            mRenderTargetTexture = new RenderTargetTexture(
                SurfaceFormat.Color, DestinationRectangle.Width, DestinationRectangle.Height, true);

            FlatRedBallServices.AddDisposable("Render Target Texture" + sCreatedCount,
                mRenderTargetTexture, mContentManager);

            sCreatedCount++;
#endif
        }


        public void SetRelativeYEdgeAt(float zDistance, float verticalDistance)
        {
            FieldOfView = (float)(2 * System.Math.Atan((double)((.5 * verticalDistance) / zDistance)));
        }


        public void ScaleDestinationRectangle(float scaleAmount)
        {
            float newWidth = DestinationRectangle.Width * scaleAmount;
            float newHeight = DestinationRectangle.Height * scaleAmount;

            float destinationCenterX = DestinationRectangle.Left + DestinationRectangle.Width / 2.0f;
            float destinationCenterY = DestinationRectangle.Top + DestinationRectangle.Height / 2.0f;

            DestinationRectangle = new Rectangle(
                (int)(destinationCenterX - newWidth / 2.0f),
                (int)(destinationCenterY - newHeight / 2.0f),
                (int)(newWidth),
                (int)(newHeight));



        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Layer from this Camera.  Does not empty the layer or
        /// remove contained objects from their respective managers.
        /// </summary>
        /// <param name="layerToRemove">The layer to remove</param>
        #endregion
        public void RemoveLayer(Layer layerToRemove)
        {
            mLayers.Remove(layerToRemove);
            layerToRemove.mCameraBelongingTo = null;
        }

        #region XML Docs
        /// <summary>
        /// Sets the visible borders when the camera is looking down the Z axis.
        /// </summary>
        /// <remarks>
        /// This sets visibility ranges for the camera.  That is, if the camera's maximumX is set to 100 at a zToSetAt of 
        /// 0, the camera will never be able to see the point x = 101, z = 0.  The camera imposes these limitations 
        /// by calculating the actual minimum and maximum values according to the variables passed.  Also, 
        /// the camera keeps track of these visible limits and readjusts the mimimum and maximum values 
        /// when the camera moves in the z direction. Therefore, it is only necessary to set these 
        /// values once, and the camera will remeber that these are the visibility borders, regardless of 
        /// its position.  It is important to note that the visiblity borders can be violated if they are too 
        /// close together - if a camera moves so far back that its viewable area at the set Z is greater than 
        /// the set minimumX and maximumX range, the camera will show an area outside of this range.
        /// <seealso cref="FlatRedBall.Camera.ClearBorders"/>
        /// </remarks>
        /// <param name="minimumX">The minimum x value of the visiblity border.</param>
        /// <param name="minimumY">The minimum y value of the visiblity border.</param>
        /// <param name="maximumX">The maximum x value of the visiblity border.</param>
        /// <param name="maximumY">The maximum y value of the visiblity border.</param>
        /// <param name="zToSetAt">The z value of the plane to use for the visibility border.</param>
        #endregion
        public void SetBordersAtZ(float minimumX, float minimumY, float maximumX, float maximumY, float zToSetAt)
        {
            mBaseZ = zToSetAt;
            mBaseMinimumX = minimumX;
            mBaseMinimumY = minimumY;
            mBaseMaximumX = maximumX;
            mBaseMaximumY = maximumY;
            CalculateMaxAndMins();

            X = System.Math.Min(X, mMaximumX);
            X = System.Math.Max(X, mMinimumX);
            Y = System.Math.Min(Y, mMaximumY);
            Y = System.Math.Max(Y, mMinimumY);
        }


        public void SetBordersAt(AxisAlignedRectangle visibleBounds)
        {
            SetBordersAtZ(visibleBounds.Left, visibleBounds.Bottom, visibleBounds.Right, visibleBounds.Top, visibleBounds.Z);
        }

        #region XML Docs
		/// <summary>
		/// Copies all fields from the argument to the camera instance.
		/// </summary>
		/// <remarks>
		/// This method will not copy the name, InstructionArray, or children PositionedObjects 
        /// (objects attached to the cameraToSetTo).
		/// </remarks>
		/// <param name="cameraToSetTo">The camera to clone.</param>
		#endregion
		public void SetCameraTo(Camera cameraToSetTo)
		{
			this.mAspectRatio = cameraToSetTo.mAspectRatio;
            this.mBaseMaximumX = cameraToSetTo.mBaseMaximumX;
            this.mBaseMaximumY = cameraToSetTo.mBaseMaximumY;
            this.mBaseMinimumX = cameraToSetTo.mBaseMinimumX;
            this.mBaseMinimumY = cameraToSetTo.mBaseMinimumY;
            this.mBaseZ = cameraToSetTo.mBaseZ;
			
			// cannot set children, because any PO can only be attached to one other PO
			this.CameraCullMode = cameraToSetTo.CameraCullMode;
			this.mParent = cameraToSetTo.Parent;
            this.mDestinationRectangle = cameraToSetTo.mDestinationRectangle;
			this.mFieldOfView = cameraToSetTo.mFieldOfView;

			// not setting InstructionArray.  May want to do this later

            this.MaximumX = cameraToSetTo.MaximumX;
            this.MaximumY = cameraToSetTo.MaximumY;

            this.MinimumX = cameraToSetTo.MinimumX;
            this.MinimumY = cameraToSetTo.MinimumY;
			
			// does not set name

			this.mOrthogonal = cameraToSetTo.mOrthogonal;
			this.mOrthogonalWidth = cameraToSetTo.mOrthogonalWidth;
            this.mOrthogonalHeight = cameraToSetTo.mOrthogonalHeight;
			this.RelativeX = cameraToSetTo.RelativeX;

            this.RelativeAcceleration = cameraToSetTo.RelativeAcceleration;

			this.RelativeXVelocity = cameraToSetTo.RelativeXVelocity;
			this.RelativeY = cameraToSetTo.RelativeY;
			this.RelativeYVelocity = cameraToSetTo.RelativeYVelocity;
			this.RelativeZ = cameraToSetTo.RelativeZ;
			this.RelativeZVelocity = cameraToSetTo.RelativeZVelocity;
			
			this.X = cameraToSetTo.X;
			this.XAcceleration = cameraToSetTo.XAcceleration;
			this.mXEdge = cameraToSetTo.mXEdge;
			this.XVelocity = cameraToSetTo.XVelocity;
			
			this.Y = cameraToSetTo.Y;
			this.YAcceleration = cameraToSetTo.YAcceleration;
			this.mYEdge = cameraToSetTo.mYEdge;
			this.YVelocity = cameraToSetTo.YVelocity;

			this.Z = cameraToSetTo.Z;
			this.ZAcceleration = cameraToSetTo.ZAcceleration;
			this.ZVelocity = cameraToSetTo.ZVelocity;
			this.mNearClipPlane = cameraToSetTo.mNearClipPlane;
            this.mFarClipPlane = cameraToSetTo.mFarClipPlane;
		}

        public void SetLookAtRotationMatrix(Vector3 LookAtPoint)
        {
            SetLookAtRotationMatrix(LookAtPoint, new Vector3(0, 1, 0));
        }

        public void SetLookAtRotationMatrix(Vector3 LookAtPoint, Vector3 upDirection)
        {
            Matrix newMatrix = Matrix.Invert(
                    Matrix.CreateLookAt(
                        Position,
                        LookAtPoint,
                        upDirection));

            // Kill the translation
            newMatrix.M41 = 0;
            newMatrix.M42 = 0;
            newMatrix.M43 = 0;
            // leave M44 to 1

            RotationMatrix = newMatrix;


        }

        public void SetDeviceViewAndProjection(BasicEffect effect, bool relativeToCamera)
        {
            #if !SILVERLIGHT
            // Set up our view matrix. A view matrix can be defined given an eye point,
            // a point to lookat, and a direction for which way is up. 
            effect.View = GetLookAtMatrix(relativeToCamera);
            effect.Projection = GetProjectionMatrix();
            #endif
        }



        public void SetDeviceViewAndProjection(GenericEffect effect, bool relativeToCamera)
        {
            // Set up our view matrix. A view matrix can be defined given an eye point,
            // a point to lookat, and a direction for which way is up. 
            effect.View = GetLookAtMatrix(relativeToCamera);
            effect.Projection = GetProjectionMatrix();
        }

        public void SetDeviceViewAndProjection(Effect effect, bool relativeToCamera)
        {
            //TimeManager.SumTimeSection("Start of SetDeviceViewAndProjection");

            #region Create the matrices
            // Get view, projection, and viewproj values
            Matrix view = (relativeToCamera)? mViewRelative : mView;
            Matrix viewProj;
            Matrix.Multiply(ref view, ref mProjection, out viewProj);
            #endregion

            if (effect is AlphaTestEffect)
            {
                AlphaTestEffect asAlphaTestEffect = effect as AlphaTestEffect;

                asAlphaTestEffect.View = view;
                asAlphaTestEffect.Projection = mProjection;

            }
            else
            {
                //TimeManager.SumTimeSection("Create the matrices");

                //EffectParameterBlock block = new EffectParameterBlock(effect);

                #region Get all valid parameters
                EffectParameter paramNameViewProj = effect.Parameters["ViewProj"];
                EffectParameter paramNameView = effect.Parameters["View"];
                EffectParameter paramNameProjection = effect.Parameters["Projection"];

                //EffectParameter paramSemViewProj = effect.Parameters.GetParameterBySemantic("VIEWPROJ");
                //EffectParameter paramSemView = effect.Parameters.GetParameterBySemantic("VIEW");
                //EffectParameter paramSemProjection = effect.Parameters.GetParameterBySemantic("PROJECTION");
                #endregion

                //TimeManager.SumTimeSection("Get all valid parameters");

                #region Set all available parameters

                //if (paramNameProjection != null || paramNameView != null ||
                //    paramNameViewProj != null || paramSemProjection != null ||
                //    paramSemView != null || paramSemViewProj != null)
                //{
                //block.Begin();

                if (paramNameView != null) paramNameView.SetValue(view);
                //if (paramSemView != null) paramSemView.SetValue(view);
                if (paramNameProjection != null) paramNameProjection.SetValue(mProjection);
                //if (paramSemProjection != null) paramSemProjection.SetValue(mProjection);
                if (paramNameViewProj != null) paramNameViewProj.SetValue(viewProj);
                //if (paramSemViewProj != null) paramSemViewProj.SetValue(viewProj);

                //block.End();
                //block.Apply();
                //}

                #endregion

                //TimeManager.SumTimeSection("Set available parameters");
            }
        }

        public override void TimedActivity(float secondDifference, double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            // This will be done both in UpdateDependencies as well as here
            X = System.Math.Min(X, mMaximumX);
            X = System.Math.Max(X, mMinimumX);
            Y = System.Math.Min(Y, mMaximumY);
            Y = System.Math.Max(Y, mMinimumY);

            if (!double.IsNaN(this.mBaseMaximumX))
            {
                CalculateMaxAndMins();
            }

            #region update the destination rectangle (for viewport)
            if (mTopDestinationVelocity != 0f || mBottomDestinationVelocity != 0f ||
                mLeftDestinationVelocity != 0f || mRightDestinationVelocity != 0f)
            {
                mTopDestination += mTopDestinationVelocity * TimeManager.SecondDifference;
                mBottomDestination += mBottomDestinationVelocity * TimeManager.SecondDifference;
                mLeftDestination += mLeftDestinationVelocity * TimeManager.SecondDifference;
                mRightDestination += mRightDestinationVelocity * TimeManager.SecondDifference;

                UpdateDestinationRectangle();
                FixAspectRatioYConstant();
            }
            #endregion
            
        }


        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);

            // This will be done both in TimedUpdate as well as here
            X = System.Math.Min(X, mMaximumX);
            X = System.Math.Max(X, mMinimumX);
            Y = System.Math.Min(Y, mMaximumY);
            Y = System.Math.Max(Y, mMinimumY);

            if (!double.IsNaN(this.mBaseMaximumX))
            {
                CalculateMaxAndMins();
            }

            // I'm not sure if this is the proper fix but on WP7 it prevents
            // tile maps from rendering improperly when scrolling.
            if (ShiftsHalfUnitForRendering && this.Parent != null)
            {
                Position.X = (int)Position.X + .25f;
                Position.Y = (int)Position.Y - .25f;
            }

            // This should happen AFTER mins and maxes are set
            UpdateViewProjectionMatrix(true);

        }


        public void UpdateViewProjectionMatrix()
        {
            UpdateViewProjectionMatrix(false);
        }

        public void UpdateViewProjectionMatrix(bool updateFrustum)
        {
            mView = GetLookAtMatrix(false);
            mViewRelative = GetLookAtMatrix(true);
			//NOTE: Certain objects need to be rendered "Pixel Perfect" on screen.
			//  However, DX9 (and by extension XNA) has an "issue" moving from texel space to pixel space.
			//  http://msdn.microsoft.com/en-us/library/bb219690(v=vs.85).aspx
			//  if an object needs to be pixel perfect, it needs to have it's projection matrix shifted by -.5 x and -.5 y
			//EXAMPLE:
			//  mProjection = Matrix.CreateTranslation(-0.5f, -0.5f, 0) * GetProjectionMatrix();
			mProjection = GetProjectionMatrix();

            if (updateFrustum)
            {
                Matrix.Multiply(ref mView, ref mProjection, out mViewProjection);
                mBoundingFrustum.Matrix = (mViewProjection);
            }
        }

        /// <summary>
        /// Sets the camera to be 2D (far-away things do not get smaller) by
        /// setting Orthogonal to true and adjusts the OrthogonalWidth and OrthogonalHeight
        /// to match the pixel resolution. In other words, this makes 1 unit in game match 1 pixel on screen.
        /// </summary>
        public void UsePixelCoordinates()
        {
            UsePixelCoordinates(false,
                mDestinationRectangle.Width,
                mDestinationRectangle.Height);
        }

        [Obsolete("Use parameterless method")]
        public void UsePixelCoordinates(bool moveCornerToOrigin)
        {
            UsePixelCoordinates(moveCornerToOrigin,
                mDestinationRectangle.Width,
                mDestinationRectangle.Height);
        }


        public void WorldToScreen(float x, float y, float z, out int screenX, out int screenY)
        {
            screenX = 0;
            screenY = 0;

            MathFunctions.AbsoluteToWindow(x, y, z, ref screenX, ref screenY, this);
        }

        public void WorldToScreen(Vector3 position, out int screenX, out int screenY)
        {
            screenX = 0;
            screenY = 0;

            MathFunctions.AbsoluteToWindow(position.X, position.Y, position.Z, ref screenX, ref screenY, this);
        }

        /// Sets the viewport for this camera to a standard split-screen viewport
        /// </summary>
        /// <param name="viewport">The viewport to use for this camera. If null, the camera will not automatically 
        /// adjust itself.</param>
        public void SetSplitScreenViewport(SplitScreenViewport? viewport)
        {
            if(viewport == null)
            {
                mUsesSplitScreenViewport = false;

            }
            else
            {
                mUsesSplitScreenViewport = true;
            
                splitScreenViewport = viewport.Value;

                if(viewport == SplitScreenViewport.LeftThird)
                {
                    mTopDestination = 0;
                    mBottomDestination = FlatRedBallServices.ClientHeight;
                    mLeftDestination = 0;
                    mRightDestination = FlatRedBallServices.ClientWidth / 3;
                }
                else if(viewport == SplitScreenViewport.MiddleThird)
                {
                    mTopDestination = 0;
                    mBottomDestination = FlatRedBallServices.ClientHeight;
                    mLeftDestination = FlatRedBallServices.ClientWidth / 3;
                    mRightDestination = 2 * FlatRedBallServices.ClientWidth / 3; 
                }
                else if(viewport == SplitScreenViewport.RightThird)
                {
                    mTopDestination = 0;
                    mBottomDestination = FlatRedBallServices.ClientHeight;
                    mLeftDestination = 2 * FlatRedBallServices.ClientWidth / 3;
                    mRightDestination = FlatRedBallServices.ClientWidth;
                }
                else
                {
                    // Set the left
                    mLeftDestination = //FlatRedBallServices.GraphicsDevice.Viewport.X + ((
                        ((
                        viewport != SplitScreenViewport.RightHalf &&
                        viewport != SplitScreenViewport.TopRight &&
                        viewport != SplitScreenViewport.BottomRight) ?
                        0 : FlatRedBallServices.ClientWidth / 2);

                    // Set the top
                    mTopDestination = //FlatRedBallServices.GraphicsDevice.Viewport.Y + ((
                        ((
                        viewport != SplitScreenViewport.BottomHalf &&
                        viewport != SplitScreenViewport.BottomLeft &&
                        viewport != SplitScreenViewport.BottomRight) ?
                        0 : FlatRedBallServices.ClientHeight / 2);

                    // Set the right (left + width)
                    mRightDestination = mLeftDestination + ((
                        viewport != SplitScreenViewport.FullScreen &&
                        viewport != SplitScreenViewport.BottomHalf &&
                        viewport != SplitScreenViewport.TopHalf) ?
                        FlatRedBallServices.ClientWidth / 2 :
                        FlatRedBallServices.ClientWidth);

                    // Set the bottom (top + height)
                    mBottomDestination = mTopDestination + ((
                        viewport != SplitScreenViewport.FullScreen &&
                        viewport != SplitScreenViewport.LeftHalf &&
                        viewport != SplitScreenViewport.RightHalf) ?
                        FlatRedBallServices.ClientHeight / 2 :
                        FlatRedBallServices.ClientHeight);

                }
                // Update the destination rectangle
                UpdateDestinationRectangle();


            }

        }
        #endregion

        #region Internal Methods

        //internal void FlushLayers()
        //{
        //    int layerCount = mLayers.Count;

        //    for (int i = 0; i < layerCount; i++)
        //    {
        //        mLayers[i].Flush();
        //    }

        //}

        internal int UpdateSpriteInCameraView(SpriteList spriteList)
        {
            return UpdateSpriteInCameraView(spriteList, false);
        }

        internal int UpdateSpriteInCameraView(SpriteList spriteList, bool relativeToCamera)
        {
            int numberVisible = 0;


            for (int i = 0; i < spriteList.Count; i++)
            {
                Sprite s = spriteList[i];
                if (!s.AbsoluteVisible || 
                    // If using InterpolateColor, then alpha is used for interpolation of colors and
                    // not transparency
                    (s.ColorOperation != ColorOperation.InterpolateColor && s.Alpha < .0001f ))
                {
                    s.mInCameraView = false;
                    continue;
                }

                s.mInCameraView = IsSpriteInView(s, relativeToCamera);

                if (s.mInCameraView)
                {
                    numberVisible++;
                }
            }
            return numberVisible;
        }

        internal void UpdateOnResize()
        {
            if (mUsesSplitScreenViewport) SetSplitScreenViewport(splitScreenViewport);
        }

        #endregion

        #region Private Methods

        #region XML Docs
        /// <summary>
        /// Calculates the minimum and maximum X values for the camera based off of its
        /// base values (such as mBaseMaximumX) and its current view
        /// </summary>
        #endregion

        public void CalculateMaxAndMins()
        {
            // Vic says:  This method used to be private.  But now we want it public so that
            // Entities that control the camera can force calculate it.
            if (mOrthogonal)
            {
                mMinimumX = mBaseMinimumX + mOrthogonalWidth / 2.0f;
                mMaximumX = mBaseMaximumX - mOrthogonalWidth / 2.0f;

                mMinimumY = mBaseMinimumY + mOrthogonalHeight / 2.0f;
                mMaximumY = mBaseMaximumY - mOrthogonalHeight / 2.0f;
            }
            else
            {
                mMinimumX = mBaseMinimumX + mXEdge * (mBaseZ - Z) / (100f * MathFunctions.ForwardVector3.Z);
                mMaximumX = mBaseMaximumX - mXEdge * (mBaseZ - Z) / (100f * MathFunctions.ForwardVector3.Z);

                mMinimumY = mBaseMinimumY + mYEdge * (mBaseZ - Z) / (100f * MathFunctions.ForwardVector3.Z);
                mMaximumY = mBaseMaximumY - mYEdge * (mBaseZ - Z) / (100f * MathFunctions.ForwardVector3.Z);
            }
        }

        #region XML Docs
        /// <summary>
        /// Updates the destination rectangle (for the viewport).  Also fixes the aspect ratio.
        /// </summary>
        #endregion
        private void UpdateDestinationRectangle()
        {
            //usesSplitScreenViewport = false;

            //mDestinationRectangle = value;

            //mTopDestination = mDestinationRectangle.Top;
            //mBottomDestination = mDestinationRectangle.Bottom;
            //mLeftDestination = mDestinationRectangle.Left;
            //mRightDestination = mDestinationRectangle.Right;

            //FixAspectRatioYConstant();


            mDestinationRectangle = new Rectangle(
                (int)mLeftDestination,
                (int)mTopDestination,
                (int)(mRightDestination - mLeftDestination),
                (int)(mBottomDestination - mTopDestination));

            FixAspectRatioYConstant();


#if SUPPORTS_POST_PROCESSING
            // Update post-processing buffers
            if (Renderer.UseRenderTargets && PostProcessingManager.IsInitialized)
            {
                foreach (PostProcessingEffectBase effect in PostProcessing.EffectCombineOrder)
                {
                    effect.UpdateToScreenSize();
                }
            }
#endif
        }

        #endregion

        #region Protected Methods


        #endregion



        /// <summary>
        /// Sets the aspectRatio to match the width/height of the area that the camera is drawing to.
        /// </summary>
        /// <remarks>
        /// This is usually used in applications with split screen or when on a widescreen display.
        /// </remarks>
        public void FixAspectRatioYConstant()
        {
            // We may have a 0-height
            // DestinationRectangle in
            // test scenarios.
            if (mDestinationRectangle.Height != 0)
            {
                this.AspectRatio = (float)mDestinationRectangle.Width / (float)mDestinationRectangle.Height;

                mOrthogonalWidth = mOrthogonalHeight * mAspectRatio;
            }
        }

        public void FixAspectRatioXConstant()
        {
            float oldWidth = mOrthogonalWidth;
            float newAspectRatio = mDestinationRectangle.Width / (float)mDestinationRectangle.Height;
            this.FieldOfView *= newAspectRatio / mAspectRatio;
            AspectRatio = newAspectRatio;

            mOrthogonalWidth = oldWidth;
            mOrthogonalHeight = mOrthogonalWidth / mAspectRatio;

        }

        /// <summary>
        /// Checks if an Entity is within the Camera's boundaries.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="buffer">A buffer zone to account for entity size on boundary check. Defaults to 0</param>
        /// <returns>Returns true in case it is inside the camera's boundaries, false otherwise</returns>
        public bool IsEntityInView(PositionedObject entity, float buffer = 0)
        {
            return IsEntityInView(entity.Position, buffer);
        }

        /// <summary>
        /// Checks if an object with Vector3 position is inside the Camera's boundaries.
        /// </summary>
        /// <param name="position">The position of the entity to check</param>
        /// <param name="buffer">A buffer zone to account for entity size on boundary check. Defaults to 0</param>
        /// <returns>Returns true in case it is inside the camera's boundaries, false otherwise</returns>
        public bool IsEntityInView(Vector3 position, float buffer = 0)
        {
            var z = position.Z;

            bool isOnScreen = position.X < Camera.Main.AbsoluteRightXEdgeAt(z) + buffer &&
                                position.X > Camera.Main.AbsoluteLeftXEdgeAt(z) - buffer &&
                                position.Y < Camera.Main.AbsoluteTopYEdgeAt(z) + buffer &&
                                position.Y > Camera.Main.AbsoluteBottomYEdgeAt(z) - buffer;

            return isOnScreen;
        }

        public bool IsSpriteInView(Sprite sprite)
        {
            return IsSpriteInView(sprite, false);
        }

        static float mLongestDimension;
        static float mDistanceFromCamera;
        /// <summary>
        /// Returns whether the argument sprite is in view, considering the CameraCullMode. This will always return
        /// true if cull mode is set to None.
        /// </summary>
        /// <remarks>
        /// This method does not do a perfectly precise check of whether the Sprite is on screen or not, as such
        /// a check would require considering the Sprite's rotation. Instead, this uses approximations to avoid
        /// trigonometric functions, and will err on the side of returning true when a Sprite may actually be out
        /// of view.
        /// </remarks>
        /// <param name="sprite">The sprite to check in view</param>
        /// <param name="relativeToCamera">Whether the sprite's position is relative to the camera. This value may be true if the 
        /// sprite is on a Layer, and the Layer's RelativeToCamera value is true.</param>
        /// <returns>Whether the sprite is in view</returns>
        /// <seealso cref="FlatRedBall.Graphics.Layer.RelativeToCamera"/>
        public bool IsSpriteInView(Sprite sprite, bool relativeToCamera)
        {
            switch (CameraCullMode)
            {

                case CameraCullMode.UnrotatedDownZ:
                    {
                        mLongestDimension = (float)(System.Math.Max(sprite.ScaleX, sprite.ScaleY) * 1.42f);


                        if (mOrthogonal)
                        {
                            if (relativeToCamera)
                            {
                                if (System.Math.Abs(sprite.X) - mLongestDimension > mOrthogonalWidth)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(sprite.Y) - mLongestDimension > mOrthogonalHeight)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (System.Math.Abs(X - sprite.X) - mLongestDimension > mOrthogonalWidth)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(Y - sprite.Y) - mLongestDimension > mOrthogonalHeight)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            // Multiply by 1.5 to increase the range in case the Camera is rotated
                            if (relativeToCamera)
                            {
                                mDistanceFromCamera = (-sprite.Z) / 100.0f;

                                if (System.Math.Abs(sprite.X) - mLongestDimension > mXEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(sprite.Y) - mLongestDimension > mYEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                mDistanceFromCamera = (Z - sprite.Z) / 100.0f;

                                if (System.Math.Abs(X - sprite.X) - mLongestDimension > mXEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(Y - sprite.Y) - mLongestDimension > mYEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                case CameraCullMode.None:
                    return true;
            }
            return true;

        }

        public bool IsTextInView(Text text, bool relativeToCamera)
        {
            if (this.CameraCullMode == Graphics.CameraCullMode.UnrotatedDownZ)
            {
                float cameraLeft;
                float cameraRight;
                float cameraTop;
                float cameraBottom;

                if (relativeToCamera)
                {
                    cameraLeft = -this.RelativeXEdgeAt(text.Z);
                    cameraRight = this.RelativeXEdgeAt(text.Z);
                    cameraTop = this.RelativeYEdgeAt(text.Z);
                    cameraBottom = -this.RelativeYEdgeAt(text.Z);

                }
                else
                {
                    cameraLeft = this.AbsoluteLeftXEdgeAt(text.Z);
                    cameraRight = this.AbsoluteRightXEdgeAt(text.Z);
                    cameraTop = this.AbsoluteTopYEdgeAt(text.Z);
                    cameraBottom = this.AbsoluteBottomYEdgeAt(text.Z);
                }
                float textVerticalCenter = text.VerticalCenter;
                float textHorizontalCenter = text.HorizontalCenter;

                float longestCenterToEdge
                    = (float)(System.Math.Max(text.Width, text.Height) * 1.42f / 2.0f);


                float textLeft = textHorizontalCenter - longestCenterToEdge;
                float textRight = textHorizontalCenter + longestCenterToEdge;
                float textTop = textVerticalCenter + longestCenterToEdge;
                float textBottom = textVerticalCenter - longestCenterToEdge;

                return textRight > cameraLeft &&
                    textLeft < cameraRight &&
                    textBottom < cameraTop &&
                    textTop > cameraBottom;
            }


            return true;
        }

        public bool IsPointInView(double x, double y, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return (x > Position.X - mOrthogonalWidth / 2.0f && x < Position.X + mOrthogonalWidth / 2.0f) &&
                    y > Position.Y - mOrthogonalHeight / 2.0f && y < Position.Y + mOrthogonalHeight / 2.0f;
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (x > Position.X - mXEdge * cameraDistance && x < Position.X + mXEdge * cameraDistance &&
                    y > Position.Y - mYEdge * cameraDistance && y < Position.Y + mYEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Determines if the X value is in view, assuming the camera is viewing down the Z axis.
        /// </summary>
        /// <remarks>
        /// Currently, this method assumes viewing down the Z axis.
        /// </remarks>
        /// <param name="x">The absolute X position of the point.</param>
        /// <param name="absoluteZ">The absolute Z position of the point.</param>
        /// <returns></returns>
        public bool IsXInView(double x, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return (x > Position.X - mOrthogonalWidth / 2.0f && x < Position.X + mOrthogonalWidth / 2.0f);
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (x > Position.X - mXEdge * cameraDistance && x < Position.X + mXEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Determines if the Y value is in view, assuming the camera is viewing down the Z axis.
        /// </summary>
        /// <remarks>
        /// Currently, this method assumes viewing down the Z axis.
        /// </remarks>
        /// <param name="y">The absolute Y position of the point.</param>
        /// <param name="absoluteZ">The absolute Z position of the point.</param>
        /// <returns></returns>
        public bool IsYInView(double y, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return y > Position.Y - mOrthogonalHeight / 2.0f && y < Position.Y + mOrthogonalHeight / 2.0f;
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (y > Position.Y - mYEdge * cameraDistance && y < Position.Y + mYEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }


        /// <summary>
        /// Returns the number of pixels per unit at the given absolute Z value.  Assumes
        /// that the Camera is unrotated.
        /// </summary>
        /// <remarks>
        /// If using the PixelsPerUnitAt for a rotated camera, use the overload which
        /// takes a Vector3 argument.
        /// </remarks>
        /// <param name="absoluteZ">The absolute Z position.</param>
        /// <returns>The number of pixels per world unit (perpendicular to the camera's forward vector).</returns>
        public float PixelsPerUnitAt(float absoluteZ)
        {
            // June 7, 2011
            // This used to use
            // width values, but
            // that means aspect ratio
            // can screw with these values
            // which we don't want.  Instead
            // we should use height, as that is
            // usually what FRB games use as their
            //// fixed dimension
            //if (mOrthogonal)
            //{
            //    return mDestinationRectangle.Width / mOrthogonalWidth;
            //}
            //else
            //{
            //    return mDestinationRectangle.Width / (2 * RelativeXEdgeAt(absoluteZ));
            //}

            if (mOrthogonal)
            {
                return mDestinationRectangle.Height / mOrthogonalHeight;
            }
            else
            {
                return mDestinationRectangle.Height / (2 * RelativeYEdgeAt(absoluteZ));
            }

        }

        public float PixelsPerUnitAt(ref Vector3 absolutePosition)
        {

            return PixelsPerUnitAt(ref absolutePosition, mFieldOfView, mOrthogonal, mOrthogonalHeight);
        }

        public float PixelsPerUnitAt(ref Vector3 absolutePosition, float fieldOfView, bool orthogonal, float orthogonalHeight)
        {
            if (orthogonal)
            {
                return mDestinationRectangle.Height / orthogonalHeight;
            }
            else
            {
                float distance = Vector3.Dot(
                    (absolutePosition - Position), RotationMatrix.Forward);

                return mDestinationRectangle.Height /
                    (2 * RelativeYEdgeAt(Position.Z + (Math.MathFunctions.ForwardVector3.Z * distance), fieldOfView, mAspectRatio, orthogonal, orthogonalHeight));
            }
        }

        public float RelativeXEdgeAt(float absoluteZ)
        {
            return RelativeXEdgeAt(absoluteZ, mFieldOfView, mAspectRatio, mOrthogonal, mOrthogonalWidth);
        }

        public float RelativeXEdgeAt(float absoluteZ, float fieldOfView, float aspectRatio, bool orthogonal, float orthogonalWidth)
        {
            if (orthogonal)
            {
                return orthogonalWidth / 2.0f;
            }
            else
            {
                float yEdge = (float)(100 * System.Math.Tan(fieldOfView / 2.0));
                float xEdge = yEdge * aspectRatio;
                return xEdge * (absoluteZ - Position.Z) / 100 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            }
        }

        public float RelativeYEdgeAt(float absoluteZ)
        {
            return RelativeYEdgeAt(absoluteZ, mFieldOfView, AspectRatio, Orthogonal, OrthogonalHeight);
        }

        public float RelativeYEdgeAt(float absoluteZ, float fieldOfView, float aspectRatio, bool orthogonal, float orthogonalHeight)
        {
            if (orthogonal)
            {
                // fieldOfView is ignored if it's Orthogonal
                return orthogonalHeight / 2.0f;
            }
            else
            {
                float yEdge = (float)(System.Math.Tan(fieldOfView / 2.0));

                return yEdge * (absoluteZ - Position.Z) / FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            }
        }


        /// <summary>
        /// Sets the camera to Orthogonal, sets the OrthogonalWidth and
        /// OrthogonalHeight to match the argument values, and can move the
        /// so the bottom-left corner of the screen is at the origin.
        /// </summary>
        /// <param name="moveCornerToOrigin">Whether the camera should be repositioned
        /// so the bottom left is at the origin.</param>
        /// <param name="desiredWidth">The desired unit width of the view.</param>
        /// <param name="desiredHeight">The desired unit height of the view.</param>
        public void UsePixelCoordinates(bool moveCornerToOrigin, int desiredWidth, int desiredHeight)
        {
            this.Orthogonal = true;
            OrthogonalWidth = desiredWidth;
            OrthogonalHeight = desiredHeight;

            if (moveCornerToOrigin)
            {
                X = OrthogonalWidth / 2.0f;
                Y = OrthogonalHeight / 2.0f;
            }
        }

        /// <summary>
        /// Adjusts the camera's Z value so that 1 unit equals 1 pixel at the argument absolute Z value.
        /// Note that objects closer to the camera will appear bigger and objects further will appear smaller.
        /// This function assumes that Orthogonal is set to false.
        /// </summary>
        /// <param name="zToMakePixelPerfect">The absolute Z value to make pixel perfect.</param>
        public void UsePixelCoordinates3D(float zToMakePixelPerfect)
        {
            double distance = GetZDistanceForPixelPerfect();
            this.Z = -MathFunctions.ForwardVector3.Z * (float)(distance) + zToMakePixelPerfect;
            this.FarClipPlane = System.Math.Max(this.FarClipPlane,
                (float)distance * 2);
        }

        public float GetZDistanceForPixelPerfect()
        {
            double sin = System.Math.Sin(FieldOfView / 2.0);
            double cos = System.Math.Cos(FieldOfView / 2.0f);


            double edgeToEdge = 2 * sin;
            float desiredHeight = this.DestinationRectangle.Height;

            double distance = cos * desiredHeight / edgeToEdge;
            return (float)distance;
        }


        public float WorldXAt(float screenX, float zPosition)
        {
            return WorldXAt(screenX, zPosition, this.Orthogonal, this.OrthogonalWidth);
        }

        public float WorldXAt(float screenX, float zPosition, Layer layer)
        {
            if (layer == null || layer.LayerCameraSettings == null)
            {
                return WorldXAt(screenX, zPosition, this.Orthogonal, this.OrthogonalWidth);
            }
            else
            {
                LayerCameraSettings lcs = layer.LayerCameraSettings;

                Camera cameraToUse = layer.CameraBelongingTo;

                if (cameraToUse == null)
                {
                    cameraToUse = this;
                }

                // If the orthogonal resolution per destination width/height
                // of the layer matches the Camera's orthogonal per destination, then
                // we can just use the camera.  This is the most common case so we'll just
                // use that.
                float destinationLeft = cameraToUse.DestinationRectangle.Left;
                float destinationRight = cameraToUse.DestinationRectangle.Right;


                float destinationWidth = destinationRight - destinationLeft;

                float horizontalPercentage = (screenX - destinationLeft) / (float)destinationWidth;

                float orthogonalWidthToUse = cameraToUse.OrthogonalWidth;

                var useLayerOrtho = lcs.Orthogonal && !cameraToUse.Orthogonal;

                if (useLayerOrtho)
                {
                    orthogonalWidthToUse = lcs.OrthogonalWidth;
                }

                // used for adjusting the ortho width/height if the Layer is zoomed
                float layerMultiplier = 1;
                float bottomDestination = lcs.BottomDestination;
                float topDestination = lcs.TopDestination;
                if (bottomDestination == -1 || topDestination == -1)
                {
                    bottomDestination = cameraToUse.BottomDestination;
                    topDestination = cameraToUse.TopDestination;
                }

                // Make sure the destinations aren't equal or else we'd divide by 0
                if (lcs.Orthogonal && bottomDestination != topDestination)
                {
                    layerMultiplier = lcs.OrthogonalHeight / (float)(bottomDestination - topDestination);
                }

                float cameraMultiplier = 1;
                // Make sure the destinations aren't equal or else we'd divide by 0
                if (cameraToUse.Orthogonal && cameraToUse.BottomDestination != cameraToUse.TopDestination)
                {
                    cameraMultiplier = cameraToUse.OrthogonalHeight / (float)(cameraToUse.BottomDestination - cameraToUse.TopDestination);
                }
                layerMultiplier /= cameraMultiplier;

                if (!useLayerOrtho)
                {
                    orthogonalWidthToUse *= layerMultiplier;
                }

                // I think we want to use the Camera's orthogonalWidth if it's orthogonal
                //return GetWorldXGivenHorizontalPercentage(zPosition, cameraToUse, lcs.Orthogonal, lcs.OrthogonalWidth, horizontalPercentage);
                return GetWorldXGivenHorizontalPercentage(zPosition, lcs.Orthogonal, orthogonalWidthToUse, horizontalPercentage);
            }
        }

        public float WorldXAt(float screenX, float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth)
        {
            float screenRelativeX = screenX;
            return WorldXAt(zPosition, overridingOrthogonal, overridingOrthogonalWidth, screenRelativeX);
        }

        public float WorldXAt(float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth, float screenX)
        {

            float horizontalPercentage = (screenX - this.DestinationRectangle.Left) / (float)this.DestinationRectangle.Width;

            return GetWorldXGivenHorizontalPercentage(zPosition, overridingOrthogonal, overridingOrthogonalWidth, horizontalPercentage);
        }

        private float GetWorldXGivenHorizontalPercentage(float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth, float horizontalPercentage)
        {
            if (!overridingOrthogonal)
            {
                float absoluteLeft = this.AbsoluteLeftXEdgeAt(zPosition);
                float width = this.RelativeXEdgeAt(zPosition) * 2;
                return absoluteLeft + width * horizontalPercentage;
            }
            else
            {
                float xDistanceFromEdge = horizontalPercentage * overridingOrthogonalWidth;
                return (this.X + -overridingOrthogonalWidth / 2.0f + xDistanceFromEdge);
            }
        }


        public float WorldYAt(float screenY, float zPosition)
        {
            return WorldYAt(screenY, zPosition, this.Orthogonal, this.OrthogonalHeight);
        }

        public float WorldYAt(float screenY, float zPosition, Layer layer)
        {
            if (layer == null || layer.LayerCameraSettings == null)
            {
                return WorldYAt(screenY, zPosition, this.Orthogonal, this.OrthogonalHeight);
            }
            else
            {
                LayerCameraSettings lcs = layer.LayerCameraSettings;

                Camera cameraToUse = layer.CameraBelongingTo;

                if (cameraToUse == null)
                {
                    cameraToUse = this;
                }


                // If the orthogonal resolution per destination width/height
                // of the layer matches the Camera's orthogonal per destination, then
                // we can just use the camera.  This is the most common case so we'll just
                // use that.
                //return WorldYAt(zPosition, cameraToUse, lcs.Orthogonal, lcs.OrthogonalHeight);
                // If we have a 2D layer ona 3D camera, then we shouldn't use the Camera's orthogonal values
                float orthogonalHeightToUse = cameraToUse.OrthogonalHeight;

                // multiplier is used if the orghogonal height of the layer doesn't match the orthogonal height of the 
                // camera. But if we're going to pass the ortho height of the layer, then the multiplier should be 1

                var usedLayerOrtho = lcs.Orthogonal && !cameraToUse.Orthogonal;

                if (usedLayerOrtho)
                {
                    orthogonalHeightToUse = lcs.OrthogonalHeight;
                }

                float layerMultiplier = 1;
                float bottomDestination = lcs.BottomDestination;
                float topDestination = lcs.TopDestination;
                if (bottomDestination == -1 || topDestination == -1)
                {
                    bottomDestination = cameraToUse.BottomDestination;
                    topDestination = cameraToUse.TopDestination;
                }

                if (lcs.Orthogonal && bottomDestination != topDestination)
                {
                    layerMultiplier = lcs.OrthogonalHeight / (float)(bottomDestination - topDestination);
                }

                float cameraMultiplier = 1;
                if (cameraToUse.Orthogonal && cameraToUse.BottomDestination != cameraToUse.TopDestination)
                {
                    cameraMultiplier = cameraToUse.OrthogonalHeight / (float)(cameraToUse.BottomDestination - cameraToUse.TopDestination);
                }
                layerMultiplier /= cameraMultiplier;

                if (!usedLayerOrtho)
                {
                    orthogonalHeightToUse *= layerMultiplier;
                }

                return WorldYAt(screenY, zPosition, lcs.Orthogonal, orthogonalHeightToUse);
            }
        }

        public float WorldYAt(float screenY, float zPosition, bool overridingOrthogonal, float overridingOrthogonalHeight)
        {
            float screenRelativeY = screenY;

            return WorldYAt(zPosition, overridingOrthogonal, overridingOrthogonalHeight, screenRelativeY);
        }

        public float WorldYAt(float zPosition, bool orthogonal, float orthogonalHeight, float screenY)
        {
            float verticalPercentage = (screenY - this.DestinationRectangle.Top) / (float)this.DestinationRectangle.Height;

            if (!orthogonal)
            {
                float absoluteTop = this.AbsoluteTopYEdgeAt(zPosition);
                float height = this.RelativeYEdgeAt(zPosition) * 2;
                return absoluteTop - height * verticalPercentage;
            }
            else
            {
                float yDistanceFromEdge = verticalPercentage * orthogonalHeight;
                return (this.Y + orthogonalHeight / 2.0f - yDistanceFromEdge);
            }
        }



    }
}