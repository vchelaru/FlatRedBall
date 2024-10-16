using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.PostProcessing
{
    /// <summary>
    /// An interface for classes which wrap post processing effect (shader) files.
    /// This is responsible for applying a source shader to the current GraphicsDevice render targets.
    /// </summary>
    /// <remarks>
    /// This interface defines the minimum methods and properties necessary for a class to be included in
    /// FlatRedBall's PostProcessing list.
    /// </remarks>
    public interface IPostProcess
    {
        /// <summary>
        /// Whether FlatRedBall internally applies this effect. If this is disabled, then its Apply method is not called.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Applies the sourceTexture (which is typically a RenderTarget which was rendered to by FlatRedBall) to the current GraphicsDevice RenderTargets.
        /// </summary>
        /// <remarks>
        /// This method should not change the GraphicsDevice's RenderTargets unless it is doing so for multiple pass shaders. In this case, it should restore the GraphicsDevice's RenderTargets to their original state.
        /// </remarks>
        /// <param name="sourceTexture">The texture to apply to apply to the render target using the contained effect.</param>
        void Apply(Texture2D sourceTexture);
    }
}
