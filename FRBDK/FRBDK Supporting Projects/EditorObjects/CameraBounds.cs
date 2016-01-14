using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Scene;

namespace EditorObjects
{
    public class CameraBounds
    {
        #region Fields

        AxisAlignedRectangle mRectangle;

        // Can use an actual Camera or a CameraSave
        Camera mCamera;
        CameraSave mCameraSave;

        #endregion

        #region Properties

        public Camera Camera
        {
            get { return mCamera; }
        }

        public CameraSave CameraSave
        {
            get { return mCameraSave; }
            set { mCameraSave = value; }
        }

        public bool Visible
        {
            get { return mRectangle.Visible; }
            set { mRectangle.Visible = value; }
        }

        #endregion

        #region Methods

        public CameraBounds(Camera cameraShowing)
        {
            if (cameraShowing == null)
            {
                throw new NullReferenceException("Need a non-null Camera to show");
            }

            mCamera = cameraShowing;

            mRectangle = ShapeManager.AddAxisAlignedRectangle();

        }

        public CameraBounds(CameraSave cameraSave)
        {
            mCameraSave = cameraSave;
            mRectangle = ShapeManager.AddAxisAlignedRectangle();
        }

        public void UpdateBounds(float absoluteZTarget)
        {
            if (mCamera != null)
            {
                mRectangle.X = mCamera.X;
                mRectangle.Y = mCamera.Y;

                mRectangle.ScaleX =
                    Math.Max(0, mCamera.RelativeXEdgeAt(absoluteZTarget));
                mRectangle.ScaleY =
                    Math.Max(0, mCamera.RelativeYEdgeAt(absoluteZTarget));

                mRectangle.Z = absoluteZTarget;
            }
            else if (mCameraSave != null)
            {
                mRectangle.X = mCameraSave.X;
                mRectangle.Y = mCameraSave.Y;

                if (mCameraSave.Orthogonal)
                {
                    mRectangle.Width = mCameraSave.OrthogonalWidth;
                    mRectangle.Height = mCameraSave.OrthogonalHeight;
                }
                else
                {
                    
                    float fieldOfView = (float)System.Math.PI / 4.0f;
                    float yEdge = (float)(100 * System.Math.Tan(fieldOfView / 2.0));
                    float xEdge = yEdge * mCameraSave.AspectRatio;
                    mRectangle.ScaleX = xEdge * (absoluteZTarget - mCameraSave.Z) / 100 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;

                    yEdge = (float)(System.Math.Tan(fieldOfView / 2.0));
                    mRectangle.ScaleY = yEdge * (absoluteZTarget - mCameraSave.Z) / FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
                }
            }
        }


        
        #endregion
    }
}
