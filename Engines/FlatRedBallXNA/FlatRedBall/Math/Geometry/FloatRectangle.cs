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

        #region Properties

        public float Width { get => Right - Left; }
        public float Height { get => Top - Bottom; }

        /// <summary>
        /// A <see cref="Point"/> located in the center of this <see cref="Rectangle"/>.
        /// </summary>
        public Point Center
        {
            get
            {
                return new Point(Left + (Width / 2), Top - (Height / 2));
            }
            set
            {
                float halfWidth = Width / 2;
                float halfHeight = Height / 2;

                Top = (float)value.Y + halfHeight;
                Bottom = (float)value.Y - halfHeight;
                Right = (float)value.X + halfWidth;
                Left = (float)value.X - halfWidth;
            }
        }

        #endregion

        public FloatRectangle(float top, float bottom, float left, float right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Checks whether this rectangle's defined area overlaps with another <see cref="FloatRectangle"/>.
        /// </summary>
        /// <param name="value">Rectangle to check.</param>
        /// <returns></returns>
        public bool Intersects(FloatRectangle value)
        {
            return value.Left < Right &&
                   Left < value.Right &&
                   value.Bottom < Top &&
                   Bottom < value.Top;
        }

        /// <summary>
        /// Gets whether or not the provided <see cref="FloatRectangle"/> lies within the bounds of this <see cref="FloatRectangle"/>.
        /// </summary>
        /// <param name="value">The <see cref="FloatRectangle"/> to check for inclusion in this <see cref="FloatRectangle"/>.</param>
        /// <returns><c>true</c> if the provided <see cref="FloatRectangle"/>'s bounds lie entirely inside this <see cref="FloatRectangle"/>; <c>false</c> otherwise.</returns>
        public bool Contains(FloatRectangle value)
        {
            return value.Left >= Left && value.Right <= Right && value.Top <= Top && value.Bottom >= Bottom;
        }

        /// <summary>
        /// Adjusts the edges of this <see cref="FloatRectangle"/> by specified horizontal and vertical amounts. 
        /// </summary>
        /// <param name="horizontalAmount">Value to adjust the left and right edges.</param>
        /// <param name="verticalAmount">Value to adjust the top and bottom edges.</param>
        public void Inflate(float horizontalAmount, float verticalAmount)
        {
            Left -= horizontalAmount;
            Right += horizontalAmount;
            Top += verticalAmount;
            Bottom -= verticalAmount;
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
