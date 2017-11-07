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
        public static float GetLeft(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteX();
        }

        public static float GetTop(this GraphicalUiElement graphicalUiElement)
        {
            return ((IRenderableIpso)graphicalUiElement).GetAbsoluteY();
        }
    }
}
