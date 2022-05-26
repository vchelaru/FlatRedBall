using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math
{
    #region XML Docs
    /// <summary>
    /// Provides an interface for 3D position.
    /// </summary>
    /// <remarks>
    /// Does not include Velocity and Acceleration like IPositionable.
    /// </remarks>
    #endregion
    public interface IStaticPositionable
    {
        /// <summary>
        /// The absolute X position.
        /// </summary>
        float X { get; set;}


        /// <summary>
        /// The absolute Y position.
        /// </summary>
        float Y { get; set;}


        /// <summary>
        /// The absolute Z position.
        /// </summary>
        float Z { get; set;}
    }
}
