using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX;
#else//if FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Math.Geometry
{
    public struct Point3D
    {
        #region Fields

        public double X;
        public double Y;
        public double Z;

        #endregion

        #region Properties and Overloaded Operators
        public static Point3D operator -(Point3D p1, Vector3 p2)
        {
            return new Point3D(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Point3D operator -(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }


        public static Point3D operator +(Point3D p1, Vector3 v2)
        {
            return new Point3D(p1.X + v2.X, p1.Y + v2.Y);
        }

        #endregion

        #region Methods

        #region Constructors

        public Point3D(Vector2 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = 0;
        }

        public Point3D(Vector3 vector3)
        {
            X = vector3.X;
            Y = vector3.Y;
            Z = vector3.Z;
        }

        public Point3D(double x, double y)
        {
            X = x;
            Y = y;
            Z = 0;
        }        
        
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        public double Length()
        {
            return System.Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public double LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }

        public Vector2 ToVector2()
        {
            return new Vector2((float)X, (float)Y);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        #endregion
    }
}
