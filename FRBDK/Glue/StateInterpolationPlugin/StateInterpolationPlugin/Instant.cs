using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.StateInterpolation
{
    public class Instant
    {
        public static float EaseOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            if(timeElapsed == durationInSeconds)
            {
                return startingValue + amountToAdd;
            }
            else
            {
                return startingValue;
            }
        }
        public static float EaseIn(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            if (timeElapsed == durationInSeconds)
            {
                return startingValue;
            }
            else
            {
                return startingValue + amountToAdd;
            }
        }

        public static float EaseInOut(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
        {
            if (timeElapsed == durationInSeconds || timeElapsed== 0)
            {
                return startingValue + amountToAdd;
            }
            else
            {
                return startingValue;
            }
        }
    }
}
