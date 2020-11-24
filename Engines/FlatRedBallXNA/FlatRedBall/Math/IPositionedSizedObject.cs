using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math
{
    public interface IPositionedSizedObject
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }

        float Width { get; }
        float Height { get; }
    }
}
