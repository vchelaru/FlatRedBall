using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math;

namespace FlatRedBall.AI.LineOfSight
{
    /// <summary>
    /// A viewer in a VisibilityGrid.  This typically is a unit which removes fog of war around it.
    /// </summary>
    public interface IViewer : IStaticPositionable
    {
        /// <summary>
        /// The distance in world units that this IViewer can see.
        /// </summary>
        float WorldViewRadius
        {
            get;
            set;
        }

    }
}
