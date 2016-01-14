using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;

namespace ToolTemplate
{
    public class EditorOptions
    {
        #region Fields
        private bool mSortYSpritesSecondary = false;
        #endregion


        public bool Filtering
        {
            get { return FlatRedBallServices.GraphicsOptions.TextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear; }
            set
            {
                if (value)
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
                else
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
            }
        }

        public bool SortYSpritesSecondary
        {
            get { return mSortYSpritesSecondary; }
            set { mSortYSpritesSecondary = value; }
        }

        public Color BackgroundColor
        {
            get { return SpriteManager.Camera.BackgroundColor; }
            set { SpriteManager.Camera.BackgroundColor = value; }
        }
    }
}
