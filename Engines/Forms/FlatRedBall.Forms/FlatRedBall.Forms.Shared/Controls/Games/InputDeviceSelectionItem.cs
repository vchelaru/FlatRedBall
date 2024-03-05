using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class InputDeviceSelectionItem : FrameworkElement
    {
        GraphicalUiElement TextElement;

        /// <summary>
        /// Event raised when the input device is removed by this item. This can happen through a UI button click.
        /// </summary>
        public event EventHandler InputDeviceRemoved;

        IInputDevice inputDevice;
        public virtual IInputDevice InputDevice 
        { 
            get => inputDevice;
            set
            {
                inputDevice = value;

                UpdateToInputDevice();
            }
        }

        public InputDeviceSelectionItem() : base() { UpdateToInputDevice(); }

        public InputDeviceSelectionItem(GraphicalUiElement visual) : base(visual) { UpdateToInputDevice(); }

        protected override void ReactToVisualChanged()
        {
            TextElement = this.Visual.GetGraphicalUiElementByName("TextInstance");

            // This is optional:
            var removeDeviceButton = this.Visual.GetGraphicalUiElementByName("RemoveDeviceButtonInstance");
            if(removeDeviceButton != null)
            {
                removeDeviceButton.Click += (unused) => InputDeviceRemoved?.Invoke(this, null);
            }

            base.ReactToVisualChanged();

            UpdateToInputDevice();
        }

        private void UpdateToInputDevice()
        {
            if (inputDevice != null)
            {
                Visual.SetProperty("JoinedCategoryState", "HasInputDevice");

                var text = string.Empty;
                if(inputDevice is Xbox360GamePad gamepad)
                {
#if MONOGAME_381

                    text = gamepad.Capabilities.DisplayName ?? "Gamepad";
#else
                    text = "Xbox360GamePad";

#endif
                }
                else if(inputDevice is Keyboard)
                {
                    text = "Keyboard";
                }
                else
                {
                    text = inputDevice.ToString();
                }
                TextElement?.SetProperty("Text", text);
            }
            else
            {
                Visual.SetProperty("JoinedCategoryState", "NoInputDevice");
            }
        }
    }
}
