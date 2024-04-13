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

        public bool ShouldSwapClearRenderTarget { get; set; }

        bool isSwapped;
        public RenderTarget2D CurrentRenderTarget => isSwapped ? RenderTargetB : RenderTargetA;
        public RenderTarget2D CurrentTexture => isSwapped ? RenderTargetA : RenderTargetB;


        public SwapChain(int width, int height, bool shouldSwapClearRenderTarget = true,
            SurfaceFormat? surfaceFormat = null)
        {
            surfaceFormat = surfaceFormat ?? FlatRedBallServices.GraphicsDevice.DisplayMode.Format;
            ShouldSwapClearRenderTarget = shouldSwapClearRenderTarget;
            CreateRenderTarget(ref RenderTargetA, width, height, surfaceFormat.Value);
            CreateRenderTarget(ref RenderTargetB, width, height, surfaceFormat.Value);
            spriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);
        }

        public void RenderToScreen()
        {
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(null);

            // ...and draw the RenderTarget to the screen
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            var destinationRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, Renderer.SwapChain.CurrentRenderTarget.Width, Renderer.SwapChain.CurrentRenderTarget.Height);

            spriteBatch.Draw(Renderer.SwapChain.CurrentRenderTarget, destinationRectangle, 
                Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
        }

        public void Swap()
        {
            isSwapped = !isSwapped;
            FlatRedBallServices.GraphicsDevice.SetRenderTarget(Renderer.SwapChain.CurrentRenderTarget);
            if(ShouldSwapClearRenderTarget)
            {
                FlatRedBallServices.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
            }
        }


        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height)
        {
            CreateRenderTarget(ref renderTarget, width, height, FlatRedBallServices.GraphicsDevice.DisplayMode.Format, RenderTargetUsage.DiscardContents);
        }

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat)
        {
            CreateRenderTarget(ref renderTarget, width, height, surfaceFormat, RenderTargetUsage.DiscardContents);
        }

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat, RenderTargetUsage renderTargetUsage)
        {
            if (renderTarget == null
                || renderTarget.Width != width
                || renderTarget.Height != height
                || renderTarget.Format != surfaceFormat
                || renderTarget.RenderTargetUsage != renderTargetUsage)
            {
                if (renderTarget != null)
                    renderTarget.Dispose();

                lock (FlatRedBallServices.GraphicsDevice)
                {
                    renderTarget = new RenderTarget2D(FlatRedBallServices.GraphicsDevice, width, height, false, surfaceFormat, (DepthFormat)0, 0, renderTargetUsage);
                }
            }
        }
    }
}
