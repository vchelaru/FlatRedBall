using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace FlatRedBall.Forms.GumExtensions
{
    public static class GraphicalUiElementExtensions
    {
        /// <summary>
        /// Returns the absolute X position of the argument GraphicalUiElement.
        /// </summary>
        /// <param name="graphicalUiElement"></param>
        /// <returns></returns>
        [Obsolete("Use GraphicalUiElement.AbsoluteLeft")]
        public static float GetLeft(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteX();
        }

        /// <summary>
        /// Returns the absolute Y position of the argument GraphicalUiElement.
        /// </summary>
        /// <param name="graphicalUiElement"></param>
        /// <returns></returns>
        [Obsolete("Use GraphicalUiElement.AbsoluteTop")]
        public static float GetTop(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteY();
        }
    }
}
