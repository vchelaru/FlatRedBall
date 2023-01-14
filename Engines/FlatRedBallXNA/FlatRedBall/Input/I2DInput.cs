using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
    /// <summary>
    /// Provides a common interface for input devices which can return values on the X and Y axis, such as an analog stick.
    /// </summary>
	public interface I2DInput
	{

        /// <summary>
        /// The X value of the input device, typically between 0 and 1.
        /// </summary>
        /// <example>
        /// // Assuming inputDevice is an I2DInput instance
        /// this.XVelocity = inputDevice.X * this.MaxSpeed;
        /// </example>
		float X { get; }

        /// <summary>
        /// The Y value of the input device, typically between 0 and 1.
        /// </summary>
        /// <example>
        /// // Assuming inputDevice is an I2DInput instance
        /// this.YVelocity = inputDevice.Y * this.MaxSpeed;
        /// </example>
        float Y { get; }

        /// <summary>
        /// The rate of change of the input device on the X axis. This measures how fast the user is changing the device. For example,
        /// it can be used to tell how fast the user's thumb is moving on an analog stick.
        /// </summary>
		float XVelocity { get; }

        /// <summary>
        /// The rate of change of the input device on the Y axis. This measures how fast the user is changing the device. For example,
        /// it can be used to tell how fast the user's thumb is moving on an analog stick.
        /// </summary>
		float YVelocity { get; }

        /// <summary>
        /// The distance from (0,0) of the input device. It can be used to detect if any input is being applied on this input device.
        /// </summary>
		float Magnitude { get; }

        // can't do this because it's not supported in older versions of FRB
        //public Vector2 Position => new Vector2(X, Y);
	}

    /// <summary>
    /// Implementation of I2DInput which always returns 0s. Can be used for classes 
    /// requiring an I2DInput implementation 
    /// (like IInputDevice) which should always return 0.
    /// </summary>
    public class Zero2DInput : I2DInput
    {
        public static Zero2DInput Instance = new Zero2DInput();

        public float X => 0;
        public float Y => 0;
        public float XVelocity => 0;
        public float YVelocity => 0;
        public float Magnitude => 0;
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

        float Zero() => 0;
        public DelegateBased2DInput(Func<float> x, Func<float> y)
        {
            this.x = x;
            this.y = y;
            this.xVelocity = Zero;
            this.yVelocity = Zero;
        }


        public DelegateBased2DInput(Func<float> x, Func<float> y, Func<float> xVelocity, Func<float> yVelocity)
        {
            this.x = x;
            this.y = y;
            this.xVelocity = xVelocity;
            this.yVelocity = yVelocity;
        }
    }

    public static class I2DInputExtensions
    {
        public static Multiple2DInputs Or(this I2DInput thisInput, I2DInput input)
        {
            Multiple2DInputs toReturn;
            if(thisInput is Multiple2DInputs)
            {
                toReturn = (Multiple2DInputs)thisInput;
            }
            else
            {
                toReturn = new Multiple2DInputs();
                toReturn.Inputs.Add(thisInput);
            }

            toReturn.Inputs.Add(input);

            return toReturn;
        }

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
                return MathFunctions.NormalizeAngle((float)System.Math.Atan2(instance.Y, instance.X));
            }
        }

        /// <summary>
        /// Creates a new 1DInput that returns the horizontal values from the argument
        /// I2DInput.
        /// </summary>
        /// <param name="instance">The instance to use for the horizontal (X) values</param>
        /// <returns>A new I1DInput which reflects the 2D horizontal values.</returns>
        public static I1DInput CreateHorizontal(this I2DInput instance)
        {
            return new DelegateBased1DInput(
                () => instance.X, () => instance.XVelocity);
        }

        /// <summary>
        /// Creates a new 1DInput that returns the vertical value from the argument
        /// I2DInput.
        /// </summary>
        /// <param name="instance">The instance to use for the vertical (Y) values</param>
        /// <returns>A new I1DInput which reflects the 2D vertical values.</returns>
        public static I1DInput CreateVertical(this I2DInput instance)
        {
            return new DelegateBased1DInput(
                () => instance.Y, () => instance.YVelocity);

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

}

