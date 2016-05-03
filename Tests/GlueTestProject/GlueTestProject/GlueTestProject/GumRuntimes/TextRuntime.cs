using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueTestProject.GumRuntimes
{
    partial class TextRuntime
    {
        public RenderingLibrary.Graphics.BitmapFont GetBitmapFont()
        {
            var internalText = this.RenderableComponent as RenderingLibrary.Graphics.Text;

            return internalText.BitmapFont;
        }
    }
}
