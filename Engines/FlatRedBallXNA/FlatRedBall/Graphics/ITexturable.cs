using System;
using System.Collections.Generic;
using System.Text;

#if !FRB_MDX
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace FlatRedBall.Graphics
{
    public interface ITexturable
    {
        Texture2D Texture { get; set;}
    }
}
