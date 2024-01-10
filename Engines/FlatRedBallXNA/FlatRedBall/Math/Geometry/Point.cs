using System;
using System.Collections.Generic;
using System.Text;

using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace FlatRedBall.Math.Geometry
{
    public struct Point
    {
        #region Fields

        public static Point Zero = new Point(0, 0);

        public double X;
        public double Y;

        #endregion

        #region Overloaded Operators

        public static bool operator ==(Point p0, Point p1)
        {
            return p0.X == p1.X && p0.Y == p1.Y;
        }

        public static bool operator !=(Point p0, Point p1)
        {
            return !(p0 == p1);
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Point operator -(Point p1, Vector3 v2)
        {
            return new Point(p1.X - v2.X, p1.Y - v2.Y);
        }

        public static Vector3 operator -(Vector3 v1, Point p2)
        {
            return new Vector3(v1.X - (float)p2.X, v1.Y - (float)p2.Y, v1.Z);
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point operator +(Point p1, Vector3 p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point operator *(Point p1, float value)
        {
            return new Point(p1.X * value, p1.Y * value);
        }

        public static Point operator *(Point p1, double value)
        {
            return new Point(p1.X * value, p1.Y * value);
        }

        public static Point operator /(Point p1, float floatValue)
        {
            return new Point(p1.X / floatValue, p1.Y / floatValue);
        }


        #endregion

        #region Properties



        #endregion

        #region Methods

        #region Constructors

        public Point(ref Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
        }

        public Point(ref Vector2 vector)
        {
            X = vector.X;
            Y = vector.Y;
        }

        public Point(float x, float y)
        {
            X = x; Y = y;
        }

        public Point(double x, double y)
        {
            X = x; Y = y;
        }
        #endregion

        #region Public Static Methods


        public static double Dot(Point p1, Point p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y;
        }

        public static double DistanceTo(Point p1, Point p2)
        {
            return System.Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        }

        public static void Normalize(ref Point pointToNormalize, out Point result)
        {
            double length = System.Math.Sqrt(pointToNormalize.X * pointToNormalize.X * +pointToNormalize.Y * pointToNormalize.Y);

            result.X = pointToNormalize.X / length;
            result.Y = pointToNormalize.Y / length;
        }


        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Point))
                return false;
            Point p = (Point)obj;
            return this.X == p.X && this.Y == p.Y;
        }

        public bool Equals(Point p)
        {
            return this.X == p.X && this.Y == p.Y;
        }

        public bool EqualdWithin(Point p, double error)
        {
            return System.Math.Abs(p.X - this.X) < error &&
                System.Math.Abs(p.Y - this.Y) < error;
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode() % 7;
        }

        public double LengthSquared()
        {
            return X * X + Y * Y;
        }

        public void Normalize()
        {

            double length = System.Math.Sqrt(X * X + Y * Y);

            X /= length;
            Y /= length;

        }

        public override string ToString()
        {
            return string.Format(
                "({0}, {1})", X, Y);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)this.X, (float)this.Y, 0);
        }

        #endregion

        #endregion
    }
}
