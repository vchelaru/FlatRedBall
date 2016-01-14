using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX;
#elif FRB_XNA || WINDOWS_PHONE
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics;
#endif

namespace FlatRedBall.Content.Scene
{
    public class SpriteVertexSave
    {
        public Vector3 Vector;

        public float ScaleX;
        public float ScaleY;

        public float TextureU;
        public float TextureV;

        public float TextureUVelocity;
        public float TextureVVelocity;

        public TextureStateSave TextureState;

        public static SpriteVertexSave FromSpriteVertex(SpriteVertex spriteVertex)
        {
            SpriteVertexSave svs = new SpriteVertexSave();
            svs.TextureU = spriteVertex.TextureCoordinate.X;
            svs.TextureV = spriteVertex.TextureCoordinate.Y;

            return svs;
        }
    }
}
