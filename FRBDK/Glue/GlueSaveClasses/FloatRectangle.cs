using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GlueSaveClasses
{
    #region XML Docs
    /// <summary>
    /// A rectangle class using floats for its bounds.  
    /// </summary>
    #endregion
    [TypeConverter(typeof(FloatRectangleTypeConverter))]
    public struct FloatRectangle : IEquatable<FloatRectangle>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// A Rectangle with its top-left point at (0,0) with a width and height of 1.
        /// </summary>
        #endregion
        public static FloatRectangle Default = new FloatRectangle();

        public float Y;
        public float Height;
        public float X;
        public float Width;

        #endregion


        public FloatRectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return "X:" + X + " Y:" + Y + " Width:" + Width + " Height:" + Height;
        }

        #region IEquatable<FloatRectangle> Members

        public bool Equals(FloatRectangle other)
        {
            return this.X == other.X &&
                this.Y == other.Y &&
                this.Width == other.Width &&
                this.Height == other.Height;
        }

        #endregion
    }
}
