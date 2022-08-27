using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public class AnalogStickVertical : I1DInput
    {
        private AnalogStick analogStick;

        public float Deadzone { get; set; }

        public AnalogStickVertical(AnalogStick analogStick)
        {
            this.analogStick = analogStick;
        }

        public float Value
        {
            get
            {
                if (analogStick.Position.Y > Deadzone || analogStick.Position.Y < -Deadzone)
                {
                    return analogStick.Position.Y;
                }
                else
                {
                    return 0;
                }
            }
        }

        public float Velocity => analogStick.Velocity.Y;

        bool I1DInput.IsAnalog => true;

    }
}
