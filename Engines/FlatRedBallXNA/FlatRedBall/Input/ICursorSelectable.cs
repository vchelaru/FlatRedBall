using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Input
{
    /// <summary>
    /// Defines an interface for objects which can be selectable by the cursor.
    /// </summary>
    public interface ICursorSelectable : IPositionable, IReadOnlyScalable, IRotatable
    {
        /// <summary>
        /// Whether the instance is currently selectable (active).
        /// </summary>
        bool CursorSelectable { get; set;}
    }
}
