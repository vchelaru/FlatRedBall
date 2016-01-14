using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
#if FRB_XNA
#elif FRB_MDX
using Vector3 = Microsoft.DirectX.Vector3;
using FlatRedBall.Math;
#endif

namespace FlatRedBall.Graphics
{

    internal class BatchForwardVectorSorter : IComparer<IDrawableBatch>
    {
        Camera mCamera;

        public BatchForwardVectorSorter(Camera camera)
        {
            mCamera = camera;
        }

        #region XML Docs
        /// <summary>
        /// Larger comes first.
        /// </summary>
        /// <param name="first">The first instance.</param>
        /// <param name="second">The second instance.</param>
        /// <returns>-1 if the first comes first, 1 if the second comes first, 0 if they're equal.</returns>
        #endregion
        public int Compare(IDrawableBatch first, IDrawableBatch second)
        {
            Vector3 firstCameraRelativePosition = new Vector3(
                first.X - mCamera.X,
                first.Y - mCamera.Y,
                first.Z - mCamera.Z);

            Vector3 secondCameraRelativePosition = new Vector3(
                second.X - mCamera.X,
                second.Y - mCamera.Y,
                second.Z - mCamera.Z);
            
            float firstDistance;
#if FRB_MDX
            Vector3 forwardVector = mCamera.RotationMatrix.Forward();
            Vector3Extensions.Dot(ref firstCameraRelativePosition, ref forwardVector, out firstDistance);

            float secondDistance;
            Vector3Extensions.Dot(ref secondCameraRelativePosition, ref forwardVector, out secondDistance);
#else
            Vector3 forwardVector = mCamera.RotationMatrix.Forward;
                        Vector3.Dot(ref firstCameraRelativePosition, ref forwardVector, out firstDistance);

            float secondDistance;
            Vector3.Dot(ref secondCameraRelativePosition, ref forwardVector, out secondDistance);
#endif


            if (firstDistance < secondDistance)
                return 1;
            else if (firstDistance > secondDistance)
                return -1;
            else
                return 0;

        }
    }

}
