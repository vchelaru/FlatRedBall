using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#elif FRB_MDX
using Vector3 = Microsoft.DirectX.Vector3;
#endif


using FlatRedBall.Math.Splines;

namespace FlatRedBall.Content.Math.Splines
{
    public class SplinePointSave
    {
        #region Fields

        public Vector3 Position;
        public Vector3 Velocity;
        public bool UseCustomVelocityValue;

        // Velocity can be re-calculated so no need to have it here
        //public Vector3 Acceleration;

        public double Time;

        #endregion

        #region Methods

        public static SplinePointSave FromSplinePoint(SplinePoint splinePoint)
        {
            SplinePointSave sps = new SplinePointSave();
            sps.Position = splinePoint.Position;
            sps.Velocity = splinePoint.Velocity;
            sps.Time = splinePoint.Time;
            sps.UseCustomVelocityValue = splinePoint.UseCustomVelocityValue;

            return sps;
        }

        public SplinePoint ToSplinePoint()
        {
            SplinePoint sp = new SplinePoint();
            sp.Position = this.Position;
            sp.Velocity = this.Velocity;
            sp.Time = this.Time;
            sp.UseCustomVelocityValue = this.UseCustomVelocityValue;
            // Acceleration will be calculated later
            return sp;
        }

        #endregion
    }
}
