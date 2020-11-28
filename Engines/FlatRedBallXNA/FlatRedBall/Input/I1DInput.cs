using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	public interface I1DInput
	{
		float Value { get; }
		float Velocity { get; }
	}

    /// <summary>
    /// Implementation of I1DInput which always returns 0s. Can be used for classes
    /// requiring an I1DInput implementation
    /// (like IInputDevice) which should always return 0.
    /// </summary>
    public class Zero1DInput : I1DInput
    {
        public static Zero1DInput Instance = new Zero1DInput();
        public float Value => 0;
        public float Velocity => 0;
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

    public static class I1DInputExtensions
    {
        public static Multiple1DInputs Or(this I1DInput thisInput, I1DInput input)
        {
            Multiple1DInputs toReturn;
            if (thisInput is MultiplePressableInputs)
            {
                toReturn = (Multiple1DInputs)thisInput;
            }
            else
            {
                toReturn = new Multiple1DInputs();
                toReturn.Inputs.Add(thisInput);
            }

            toReturn.Inputs.Add(input);

            return toReturn;
        }
    }

    public class Multiple1DInputs : I1DInput
    {
        public List<I1DInput> Inputs
        {
            get;
            private set;
        } = new List<I1DInput>();

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

