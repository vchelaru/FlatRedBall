using FlatRedBall.Gui;
using FlatRedBall.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Primitives
{
    public class ButtonBase : FrameworkElement, IInputReceiver
    {
        #region Fields / Properties

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the user pushes, then releases the control.
        /// This means the cursor is over the button, the button was originally pushed,
        /// the primary button was pressed last frame, but is no longer pressed this frame.
        /// The "click" terminology comes from the Cursor's PrimaryClick property.
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Event raised when the user pushes on the control. 
        /// This means the cursor is over the button and the primary button was not pressed last frame, but is pressed this frame.
        /// The "push" terminology comes from the Cursor's PrimaryPush property.
        /// </summary>
        public event EventHandler Push;
        public event FocusUpdateDelegate FocusUpdate;

        /// <summary>
        /// Event raised when any button is pressed on an Xbox360GamePad which is being used by the 
        /// GuiManager.GamePadsForUiControl.
        /// </summary>
        public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
        public event Action<int> GenericGamepadButtonPushed;

        public event Action<FlatRedBall.Input.Mouse.MouseButtons> MouseButtonPushed;


        #endregion

        #region Initialize

        public ButtonBase() : base() { }

        public ButtonBase(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            base.ReactToVisualChanged();
        }

        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            UpdateState();

            OnClick();

            Click?.Invoke(this, null);
            MouseButtonPushed?.Invoke(FlatRedBall.Input.Mouse.MouseButtons.LeftButton);
        }

        private void HandlePush(IWindow window)
        {
            UpdateState();

            Push?.Invoke(this, null);
        }

        private void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        private void HandleRollOn(IWindow window)
        {
            UpdateState();
        }

        private void HandleRollOff(IWindow window)
        {
            UpdateState();
        }

        #endregion

        protected virtual void OnClick() { }

        public void PerformClick()
        {
            HandleClick(this.Visual);
        }

        #region IInputReceiver Methods

        public void OnFocusUpdate()
        {
            var gamepads = GuiManager.GamePadsForUiControl;
            for(int i = 0; i < gamepads.Count; i++)
            {
                var gamepad = gamepads[i];

                HandleGamepadNavigation(gamepad);
                
                if(gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A) && 
                    // A button may be focused, then through the action of clicking the button
                    // (like buying items) it may lose its enabled state,but
                    // remain focused as to not focus a new item.
                    IsEnabled)
                {
                    //this.HandlePush(null);
                    this.HandleClick(null);

                    ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.A);
                }

                void RaiseIfPushedAndEnabled(FlatRedBall.Input.Xbox360GamePad.Button button)
                {
                    if (IsEnabled && gamepad.ButtonPushed(button))
                    {
                        ControllerButtonPushed?.Invoke(button);
                    }
                }

                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.B);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.X);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Y);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Start);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Back);

                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                }
            }

            for (int i = 0; i < GuiManager.GenericGamePadsForUiControl.Count; i++)
            {
                var gamepad = GuiManager.GenericGamePadsForUiControl[i];

                HandleGamepadNavigation(gamepad);

                if((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed && IsEnabled)
                {
                    //this.HandlePush(null);
                    this.HandleClick(null);
                }

                if(IsEnabled)
                {
                    for(int buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                    {
                        if(gamepad.ButtonPushed(buttonIndex))
                        {
                            GenericGamepadButtonPushed?.Invoke(buttonIndex);
                        }
                    }
                }
            }

            FocusUpdate?.Invoke(this);
        }

        public void OnGainFocus()
        {
        }

        public void LoseFocus()
        {
            IsFocused = false;
        }

        public void ReceiveInput()
        {
        }

        public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
            var args = new Input.KeyEventArgs();
            args.Key = key;
            base.RaiseKeyDown(args);
        }

        public void HandleCharEntered(char character)
        {
        }

        #endregion
    }
}
