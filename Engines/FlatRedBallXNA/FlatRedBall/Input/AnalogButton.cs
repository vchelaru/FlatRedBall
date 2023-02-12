using System;
using System.Collections.Generic;
using System.Text;
using static FlatRedBall.Input.Xbox360GamePad;

namespace FlatRedBall.Input
{
    /// <summary>
    /// A button or single-axis input device which can return a range of values.
    /// Common examples include shoulder triggers on the Xbox360GamePad.
    /// </summary>
    public class AnalogButton : I1DInput, IRepeatPressableInput
    {
        #region Fields

        float mPosition = 0;

        float mLastPosition = 0;

        float mVelocity = 0;

        bool lastDPadDown = false;

        public string Name { get; set; }

        double mLastButtonPush;
        double mLastRepeatRate;

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

        public bool WasJustPressed => !lastDPadDown && IsDown;

        public bool WasJustReleased => lastDPadDown && !IsDown;



        float I1DInput.Value => Position;

        bool I1DInput.IsAnalog => true;

        public bool WasJustPressedOrRepeated
        {
            get
            {
                const double timeAfterPush = .35;
                const double timeBetweenRepeating = .12;
                bool repeatedThisFrame = mLastButtonPush == TimeManager.CurrentTime;

                if (repeatedThisFrame ||
                (
                    IsDown &&
                    TimeManager.CurrentTime - mLastButtonPush > timeAfterPush &&
                    TimeManager.CurrentTime - mLastRepeatRate > timeBetweenRepeating)
                    )
                {
                    mLastRepeatRate = TimeManager.CurrentTime;
                    return true;
                }
                return false;

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

            if(IsDown)
            {
                mLastButtonPush = TimeManager.CurrentTime;
            }
        }

        #endregion

    }
}
