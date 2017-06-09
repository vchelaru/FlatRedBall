using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    #region XML Docs
    /// <summary>
    /// A button or single-axis input device which can return a range of values like the shoulder triggers.
    /// </summary>
    #endregion
    public class AnalogButton : I1DInput, IPressableInput
    {
        #region Fields

        float mPosition = 0;

        float mLastPosition = 0;

        float mVelocity = 0;

        bool lastDPadDown = false;

        #endregion

        #region Properties

        internal float LastPosition
        {
            get { return mLastPosition; }
        }

        /// <summary>
        /// The current value of the AnalogButton, with ranges 0 - 1
        /// </summary>
        public float Position
        {
            get { return mPosition; }
        }

        public float Velocity
        {
            get { return mVelocity; }
        }

        public bool IsDown
        {
            get
            {
                if (lastDPadDown)
                {
                    return mPosition > AnalogStick.DPadOffValue;
                }
                else
                {
                    return mPosition > AnalogStick.DPadOnValue;
                }

            }
        }

        public bool WasJustPressed
        {
            get
            {
                return !lastDPadDown && IsDown;
            }
        }

        public bool WasJustReleased
        {
            get
            {
                return lastDPadDown && !IsDown;
            }
        }

        float I1DInput.Value
        {
            get
            {
                return Position;
            }
        }

        #endregion

        #region Methods

        public void Clear()
        {
            mPosition = 0;
            mVelocity = 0;
        }

        public void Update(float newPosition)
        {
            mLastPosition = mPosition;
            lastDPadDown = IsDown;

            if (TimeManager.SecondDifference != 0)
            {
                mVelocity = (newPosition - mPosition) / TimeManager.SecondDifference;
            }
            mPosition = newPosition;
        }

        #endregion

    }
}
