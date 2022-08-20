using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public class AnalogStickVertical : I1DInput
    {
        private AnalogStick analogStick;

        public AnalogStickVertical(AnalogStick analogStick)
        {
            this.analogStick = analogStick;
        }

        public float Value => analogStick.Position.Y;

        public float Velocity => analogStick.Velocity.Y;

        bool I1DInput.IsAnalog => true;

    }
}
