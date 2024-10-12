using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.PostProcessing
{
    public interface IPostProcess
    {
        bool IsEnabled { get; }
        void Apply(Texture2D sourceTexture);
    }
}
