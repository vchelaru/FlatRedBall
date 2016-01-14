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
        /// Gets and sets the absolute X Velocity.  Meausred in units per second.
        /// </summary>
        #endregion
        float XVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Y Velocity.  Meausred in units per second.
        /// </summary>
        #endregion
        float YVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Z Velocity.  Meausred in units per second.
        /// </summary>
        #endregion
        float ZVelocity { get; set;}


        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute X Acceleration.  Meausred in units per second.
        /// </summary>
        #endregion
        float XAcceleration { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Y Acceleration.  Meausred in units per second.
        /// </summary>
        #endregion
        float YAcceleration { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute Z Acceleration.  Meausred in units per second.
        /// </summary>
        #endregion
        float ZAcceleration { get; set;}
    }
}
