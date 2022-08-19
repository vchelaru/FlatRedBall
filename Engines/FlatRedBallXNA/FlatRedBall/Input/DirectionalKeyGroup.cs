using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    /// <summary>
    /// An object implementing I2DInput which can be used to read 2D input from the keyboard.
    /// Instances of DirectionalKeyGroup are typically created through the Keyboard's 
    /// Get2DInput method.
    /// </summary>
    public class DirectionalKeyGroup : I2DInput, I1DInput
    {
        public Keys UpKey { get; set; }
        public Keys DownKey { get; set; }
        public Keys LeftKey { get; set; }
        public Keys RightKey { get; set; }

        public bool IsRadial { get; set; } = true;

        #region I2DInput

        const float sinOf45Degrees = 0.707106781f;

        float I2DInput.X
        {
            get 
            {
                float magnitude = 1;

                if(IsRadial && (InputManager.Keyboard.KeyDown(DownKey) ||
                    InputManager.Keyboard.KeyDown(UpKey)))
                {
                    magnitude = sinOf45Degrees;
                }

                if (InputManager.Keyboard.KeyDown(LeftKey))
                {
                    return -magnitude;
                }
                else if (InputManager.Keyboard.KeyDown(RightKey))
                {
                    return magnitude;
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
                float magnitude = 1;


                if (IsRadial && (InputManager.Keyboard.KeyDown(LeftKey) ||
                    InputManager.Keyboard.KeyDown(RightKey)))
                {
                    magnitude = sinOf45Degrees;
                }

                if (InputManager.Keyboard.KeyDown(DownKey))
                {
                    return -magnitude;
                }
                else if (InputManager.Keyboard.KeyDown(UpKey))
                {
                    return magnitude;
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

        bool I1DInput.IsAnalog => false;

    }
}
