using System;
using System.Collections.Generic;
using System.Text;

//#if SILVERLIGHT
//using SilverArcade.SilverSprite;
//using Vector2 = SilverArcade.SilverSprite.Vector2;
//#else 
using Microsoft.Xna.Framework;
using FlatRedBall.Math;
//#endif

namespace FlatRedBall.Input
{
    #region XML Docs
    /// <summary>
    /// A two-axis input device which can return a range of values on both axes.
    /// </summary>
    #endregion
    public class AnalogStick : I2DInput
    {
        #region Fields

        Vector2 mPosition;
        Vector2 mVelocity;
        double mMagnitude;
        double mAngle;

        AnalogButton leftAsButton;
        AnalogButton rightAsButton;
        AnalogButton upAsButton;
        AnalogButton downAsButton;

        #region XML Docs
        /// <summary>
        /// The DPadOnValue and DPadOffValue
        /// values are used to simulate D-Pad control
        /// with the analog stick.  When the user is above
        /// the absolute value of the mDPadOnValue then it is
        /// as if the DPad is held down.  To release the DPad the
        /// value must come under the off value.  If there was only
        /// one value then the user could hold the stick near the threshold
        /// and get rapid on/off values due to the inaccuracy of the analog stick. 
        /// </summary>
        #endregion
        internal const float DPadOnValue = .550f;
        internal const float DPadOffValue = .450f;

        bool[] mLastDPadDown = new bool[4];
        bool[] mCurrentDPadDown = new bool[4];

        /* The following would be useful to add
         * float mAngularVelocity;
         * float mDeadzone;
         */

        double[] mLastDPadPush = new double[4];
        double[] mLastDPadRepeatRate = new double[4];

        #endregion

        #region Properties

        public AnalogButton LeftAsButton
        {
            get { return leftAsButton; }
        }

        public AnalogButton RightAsButton
        {
            get { return rightAsButton; }
        }

        public AnalogButton UpAsButton
        {
            get { return upAsButton; }
        }

        public AnalogButton DownAsButton
        {
            get { return downAsButton; }
        }

        #region XML Docs
        /// <summary>
        /// Returns the angle of the analog stick.  0 is to the right, Pi/2 is up, Pi is to the left, and 3*Pi/2 is down.
        /// </summary>
        #endregion
        public double Angle
        {
            get { return mAngle; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the distance from the center position of the analog stick.
        /// </summary>
        #endregion
        public double Magnitude
        {
            get { return mMagnitude; }
        }

        #region XML Docs
        /// <summary>
        /// The position of the analog stick.  The range for each component is -1 to 1.
        /// </summary>
        #endregion
        public Vector2 Position
        {
            get { return mPosition; }
        }


        public Vector2 Velocity
        {
            get { return mVelocity; }
        }


        #endregion

        #region Methods

        public AnalogStick()
        {
            leftAsButton = new AnalogButton();
            rightAsButton = new AnalogButton();
            upAsButton = new AnalogButton();
            downAsButton = new AnalogButton();
        }

        public bool AsDPadDown(Xbox360GamePad.DPadDirection direction)
        {
            switch (direction)
            {
                case Xbox360GamePad.DPadDirection.Left:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Left])
                    {
                        return mPosition.X < -DPadOffValue;
                    }
                    else
                    {
                        return mPosition.X < -DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Right:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Right])
                    {
                        return mPosition.X > DPadOffValue;
                    }
                    else
                    {
                        return mPosition.X > DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Up:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Up])
                    {
                        return mPosition.Y > DPadOffValue;
                    }
                    else
                    {
                        return mPosition.Y > DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Down:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Down])
                    {
                        return mPosition.Y < -DPadOffValue;
                    }
                    else
                    {
                        return mPosition.Y < -DPadOnValue;
                    }

                    //break;
                default:
                    
                    return false;
                    //break;
            }
        }

        public bool AsDPadPushed(Xbox360GamePad.DPadDirection direction)
        {
            // If the last was not down and this one is, then report a push.
            return mLastDPadDown[(int)direction] == false && AsDPadDown(direction);
        }

        public bool AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection direction)
        {
            // Ignoring is performed inside this call.
            return AsDPadPushedRepeatRate(direction, .35, .12);
        }


        public bool AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection direction, double timeAfterPush, double timeBetweenRepeating)
        {
            if (AsDPadPushed(direction))
                return true;

            // If this method is called multiple times per frame this line
            // of code guarantees that the user will get true every time until
            // the next TimeManager.Update (next frame).
            bool repeatedThisFrame = mLastDPadPush[(int)direction] == TimeManager.CurrentTime;

            if (repeatedThisFrame ||
                (
                AsDPadDown(direction) &&
                TimeManager.CurrentTime - mLastDPadPush[(int)direction] > timeAfterPush &&
                TimeManager.CurrentTime - mLastDPadRepeatRate[(int)direction] > timeBetweenRepeating)
                )
            {
                mLastDPadRepeatRate[(int)direction] = TimeManager.CurrentTime;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            mPosition.X = mPosition.Y = 0;
            mVelocity.X = mVelocity.Y = 0;
            mAngle = 0;
        }

        public override string ToString()
        {
            return "Position" + mPosition.ToString();
        }

        public void Update(Vector2 newPosition)
        {
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Up] = AsDPadDown(Xbox360GamePad.DPadDirection.Up);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Down] = AsDPadDown(Xbox360GamePad.DPadDirection.Down);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Left] = AsDPadDown(Xbox360GamePad.DPadDirection.Left);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Right] = AsDPadDown(Xbox360GamePad.DPadDirection.Right);

            mVelocity = Vector2.Multiply((newPosition - mPosition), 1/TimeManager.SecondDifference);
            mPosition = newPosition;

            UpdateAccordingToPosition();

            for (int i = 0; i < 4; i++)
            {
                if (AsDPadPushed((Xbox360GamePad.DPadDirection)i))
                {
                    mLastDPadPush[i] = TimeManager.CurrentTime;
                }
            }

            leftAsButton.Update(-System.Math.Min(0, mPosition.X));
            rightAsButton.Update(System.Math.Max(0, mPosition.X));

            downAsButton.Update(-System.Math.Min(0, mPosition.Y));
            upAsButton.Update(System.Math.Max(0, mPosition.Y));
        }

        private void UpdateAccordingToPosition()
        {
            mAngle = System.Math.Atan2(mPosition.Y, mPosition.X);
            mAngle = MathFunctions.RegulateAngle(mAngle);
            mMagnitude = mPosition.Length();
        }

        #endregion

        float I2DInput.X
        {
            get { return Position.X; }
        }

        float I2DInput.Y
        {
            get { return Position.Y; }
        }

        float I2DInput.XVelocity
        {
            get { return Velocity.X; }
        }

        float I2DInput.YVelocity
        {
            get { return Velocity.Y; }
        }


        float I2DInput.Magnitude
        {
            get { return (float) this.mMagnitude; }
        }

        I1DInput horizontal;
        public I1DInput Horizontal
        {
            get
            {
                if (horizontal == null)
                {
                    horizontal = new AnalogStickHorizontal(this);
                }

                return horizontal;
            }
        }
    }
}
