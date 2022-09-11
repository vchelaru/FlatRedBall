using System;
using System.Collections.Generic;
using System.Text;
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

    }

    public class DelegateBasedIndexedButton : IPressableInput
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
        public IPressableInput DefaultUpPressable => defaultUpPressable;


        DelegateBasedIndexedButton defaultDownPressable;
        public IPressableInput DefaultDownPressable => defaultDownPressable;

        DelegateBasedIndexedButton defaultLeftPressable;
        public IPressableInput DefaultLeftPressable => defaultLeftPressable;

        DelegateBasedIndexedButton defaultRightPressable;
        public IPressableInput DefaultRightPressable => defaultRightPressable;

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

        #endregion

        public GenericGamePad(int gamepadIndex)
        {
            GamepadIndex = gamepadIndex;

            for (int i = 0; i < lastDPadPush.Length; i++)
            {
                lastDPadPush[i] = -1;
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

        private void ApplyInputDeviceMap(InputDeviceMap inputDeviceMap)
        {
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
                var xAxis = joystickState.Axes[i*2] / analogStickMaxValue;
                var yAxis = -1 * joystickState.Axes[i*2 + 1] / analogStickMaxValue;

                AnalogSticks[i].Update(new Microsoft.Xna.Framework.Vector2(xAxis, yAxis));

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

        public bool ButtonDown(int buttonIndex)
        {
            if (InputManager.mIgnorePushesThisFrame ||
                //mButtonsIgnoredForThisFrame[(int)button] || 
                InputManager.CurrentFrameInputSuspended ||
                buttonIndex >= joystickState.Buttons.Length)
                return false;

            return joystickState.Buttons[buttonIndex] == ButtonState.Pressed;
        }

        internal void Update()
        {
            var state = Joystick.GetState(GamepadIndex);
            var caps = Joystick.GetCapabilities(GamepadIndex);

            // each analog stick has an up/down
            var currentAnalogStickCount = caps.AxisCount / 2;
            if(AnalogSticks.Length != currentAnalogStickCount)
            {
                RecreateAnalogSticks(currentAnalogStickCount);
                RecreateDirectionalInputs();
            }

            Update(state);
        }

        public string GetJoystickStateInfo()
        {
            string toReturn = $"IsConnected:{joystickState.IsConnected}\n";

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
            return $"{GamepadIndex} Connected:{IsConnected}";
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
            bool repeatedThisFrame = lastDPadRepeatRate[(int)dPadDirection] == TimeManager.CurrentTime;

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

    }
}
