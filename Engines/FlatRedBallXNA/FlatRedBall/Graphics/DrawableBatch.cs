using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics
{
    public interface IDrawableBatch
    {
        #region Properties

        /// <summary>
        /// The X value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        float X
        {
            get;
            set;
        }

        /// <summary>
        /// The Y value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        float Y
        {
            get;
            set;
        }

        /// <summary>
        /// The Z value to use for sorting.  This does NOT affect the position
        /// of objects drawn by the DrawableBatch.
        /// </summary>
        float Z
        {
            get;
            set;
        }

        /// <summary>
        /// Whether or not this batch should be updated
        /// </summary>
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

        /// <summary>
        /// Used to update the drawable batch
        /// </summary>
        void Update();

        /// <summary>
        /// Used to destroy any assets that need to be destroyed
        /// </summary>
        void Destroy();

        #endregion
    }
}
