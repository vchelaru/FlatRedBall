using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	public interface I2DInput
	{
		float X { get; }
		float Y { get; }
		float XVelocity { get; }
		float YVelocity { get; }

		float Magnitude { get; }
	}

    public class DelegateBased2DInput : I2DInput
    {
        Func<float> x;
        Func<float> y;
        Func<float> xVelocity;
        Func<float> yVelocity;

        public float X
        {
            get
            {
                return this.x();
            }
        }

        public float Y
        {
            get
            {
                return this.y();
            }
        }

        public float XVelocity
        {
            get
            {
                return this.xVelocity();
            }
        }

        public float YVelocity
        {
            get
            {
                return this.yVelocity();
            }
        }

        public float Magnitude
        {
            get
            {
                float xValue = x();
                float yValue = y();

                float toReturn = (float)System.Math.Sqrt(xValue * xValue + yValue * yValue);

                return toReturn;
            }
        }

        public DelegateBased2DInput(Func<float> x, Func<float> y, Func<float> xVelocity, Func<float> yVelocity)
        {
            this.x = x;
            this.y = y;
            this.xVelocity = xVelocity;
            this.yVelocity = yVelocity;
        }

    }

    /// <summary>
    /// Provides a single I2DInput implementation which can read from multiple I2DInputs at once.
    /// </summary>
    /// <remarks>
    /// This is useful for games which want to read from multiple devices, such as letting all controllers
    /// control one character, or letting keyboard and gamepad control a character at the same time.
    /// </remarks>
    public class Multiple2DInputs : I2DInput
    {
        public float X
        {
            get 
            {
                float toReturn = 0;
                foreach(var input in Inputs)
                {
                    if(System.Math.Abs(input.X) > (System.Math.Abs(toReturn)))
                    {
                        toReturn = input.X;
                    }
                }
                return toReturn;
            }
        }

        public float Y
        {
            get
            {
                float toReturn = 0;
                foreach (var input in Inputs)
                {
                    if (System.Math.Abs(input.Y) > (System.Math.Abs(toReturn)))
                    {
                        toReturn = input.Y;
                    }
                }
                return toReturn;
            }
        }

        public float XVelocity
        {
            get
            {
                float toReturn = 0;
                foreach (var input in Inputs)
                {
                    if (System.Math.Abs(input.XVelocity) > (System.Math.Abs(toReturn)))
                    {
                        toReturn = input.XVelocity;
                    }
                }
                return toReturn;
            }
        }

        public float YVelocity
        {
            get
            {
                float toReturn = 0;
                foreach (var input in Inputs)
                {
                    if (System.Math.Abs(input.YVelocity) > (System.Math.Abs(toReturn)))
                    {
                        toReturn = input.YVelocity;
                    }
                }
                return toReturn;
            }
        }

        public float Magnitude
        {
            get
            {
                float toReturn = 0;
                foreach (var input in Inputs)
                {
                    if (System.Math.Abs(input.Magnitude) > (System.Math.Abs(toReturn)))
                    {
                        toReturn = input.Magnitude;
                    }
                }
                return toReturn;
            }
        }
        
        /// <summary>
        /// Contains the list of inputs to read from. Any number of inputs can be added to this using the Add method.
        /// </summary>
        /// <example>
        /// // Assuming that keyboard2DInput and gamepad2DInput exist:
        /// var multipleInputs = new Multiple2DInputs();
        /// multipleInputs.Inputs.Add(keyboard2DInput);
        /// multipleInputs.Inputs.Add(gamepad2DInput);
        /// </example>
        public List<I2DInput> Inputs
        {
            get;
            private set;
        }

        public Multiple2DInputs()
        {
            Inputs = new List<I2DInput>();
        }
    }

    public static class I2DInputExtensions
    {
        /// <summary>
        /// Returns the angle in radians of the input object, where 0 is to the right, rotating counterclockwise.
        /// Returns null if the X and Y values are 0 (meaning the input device is centered)
        /// </summary>
        /// <param name="instance">The I2DInput instance</param>
        /// <returns>The angle, or null if X and Y are 0</returns>
        public static float? GetAngle(this I2DInput instance)
        {
            if(instance.X == 0 && instance.Y == 0)
            {
                return null;
            }
            else
            {
                return (float)System.Math.Atan2(instance.Y, instance.X);
            }
        }
    }
}

