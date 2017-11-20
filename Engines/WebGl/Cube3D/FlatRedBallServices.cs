using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cube3D
{
    public static class FlatRedBallServices
    {
        public static Texture2D Load(string fileName)
        {
            Texture2D toReturn = new Texture2D();

            toReturn.InitTexture(Renderer.WebGlRenderingContext, fileName);

            return toReturn;
        }
    }
}
