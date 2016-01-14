using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.Shadows
{

    /// <summary>
    /// The base class for all shadowing techniques.
    /// </summary>
    public abstract class ShadowMaker
    {

        #region Fields
        protected Vector3 mLightDirection = new Vector3(0.0f, 0.0f, -1.0f);
        protected Vector3 mMapCenterPoint;
        protected Matrix mShadowCameraMatrix;
        protected float mLightDist;
        #endregion

        #region Properties
        public Matrix ShadowCameraMatrix
        {
            get { return mShadowCameraMatrix; }
        }

        public Vector3 LightDirection
        {
            get { return mLightDirection; }
            set { mLightDirection = Vector3.Normalize(value); }
        }
        #endregion

        #region Methods

        #region Constructor
        public ShadowMaker()
        {
        }
        #endregion

        #region Public Methods
        public virtual void PreRenderProcess(Camera camera)
        {
        }
        public virtual void PostRenderProcess(Camera camera)
        {
        }
        public void SetCenterPoint(ref Vector3 point)
        {
            mMapCenterPoint = point;
        }
        #endregion

        #region Internal Methods

        internal virtual void SetupEffectValues(EffectCache cache)
        {
        }

        internal virtual void SetupEffectValues(Effect effect)
        {
        }

        #endregion

        #region Private Methods
        #endregion


        #endregion



    }

    /// <summary>
    /// Ye old shadow mappy.
    /// </summary>
    public class ShadowMap : ShadowMaker
    {
        #region Fields
        int mResolutionX;
        int mResolutionY;
        RenderTargetTexture mRenderTargetTexture = null;

#if !XNA4
        DepthStencilBuffer mShadowDepthBuffer = null;
#endif

        /// <summary>
        /// This is a camera used to render the depth of the scene into the shadow maps depth buffer.
        /// It is NOT a standard camera and is not used in the standard fashion IE don't try to put it with the other scene cameras.
        /// The parameters for this camera change based on the camera that this shadow map is owned by.  The idea is that we want to get the best
        /// resolution that we can for the shadows for what is in view by the main camera.
        /// Actually, this camera is not used for much at all now except for a place holder
        /// in the rendering code where a camera is needed.  I could just use the scene camera, but this
        /// camera is good for debugging stuff.
        /// </summary>
        Camera mLightSourceCamera;
        BoundingBox mBoundingBox;
        private bool mInShadowCreationPass=false;
        #endregion

        #region Properties
        public Texture2D ShadowDepthTexture
        {
            get { return mRenderTargetTexture.Texture; }
        }
        public BoundingBox BoundingBox
        {
            get { return mBoundingBox; }
            set { mBoundingBox = value; }
        }
        #endregion

        #region Methods

        #region Constructor
        public ShadowMap(string contentManagerName, int resolution)
            : base()
        {
            mResolutionX = resolution;
            mResolutionY = resolution;


            //MDS_TEMP
            bool hasMikeUpdatedThisCode = false;
            if (hasMikeUpdatedThisCode && IsF32Supported())
            {
                mRenderTargetTexture = new RenderTargetTexture(SurfaceFormat.Single, mResolutionX, mResolutionY);
            }
            else
            {
                mRenderTargetTexture = new RenderTargetTexture(SurfaceFormat.Color, mResolutionX, mResolutionY);
            }

            mShadowDepthBuffer = new DepthStencilBuffer(FlatRedBallServices.GraphicsDevice, mResolutionX, mResolutionY, DepthFormat.Depth24Stencil8);

            mLightSourceCamera = new Camera(contentManagerName, mResolutionX, mResolutionY);
            mLightSourceCamera.AspectRatio = mResolutionX / mResolutionY;

            //Create a default bounding box
            Vector3 bboxMin = new Vector3(-5.0f, -5.0f, -100.0f);
            Vector3 bboxMax = new Vector3(5.0f, 5.0f, 100.0f);
            mBoundingBox = new BoundingBox(bboxMin, bboxMax);
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Tells us if this system supports the things needed to make our shadow maps.
        /// </summary>
        /// <returns></returns>
        public static bool IsF32Supported()
        {
            if (FlatRedBallServices.GraphicsDevice.CreationParameters.Adapter.CheckDeviceFormat(
                FlatRedBallServices.GraphicsDevice.CreationParameters.DeviceType,
                FlatRedBallServices.GraphicsDevice.DisplayMode.Format,
                TextureUsage.None, QueryUsages.None,
                ResourceType.RenderTarget,
                SurfaceFormat.Single))
            {
                return true;
            }
            return false;
        }

        public override void PreRenderProcess(Camera camera)
        {
            mInShadowCreationPass = true;
            UpdateShadowRenderConstants(camera);

            DepthStencilBuffer old = FlatRedBallServices.GraphicsDevice.DepthStencilBuffer;

            // Set the render target
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, mRenderTargetTexture.RenderTarget);
            FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = mShadowDepthBuffer;

            // Update light source camera
            mLightSourceCamera.DrawsCameraLayer = camera.DrawsCameraLayer;
            mLightSourceCamera.DrawsWorld = camera.DrawsWorld;

            for (int i = mLightSourceCamera.Layers.Count - 1; i >= 0; i--)
            {
                mLightSourceCamera.RemoveLayer(mLightSourceCamera.Layers[i]);
            }
            for (int i = camera.Layers.Count - 1; i >= 0; i--)
            {
                Layer layer = camera.Layers[i];
                camera.RemoveLayer(layer);
                mLightSourceCamera.AddLayer(layer);
            }

            // Draw the scene as seen from the light source
            mLightSourceCamera.MyShadow = this;
            //Renderer.DrawShadowCasters(mLightSourceCamera);

            // Return layers to original camera
            for (int i = mLightSourceCamera.Layers.Count - 1; i >= 0; i--)
            {
                Layer layer = mLightSourceCamera.Layers[i];
                mLightSourceCamera.RemoveLayer(layer);
                camera.AddLayer(layer);
            }

            // Get the rendered texture
            mRenderTargetTexture.ResolveTexture();
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, null);
            FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = old;
            
            mInShadowCreationPass = false;
        }
        #endregion

        #region Private Methods

        private double val = 0.0;
        private void UpdateLightDirectionTemp()
        {
            val += 0.02;
            Vector3 newDir = new Vector3();
            newDir.X = (float)System.Math.Cos(val);
            newDir.Y = (float)System.Math.Sin(val);
            newDir.Z = -1.5f + (float)System.Math.Sin(val);
            mLightDirection = newDir;
            mLightDirection.Normalize();
        }


        private void UpdateShadowRenderConstants(Camera camera)
        {

            //MDS_DEBUG_HELPER override the light direction using the code below.
            //UpdateLightDirectionTemp();
            //    or
            //mLightDirection = new Vector3(0.0f, 1.0f, -1.0f);
            //mLightDirection.Normalize();

            Vector3 right = Vector3.Normalize(Vector3.Cross(mLightDirection, Vector3.Backward));
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, mLightDirection));
            Vector3 forward = mLightDirection;

            //These values should be based on the bounding box of the scene.
            //IE what gets shadowed.
            float width = (mBoundingBox.Max.X - mBoundingBox.Min.X);
            float height = (mBoundingBox.Max.Y - mBoundingBox.Min.Y);
            float depth = (mBoundingBox.Max.Z - mBoundingBox.Min.Z);

            float inv_width = 1.0f / width;
            float inv_height = 1.0f / height;
            float inv_depth = 1.0f / depth;
            mLightDist = 0.5f * depth;

            Vector3.Multiply(ref right, inv_width, out right);
            Vector3.Multiply(ref up, inv_height, out up);
            Vector3.Multiply(ref forward, inv_depth, out forward);

            //This is actually the transpose of what gets used in the shader since Microsoft doesn't
            //like to follow pre established standards
            mShadowCameraMatrix = new Matrix(
                right.X, up.X, forward.X, 0.0f,//cameraAtPos.X,//cameraAt.X,
                right.Y, up.Y, forward.Y, 0.0f,//cameraAtPos.Y,
                right.Z, up.Z, forward.Z, 0.0f,//cameraAtPos.Z,
                0.0f, 0.0f, 0.0f, 1.0f
                );
        }

        public override void PostRenderProcess(Camera camera)
        {
        }
        #endregion

        internal override void SetupEffectValues(EffectCache cache)
        {
            if (cache.CacheShared)
            {

                #region Calculate center point

                Vector3 cp = mMapCenterPoint;
                Vector3 offset = (BoundingBox.Max + BoundingBox.Min) * 0.5f;
                cp -= offset;

                #endregion

                // Set parameters
                List<EffectParameter> paramList;
                #region ShadowMapTexture
                paramList = cache[EffectCache.EffectParameterNamesEnum.ShadowMapTexture];
                if (!mInShadowCreationPass && paramList != null)
                {
                    foreach (EffectParameter param in paramList)
                    {
                        param.SetValue(ShadowDepthTexture);
                    }
                }
                #endregion
                #region ShadowCameraMatrix
                paramList = cache[EffectCache.EffectParameterNamesEnum.ShadowCameraMatrix];
                if (paramList != null)
                {
                    foreach (EffectParameter param in paramList)
                    {
                        param.SetValue(ShadowCameraMatrix);
                    }
                }
                #endregion
                #region ShadowLightDirection
                paramList = cache[EffectCache.EffectParameterNamesEnum.ShadowLightDirection];
                if (paramList != null)
                {
                    foreach (EffectParameter param in paramList)
                    {
                        param.SetValue(mLightDirection);
                    }
                }
                #endregion
                #region ShadowLightDist
                paramList = cache[EffectCache.EffectParameterNamesEnum.ShadowLightDist];
                if (paramList != null)
                {
                    foreach (EffectParameter param in paramList)
                    {
                        param.SetValue(mLightDist);
                    }
                }
                #endregion
                #region ShadowCameraAt
                paramList = cache[EffectCache.EffectParameterNamesEnum.ShadowCameraAt];
                if (paramList != null)
                {
                    foreach (EffectParameter param in paramList)
                    {
                        param.SetValue(cp);
                    }
                }
                #endregion

            }
        }

        internal override void SetupEffectValues(Effect effect)
        {
            TimeManager.SumTimeSection("Start Setup Effect Values");

            #region Get EffectParameters

            EffectParameter shadowMapTexture = effect.Parameters["ShadowMapTexture"];
            EffectParameter shadowCameraMatrix = effect.Parameters["ShadowCameraMatrix"];
            EffectParameter lightDirection = effect.Parameters["ShadowLightDirection"];
            EffectParameter lightDist = effect.Parameters["ShadowLightDist"];
            EffectParameter cameraAt = effect.Parameters["ShadowCameraAt"];

            #endregion

            TimeManager.SumTimeSection("Get EffectParameters");

            #region Calculate center point

            Vector3 cp = mMapCenterPoint;
            Vector3 offset = (BoundingBox.Max + BoundingBox.Min)*0.5f;
            cp -= offset;

            #endregion

            TimeManager.SumTimeSection("Calculate center points");

            #region Set Parameters

            if (null != shadowMapTexture && !mInShadowCreationPass) shadowMapTexture.SetValue(ShadowDepthTexture);
            if (null != shadowCameraMatrix) shadowCameraMatrix.SetValue(ShadowCameraMatrix);
            if (null != lightDirection) lightDirection.SetValue(mLightDirection);
            if (null != lightDist) lightDist.SetValue(mLightDist);
            if (null != cameraAt) cameraAt.SetValue(cp);

            #endregion

            TimeManager.SumTimeSection("Set Parameters");
        }



        #endregion
    }
}
