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
        public static float GetLeft(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteX();
        }

        /// <summary>
        /// REturns the absolute Y position of the argument GraphicalUiElement.
        /// </summary>
        /// <param name="graphicalUiElement"></param>
        /// <returns></returns>
        public static float GetTop(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteY();
        }
    }
}
