using System;

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
}

