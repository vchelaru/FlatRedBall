using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math
{
    #region XML Docs
    /// <summary>
    /// Provides an interface for 3D position, velocity, and acceleration.
    /// </summary>
    /// <remarks>
    /// For an interface which provides only and no velocity and acceleration, see
    /// FlatRedBall.Math.IStaticPositionable.
    /// <seealso cref="FlatRedBall.Math.IStaticPositionable"/>
    /// </remarks>
    #endregion
    public interface IPositionable : IStaticPositionable
    {
        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute X Velocity.  Measured in units per second.
        /// </summary>
        #endregion
        float XVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Y Velocity.  Measured in units per second.
        /// </summary>
        #endregion
        float YVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Z Velocity.  Measured in units per second.
        /// </summary>
        #endregion
        float ZVelocity { get; set;}


        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute X Acceleration.  Measured in units per second.
        /// </summary>
        #endregion
        float XAcceleration { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Y Acceleration.  Measured in units per second.
        /// </summary>
        #endregion
        float YAcceleration { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Z Acceleration.  Measured in units per second.
        /// </summary>
        #endregion
        float ZAcceleration { get; set;}
    }
}
