using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing;

internal static class PostProcessLogic
{
    public static void DrawWithPostProcessing(List<IPostProcess> postProcesses,
        SwapChain swapChain, Action drawCall)
    {
        bool hasGlobalPostProcessing = false;

        foreach (var item in postProcesses)
        {
            if (item.IsEnabled)
            {
                hasGlobalPostProcessing = true;
                break;
            }
        }
#if DEBUG
        if (hasGlobalPostProcessing && swapChain == null)
        {
            throw new InvalidOperationException("SwapChain must be set prior to rendering the first frame if using any post processing");
        }
#endif
        RenderTarget2D previousRenderTarget = null;
        if (hasGlobalPostProcessing)
        {
            var renderTargetCount = 0;
#if WEB

            renderTargetCount = Renderer.GraphicsDevice.GetRenderTargets().Length;
#else
            renderTargetCount = Renderer.GraphicsDevice.RenderTargetCount;
#endif
            if (renderTargetCount > 0)
            {
                previousRenderTarget = Renderer.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
            }
            SetStatesAndRenderTargetForPostProcessing(swapChain);
        }
        else
        {
            // Just in case we removed all post processing, but are on "B"
            swapChain?.ResetForFrame();
        }

        drawCall();

        if (hasGlobalPostProcessing)
        {
            ApplyPostProcessing(postProcesses, swapChain, previousRenderTarget);
        }
    }

    static void ApplyPostProcessing(List<IPostProcess> postProcesses, SwapChain swapChain, RenderTarget2D previousRenderTarget)
    {
        foreach (var postProcess in postProcesses)
        {
            if (postProcess.IsEnabled)
            {
#if DEBUG
                Renderer.RenderBreaks.Add(new RenderBreak() { ObjectCausingBreak = postProcess });
#endif
                swapChain.Swap();
                postProcess.Apply(swapChain.CurrentTexture);
            }
        }

#if DEBUG
        Renderer.RenderBreaks.Add(new RenderBreak() { ObjectCausingBreak = swapChain });
#endif
        swapChain.RenderTo(previousRenderTarget);
    }

    static void SetStatesAndRenderTargetForPostProcessing(SwapChain swapChain)
    {
        Renderer.ForceSetBlendOperation();
        Renderer.ForceSetColorOperation(Renderer.ColorOperation);

        swapChain.ResetForFrame();

        // Set the render target before drawing anything
        Renderer.GraphicsDevice.SetRenderTarget(swapChain.CurrentRenderTarget);
    }

}
