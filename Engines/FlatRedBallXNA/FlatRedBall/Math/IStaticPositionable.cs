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
        #region XML Docs
        /// <summary>
        /// The absolute X position.
        /// </summary>
        #endregion
        float X { get; set;}


        #region XML Docs
        /// <summary>
        /// The absolute Y position.
        /// </summary>
        #endregion
        float Y { get; set;}


        #region XML Docs
        /// <summary>
        /// The absolute Z position.
        /// </summary>
        #endregion
        float Z { get; set;}
    }
}
