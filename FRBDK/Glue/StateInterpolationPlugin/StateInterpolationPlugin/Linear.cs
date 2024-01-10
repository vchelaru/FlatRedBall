using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public static class Linear
    {
        public static float EaseNone(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * timeElapsed / durationInSeconds + startingValue;
        }

        public static float EaseIn(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * timeElapsed / durationInSeconds + startingValue;
        }

        public static float EaseOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * timeElapsed / durationInSeconds + startingValue;
        }

        public static float EaseInOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            return amountToAdd * timeElapsed / durationInSeconds + startingValue;
	    }
    }
}
