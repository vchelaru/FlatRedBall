using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Tile;

#if FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.ManagedSpriteGroups
{
    #region XML Docs
    /// <summary>
    /// A Texture2D and relative integer position.
    /// </summary>
    /// <remarks>
    /// This struct is generally used to detect patterns on FlatRedBall.ManagedSpriteGroups.SpriteGrids.
    /// </remarks>
    #endregion
    public class GridRelativeTexture : GridRelativeState<Texture2D>
    {

        #region XML Docs
        /// <summary>
        /// Creates a new GridRelativeTexture.
        /// </summary>
        /// <param name="x">The relative X position to assign.</param>
        /// <param name="y">The relative Y position to assign.</param>
        /// <param name="texture2D">The Texture2D reference to assign.</param>
        #endregion
        public GridRelativeTexture(int x, int y, Texture2D texture2D) : 
            base(x,y,texture2D)
        {
        }
    }
}
