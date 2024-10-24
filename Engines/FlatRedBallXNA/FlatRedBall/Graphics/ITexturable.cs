using System;
using System.Collections.Generic;
using System.Text;

using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace FlatRedBall.Graphics
{
    public interface ITexturable
    {
        Texture2D Texture { get; set;}
    }
}
