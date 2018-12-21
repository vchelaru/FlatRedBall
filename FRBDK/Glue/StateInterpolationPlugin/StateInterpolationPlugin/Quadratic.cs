using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Quadratic
    {
        public static float EaseIn(float timeElapsed, float startingValue, float endingValue, float durationInSeconds)
        {
		    return endingValue*(timeElapsed/=durationInSeconds)*timeElapsed + startingValue;
	    }

        public static float EaseOut(float timeElapsed, float startingValue, float endingValue, float durationInSeconds)
        {
		    return -endingValue *(timeElapsed/=durationInSeconds)*(timeElapsed-2) + startingValue;
	    }

        public static float EaseInOut(float timeElapsed, float startingValue, float endingValue, float durationInSeconds)
        {
            if ((timeElapsed /= durationInSeconds / 2) < 1)
            {
                return endingValue / 2 * timeElapsed * timeElapsed + startingValue;
            }
		    return -endingValue/2 * ((--timeElapsed)*(timeElapsed-2) - 1) + startingValue;
	    }
    }
}
