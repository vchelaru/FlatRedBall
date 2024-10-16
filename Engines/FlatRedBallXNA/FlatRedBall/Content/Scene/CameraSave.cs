using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Content.Scene
{
    #region XML Docs
    /// <summary>
    /// An XML serializable "Save" class which can be included in other Save classes to store Camera information.
    /// </summary>
    #endregion
    public class CameraSave
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The absolute X position.
        /// </summary>
        #endregion
        public float X;

        #region XML Docs
        /// <summary>
        /// The absolute Y position.
        /// </summary>
        #endregion
        public float Y;

        #region XML Docs
        /// <summary>
        /// The absolute Z position.
        /// </summary>
        #endregion
        public float Z;

        #region XML Docs
        /// <summary>
        /// Whether the Camera is using an orthogonal projection matrix.  If this is false, the Camera is using a perspective projection matrix.
        /// </summary>
        #endregion
        public bool Orthogonal;

        #region XML Docs
        /// <summary>
        /// The orthogonal height of the camera's view.  
        /// </summary>
        #endregion
        public float OrthogonalHeight = 600;

		public float OrthogonalWidth = -1;

        public float NearClipPlane = 1;

        public float FarClipPlane = 1000;

        public float AspectRatio = 4/3.0f;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new CameraSave.  This is used by the XmlSerializer when deserializing an XML.
        /// Usually CameraSaves are created using the FromCamera static method.
        /// </summary>
        public CameraSave()
        { 
        }

        /// <summary>
        /// Creates a new CameraSave instance using members from the passed Camera argument.
        /// </summary>
        /// <param name="camera">The Camera to copy properties from.</param>
        public static CameraSave FromCamera(Camera camera)
        {
			return FromCamera(camera, false);
		}

		public static CameraSave FromCamera(Camera camera, bool setWidth)
		{
            CameraSave cameraSave = new CameraSave();
            cameraSave.X = (float)camera.X;
            cameraSave.Y = (float)camera.Y;
            cameraSave.Z = (float)camera.Z;
            cameraSave.Orthogonal = camera.Orthogonal;
            cameraSave.OrthogonalHeight = camera.OrthogonalHeight;

			if (setWidth)
			{
				cameraSave.OrthogonalWidth = camera.OrthogonalWidth;
			}

            cameraSave.NearClipPlane = camera.NearClipPlane;
            cameraSave.FarClipPlane = camera.FarClipPlane;

            return cameraSave;
        }

        /// <summary>
        /// Sets the argument Camera's properties to the properties stored in this CameraSave.
        /// </summary>
        /// <remarks>
        /// Usually "Save" classes include a To[RuntimeType] method.  The CameraSave does not follow
        /// this pattern because it's most common that a CameraSave is loaded when a Camera is already
        /// created by the engine.  In this case, it's not very convenient to have to destroy the existing
        /// Camera and replace it by a new instance.  Instead, the SetCamera method will simply set the properties
        /// on an existing Camera
        /// </remarks>
        /// <param name="camera">The Camera to set the properties on.</param>
        public void SetCamera(Camera camera)
        {
            camera.Position.X = X;
            camera.Position.Y = Y;
            camera.Position.Z = Z;





            camera.Orthogonal = Orthogonal;

            // Why do we fix aspect ratio *before* setting
            // the width and height?  Shouldn't we do it after?
            //camera.FixAspectRatioYConstant();
	
            // Tools may not want to specify what the OrthogonalWidth/Height
            // are because they want the Camera to load in 2D.
            if (OrthogonalWidth > 0)
			{
				camera.OrthogonalWidth = OrthogonalWidth;
			}
            if (OrthogonalHeight > 0)
            {
                camera.OrthogonalHeight = OrthogonalHeight;
            }

            // This was above where the Ortho values are set, not sure why
            camera.FixAspectRatioYConstant();

            camera.NearClipPlane = NearClipPlane;
            camera.FarClipPlane = FarClipPlane;


        }

        #endregion
    }
}
