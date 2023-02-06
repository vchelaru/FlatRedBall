using System;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Bounce
    {
        public static float EaseOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            if ((timeElapsed /= durationInSeconds) < (1 / 2.75))
            {
                return amountToAdd * (7.5625f * timeElapsed * timeElapsed) + startingValue;
            }
            else if (timeElapsed < (2 / 2.75))
            {
                return amountToAdd * (7.5625f * (timeElapsed -= (1.5f / 2.75f)) * timeElapsed + .75f) + startingValue;
            }
            else if (timeElapsed < (2.5 / 2.75))
            {
                return amountToAdd * (7.5625f * (timeElapsed -= (2.25f / 2.75f)) * timeElapsed + .9375f) + startingValue;
            }
            else
            {
                return amountToAdd * (7.5625f * (timeElapsed -= (2.625f / 2.75f)) * timeElapsed + .984375f) + startingValue;
            }
        }

        public static float EaseIn(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd - EaseOut(durationInSeconds - timeElapsed, 0, amountToAdd, durationInSeconds) + startingValue;
        }

        public static float EaseInOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            if (timeElapsed < durationInSeconds / 2) return EaseIn(timeElapsed * 2, 0, amountToAdd, durationInSeconds) * 0.5f + startingValue;
            else return EaseOut(timeElapsed * 2 - durationInSeconds, 0, amountToAdd, durationInSeconds) * .5f + amountToAdd * 0.5f + startingValue;
        }
    }
}
