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

        public float Value
        {
            get { return analogStick.Position.Y; }
        }

        public float Velocity
        {
            get { return analogStick.Velocity.Y; }
        }
    }
}
