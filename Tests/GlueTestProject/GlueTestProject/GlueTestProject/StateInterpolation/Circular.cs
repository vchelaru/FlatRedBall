using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Circular
    {
        public static float EaseIn(float t, float b, float c, float d)
        {
            return -c * ((float)System.Math.Sqrt(1 - (t /= d) * t) - 1) + b;
	    }

        public static float EaseOut(float t, float b, float c, float d)
        {
            return c * (float)System.Math.Sqrt(1 - (t = t / d - 1) * t) + b;
	    }

        public static float EaseInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
            {
                return -c / 2 * ((float)System.Math.Sqrt(1 - t * t) - 1) + b;
            }
            return c / 2 * ((float)System.Math.Sqrt(1 - (t -= 2) * t) + 1) + b;
	    }
    }
}
