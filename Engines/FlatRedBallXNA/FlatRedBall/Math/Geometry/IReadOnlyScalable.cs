using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    #region XML Docs
    /// <summary>
    /// Represents an object which has read-only scale values on two axes.
    /// </summary>
    #endregion
    public interface IReadOnlyScalable
    {
        #region XML Docs
        /// <summary>
        /// Gets the X Scale of the object.
        /// </summary>
        #endregion
        float ScaleX { get;}

        #region XML Docs
        /// <summary>
        /// Gets the Y Scale of the object.
        /// </summary>
        #endregion
        float ScaleY { get;}
    }
}
