using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Input;

namespace FlatRedBall.Gui
{
    public partial class Cursor
    {

        private bool TryHandleCursorPositionSetByTouchScreen()
        {
            bool handled = false;
            // We'll only consider this if the touch screen is pressed.  Windows 8 
            // uses both the touch screen and the cursor and we want to obey both - so
            // we'll first check the touch screen if it's down, otherwise use the mouse.

			bool shouldReadValues = (SupportedInputDevices & Gui.InputDevice.TouchScreen) == InputDevice.TouchScreen && 
                InputManager.TouchScreen.ScreenDown;

			// Update February 9, 2014
			// For iOS we should just use the values no matter what.
			// Update October 26, 2014
			// Android too
			#if IOS || ANDROID
			shouldReadValues = true;
			#endif


			if (shouldReadValues)
            {
                LastInputDevice = InputDevice.TouchScreen;
                mScreenX = InputManager.TouchScreen.AverageTouchPoint.X;
                mScreenY = InputManager.TouchScreen.AverageTouchPoint.Y;

                //We need to ignore previous frame values at the start and end of pinching.
                if (InputManager.TouchScreen.PinchStarted || InputManager.TouchScreen.PinchStopped)
                {
                    mLastRay = GetRay();
                    mLastScreenX = (int)mScreenX;
                    mLastScreenY = (int)mScreenY;

                }
                handled = true;
            }
            return handled;
        }


        private void GetPushDownClickFromTouchScreen()
        {
            PrimaryClick |= InputManager.TouchScreen.ScreenReleased && ignoreNextFrameInput == false;
            PrimaryPush |= InputManager.TouchScreen.ScreenPushed;

            // We used to 
            // just use ScreenDown, 
            // but it was possible to 
            // have a PrimaryClick on the 
            // same frame as when PrimaryDown
            // was true.  This happened on the
            // Windows Phone 7 device, and it would
            // happen on a regular release - did not
            // require super fast clicking or anything
            // like that.
            //PrimaryDown = InputManager.TouchScreen.ScreenDown;
            PrimaryDown |= (InputManager.TouchScreen.ScreenDown && !PrimaryClick) || PrimaryPush;
            PrimaryDoubleClick |= InputManager.TouchScreen.DoubleTap;

            SecondaryDown |= InputManager.TouchScreen.CurrentNumberOfTouches > 1;
            SecondaryPush |= InputManager.TouchScreen.CurrentNumberOfTouches > 1 && InputManager.TouchScreen.LastFrameNumberOfTouches < 2;
            SecondaryClick |= InputManager.TouchScreen.CurrentNumberOfTouches < 2 && InputManager.TouchScreen.LastFrameNumberOfTouches > 1 &&
                ignoreNextFrameInput == false;
        }
    }
}
