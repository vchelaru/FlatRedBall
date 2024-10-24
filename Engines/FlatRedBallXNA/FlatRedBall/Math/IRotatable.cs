using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace FlatRedBall.Math
{
    /// <summary>
    /// Provides an interface for objects which can be rotated in 3D space.  Includes
    /// absolute rotation values and rotational velocity values.
    /// </summary>
    public interface IRotatable
    {
        /// <summary>
        /// Gets and sets the absolute rotation matrix.
        /// </summary>
        /// <remarks>
        /// Implementers should mirror changes to the RotationMatrix in the
        /// individual rotation values.
        /// </remarks>
        Matrix RotationMatrix { get; set;}

        /// <summary>
        /// Gets and sets the rotation on the X Axis.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        float RotationX { get; set;}

        /// <summary>
        /// Gets and sets the rotation on the Y Axis.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        float RotationY { get; set;}

        /// <summary>
        /// Gets and sets the rotation on the Z Axis in radians.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        float RotationZ { get; set;}


        /// <summary>
        /// Gets and sets the rotational velocity on the X Axis.
        /// </summary>
        float RotationXVelocity { get; set;}

        /// <summary>
        /// Gets and sets the rotational velocity on the Y Axis.
        /// </summary>
        float RotationYVelocity { get; set;}

        /// <summary>
        /// Gets and sets the rotational velocity on the Z Axis.
        /// </summary>
        float RotationZVelocity { get; set;}


    }


}
