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

