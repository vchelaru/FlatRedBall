using System;
using System.Collections.Generic;
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

        public float Value => analogStick.Position.X; 

        public float Velocity => analogStick.Velocity.X; 

        bool I1DInput.IsAnalog => true;

    }
}
