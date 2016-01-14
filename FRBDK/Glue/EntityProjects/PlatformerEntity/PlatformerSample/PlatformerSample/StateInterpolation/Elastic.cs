using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public class Elastic
    {
        public static float EaseIn(float t, float b, float c, float d)
        {
		    if (t==0) 
            {
                return b;
            }
            if ((t /= d) == 1) 
            {
                return b+c;
            }
            float p = d * .3f;
            float s = p / 4;
            return -(float)(c * System.Math.Pow(2, 10 * (t -= 1)) * System.Math.Sin((t * d - s) * (2 * System.Math.PI) / p)) + b;
        }

        public static float EaseOut(float t, float b, float c, float d)
        {
		    if (t==0) 
            {
                return b;
            }
            if ((t /= d) == 1) 
            {
                return b+c;
            }
            float p = d * .3f;
            float s = p / 4;
            return (float)(c * System.Math.Pow(2, -10 * t) * System.Math.Sin((t * d - s) * (2 * System.Math.PI) / p) + c + b);
	    }

        public static float EaseInOut(float t, float b, float c, float d)
        {
            if (t == 0)
            {
                return b;
            }
            if ((t /= d / 2) == 2)
            {
                return b + c;
            }
            float p = d * (.3f * 1.5f);
            float a = c;
            float s = p / 4;
            if (t < 1)
            {
                return -.5f * (float)(a * System.Math.Pow(2, 10 * (t -= 1)) * System.Math.Sin((t * d - s) * (2 * System.Math.PI) / p)) + b;
            }
            return (float)(a * System.Math.Pow(2, -10 * (t -= 1)) * System.Math.Sin((t * d - s) * (2 * System.Math.PI) / p) * .5 + c + b);
	    }
    }
}
