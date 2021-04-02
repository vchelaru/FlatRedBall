using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public class InputDeviceBase : IInputDevice
    {
        double lastUpdate;

        Vector2 _2DInputValueThisFrame;
        I2DInput default2DInput;
        I2DInput IInputDevice.Default2DInput => default2DInput;

        bool isUpPressedThisFrame;
        bool wasUpPressedLastFrame;
        IPressableInput defaultUpPressable;
        IPressableInput IInputDevice.DefaultUpPressable => defaultUpPressable;

        bool isDownPressedThisFrame;
        bool wasDownPressedLastFrame;
        IPressableInput defaultDownPressable;
        IPressableInput IInputDevice.DefaultDownPressable => defaultDownPressable;

        bool isLeftPressedThisFrame;
        bool wasLeftPressedLastFrame;
        IPressableInput defaultLeftPressable;
        IPressableInput IInputDevice.DefaultLeftPressable => defaultLeftPressable;

        bool isRightPressedThisFrame;
        bool wasRightPressedLastFrame;
        IPressableInput defaultRightPressable;
        IPressableInput IInputDevice.DefaultRightPressable => defaultRightPressable;

        float horizontalInputThisFrame;
        I1DInput defaultHorizontalInput;
        I1DInput IInputDevice.DefaultHorizontalInput => defaultHorizontalInput;

        float verticalInputThisFrame;
        I1DInput defaultVerticalInput;
        I1DInput IInputDevice.DefaultVerticalInput => defaultVerticalInput;

        bool isPrimaryActionPressedThisFrame;
        bool wasPrimaryActionPressedLastFrame;
        IPressableInput defaultPrimaryActionInput;
        IPressableInput IInputDevice.DefaultPrimaryActionInput => defaultPrimaryActionInput;

        bool isSecondaryActionPressedThisFrame;
        bool wasSecondaryActionPressedLastFrame;
        IPressableInput defaultSecondaryActionInput;
        IPressableInput IInputDevice.DefaultSecondaryActionInput => defaultSecondaryActionInput;

        bool isConfirmInputPressedThisFrame;
        bool wasConfirmInputPressedLastFrame;
        IPressableInput defaultConfirmInput;
        IPressableInput IInputDevice.DefaultConfirmInput => defaultConfirmInput;

        bool isJoinPressedThisFrame;
        bool wasJoinPressedLastFrame;
        IPressableInput defaultJoinInput;
        IPressableInput IInputDevice.DefaultJoinInput => defaultJoinInput;

        bool isPausePressedThisFrame;
        bool wasPausePressedLastFrame;
        IPressableInput defaultPauseInput;
        IPressableInput IInputDevice.DefaultPauseInput => defaultPauseInput;

        bool isBackPressedThisFrame;
        bool wasBackPressedLastFrame;
        IPressableInput defaultBackInput;
        IPressableInput IInputDevice.DefaultBackInput => defaultBackInput;



        public InputDeviceBase()
        {
            default2DInput = new DelegateBased2DInput(
                () =>
                {
                    TryUpdateAll();
                    return _2DInputValueThisFrame.X;
                }, 
                () =>
                {
                    TryUpdateAll();
                    return _2DInputValueThisFrame.Y;
                }, 
                Return0, Return0);
            
            defaultUpPressable = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isUpPressedThisFrame;
                }, 
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isUpPressedThisFrame, wasUpPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isUpPressedThisFrame, wasUpPressedLastFrame);
                });

            defaultDownPressable = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isDownPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isDownPressedThisFrame, wasDownPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isDownPressedThisFrame, wasDownPressedLastFrame);
                });

            defaultLeftPressable = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isLeftPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isLeftPressedThisFrame, wasLeftPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isLeftPressedThisFrame, wasLeftPressedLastFrame);
                });

            defaultRightPressable = new DelegateBasedPressableInput
                (() =>
                {
                    TryUpdateAll();
                    return isRightPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isRightPressedThisFrame, wasRightPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isRightPressedThisFrame, wasRightPressedLastFrame);
                });

            defaultHorizontalInput = new DelegateBased1DInput(
                () =>
                {
                    TryUpdateAll();
                    return horizontalInputThisFrame;
                }, Return0);
            defaultVerticalInput = new DelegateBased1DInput(
                () =>
                {
                    TryUpdateAll();
                    return verticalInputThisFrame;
                }, Return0);

            defaultPrimaryActionInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isPrimaryActionPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isPrimaryActionPressedThisFrame, wasPrimaryActionPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isPrimaryActionPressedThisFrame, wasPrimaryActionPressedLastFrame);
                });

            defaultSecondaryActionInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isSecondaryActionPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isSecondaryActionPressedThisFrame, wasSecondaryActionPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isSecondaryActionPressedThisFrame, wasSecondaryActionPressedLastFrame);
                });

            defaultConfirmInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isConfirmInputPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isConfirmInputPressedThisFrame, wasConfirmInputPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isConfirmInputPressedThisFrame, wasConfirmInputPressedLastFrame);
                });

            defaultJoinInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isJoinPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isJoinPressedThisFrame, wasJoinPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isJoinPressedThisFrame, wasJoinPressedLastFrame);
                });

            defaultPauseInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isPausePressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isPausePressedThisFrame, wasPausePressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isPausePressedThisFrame, wasPausePressedLastFrame);
                });

            defaultBackInput = new DelegateBasedPressableInput(
                () =>
                {
                    TryUpdateAll();
                    return isBackPressedThisFrame;
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustPressed(isBackPressedThisFrame, wasBackPressedLastFrame);
                },
                () =>
                {
                    TryUpdateAll();
                    return WasJustReleased(isBackPressedThisFrame, wasBackPressedLastFrame);
                });
            //defaultUpPressable = new DelegateBasedPressableInput();

        }

        bool WasJustPressed(bool thisFrame, bool lastFrame) => thisFrame && !lastFrame;
        bool WasJustReleased(bool thisFrame, bool lastFrame) => !thisFrame && lastFrame;

        void TryUpdateAll()
        {
            if(lastUpdate != TimeManager.CurrentTime)
            {

                wasUpPressedLastFrame = isUpPressedThisFrame;
                wasDownPressedLastFrame = isDownPressedThisFrame;
                wasLeftPressedLastFrame = isLeftPressedThisFrame;
                wasRightPressedLastFrame = isRightPressedThisFrame;

                wasPrimaryActionPressedLastFrame = isPrimaryActionPressedThisFrame;
                wasSecondaryActionPressedLastFrame = isSecondaryActionPressedThisFrame;
                wasConfirmInputPressedLastFrame = isConfirmInputPressedThisFrame;
                wasJoinPressedLastFrame = isJoinPressedThisFrame;
                wasPausePressedLastFrame = isPausePressedThisFrame;
                wasBackPressedLastFrame = isBackPressedThisFrame;

                lastUpdate = TimeManager.CurrentTime;

                _2DInputValueThisFrame.X = GetDefault2DInputX();
                _2DInputValueThisFrame.Y = GetDefault2DInputY();

                isUpPressedThisFrame = GetUpPressed();
                isDownPressedThisFrame = GetDownPressed();
                isLeftPressedThisFrame = GetLeftPressed();
                isRightPressedThisFrame = GetRightPressed();

                horizontalInputThisFrame = GetHorizontalValue();
                verticalInputThisFrame = GetVerticalValue();

                isPrimaryActionPressedThisFrame = GetPrimaryActionPressed();
                isSecondaryActionPressedThisFrame = GetSecondaryActionPressed();
                isConfirmInputPressedThisFrame = GetConfirmPressed();
                isJoinPressedThisFrame = GetJoinPressed();
                isPausePressedThisFrame = GetPausePressed();
                isBackPressedThisFrame = GetBackPressed();
            }
        }

        float Return0() => 0;
        bool ReturnFalse() => false;

        protected virtual float GetDefault2DInputX() => 0;
        protected virtual float GetDefault2DInputY() => 0;
        
        protected virtual bool GetUpPressed() => false;
        protected virtual bool GetDownPressed() => false;
        protected virtual bool GetLeftPressed() => false;
        protected virtual bool GetRightPressed() => false;

        protected virtual float GetHorizontalValue() => 0;
        protected virtual float GetVerticalValue() => 0;

        protected virtual bool GetPrimaryActionPressed() => false;
        protected virtual bool GetSecondaryActionPressed() => false;
        protected virtual bool GetConfirmPressed() => false;
        protected virtual bool GetJoinPressed() => false;
        protected virtual bool GetPausePressed() => false;
        protected virtual bool GetBackPressed() => false;


    }
}
