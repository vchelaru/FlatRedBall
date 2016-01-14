using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Input
{
    public class Xbox360DPadReference : I2DInput
    {
        public Xbox360GamePad GamePad { get; set; }
        public float X
        {
            get 
            { 
                if(GamePad.ButtonDown(Xbox360GamePad.Button.DPadLeft))
                {
                    return -1;   
                }
                else if(GamePad.ButtonDown(Xbox360GamePad.Button.DPadRight))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public float Y
        {
            get 
            {
                if (GamePad.ButtonDown(Xbox360GamePad.Button.DPadDown))
                {
                    return -1;
                }
                else if (GamePad.ButtonDown(Xbox360GamePad.Button.DPadUp))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        float I2DInput.XVelocity
        {
            get 
            {
                if (GamePad.ButtonPushed(Xbox360GamePad.Button.DPadLeft))
                {
                    return -1 / TimeManager.SecondDifference;
                }
                else if (GamePad.ButtonPushed(Xbox360GamePad.Button.DPadRight))
                {
                    return 1 / TimeManager.SecondDifference;
                }
                else
                {
                    return 0;
                }
            }
        }

        float I2DInput.YVelocity
        {
            get 
            {
                if (GamePad.ButtonPushed(Xbox360GamePad.Button.DPadDown))
                {
                    return -1 / TimeManager.SecondDifference;
                }
                else if (GamePad.ButtonPushed(Xbox360GamePad.Button.DPadUp))
                {
                    return 1 / TimeManager.SecondDifference;
                }
                else
                {
                    return 0;
                }
            }
        }

        float I2DInput.Magnitude
        {
            get 
            {
                // normally we'd square the components but since they're 0,1 or -2 we just take the sign
                var sum = System.Math.Sign(this.X) + System.Math.Sign(this.Y);

                return (float) System.Math.Sqrt(sum);
            }
        }
    }
}
