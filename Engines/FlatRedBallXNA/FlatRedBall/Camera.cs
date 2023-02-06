
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

        #region XML Docs
        /// <summary>
        /// Whether or not lighting is enabled for this camera
        /// </summary>
        #endregion
        internal bool mLightingEnabled = false;
        
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

        #region XML Docs
        /// <summary>
        /// A Camera-specific layer.  Objects on this layer will not appear
        /// in any other cameras.
        /// </summary>
        /// <remarks>
        /// This instance is automatically created when the Camera is instantiated.
        /// </remarks>
        #endregion
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

        #region Public Methods

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

                // Update the destination rectangle
                UpdateDestinationRectangle();


            }

        }

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

        #endregion

    }
}