using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static FlatRedBall.Input.Xbox360GamePad;

namespace FlatRedBall.Input
{
    #region Classes

    public class InputDeviceMap
    {
        public int PrimaryAction { get; set; } = 1;
        public int SecondaryAction { get; set; } = 0;
        public int Confirm { get; set; } = 1;
        public int Cancel { get; set; } = 2;
        public int Join { get; set; } = 9;
        public int Pause { get; set; } = 9;
        public int Back { get; set; } = 2;

        public int XboxX { get; set; } = 0;
        public int XboxA { get; set; } = 1;
        public int XboxB { get; set; } = 2;
        public int XboxY { get; set; } = 3;

    }

    public class DelegateBasedIndexedButton : IRepeatPressableInput
    {
        public int Index;
        private GenericGamePad gamepad;



        public DelegateBasedIndexedButton(GenericGamePad gamepad) 
        {
            this.gamepad = gamepad;
        }

        public bool IsDown => gamepad.ButtonDown(Index);
        public bool WasJustPressed => gamepad.ButtonPushed(Index);
        public bool WasJustReleased => gamepad.ButtonReleased(Index);

        public bool WasJustPressedOrRepeated => gamepad.ButtonRepeatRate(Index);
    }
    #endregion

    public class GenericGamePad : IInputDevice
    {
        #region Fields/Properties

        JoystickState lastJoystickState;
        JoystickState joystickState;

        public AnalogStick[] AnalogSticks { get; private set; }

        public I2DInput Default2DInput { get; private set; }

        DelegateBasedIndexedButton defaultUpPressable;
        public IRepeatPressableInput DefaultUpPressable => defaultUpPressable;


        DelegateBasedIndexedButton defaultDownPressable;
        public IRepeatPressableInput DefaultDownPressable => defaultDownPressable;

        DelegateBasedIndexedButton defaultLeftPressable;
        public IRepeatPressableInput DefaultLeftPressable => defaultLeftPressable;

        DelegateBasedIndexedButton defaultRightPressable;
        public IRepeatPressableInput DefaultRightPressable => defaultRightPressable;

        public I1DInput DefaultHorizontalInput { get; private set; }

        public I1DInput DefaultVerticalInput { get; private set; }

        DelegateBasedIndexedButton defaultPrimaryActionInput;
        public IPressableInput DefaultPrimaryActionInput => defaultPrimaryActionInput;

        DelegateBasedIndexedButton defaultSecondaryActionInput;
        public IPressableInput DefaultSecondaryActionInput => defaultSecondaryActionInput;

        DelegateBasedIndexedButton defaultConfirmInput;
        public IPressableInput DefaultConfirmInput => defaultConfirmInput;

        DelegateBasedIndexedButton defaultCancelInput;
        public IPressableInput DefaultCancelInput => defaultCancelInput;

        DelegateBasedIndexedButton defaultJoinInput;
        public IPressableInput DefaultJoinInput => defaultJoinInput;

        DelegateBasedIndexedButton defaultPauseInput;
        public IPressableInput DefaultPauseInput => defaultPauseInput;

        DelegateBasedIndexedButton defaultBackInput;
        public IPressableInput DefaultBackInput => defaultBackInput;

        I1DInput dPadHorizontal;
        public I1DInput DPadHorizontal => dPadHorizontal;

        I1DInput dPadVertical;
        public I1DInput DPadVertical => dPadVertical;

        I2DInput dPad;
        public I2DInput DPad => dPad;

        public int GamepadIndex { get; private set; }
        const float analogStickMaxValue = 32768;

        public bool IsConnected => this.joystickState.IsConnected;

        double[] lastDPadPush = new double[4];
        double[] lastDPadRepeatRate = new double[4];

        // Feb 12, 2023 - is this a valid assumption? Can there be more?
        const int MaxNumberOfButtons = 512;
        double[] lastButtonPush = new double[MaxNumberOfButtons];
        double[] lastButtonRepeatRate = new double[MaxNumberOfButtons];


        public int NumberOfButtons { get; private set; }

        public float Deadzone { get; set; } = .1f;

        public DeadzoneType DeadzoneType { get; set; }
             = DeadzoneType.Radial;
        InputDeviceMap InputDeviceMap { get; set; }

        JoystickCapabilities JoystickCapabilities;

        bool WasConnectedThisFrame
        {
            get
            { 

                return !lastJoystickState.IsConnected && joystickState.IsConnected;
            }
        }

        #endregion

        public GenericGamePad(int gamepadIndex)
        {
            GamepadIndex = gamepadIndex;

            for (int i = 0; i < lastDPadPush.Length; i++)
            {
                lastDPadPush[i] = -1;
            }

            for(int i = 0; i < MaxNumberOfButtons; i++)
            {
                lastButtonPush[i] = -1;
            }

            defaultUpPressable = new DelegateBasedIndexedButton(this);
            defaultDownPressable = new DelegateBasedIndexedButton(this);
            defaultLeftPressable = new DelegateBasedIndexedButton(this);
            defaultRightPressable = new DelegateBasedIndexedButton(this);

            defaultPrimaryActionInput = new DelegateBasedIndexedButton(this);
            defaultSecondaryActionInput = new DelegateBasedIndexedButton(this);
            defaultConfirmInput = new DelegateBasedIndexedButton(this);
            defaultCancelInput = new DelegateBasedIndexedButton(this);
            defaultJoinInput = new DelegateBasedIndexedButton(this);
            defaultPauseInput = new DelegateBasedIndexedButton(this);
            defaultBackInput = new DelegateBasedIndexedButton(this);

            dPadHorizontal = new DelegateBased1DInput(() =>
            {
                if (joystickState.Hats.Length == 0) return 0;
                var hat = joystickState.Hats[0];
                if (hat.Left == ButtonState.Pressed)
                {
                    return -1;
                }
                else if (hat.Right == ButtonState.Pressed)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });

            dPadVertical = new DelegateBased1DInput(() =>
            {
                if (joystickState.Hats.Length == 0) return 0;
                var hat = joystickState.Hats[0];
                if (hat.Down == ButtonState.Pressed)
                {
                    return -1;
                }
                else if (hat.Up == ButtonState.Pressed)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });

            dPad = new DelegateBased2DInput(
                () => dPadHorizontal.Value,
                () => dPadVertical.Value
                );

            var stickCount = 2;
            RecreateAnalogSticks(stickCount);
            RecreateDirectionalInputs();

            joystickState = new JoystickState();
            lastJoystickState = new JoystickState();

            ApplyInputDeviceMap(new InputDeviceMap());
        }

        private void RecreateAnalogSticks(int stickCount)
        {
            AnalogSticks = new AnalogStick[stickCount];
            for (int i = 0; i < AnalogSticks.Length; i++)
            {
                AnalogSticks[i] = new AnalogStick();
            }
        }

        private void RecreateDirectionalInputs()
        {
            if(AnalogSticks.Length > 0)
            {
                DefaultHorizontalInput = DPadHorizontal.Or(AnalogSticks[0].Horizontal);
                DefaultVerticalInput = DPadVertical.Or(AnalogSticks[0].Vertical);
                Default2DInput = DPad.Or(AnalogSticks[0]);
            }
            else
            {
                DefaultHorizontalInput = DPadHorizontal;
                DefaultVerticalInput = DPadVertical;
                Default2DInput = DPad;
            }
        }

        public void ApplyInputDeviceMap(InputDeviceMap inputDeviceMap)
        {
            this.InputDeviceMap = inputDeviceMap;
            defaultPrimaryActionInput.Index = inputDeviceMap.PrimaryAction;
            defaultSecondaryActionInput.Index = inputDeviceMap.SecondaryAction;
            defaultConfirmInput.Index = inputDeviceMap.Confirm;
            defaultCancelInput.Index = inputDeviceMap.Cancel;
            defaultJoinInput.Index = inputDeviceMap.Join;
            defaultPauseInput.Index = inputDeviceMap.Pause;
            defaultBackInput.Index = inputDeviceMap.Back;
        }

        public void Update(JoystickState newJoystickState)
        {
            lastJoystickState = joystickState;
            joystickState = newJoystickState;

            for(int i = 0; i < AnalogSticks.Length; i++)
            {
                var stickPosition = new Vector2(0,0);

                if (IsConnected)
                {
                    stickPosition = new Vector2(
                        joystickState.Axes[i*2] / analogStickMaxValue,
                        -1 * joystickState.Axes[i*2 + 1] / analogStickMaxValue);

                    if (Deadzone > 0)
                    {
                        switch (DeadzoneType)
                        {
                            case DeadzoneType.Radial:
                                stickPosition = GetRadialDeadzoneValue(stickPosition);
                                break;
                            case DeadzoneType.Cross:
                                stickPosition = GetCrossDeadzoneValue(stickPosition);
                                break;
                        }

                    }
                }

                AnalogSticks[i].Update(new Microsoft.Xna.Framework.Vector2(stickPosition.X, stickPosition.Y));

            }

        }

        public bool ButtonPushed(int buttonIndex)
        {
            if (InputManager.mIgnorePushesThisFrame || 
                //mButtonsIgnoredForThisFrame[(int)button] || 
                InputManager.CurrentFrameInputSuspended ||
                buttonIndex >= joystickState.Buttons.Length ||
                buttonIndex >= lastJoystickState.Buttons.Length)
                return false;

            return joystickState.Buttons[buttonIndex] == ButtonState.Pressed &&
                lastJoystickState.Buttons[buttonIndex] == ButtonState.Released;
        }

        public bool ButtonReleased(int buttonIndex)
        {
            if (InputManager.mIgnorePushesThisFrame ||
                //mButtonsIgnoredForThisFrame[(int)button] || 
                InputManager.CurrentFrameInputSuspended ||
                buttonIndex >= joystickState.Buttons.Length ||
                buttonIndex >= lastJoystickState.Buttons.Length)
                return false;

            return joystickState.Buttons[buttonIndex] == ButtonState.Released &&
                lastJoystickState.Buttons[buttonIndex] == ButtonState.Pressed;
        }

        public bool ButtonPushed(Xbox360GamePad.Button xboxButton)
        {
            switch(xboxButton)
            {
                case Button.X:
                    return ButtonPushed(InputDeviceMap.XboxX);
                case Button.Y:
                    return ButtonPushed(InputDeviceMap.XboxY);
                case Button.A:
                    return ButtonPushed(InputDeviceMap.XboxA);
                case Button.B:
                    return ButtonPushed(InputDeviceMap.XboxB);
            }
            return false;
        }

        public bool ButtonReleased(Xbox360GamePad.Button xboxButton)
        {
            switch (xboxButton)
            {
                case Button.X:
                    return ButtonReleased(InputDeviceMap.XboxX);
                case Button.Y:
                    return ButtonReleased(InputDeviceMap.XboxY);
                case Button.A:
                    return ButtonReleased(InputDeviceMap.XboxA);
                case Button.B:
                    return ButtonReleased(InputDeviceMap.XboxB);
            }
            return false;
        }

        public bool ButtonDown(int buttonIndex)
        {
            if (InputManager.mIgnorePushesThisFrame ||
                //mButtonsIgnoredForThisFrame[(int)button] || 
                InputManager.CurrentFrameInputSuspended ||
                buttonIndex >= joystickState.Buttons.Length)
                return false;

            return joystickState.Buttons[buttonIndex] == ButtonState.Pressed;
        }

        public bool ButtonRepeatRate(int buttonIndex, double timeAfterPush = .35, double timeBetweenRepeating = .12)
        {
            if (ButtonPushed(buttonIndex))
            {
                return true;
            }
            // The very first frame of FRB would have CurrentTime == 0. 
            // The repeat cannot happen on the first frame, so we check for that:
            bool repeatedThisFrame = TimeManager.CurrentTime > 0 && lastButtonRepeatRate[buttonIndex] == TimeManager.CurrentTime;
            if (repeatedThisFrame ||
                (
                    ButtonDown(buttonIndex) &&
                    TimeManager.CurrentTime - lastButtonPush[buttonIndex] > timeAfterPush &&
                    TimeManager.CurrentTime - lastButtonRepeatRate[buttonIndex] > timeBetweenRepeating)
                )
            {
                lastButtonRepeatRate[buttonIndex] = TimeManager.CurrentTime;
                return true;
            }
            return false;
        }

        internal void Update()
        {
#if MONOGAME
            var state = Joystick.GetState(GamepadIndex);

#if MONOGAME_381


            if(JoystickCapabilities.DisplayName == null || WasConnectedThisFrame)
#else
            // we won't worry about this on older FRBs. Maybe it would be worth it, but it may also introduce bugs if
            // the first frame is connected but doesn't have caps
            if (true)
#endif
            {
                JoystickCapabilities = Joystick.GetCapabilities(GamepadIndex);
            }

            // each analog stick has an up/down
            var currentAnalogStickCount = JoystickCapabilities.AxisCount / 2;
            if(AnalogSticks.Length != currentAnalogStickCount)
            {
                RecreateAnalogSticks(currentAnalogStickCount);
                RecreateDirectionalInputs();
            }

            NumberOfButtons = JoystickCapabilities.ButtonCount;

            Update(state);
#endif
        }

        private void UpdateLastButtonPushedValues()
        {
            // Set the last pushed and clear the ignored input

            for (int i = 0; i < NumberOfButtons; i++)
            {
                //mButtonsIgnoredForThisFrame[i] = false;

                if (ButtonPushed(i))
                {
                    lastButtonPush[i] = TimeManager.CurrentTime;
                }
            }
        }

        public string GetJoystickStateInfo()
        {
            string toReturn = "";
            
            toReturn +=
#if MONOGAME_381
                $"{JoystickCapabilities.Identifier} " + 
#endif
                "IsConnected:{joystickState.IsConnected}\n";

            for(int i = 0; i < joystickState.Buttons.Length; i++)
            {
                toReturn += $"Button {i}:{joystickState.Buttons[i]}\n";
            }

            for(int i =0; i < joystickState.Axes.Length; i++)
            {
                toReturn += $"Axes {i}:{joystickState.Axes[i]}\n";
            }

            for(int i = 0; i < joystickState.Hats.Length; i++)
            {
                toReturn += $"Hat {i}:{joystickState.Hats[i]}\n";
            }

            return toReturn;
        }

        public override string ToString()
        {
            return $"{GamepadIndex} Connected:{IsConnected} "
#if MONOGAME_381
                + $"ID:{JoystickCapabilities.Identifier}"
#endif
                ;
        }

        public bool DPadDown(DPadDirection dPadDirection)
        {
            switch(dPadDirection)
            {
                case DPadDirection.Left: return joystickState.Hats.Length != 0 && joystickState.Hats[0].Left == ButtonState.Pressed;
                case DPadDirection.Right: return joystickState.Hats.Length != 0 && joystickState.Hats[0].Right == ButtonState.Pressed;
                case DPadDirection.Up: return joystickState.Hats.Length != 0 && joystickState.Hats[0].Up == ButtonState.Pressed;
                case DPadDirection.Down: return joystickState.Hats.Length != 0 && joystickState.Hats[0].Down == ButtonState.Pressed;
            }
            return false;
        }

        public bool DPadRepeatRate(DPadDirection dPadDirection, double timeAfterPush = .35, double timeBetweenRepeating = .12)
        {
            if(lastJoystickState.Hats.Length== 0 || joystickState.Hats.Length == 0)
            {
                return false;
            }

            var lastDad = lastJoystickState.Hats[0];
            var dPad = joystickState.Hats[0];

            switch(dPadDirection)
            {
                case DPadDirection.Left:
                    if(dPad.Left == ButtonState.Pressed && lastDad.Left != ButtonState.Pressed)
                    {
                        return true;
                    }
                    break;
                case DPadDirection.Right:
                    if (dPad.Right == ButtonState.Pressed && lastDad.Right != ButtonState.Pressed)
                    {
                        return true;
                    }
                    break;
                case DPadDirection.Up:
                    if (dPad.Up == ButtonState.Pressed && lastDad.Up != ButtonState.Pressed)
                    {
                        return true;
                    }
                    break;
                case DPadDirection.Down:
                    if (dPad.Down == ButtonState.Pressed && lastDad.Down != ButtonState.Pressed)
                    {
                        return true;
                    }
                    break;
            }

            // If this method is called multiple times per frame this line
            // of code guarantees that the user will get true every time until
            // the next TimeManager.Update (next frame).
            // The very first frame of FRB would have CurrentTime == 0. 
            // The repeat cannot happen on the first frame, so we check for that:
            bool repeatedThisFrame = TimeManager.CurrentTime > 0 && lastDPadRepeatRate[(int)dPadDirection] == TimeManager.CurrentTime;

            if (repeatedThisFrame ||
                (
                DPadDown(dPadDirection) &&
                TimeManager.CurrentTime - lastDPadPush[(int)dPadDirection] > timeAfterPush &&
                TimeManager.CurrentTime - lastDPadRepeatRate[(int)dPadDirection] > timeBetweenRepeating)
                )
            {
                lastDPadRepeatRate[(int)dPadDirection] = TimeManager.CurrentTime;
                return true;
            }

            return false;
        }

        Vector2 GetRadialDeadzoneValue(Vector2 originalValue)
        {
            var deadzoneSquared = Deadzone * Deadzone;

            var originalValueLengthSquared =
                (originalValue.X * originalValue.X) +
                (originalValue.Y * originalValue.Y);

            if (originalValueLengthSquared < deadzoneSquared)
            {
                return Vector2.Zero;
            }
            else
            {
                return originalValue;
            }
        }

        Vector2 GetCrossDeadzoneValue(Vector2 originalValue)
        {
            if (originalValue.X < Deadzone && originalValue.X > -Deadzone)
            {
                originalValue.X = 0;
            }
            if (originalValue.Y < Deadzone && originalValue.Y > -Deadzone)
            {
                originalValue.Y = 0;
            }
            return originalValue;
        }
    }
}
