using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    public class InputDeviceMap
    {
        public int PrimaryAction { get; set; } = 1;
        public int SecondaryAction { get; set; } = 0;
        public int Confirm { get; set; } = 1;
        public int Join { get; set; } = 9;
        public int Pause { get; set; } = 9;
        public int Back { get; set; } = 2;

    }

    public class GenericGamePad : IInputDevice
    {
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

        public GenericGamePad(int gamepadIndex)
        {
            GamepadIndex = gamepadIndex;


            //Default2DInput = new DelegateBased2DInput(
            //    () => AnalogSticks[0].Position.X,
            //    () => AnalogSticks[0].Position.Y);

            //DefaultHorizontalInput = new DelegateBased1DInput(
            //    () => AnalogSticks[0].Position.X);
            //DefaultVerticalInput = new DelegateBased1DInput(
            //    () => AnalogSticks[0].Position.Y);

            defaultUpPressable = new DelegateBasedIndexedButton(this);
            defaultDownPressable = new DelegateBasedIndexedButton(this);
            defaultLeftPressable = new DelegateBasedIndexedButton(this);
            defaultRightPressable = new DelegateBasedIndexedButton(this);

            defaultPrimaryActionInput = new DelegateBasedIndexedButton(this);
            defaultSecondaryActionInput = new DelegateBasedIndexedButton(this);
            defaultConfirmInput = new DelegateBasedIndexedButton(this);
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
}
