using EditorObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.ViewModels
{
    public class CameraBoundsViewModel
    {
        #region Fields

        CameraBounds mCameraBounds;

        #endregion

        public bool Orthogonal
        {
            get { return mCameraBounds.Camera.Orthogonal; }
            set
            {
                mCameraBounds.Camera.Orthogonal = value;
                mCameraBounds.UpdateBounds(0);
            }
        }

        public float OrthogonalWidth
        {
            get
            {
                return mCameraBounds.Camera.OrthogonalWidth;
            }
            set
            {
                mCameraBounds.Camera.OrthogonalWidth = value;
                mCameraBounds.UpdateBounds(0);
            }
        }

        public float OrthogonalHeight
        {
            get
            {
                return mCameraBounds.Camera.OrthogonalHeight;
            }
            set
            {
                mCameraBounds.Camera.OrthogonalHeight = value;
                mCameraBounds.UpdateBounds(0);
            }
        }


        public CameraBoundsViewModel(CameraBounds cameraBounds)
        {
            if (cameraBounds.Camera == null)
            {
                throw new ArgumentException("The CameraBounds expected by this object needs to use a Camera");
            }

            mCameraBounds = cameraBounds;

        }
    }
}
