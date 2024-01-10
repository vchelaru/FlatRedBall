using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
    /// <summary>
    /// Interface defining an input device which can return a single value. Typically this value will range between
    /// 0 to +1 for analog buttons and -1 to +1 for directional input devices such as analog sticks.
    /// </summary>
	public interface I1DInput
	{
        /// <summary>
        /// The current value of the input device.
        /// </summary>
		float Value { get; }

        /// <summary>
        /// The change in value from the previous frame in units per second. 
        /// </summary>
		float Velocity { get; }

        /// <summary>
        /// Whether the hardware supports an analog value range (continuous range). If false, then
        /// the device is digital and will return discrete values such as 0 or 1.
        /// </summary>
        bool IsAnalog { get; }
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
        public bool IsAnalog => false;
    }

    public class DelegateBased1DInput : I1DInput
    {
        Func<float> value;
        Func<float> velocity;

        public bool IsAnalog { get; set; }

        float Zero() => 0;

        public DelegateBased1DInput(Func<float> value, Func<float> velocity = null)
        {
            this.value = value;
            this.velocity = velocity ?? Zero;
        }

        public float Value =>  value();

        public float Velocity => velocity();
    }

    public static class I1DInputExtensions
    {
        public static Multiple1DInputs Or(this I1DInput thisInput, I1DInput input)
        {
            Multiple1DInputs toReturn;
            if (thisInput is Multiple1DInputs inputs)
            {
                toReturn = inputs;
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
                for (int i = 0; i < Inputs.Count; i++)
                {
                    I1DInput item = Inputs[i];
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

                for (int i = 0; i < Inputs.Count; i++)
                {
                    I1DInput item = Inputs[i];
                    toReturn = Math.MathFunctions.MaxAbs(toReturn, item.Velocity);
                }
                return toReturn;
            }
        }

        public bool IsAnalog
        {
            get
            {
                for (int i = 0; i < Inputs.Count; i++)
                {
                    I1DInput item = Inputs[i];
                    if (item.IsAnalog)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

