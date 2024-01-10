using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Back
    {
        public static float EaseIn(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * (timeElapsed /= durationInSeconds) * timeElapsed * ((1.70158f + 1) * timeElapsed - 1.70158f) + startingValue;
        }

        public static float EaseOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * ((timeElapsed = timeElapsed / durationInSeconds - 1) * timeElapsed * ((1.70158f + 1) * timeElapsed + 1.70158f) + 1) + startingValue;
        }

        public static float EaseInOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            float s = 1.70158f;
            if ((timeElapsed /= durationInSeconds / 2) < 1)
            {
                return amountToAdd / 2 * (timeElapsed * timeElapsed * (((s *= (1.525f)) + 1) * timeElapsed - s)) + startingValue;
            }
            return amountToAdd / 2 * ((timeElapsed -= 2) * timeElapsed * (((s *= (1.525f)) + 1) * timeElapsed + s) + 2) + startingValue;
        }
    }
}
