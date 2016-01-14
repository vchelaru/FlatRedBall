using System;
using System.Collections.Generic;
using System.Text;
#if FRB_MDX
using Microsoft.DirectX;
#elif FRB_XNA || ZUNE
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Graphics
{
    public interface IDrawableBatch
    {
        #region Properties

        #region XML Docs
        /// <summary>
        /// The X value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        #endregion
        float X
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The Y value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        #endregion
        float Y
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The Z value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        #endregion
        float Z
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Whether or not this batch should be updated
        /// </summary>
        #endregion
        bool UpdateEveryFrame
        {
            get;
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Used to draw assets
        /// Batch is sorted by Z with sprites and text
        /// </summary>
        /// <param name="camera">The currently drawing camera</param>
        #endregion
        void Draw(Camera camera);

        #region XML Docs
        /// <summary>
        /// Used to update the drawable batch
        /// </summary>
        #endregion
        void Update();

        #region XML Docs
        /// <summary>
        /// Used to destroy any assets that need to be destroyed
        /// </summary>
        #endregion
        void Destroy();

        #endregion
    }
}
