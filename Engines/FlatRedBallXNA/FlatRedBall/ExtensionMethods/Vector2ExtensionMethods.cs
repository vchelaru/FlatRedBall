using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework
{
    public static class Vector2ExtensionMethods
    {

        /// <summary>
        /// Returns the angle in radians of the argument vector, where 0 is to the right, 
        /// and increasing the angle moves counterclockwise. 
        /// </summary>
        /// <param name="vector">The argument vector.</param>
        /// <returns>The angle in radians between 0 and 2 * PI, or null if the Vector has X and Y values both equal to 0.</returns>
        public static float? Angle(this Vector2 vector)
        {
            if (vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return MathFunctions.RegulateAngle( (float) System.Math.Atan2(vector.Y, vector.X));
            }
        }

        /// <summary>
        /// Returns the angle in degrees of the argument vector, where 0 is to the right,
        /// and increasing the angle moves counterclockwise.
        /// </summary>
        /// <param name="vector">The argument angle.</param>
        /// <returns>The angle in degrees between 0 and 360, or null if the Vector has X and Y values both equal to 0.</returns>
        public static float? AngleDegrees(this Vector2 vector)
        {
            if (vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return MathHelper.ToDegrees(MathFunctions.RegulateAngle((float)System.Math.Atan2(vector.Y, vector.X)));
            }
        }

        public static Vector3 ToVector3(this Vector2 vector2)
        {
            var toReturn = new Vector3();

            toReturn.X = vector2.X;
            toReturn.Y = vector2.Y;

            return toReturn;
        }

        /// <summary>
        /// Returns a normalized vector. Throws an exception if the argument vector has a length of 0.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns></returns>
        public static Vector2 Normalized(this Vector2 vector)
        {
            if(vector.X != 0 || vector.Y != 0)
            {
                vector.Normalize();
                return vector;
            }
            else
            {
                throw new InvalidOperationException("This vector is of length 0, so it cannot be normalized");
            }
        }

        /// <summary>
        /// Attempts to normalize the vector, or returns Vector2.Zero if the argument vector has a lenth of 0.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>A normalized vector (length 1) or Vector2.Zero if the argument vector has a length of 0.</returns>
        public static Vector2 NormalizedOrZero(this Vector2 vector)
        {
            if (vector.X != 0 || vector.Y != 0)
            {
                vector.Normalize();
                return vector;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        public static Vector2 NormalizedOrRight(this Vector2 vector)
        {
            if (vector.X != 0 || vector.Y != 0)
            {
                vector.Normalize();
                return vector;
            }
            else
            {
                return new Vector2(1,0);
            }
        }

        public static Vector2 RotatedBy(this Vector2 vector2, float radiansToRotateBy)
        {
            if (vector2.X == 0 && vector2.Y == 0)
            {
                return vector2;
            }
            else
            {
                var existingAngle = vector2.Angle().Value;
                var newAngle = existingAngle + radiansToRotateBy;
                return FromAngle(newAngle) * vector2.Length();
            }
        }

        /// <summary>
        /// Returns a unit vector pointing in the direction specified by the radians argument.
        /// </summary>
        /// <param name="angleRadians">The direction in radians, where 0 is to the right, and values
        /// increase counterclockwise.</param>
        /// <returns></returns>
        public static Vector2 FromAngle(float angleRadians)
        {
            return new Vector2((float)Math.Cos(angleRadians),
                (float)Math.Sin(angleRadians));
        }

        /// <summary>
        /// Returns a unit vector pointing in the direction specified by the degrees argument.
        /// </summary>
        /// <param name="angleDegrees">The direction in degrees, where 0 is to the right, and values
        /// increasing counterclockwise.</param>
        /// <returns></returns>
        public static Vector2 FromAngleDegrees(float angleDegrees)
        {
            var angleRadians = MathHelper.ToRadians(angleDegrees);
            return new Vector2(
                (float)Math.Cos(angleRadians),
                (float)Math.Sin(angleRadians));
        }

        /// <summary>
        /// Returns a vector in the same direction as the argument vector, but of the length specified by the length argument.
        /// </summary>
        /// <param name="vector2">The vector specifying the direction.</param>
        /// <param name="length">The desired length.</param>
        /// <returns>The resulting vector in the same direction as the argument of the desired length, or a vector of 0 length if the argument has 0 length.</returns>
        public static Vector2 AtLength(this Vector2 vector2, float length)
        {
            return vector2.NormalizedOrZero() * length;
        }

        public static Vector2 AtAngle(this Vector2 vector2, float angleRadians)
        {
            return Vector2ExtensionMethods.FromAngle(angleRadians) * vector2.Length();
        }

        public static Vector2 AtAngleDegrees(this Vector2 vector2, float angleDegrees)
        {
            return Vector2ExtensionMethods.FromAngleDegrees(angleDegrees) * vector2.Length();
        }
    }
}
