using FlatRedBall.Gui;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Primitives
{
    public class ButtonBase : FrameworkElement, IInputReceiver
    {
        protected bool isFocused;
        public override bool IsFocused
        {
            get { return isFocused; }
            set
            {
                isFocused = value && IsEnabled;

                if(isFocused)
                {
                    FlatRedBall.Input.InputManager.InputReceiver = this;
                }

                UpdateState();
            }
        }

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get; set; }


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

        protected virtual void UpdateState() { }

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            UpdateState();

            OnClick();

            Click?.Invoke(this, null);
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

        #region IInputReceiver Methods

        public void OnFocusUpdate()
        {
            for(int i = 0; i < FlatRedBall.Input.InputManager.Xbox360GamePads.Length; i++)
            {
                var gamepad = FlatRedBall.Input.InputManager.Xbox360GamePads[i];

                if(gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
                {
                    this.HandleTab(TabDirection.Down, this);
                }
                else if(gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
                {
                    this.HandleTab(TabDirection.Up, this);
                }
                
                if(gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    //this.HandlePush(null);
                    this.HandleClick(null);
                }
                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                }
            }

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
        }

        public void HandleCharEntered(char character)
        {
        }

        #endregion
    }
}
