using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Content.Scene;

namespace FlatRedBall.Content.SpriteFrame
{
    #region XML Docs
    /// <summary>
    /// Serves as the base class for SpriteFrameSave and SpriteFrameSaveContent.
    /// </summary>
    /// <typeparam name="T">The type of the ParentSprite.  This is generic because
    /// for SpriteFrameSave the generic type is SpriteSave, but for SpriteFrameSaveContent the
    /// generic type is SpriteSaveContent.</typeparam>
    #endregion
    public class SpriteFrameSaveBase<T> where T : SpriteSaveBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The BorderSides that this instance represents.
        /// </summary>
        /// <remarks>
        /// <seealso cref="FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides"/>
        /// </remarks>
        #endregion
        public int BorderSides;

        #region XML Docs
        /// <summary>
        /// The SpriteSaveBase that stores most of the Properties for the SpriteSave.
        /// </summary>
        /// <remarks>
        /// The SpriteSaveBase is used as a storage of properties because nearly all of its
        /// properties are also used by SpriteFrames.  Therefore, to prevent a lot of copy/paste,
        /// this class is used to store properties.
        /// </remarks>
        #endregion
        public T ParentSprite;

        public float TextureBorderWidth;

        public float SpriteBorderWidth;
        #endregion
    }
}
