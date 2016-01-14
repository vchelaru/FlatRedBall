using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Input
{
    #region XML Docs
    /// <summary>
    /// Defines an interface for objects which can be selectable by the cursor.
    /// </summary>
    #endregion
    public interface ICursorSelectable : IPositionable, IReadOnlyScalable, IRotatable
    {
        #region XML Docs
        /// <summary>
        /// Whether the instance is currently selectable (active).
        /// </summary>
        #endregion
        bool CursorSelectable { get; set;}
    }
}
