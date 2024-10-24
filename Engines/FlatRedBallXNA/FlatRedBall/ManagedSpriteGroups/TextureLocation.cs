using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
namespace FlatRedBall.ManagedSpriteGroups
{
    #region XML Docs
    /// <summary>
    /// Used to paint SpriteGrids or compare differences between two TextureGrids which can be used for undos.
    /// </summary>
    /// <typeparam name="T">The type contained in the TextureLocation.  Currently FlatRedBall uses
    /// Texture2D, FloatRectangle, and AnimationChain in SpriteGrids.</typeparam>
    #endregion
    public class TextureLocation<T>
    {
        #region Fields

        T mTexture;
        float mX;
        float mY;

        #endregion

        public float X
        {
            get { return mX; }
            set { mX = value; }
        }

        public float Y
        {
            get { return mY; }
            set { mY = value; }
        }

        public T Texture
        {
            get { return mTexture; }
            set { mTexture = value; }
        }

        public TextureLocation(T texture, float x, float y)
        {
            this.mTexture = texture;
            this.mX = x;
            this.mY = y;

        }
    }
}
