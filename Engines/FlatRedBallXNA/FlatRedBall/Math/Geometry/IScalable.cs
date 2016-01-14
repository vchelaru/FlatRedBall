using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    /// <summary>
    /// Represents an object that can be scaled on the X and Y axes and can have
    /// scale velocity on these two axes.
    /// </summary>
    public interface IScalable : IReadOnlyScalable
    {
        #region XML Docs
        /// <summary>
        /// Gets and sets the X Scale of the object.
        /// </summary>
        #endregion
        new float ScaleX { get; set; }

        #region XML Docs
        /// <summary>
        /// Gets and sets the Y Scale of the object
        /// </summary>
        #endregion
        new float ScaleY { get; set; }

        #region XML Docs
        /// <summary>
        /// Gets and sets the rate at which the X Scale of the object changes in units per second.
        /// </summary>
        #endregion
        float ScaleXVelocity { get; set; }

        #region XML Docs
        /// <summary>
        /// Gets and sets the rate at which the Y Scale of the object changes in units per second.
        /// </summary>
        #endregion
        float ScaleYVelocity { get; set; }
    }
}
