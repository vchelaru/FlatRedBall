using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework
{
    public static class Vector3ExtensionMethods
    {
        /// <summary>
        /// Returnes the length squared of the 2D distance. In other words, the Z component is ignored.
        /// </summary>
        /// <param name="vectorToMeasure">The vector to </param>
        /// <returns>The length squared, ignoring Z.</returns>
        public static float LengthSquared2D(this Vector3 vectorToMeasure)
        {
            return (vectorToMeasure.X * vectorToMeasure.X) +
                (vectorToMeasure.Y * vectorToMeasure.Y);
        }

        public static float Length2D(this Vector3 vector)
        {
            return (float)System.Math.Sqrt((vector.X * vector.X) +
                (vector.Y * vector.Y));
        }

        public static float? Angle(this Vector3 vector)
        {
            if(vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return (float)System.Math.Atan2(vector.Y, vector.X);
            }
        }

        public static Vector2 ToVector2(this Vector3 vector3)
        {
            var toReturn = new Vector2();

            toReturn.X = vector3.X;
            toReturn.Y = vector3.Y;

            return toReturn;
        }
    }
}
