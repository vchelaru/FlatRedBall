using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
namespace FlatRedBall.Math.Splines
{
    #region XML Docs
    /// <summary>
    /// A point in a Spline storing position, velocity, acceleration, and time information.
    /// </summary>
    #endregion
    public class SplinePoint
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The position of the SplinePoint in absolute world coordinates.
        /// </summary>
        #endregion
        public Vector3 Position;

        #region XML Docs
        /// <summary>
        /// The velocity of an object as it passes through this SplinePoint when moving along a Spline.
        /// </summary>
        #endregion
        public Vector3 Velocity;

        #region XML Docs
        /// <summary>
        /// The acceleration set when passing through this SplinePoint.  This property is usually
        /// automatically set by the containing Spline.
        /// </summary>
        #endregion
        public Vector3 Acceleration;

        #region XML Docs
        /// <summary>
        /// The time relative to the start of the Spline when an object moving through the Spline
        /// will pass through this point.
        /// </summary>
        #endregion
        public double Time;

        /// <summary>
        /// Controls whether the Velocity value is unchanged by calling
        /// CalculateVelocities on the Spline.  By default this is false,
        /// which means velocity on this SplinePoint will be set according
        /// to the position of the neighboring SplinePoints.  If this value
        /// is true, then the velocity on this will not be changed by Spline.CalculateVelocities.
        /// </summary>
        public bool UseCustomVelocityValue;

        #endregion

        #region Methods

        public SplinePoint()
        {

        }

        public SplinePoint(float x, float y, float z, double time)
        {
            this.Time = time;

            this.Position.X = x;
            this.Position.Y = y;
            this.Position.Z = z;
        }

        public override string ToString()
        {
            return "Time " + Time;
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
