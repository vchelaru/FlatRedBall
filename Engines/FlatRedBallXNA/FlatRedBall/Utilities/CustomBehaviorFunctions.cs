using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Utilities
{
    #region XML Docs
    /// <summary>
    /// Static class containing a collection of common methods used as CustomBehaviors.
    /// </summary>
    #endregion
    public static class CustomBehaviorFunctions
    {
        #region XML Docs
        /// <summary>
        /// Removes the argument Sprite when its Alpha equals 0.
        /// </summary>
        /// <param name="sprite">The Sprite to remove.</param>
        #endregion
        public static void RemoveWhenInvisible(Sprite sprite)
        {
            if (sprite.Alpha == 0)
                SpriteManager.RemoveSprite(sprite);
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Sprite when it is outside of the default Camera's view (SpriteManager.Camera).
        /// </summary>
        /// <param name="sprite">The Sprite to remove.</param>
        #endregion
        public static void RemoveWhenOutsideOfScreen(Sprite sprite)
		{
			if(SpriteManager.Camera.IsSpriteInView(sprite, false) == false)		
                SpriteManager.RemoveSprite(sprite);
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Sprite after it cycles its AnimationChain.  
        /// </summary>
        /// <param name="sprite">The Sprite to remove.</param>
        #endregion
        public static void RemoveWhenJustCycled(Sprite sprite)
        {
            if (sprite.JustCycled)
                SpriteManager.RemoveSprite(sprite);
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Sprite if it is opaque.
        /// </summary>
        /// <remarks>
        /// This method is commonly used with Sprites which have a 
        /// positive AlphaRate and which should be removed when completely visible.
        /// </remarks>
        /// <param name="si">Sprite to remove.</param>\
        #endregion
        public static void RemoveWhenAlphaIs1(Sprite si)
        {
            // Don't test against 1 because that's inaccurate.  Let's just test a very small range.
            if (si.Alpha > .999f)
            {
                SpriteManager.RemoveSprite(si);
            }
        }

    }
}
