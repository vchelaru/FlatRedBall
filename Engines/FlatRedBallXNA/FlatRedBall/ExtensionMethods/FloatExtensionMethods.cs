using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    public static class FloatExtensionMethods
    {
        /// <summary>
        /// Converts a float value from degrees to radians.
        /// </summary>
        /// <param name="degrees">A degrees value as a float</param>
        /// <returns>The value in radians</returns>
        public static float ConvertToRadians(this float degrees)
        {
            return degrees * (float)(System.Math.PI / 180f);
        }

        /// <summary>
        /// Converts a float value from radians to degrees.
        /// </summary>
        /// <param name="radians">A radians value as a float</param>
        /// <returns>The value in degrees</returns>
        public static float ConvertToDegrees(this float radians)
        {
            return radians * (float)(180f / System.Math.PI);
        }

        /// <summary>
        /// Clamps a float value to be within the min and max parameters.
        /// </summary>
        /// <param name="f">The float value to clamp</param>
        /// <param name="min">The minimum value to clamp to</param>
        /// <param name="max">The maximum value to clamp to</param>
        /// <returns>A float that falls within the provided range</returns>
        public static float Clamp(this float f, float min, float max)
        {
            f = System.Math.Max(min, f);
            f = System.Math.Min(max, f);
            return f;
        }

        public static float NormalizeAngle(this float radians)
        {
            return FlatRedBall.Math.MathFunctions.RegulateAngle(radians);
        }
    }
}
