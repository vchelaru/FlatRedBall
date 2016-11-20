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
    public class AnalogButton
    {
        #region Fields

        float mPosition = 0;

        float mLastPosition = 0;

        float mVelocity = 0;

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

            if (TimeManager.SecondDifference != 0)
            {
                mVelocity = (newPosition - mPosition) / TimeManager.SecondDifference;
            }
            mPosition = newPosition;
        }

        #endregion

    }
}
