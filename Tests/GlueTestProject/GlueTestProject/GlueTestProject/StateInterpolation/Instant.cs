using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.StateInterpolation
{
    public class Instant
    {
        public static float EaseOut(float t, float b, float c, float d)
        {
            if(t == d)
            {
                return b + c;
            }
            else
            {
                return b;
            }
        }
        public static float EaseIn(float t, float b, float c, float d)
        {
            if (t == d)
            {
                return b;
            }
            else
            {
                return b + c;
            }
        }

        public static float EaseInOut(float t, float b, float c, float d)
        {
            if (t == d || t== 0)
            {
                return b + c;
            }
            else
            {
                return b;
            }
        }
    }
}
