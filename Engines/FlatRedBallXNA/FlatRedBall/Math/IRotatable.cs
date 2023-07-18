using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX;
#else //if FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Math
{
    #region XML Docs
    /// <summary>
    /// Provides an interface for objects which can be rotated in 3D space.  Includes
    /// absolute rotation values and rotational velocity values.
    /// </summary>
    #endregion
    public interface IRotatable
    {
        #region XML Docs
        /// <summary>
        /// Gets and sets the absolute rotation matrix.
        /// </summary>
        /// <remarks>
        /// Implementers should mirror changes to the RotationMatrix in the
        /// individual rotation values.
        /// </remarks>
        #endregion
        Matrix RotationMatrix { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the rotation on the X Axis.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        #endregion
        float RotationX { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the rotation on the Y Axis.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        #endregion
        float RotationY { get; set;}

        /// <summary>
        /// Gets and sets the rotation on the Z Axis in radians.
        /// </summary>
        /// <remarks>
        /// Implementors should mirror changes to invididual rotation values in the
        /// RotationMatrix property.
        /// </remarks>
        float RotationZ { get; set;}


        #region XML Docs
        /// <summary>
        /// Gets and sets the rotational velocity on the X Axis.
        /// </summary>
        #endregion
        float RotationXVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the rotational velocity on the Y Axis.
        /// </summary>
        #endregion
        float RotationYVelocity { get; set;}

        #region XML Docs
        /// <summary>
        /// Gets and sets the rotational velocity on the Z Axis.
        /// </summary>
        #endregion
        float RotationZVelocity { get; set;}


    }


}
