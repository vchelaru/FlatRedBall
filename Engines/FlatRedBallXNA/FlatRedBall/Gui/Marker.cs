using System;

using FlatRedBall;

#if FRB_XNA || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for Marker.
	/// </summary>
	public class Marker
    {
        #region XML Docs
        /// <summary>
        /// The distance from the center to the edge on the X axis.  Increasing
        /// this makes the Marker wider.
        /// </summary>
        #endregion
        public float ScaleX = .3f;

        #region XML Docs
        /// <summary>
        /// The distance from the center to the edge on the Y axis.  Increasing
        /// this makes the Marker taller.
        /// </summary>
        #endregion
        public float ScaleY = .4f;

        #region XML Docs
        /// <summary>
        /// The object that the Marker represents.
        /// </summary>
        /// <remarks>
        /// This can be used if an object is to be selected on a MarkerTimeLine, like an animation frame.
        /// </remarks>
        #endregion
		public Object ReferenceObject;

        #region XML Docs
        /// <summary>
        /// The value in the MarkerTimeLine that the Marker is located.
        /// </summary>
        /// <remarks>
        /// Usually the value represents time.  Therefore, if a Marker is being used to show
        /// when an animation frame executes, this value should be the animation frame's time.
        /// </remarks>
        #endregion
        public double Value;

        // Currently unused but will be supported in the future.
        /// <summary>
        /// UNUSED
        /// </summary>
        public Texture2D Texture;

	}
}
