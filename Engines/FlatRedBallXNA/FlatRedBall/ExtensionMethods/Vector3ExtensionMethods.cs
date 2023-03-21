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

        /// <summary>
        /// Returns the angle in radians of the argument vector, where 0 is to the right, 
        /// and increasing the angle moves counterclockwise. 
        /// The Z value is ignored.
        /// </summary>
        /// <param name="vector">The argument vector.</param>
        /// <returns>The angle in radians, or null if the Vector has X and Y values both equal to 0.</returns>
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

        /// <summary>
        /// Returns the angle in radians of the argument vector, where 0 is to the right,
        /// and increasing the angle moves counterclockwise. The Z value is ignored. If the
        /// vector is of length 0, then a value of 0 is returned.
        /// </summary>
        /// <param name="vector">The argument vector.</param>
        /// <returns>The angle in radians, or 0 if the vector has X and Y values both equal to 0.</returns>
        public static float AngleOrZero(this Vector3 vector) => Angle(vector) ?? 0;

        /// <summary>
        /// Converts this Vector3 to a Vector2 by copying the X and Y values.
        /// </summary>
        /// <param name="vector3">The Vector3 to convert</param>
        /// <returns>A Vector2 with the same X adn Y values</returns>
        public static Vector2 ToVector2(this Vector3 vector3)
        {
            var toReturn = new Vector2();

            toReturn.X = vector3.X;
            toReturn.Y = vector3.Y;

            return toReturn;
        }

        /// <summary>
        /// Returns a new Vector3 of Length 0, or throws an InvalidOperationException if this Vector3 has a Length of 0.
        /// </summary>
        /// <param name="vector3">The Vector3 to normalize</param>
        /// <returns>The normalized vector.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Vector3 has a Length of 0. </exception>
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

        public static Vector3 NormalizedOrRight(this Vector3 vector3)
        {
            if (vector3.X != 0 || vector3.Y != 0 || vector3.Z != 0)
            {
                vector3.Normalize();
                return vector3;
            }
            else
            {
                return Vector3.Right;
            }
        }

        public static Vector3 RotatedBy(this Vector3 vector3, float radiansToRotateBy)
        {
            if(vector3.X == 0 && vector3.Y == 0)
            {
                return vector3;
            }
            else
            {
                var existingAngle = vector3.Angle().Value;
                var newAngle = existingAngle + radiansToRotateBy;
                return FromAngle(newAngle) * vector3.Length();
            }
        }

        /// <summary>
        /// Returns a unit vector with a Z value of 0 pointing in the direction
        /// specified by the radians value.
        /// </summary>
        /// <param name="radians">The direction in radians, where 0 is to the right, and
        /// values increase counterclockwise.</param>
        /// <returns>A new Vector3 pointing in the desired direction.</returns>
        public static Vector3 FromAngle(float radians)
        {
            return new Vector3(
                (float)Math.Cos(radians),
                (float)Math.Sin(radians),
                0
                );
        }

        /// <summary>
        /// Returns a vector in the same direction as the argument vector, but of the length specified by the length argument.
        /// This can safely be called on vectors with length 0, as the right direction will be used.
        /// </summary>
        /// <param name="vector3">The vector specifying the direction.</param>
        /// <param name="length">The desired length.</param>
        /// <returns>The resulting vector in the same direction as the argument of the desired length, or a vector pointing to the right if the argument has 0 length.</returns>
        public static Vector3 AtLength(this Vector3 vector3, float length)
        {
            return vector3.NormalizedOrRight() * length;
        }

        public static Vector3 AtLength(this Vector3 vector3, double length)
        {
            return vector3.NormalizedOrRight() * (float)length;
        }

        public static Vector3 AtAngle(this Vector3 vector3, float angleRadians)
        {
            return Vector3ExtensionMethods.FromAngle(angleRadians) * vector3.Length();
        }

        public static Vector3 AtAngleDegrees(this Vector3 vector3, float angleDegrees)
        {
            return Vector3ExtensionMethods.FromAngle(MathHelper.ToRadians(angleDegrees)) * vector3.Length();
        }

        public static Vector3 AddX(this Vector3 vector3, float xValue)
        {
            vector3.X += xValue;
            return vector3;
        }

        public static Vector3 AddY(this Vector3 vector3, float yValue)
        {
            vector3.Y += yValue;
            return vector3;
        }

        public static Vector3 AddZ(this Vector3 vector3, float zValue)
        {
            vector3.Z += zValue;
            return vector3;
        }


        public static Vector3 AtX(this Vector3 vector3, float xValue)
        {
            vector3.X = xValue;
            return vector3;
        }

        public static Vector3 AtY(this Vector3 vector3, float yValue)
        {
            vector3.Y = yValue;
            return vector3;
        }

        public static Vector3 AtZ(this Vector3 vector3, float zValue)
        {
            vector3.Z = zValue;
            return vector3;
        }
    }
}
