using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics
{
    // Vic:  This used to be a class.  It caused some heap allocation which
    // is probably not needed, so let's just make it a struct and save heap alloc and lower the number
    // of live references in the engine for the GC to crawl.

    public struct SpriteVertex
    {
        internal Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector2 Scale;// = new Vector2(1, 1);
        internal Vector2 ScaleVelocity;

        public Vector4 Color;
        internal Vector4 ColorRate;

        internal SpriteVertex(SpriteVertex vertex)
        {
            Position = vertex.Position;
            TextureCoordinate = vertex.TextureCoordinate;
            Scale = vertex.Scale;
            ScaleVelocity = vertex.ScaleVelocity;

            Color = vertex.Color;
            ColorRate = vertex.ColorRate;
        }
    }
}
