using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework
{
    public static class Vector2ExtensionMethods
    {
        public static Vector2 FromAngle(float angle)
        {
            return new Vector2((float)Math.Cos(angle),
                (float)Math.Sin(angle));
        }

        public static float? Angle(this Vector2 vector)
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

        public static Vector3 ToVector3(this Vector2 vector2)
        {
            var toReturn = new Vector3();

            toReturn.X = vector2.X;
            toReturn.Y = vector2.Y;

            return toReturn;
        }
    }
}
