using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.PostProcessing
{
    public class SwapChain
    {
        
        RenderTarget2D RenderTargetA;
        RenderTarget2D RenderTargetB;
        SpriteBatch spriteBatch;
        SurfaceFormat _surfaceFormat;

        public bool ShouldSwapClearRenderTarget { get; set; }

        bool isSwapped;
        public RenderTarget2D CurrentRenderTarget => isSwapped ? RenderTargetB : RenderTargetA;
        public RenderTarget2D CurrentTexture => isSwapped ? RenderTargetA : RenderTargetB;

        public int Width => RenderTargetA?.Width ?? 0;
        public int Height => RenderTargetA?.Height ?? 0;

        #region Construction / initialization

        public SwapChain(int width, int height, bool shouldSwapClearRenderTarget = true,
            SurfaceFormat? surfaceFormat = null)
        {
            _surfaceFormat = surfaceFormat ?? FlatRedBallServices.GraphicsDevice.DisplayMode.Format;
            ShouldSwapClearRenderTarget = shouldSwapClearRenderTarget;
            CreateRenderTarget(ref RenderTargetA, width, height, _surfaceFormat);
            RenderTargetA.Name = "SwapChain RenderTarget A";
            CreateRenderTarget(ref RenderTargetB, width, height, _surfaceFormat);
            RenderTargetB.Name = "SwapChain RenderTarget B";
            spriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);
        }


        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height)
        {
            CreateOrUpdateRenderTarget(ref renderTarget, width, height, FlatRedBallServices.GraphicsDevice.DisplayMode.Format, RenderTargetUsage.DiscardContents);
        }

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat)
        {
            CreateOrUpdateRenderTarget(ref renderTarget, width, height, surfaceFormat, RenderTargetUsage.DiscardContents);
        }

        /// <summary>
        /// Assigns a new render target to the argument renderTarget if the argument render target size, format, or usage
        /// does not match the parameters. If the argument renderTarget is not null then it is disposed.
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="surfaceFormat"></param>
        /// <param name="renderTargetUsage"></param>
        internal static void CreateOrUpdateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat, RenderTargetUsage renderTargetUsage)
        {
            if (renderTarget == null
                || renderTarget.Width != width
                || renderTarget.Height != height
                || renderTarget.Format != surfaceFormat
                || renderTarget.RenderTargetUsage != renderTargetUsage)
            {
                renderTarget?.Dispose();

                lock (FlatRedBallServices.GraphicsDevice)
                {
                    renderTarget = new RenderTarget2D(FlatRedBallServices.GraphicsDevice, width, height, false, surfaceFormat, (DepthFormat)0, 0, renderTargetUsage);
                }
            }
        }

        #endregion

        public void UpdateRenderTargetSize(int newWidth, int newHeight)
        {
            var shouldRecreate = RenderTargetA.Width != newWidth || RenderTargetA.Height != newHeight;

            if(shouldRecreate)
            {
                CreateRenderTarget(ref RenderTargetA, newWidth, newHeight, _surfaceFormat);
                RenderTargetA.Name = "SwapChain RenderTarget A";
                CreateRenderTarget(ref RenderTargetB, newWidth, newHeight, _surfaceFormat);
                RenderTargetB.Name = "SwapChain RenderTarget B";
            }
        }

        #region Rendering

        public void RenderToScreen()
        {
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(null);

            // ...and draw the RenderTarget to the screen
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            var destinationRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, CurrentRenderTarget.Width, CurrentRenderTarget.Height);

            spriteBatch.Draw(CurrentRenderTarget, destinationRectangle, 
                Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
        }

        public void ResetForFrame()
        {
            isSwapped = false;
        }

        public void Swap()
        {
            isSwapped = !isSwapped;
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);
            if(ShouldSwapClearRenderTarget)
            {
                FlatRedBallServices.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
            }
        }

        #endregion

    }
}
