using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX.DirectInput;

using Keys = Microsoft.DirectX.DirectInput.Key;
#endif

#if FRB_XNA
using Microsoft.Xna.Framework.Input;
#endif

namespace FlatRedBall.Input
{
    public class DirectionalKeyGroup : I2DInput, I1DInput
    {
        public Keys UpKey { get; set; }
        public Keys DownKey { get; set; }
        public Keys LeftKey { get; set; }
        public Keys RightKey { get; set; }

        #region I2DInput

        float I2DInput.X
        {
            get 
            {
                if (InputManager.Keyboard.KeyDown(LeftKey))
                {
                    return -1;
                }
                else if (InputManager.Keyboard.KeyDown(RightKey))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        float I2DInput.Y
        {
            get 
            {
                if(InputManager.Keyboard.KeyDown(DownKey))
                {
                    return -1;
                }
                else if (InputManager.Keyboard.KeyDown(UpKey))
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
                if (InputManager.Keyboard.KeyPushed(LeftKey) ||
                    InputManager.Keyboard.KeyReleased(RightKey)
                    )
                {
                    return -1/TimeManager.SecondDifference;
                }
                else if (InputManager.Keyboard.KeyPushed(RightKey) ||
                    InputManager.Keyboard.KeyReleased(LeftKey))
                {
                    return 1/TimeManager.SecondDifference;
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
                if (InputManager.Keyboard.KeyPushed(DownKey) ||
                    InputManager.Keyboard.KeyReleased(UpKey)
                    )
                {
                    return -1 / TimeManager.SecondDifference;
                }
                else if (InputManager.Keyboard.KeyPushed(UpKey) ||
                    InputManager.Keyboard.KeyReleased(DownKey))
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
                var asI2DInput = (I2DInput)this;
                return (float)System.Math.Sqrt(asI2DInput.X * asI2DInput.X + 
                    asI2DInput.Y * asI2DInput.Y);
            }
        }
        #endregion

        float I1DInput.Value
        {
            get 
            {
                var as2D = this as I2DInput;
                return as2D.X;
            }
        }

        float I1DInput.Velocity
        {
            get
            {
                var as2D = this as I2DInput;
                return as2D.XVelocity; 
            }
        }
    }
}
