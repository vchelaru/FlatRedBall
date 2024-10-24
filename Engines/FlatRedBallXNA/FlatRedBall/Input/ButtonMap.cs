using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    public class KeyboardButtonMap
    {
        #region Fields

        Keys mLeftAnalogUp = Keys.None;
        Keys mLeftAnalogDown = Keys.None;
        Keys mLeftAnalogLeft = Keys.None;
        Keys mLeftAnalogRight = Keys.None;

        Keys mRightAnalogUp = Keys.None;
        Keys mRightAnalogDown = Keys.None;
        Keys mRightAnalogLeft = Keys.None;
        Keys mRightAnalogRight = Keys.None;

        Keys mA = Keys.None;
        Keys mB = Keys.None;
        Keys mX = Keys.None;
        Keys mY = Keys.None;

        Keys mLeftShoulder = Keys.None;
        Keys mRightShoulder = Keys.None;

        Keys mLeftTrigger = Keys.None;
        Keys mRightTrigger = Keys.None;

        Keys mBack = Keys.None;
        Keys mStart = Keys.None;

        Keys mLeftStick = Keys.None;
        Keys mRightStick = Keys.None;

        Keys mDPadUp = Keys.None;
        Keys mDPadDown = Keys.None;
        Keys mDPadLeft = Keys.None;
        Keys mDPadRight = Keys.None;


        #endregion

        #region Properties

        #region Left Analog
        public Keys LeftAnalogUp
        {
            get { return mLeftAnalogUp; }
            set { mLeftAnalogUp = value; }
        }
        public Keys LeftAnalogDown
        {
            get { return mLeftAnalogDown; }
            set { mLeftAnalogDown = value; }
        }
        public Keys LeftAnalogLeft
        {
            get { return mLeftAnalogLeft; }
            set { mLeftAnalogLeft = value; }
        }
        public Keys LeftAnalogRight
        {
            get { return mLeftAnalogRight; }
            set { mLeftAnalogRight = value; }
        }
        #endregion

        #region Right Analog
        public Keys RightAnalogUp
        {
            get { return mRightAnalogUp; }
            set { mRightAnalogUp = value; }
        }
        public Keys RightAnalogDown
        {
            get { return mRightAnalogDown; }
            set { mRightAnalogDown = value; }
        }
        public Keys RightAnalogRight
        {
            get { return mRightAnalogRight; }
            set { mRightAnalogRight = value; }
        }
        public Keys RightAnalogLeft
        {
            get { return mRightAnalogLeft; }
            set { mRightAnalogLeft = value; }
        }
        #endregion

        public Keys LeftTrigger
        {
            get { return mLeftTrigger; }
            set { mLeftTrigger = value; }
        }

        public Keys RightTrigger
        {
            get { return mRightTrigger; }
            set { mRightTrigger = value; }
        }

        #region A, B, X, Y
        public Keys A
        {
            get { return mA; }
            set { mA = value; }
        }
        public Keys B
        {
            get { return mB; }
            set { mB = value; }
        }
        public Keys X
        {
            get { return mX; }
            set { mX = value; }
        }
        public Keys Y
        {
            get { return mY; }
            set { mY = value; }
        }
        #endregion

        public Keys LeftShoulder
        {
            get { return mLeftShoulder; }
            set { mLeftShoulder = value; }
        }
        public Keys RightShoulder
        {
            get { return mRightShoulder; }
            set { mRightShoulder = value; }
        }

        public Keys Back
        {
            get { return mBack; }
            set { mBack = value; }
        }
        public Keys Start
        {
            get { return mStart; }
            set { mStart = value; }
        }

        public Keys LeftStick
        {
            get { return mLeftStick; }
            set { mLeftStick = value; }
        }
        public Keys RightStick
        {
            get { return mRightStick; }
            set { mRightStick = value; }
        }

        public Keys DPadUp
        {
            get { return mDPadUp; }
            set { mDPadUp = value; }
        }
        public Keys DPadDown
        {
            get { return mDPadDown; }
            set { mDPadDown = value; }
        }
        public Keys DPadLeft
        {
            get { return mDPadLeft; }
            set { mDPadLeft = value; }
        }
        public Keys DPadRight
        {
            get { return mDPadRight; }
            set { mDPadRight = value; }
        }

        #endregion
    }
}
