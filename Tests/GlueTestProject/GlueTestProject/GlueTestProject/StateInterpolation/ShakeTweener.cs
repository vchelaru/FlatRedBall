using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.StateInterpolation
{
    public class ShakeTweener : Tweener
    {
        public float MaxAmplitude { get; set; }
        public float Frequency { get; set; }
        
        public float Amplitude
        {
            get;
            set;
        }

        public float Offset;

        public ShakeTweener()
            : base()
        {
            base._tweeningFunction = TweeningFunction;

            Duration = .4f;
            Frequency = 50f + (float)FlatRedBallServices.Random.NextDouble() * 50;
            Amplitude = 40;

            Offset = (float)FlatRedBallServices.Random.NextDouble() * Microsoft.Xna.Framework.MathHelper.Pi;
        }

        float TweeningFunction(float timeElapsed, float start, float change, float duration)
        {
            var value = System.Math.Sin((timeElapsed + Offset) * Frequency);

            var currentAmplitude = Amplitude * (duration - timeElapsed) / duration;

            return (float)(value * currentAmplitude);

        }
    }
}
