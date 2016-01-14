using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics
{
    internal class RenderTargetTexture : IDisposable
    {
        #region Fields

#if !XNA4
        public DepthStencilBuffer DepthStencilBuffer;
#endif
        public RenderTarget2D RenderTarget;
        public Texture2D Texture;

        int mWidth;
        int mHeight;
        SurfaceFormat mSurfaceFormat;
        bool mIsMultisampled;

        #endregion

        #region Properties

        public bool IsDisposed
        {
            get;
            private set;
        }

        public int Width
        {
            get { return mWidth; }
        }

        public int Height
        {
            get { return mHeight; }
        }

        public bool IsMultisampled
        {
            get { return mIsMultisampled; }
            set
            {
                if (value != mIsMultisampled)
                {
                    mIsMultisampled = value;
                    SetSize(mWidth, mHeight);
                }
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public RenderTargetTexture(RenderTarget2D renderTarget)
        {
            RenderTarget = renderTarget;
            CreateDepthStencilBuffer(RenderTarget);

            IsDisposed = false;
        }

        public RenderTargetTexture(SurfaceFormat surfaceFormat,
            int width, int height)
            : this(surfaceFormat, width, height, false)
        {
            mWidth = width;
            mHeight = height;
            mSurfaceFormat = surfaceFormat;

#if XNA4
            RenderTarget = new RenderTarget2D(Renderer.GraphicsDevice,
                width, height);
#else
            RenderTarget = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                width, height,
                1, surfaceFormat, RenderTargetUsage.DiscardContents);            
#endif


            CreateDepthStencilBuffer(RenderTarget);

            IsDisposed = false;
        }

        public RenderTargetTexture(SurfaceFormat surfaceFormat,
            int width, int height, bool isMultisampled)
        {
            mWidth = width;
            mHeight = height;
            mSurfaceFormat = surfaceFormat;
            mIsMultisampled = isMultisampled;

#if XNA4
            RenderTarget = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                width, height);
#else
            RenderTarget = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                width, height,
                1, surfaceFormat,
                mIsMultisampled ? FlatRedBallServices.GraphicsDevice.PresentationParameters.MultiSampleType : MultiSampleType.None,
                0, // The useless Multisample Quality parameter which will always be 0 in FRB
                RenderTargetUsage.DiscardContents);
#endif
            CreateDepthStencilBuffer(RenderTarget);

            IsDisposed = false;
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Resolves the texture by setting the current render target to null (back buffer) and then
        /// getting the texture from the previously drawing render target
        /// </summary>
        #endregion
        public void ResolveTexture()
        {
#if XNA4
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(null);
#else
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, null);
#endif
            //if (Texture != null)
            //{
            //    Texture.Dispose();
            //    Texture = null;
            //}

#if XNA4
            Texture = RenderTarget;
#else
            Texture = RenderTarget.GetTexture();
#endif
        }

        #region XML Docs
        /// <summary>
        /// Sets this render target on the device
        /// </summary>
        #endregion
        public void SetOnDevice()
        {
#if XNA4
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(RenderTarget);
#else
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, RenderTarget);
            FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = DepthStencilBuffer;
#endif
        }

        #endregion

        #region Internal Methods

        internal void CreateDepthStencilBuffer(RenderTarget2D renderTarget)
        {
#if XNA4
            throw new NotImplementedException();
#else
            if (DepthStencilBuffer != null && DepthStencilBuffer.IsDisposed == false)
                DepthStencilBuffer.Dispose();

            DepthStencilBuffer = new DepthStencilBuffer(
                renderTarget.GraphicsDevice,
                renderTarget.Width, renderTarget.Height,
                renderTarget.GraphicsDevice.DepthStencilBuffer.Format,
                renderTarget.MultiSampleType, renderTarget.MultiSampleQuality);
#endif
        }



        internal void SetSize(int screenwidth, int screenheight)
        {
            if (RenderTarget != null && RenderTarget.IsDisposed == false)
            {
                RenderTarget.Dispose();
            }

            mWidth = screenwidth;
            mHeight = screenheight;

#if XNA4
            throw new NotImplementedException();

            // Unreachable code, but put it back in
            // once (if ever) we remove thie NotImplementedException
            // above.
            //CreateDepthStencilBuffer(RenderTarget);

#else
            RenderTarget = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                screenwidth, screenheight,
                1, mSurfaceFormat,
                mIsMultisampled ? FlatRedBallServices.GraphicsDevice.PresentationParameters.MultiSampleType : MultiSampleType.None,
                mIsMultisampled ? FlatRedBallServices.GraphicsDevice.PresentationParameters.MultiSampleQuality : 0,
                RenderTargetUsage.DiscardContents);
            CreateDepthStencilBuffer(RenderTarget);

#endif
        }

        #endregion

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            IsDisposed = true;

#if !XNA4
            if (DepthStencilBuffer != null)
            {
                DepthStencilBuffer.Dispose();
            }
#endif

            if (RenderTarget != null)
            {
                RenderTarget.Dispose();
            }

            if (Texture != null)
            {
                Texture.Dispose();
            }
        }

        #endregion
    }

    #region XML Docs
    /// <summary>
    /// Manages a swappable pair of render targets (of the same surface format)
    /// </summary>
    #endregion
    public class RenderTargetPair
    {
        #region Fields

        private RenderTarget2D mTargetA;
        private RenderTarget2D mTargetB;

#if !XNA4
        private DepthStencilBuffer mDepthStencilBuffer;
#endif
        private int mWidth, mHeight;
        private SurfaceFormat mSurfaceFormat;

        private bool mUsingTargetA;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets the current render target
        /// </summary>
        #endregion
        public RenderTarget2D CurrentRenderTarget
        {
            get { return (mUsingTargetA) ? mTargetA : mTargetB; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the width of this render target pair
        /// </summary>
        #endregion
        public int Width { get { return mWidth; } }

        #region XML Docs
        /// <summary>
        /// Gets the height of this render target pair
        /// </summary>
        #endregion
        public int Height { get { return mHeight; } }

        #endregion

        #region Methods

        #region Constructor

        public RenderTargetPair(
            SurfaceFormat surfaceFormat,
            int width, int height)
        {
            mWidth = width;
            mHeight = height;
            mSurfaceFormat = surfaceFormat;

            mUsingTargetA = true;

#if XNA4
            mTargetA = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                FlatRedBallServices.ClientWidth,
                FlatRedBallServices.ClientHeight);

            mTargetB = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                FlatRedBallServices.ClientWidth,
                FlatRedBallServices.ClientHeight);

#else
            mTargetA = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                FlatRedBallServices.ClientWidth,
                FlatRedBallServices.ClientHeight,
                1, surfaceFormat, RenderTargetUsage.DiscardContents);
            mTargetB = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                FlatRedBallServices.ClientWidth,
                FlatRedBallServices.ClientHeight,
                1, surfaceFormat, RenderTargetUsage.DiscardContents);
#endif
            CreateDepthStencilBuffer(mTargetA);
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// This switches the currently active render target.
        /// Use to set this type of target, or switch the currently used
        /// target of this type.
        /// </summary>
        #endregion
        public void SwitchTarget()
        {
#if XNA4 
            throw new NotImplementedException();
#else
            mUsingTargetA = !mUsingTargetA;

            if (mUsingTargetA)
            {
                FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, mTargetA);
                FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = mDepthStencilBuffer;
            }
            else
            {
                FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, mTargetB);
                FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = mDepthStencilBuffer;
            }
#endif
        }

        #region XML Docs
        /// <summary>
        /// This switches the currently active render target and gets the
        /// resolved texture from the previous target.
        /// Use to set this type of target, or switch the currently used
        /// target of this type.
        /// </summary>
        #endregion
        public void SwitchTarget(ref Texture2D texture)
        {
#if XNA4
            throw new NotImplementedException();
#else
            mUsingTargetA = !mUsingTargetA;



            if (mUsingTargetA)
            {
                FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, mTargetA);
                FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = mDepthStencilBuffer;
                texture = mTargetB.GetTexture();

            }
            else
            {
                FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, mTargetB);
                FlatRedBallServices.GraphicsDevice.DepthStencilBuffer = mDepthStencilBuffer;
                texture = mTargetA.GetTexture();
            }
#endif
        }

        #endregion

        #endregion

        #region Internal Methods

        internal void SetSize(int screenwidth, int screenheight)
        {
#if XNA4
            throw new NotImplementedException();
            // Unreachable code - uncomment when exception is removed
            //CreateDepthStencilBuffer(mTargetA);

#else
            mWidth = screenwidth;
            mHeight = screenheight;

            if (mTargetA != null && mTargetA.IsDisposed == false)
                mTargetA.Dispose();
            if (mTargetB != null && mTargetB.IsDisposed == false)
                mTargetB.Dispose();


            // Resize the render targets by recreating them
            mTargetA = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                mWidth, mHeight, 1, mSurfaceFormat, RenderTargetUsage.DiscardContents);
            mTargetB = new RenderTarget2D(
                FlatRedBallServices.GraphicsDevice,
                mWidth, mHeight, 1, mSurfaceFormat, RenderTargetUsage.DiscardContents);

            CreateDepthStencilBuffer(mTargetA);

#endif
        }

        internal void CreateDepthStencilBuffer(RenderTarget2D renderTarget)
        {
#if XNA4
            throw new NotImplementedException();

            
#else
            if (mDepthStencilBuffer != null && mDepthStencilBuffer.IsDisposed == false)
                mDepthStencilBuffer.Dispose();

            mDepthStencilBuffer = new DepthStencilBuffer(
                renderTarget.GraphicsDevice,
                renderTarget.Width, renderTarget.Height,
                renderTarget.GraphicsDevice.DepthStencilBuffer.Format,
                renderTarget.MultiSampleType, renderTarget.MultiSampleQuality);
#endif
        }

        #endregion
    };
}
