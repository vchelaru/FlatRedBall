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
                    text = gamepad.Capabilities.DisplayName ?? "Gamepad";
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
