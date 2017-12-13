using FlatRedBall.Gui;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Primitives
{
    public class ButtonBase : FrameworkElement
    {
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

        #endregion

        protected override void ReactToVisualChanged()
        {
            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            base.ReactToVisualChanged();
        }

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
    }
}
