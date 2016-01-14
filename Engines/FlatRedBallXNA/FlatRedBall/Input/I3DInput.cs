using System;

namespace FlatRedBall.Input
{
	public interface I3DInput
	{
		float X { get; }
		float Y { get; }
		float Z { get; }
		float XVelocity { get; }
		float YVelocity { get; }
		float ZVelocity { get; }

		float Magnitude { get; }
	}
}

