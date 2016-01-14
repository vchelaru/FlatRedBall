using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Sinusoidal
    {
        public static float EaseIn(float t, float b, float c, float d)
        {
            return -c * (float)System.Math.Cos(t / d * (System.Math.PI / 2)) + c + b;
	    }
        public static float EaseOut(float t, float b, float c, float d)
        {
            return c * (float)System.Math.Sin(t / d * (System.Math.PI / 2)) + b;
	    }
        public static float EaseInOut(float t, float b, float c, float d)
        {
            return -c / 2 * ((float)System.Math.Cos(System.Math.PI * t / d) - 1) + b;
	    }
    }
}
