using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	public interface I1DInput
	{
		float Value { get; }
		float Velocity { get; }
	}

    public class DelegateBased1DInput : I1DInput
    {
        Func<float> value;
        Func<float> velocity;

        public DelegateBased1DInput(Func<float> value, Func<float> velocity)
        {
            this.value = value;
            this.velocity = velocity;
        }

        public float Value
        {
            get
            {
                return value();
            }
        }

        public float Velocity
        {
            get
            {
                return velocity();
            }
        }
    }

    public class Multiple1DInputs : I1DInput
    {
        public List<I1DInput> Inputs
        {
            get;
            private set;
        }

        public float Value
        {
            get
            {
                float toReturn = 0;
                foreach(var item in Inputs)
                {
                    toReturn = Math.MathFunctions.MaxAbs(toReturn, item.Value);
                }
                return toReturn;
            }
        }

        public float Velocity
        {
            get
            {
                float toReturn = 0;

                foreach (var item in Inputs)
                {
                    toReturn = Math.MathFunctions.MaxAbs(toReturn, item.Velocity);
                }
                return toReturn;
            }
        }
    }
}

