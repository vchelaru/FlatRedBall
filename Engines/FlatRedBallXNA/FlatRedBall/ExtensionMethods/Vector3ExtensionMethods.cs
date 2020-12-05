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

        /// <summary>
        /// Reeturns the length of the vector ignoring the Z value. The returned value is the same as first setting Z to 0 and calling Length.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
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

        public static Vector3 Normalized(this Vector3 vector3)
        {
            if(vector3.X != 0 || vector3.Y != 0 || vector3.Z != 0)
            {
                vector3.Normalize();
                return vector3;
            }
            else
            {
                throw new InvalidOperationException("This vector is of length 0, so it cannot be normalized");
            }
        }

        public static Vector3 NormalizedOrZero(this Vector3 vector3)
        {
            if (vector3.X != 0 || vector3.Y != 0 || vector3.Z != 0)
            {
                vector3.Normalize();
                return vector3;
            }
            else
            {
                return Vector3.Zero;
            }
        }

        public static Vector3 FromAngle(float angle)
        {
            return new Vector3(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle),
                0
                );
        }
    }
}
