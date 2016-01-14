using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics
{
    #region XML Docs
    /// <summary>
    /// Provides an interface for objects which can be have their appearance
    /// modified by alpha and color values using a variety of operations.
    /// </summary>
    #endregion
    public interface IColorable
    {
        #region XML Docs
        /// <summary>
        /// The alpha value to use with the BlendOperation.
        /// </summary>
        #endregion
        float Alpha
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The red value to use with the ColorOperation.
        /// </summary>
        #endregion
        float Red
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The green value to use with the ColorOperation.
        /// </summary>
        #endregion
        float Green
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The blue value to use with the color operation.
        /// </summary>
        #endregion
        float Blue
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the alpha component in units per second.  A negative value will make the object disappear over time.
        /// </summary>
        #endregion
        float AlphaRate
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the red component in units per second.
        /// </summary>
        #endregion
        float RedRate
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the green component in units per second.
        /// </summary>
        #endregion
        float GreenRate
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the blue component in units per second.
        /// </summary>
        #endregion
        float BlueRate
        {
            get;
            set;
        }


        #region XML Docs
        /// <summary>
        /// The color operation to perform using the color component values and 
        /// Texture (if available).
        /// </summary>
        #endregion
#if FRB_MDX
        Microsoft.DirectX.Direct3D.TextureOperation ColorOperation
#else
        ColorOperation ColorOperation
#endif
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The blend operation to perform using the alpha component value and
        /// Texture (if available).
        /// </summary>
        #endregion
        BlendOperation BlendOperation
        {
            get;
            set;
        }

    }
}
