using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Collision
{
    #region XML Docs
    /// <summary>
    /// A class storing enumerations for collisions.
    /// </summary>
    #endregion
    public static class CollisionEnumerations
    {

        public enum Side
        {
            None,
            Left,
            Right,
            Top,
            Bottom
        }

        public enum Side3D
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            Front,
            Back
        }
    }
}
