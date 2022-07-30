using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    public static class PointExtensionMethods
    {
        public static float? Angle(this Point vector)
        {
            if (vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return (float)System.Math.Atan2(vector.Y, vector.X);
            }
        }
    }
}
