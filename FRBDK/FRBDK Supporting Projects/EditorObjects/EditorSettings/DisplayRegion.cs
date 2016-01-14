using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall.Math.Geometry;



namespace EditorObjects.EditorSettings
{
    [Serializable]
    public class DisplayRegion
    {
        public float Top = 0;
        public float Bottom = 1;
        public float Left = 0;
        public float Right = 1;

        public string Name;

        public DisplayRegion()
        {
            
        }

        public DisplayRegion(FloatRectangle floatRectangle)
        {
            Top = floatRectangle.Top;
            Bottom = floatRectangle.Bottom;
            Left = floatRectangle.Left;
            Right = floatRectangle.Right;
        }

        public DisplayRegion(float top, float bottom, float left, float right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public FloatRectangle ToFloatRectangle()
        {
            return new FloatRectangle(Top, Bottom, Left, Right);
        }
    }
}
