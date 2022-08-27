using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public class AnalogStickHorizontal : I1DInput
    {
        private AnalogStick analogStick;

        public float Deadzone { get; set; }

        public AnalogStickHorizontal(AnalogStick analogStick)
        {
            this.analogStick = analogStick;
        }

        public float Value
        {
            get
            {
                if (analogStick.Position.X > Deadzone || analogStick.Position.X < -Deadzone)
                {
                    return analogStick.Position.X;
                }
                else
                {
                    return 0;
                }
            }
        }

        public float Velocity => analogStick.Velocity.X; 

        bool I1DInput.IsAnalog => true;

    }
}
