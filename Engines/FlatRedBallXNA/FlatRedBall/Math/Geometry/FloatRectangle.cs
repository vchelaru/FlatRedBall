using System;

namespace FlatRedBall.Math.Geometry
{
    /// <summary>
    /// A rectangle class using floats for its bounds. This was previously a class. There might be breaking changes.
    /// </summary>
    public struct FloatRectangle : IEquatable<FloatRectangle>
    {
        #region Fields

        /// <summary>
        /// A Rectangle with its top-left point at (0,0) with a width and height of 1.
        /// </summary>
        public static FloatRectangle Default = new FloatRectangle(0, 1, 0, 1);
        public static FloatRectangle Invalid = new FloatRectangle(float.NaN, float.NaN, float.NaN, float.NaN);
        public float Top;
        public float Bottom;
        public float Left;
        public float Right;

        #endregion

        public FloatRectangle(float top, float bottom, float left, float right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public override string ToString()
        {
            return "Top:" + Top + " Left:" + Left + " Bottom:" + Bottom + " Right:" + Right;
        }

        #region IEquatable<FloatRectangle> Members

        public bool Equals(FloatRectangle other)
        {
            return this.Top == other.Top &&
                this.Bottom == other.Bottom &&
                this.Left == other.Left &&
                this.Right == other.Right;
        }

        #endregion
    }
}
