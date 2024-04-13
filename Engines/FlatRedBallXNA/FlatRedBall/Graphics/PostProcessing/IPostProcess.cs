using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.PostProcessing
{
    public interface IPostProcess
    {
        GraphicsDevice GraphicsDevice { set; }
        void Apply(Texture2D sourceTexture);
    }
}
