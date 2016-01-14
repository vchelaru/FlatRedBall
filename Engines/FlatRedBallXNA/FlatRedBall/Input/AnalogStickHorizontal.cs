using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Input
{
    public class AnalogStickHorizontal : I1DInput
    {
        private AnalogStick analogStick;

        public AnalogStickHorizontal(AnalogStick analogStick)
        {
            this.analogStick = analogStick;
        }

        public float Value
        {
            get { return analogStick.Position.X; }
        }

        public float Velocity
        {
            get { return analogStick.Velocity.X; }
        }
    }
}
