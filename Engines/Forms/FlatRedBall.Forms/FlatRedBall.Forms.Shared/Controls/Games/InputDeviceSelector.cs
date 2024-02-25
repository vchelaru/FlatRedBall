using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace FlatRedBall.Forms.Controls.Games
{
    public class InputDeviceSelector : FrameworkElement
    {
        GraphicalUiElement InputDeviceContainerInstance;

        public List<IInputDevice> AllConnectedInputDevices { get; private set; }

        int maxPlayers = 0;
        public int MaxPlayers
        {
            get => maxPlayers;
            set
            {
                maxPlayers = value;
                UpdateInputDeviceSelectionItemsToMaxPlayerCount();
            }
        }

        List<InputDeviceSelectionItem> InputDeviceSelectionItemsInternal = new List<InputDeviceSelectionItem>();

        #region Initialize

        public InputDeviceSelector() : base() 
        {
            DoCommonInitialization();
        }

        public InputDeviceSelector(GraphicalUiElement visual) : base(visual) 
        {
            DoCommonInitialization();
        }

        private void DoCommonInitialization()
        {
            // by default we'll support all gamepads + keyboard. This can be changed:
            AllConnectedInputDevices = new List<IInputDevice>
            {
                InputManager.Keyboard,
                InputManager.Xbox360GamePads[0],
                InputManager.Xbox360GamePads[1],
                InputManager.Xbox360GamePads[2],
                InputManager.Xbox360GamePads[3]
            };
        }

        protected override void ReactToVisualChanged()
        {
            InputDeviceContainerInstance = this.Visual.GetGraphicalUiElementByName("InputDeviceContainerInstance");

            base.ReactToVisualChanged();
        }

        private void UpdateInputDeviceSelectionItemsToMaxPlayerCount()
        {
            while(InputDeviceSelectionItemsInternal.Count < MaxPlayers)
            {
                var newItem = new InputDeviceSelectionItem();
                newItem.InputDevice = null;
                InputDeviceSelectionItemsInternal.Add(newItem);
                InputDeviceContainerInstance.Children.Add(newItem.Visual);
            }
            while(InputDeviceSelectionItemsInternal.Count > MaxPlayers)
            {
                var lastItem = InputDeviceSelectionItemsInternal[InputDeviceSelectionItemsInternal.Count - 1];
                InputDeviceSelectionItemsInternal.Remove(lastItem);
                InputDeviceContainerInstance.Children.Remove(lastItem.Visual);
            }
        }


        #endregion

        public override void Activity()
        {

            foreach(var inputDevice in AllConnectedInputDevices)
            {
                if(inputDevice.DefaultJoinInput.WasJustPressed)
                {
                    HandleJoin(inputDevice);
                }
            }

            base.Activity();
        }

        private void HandleJoin(IInputDevice inputDevice)
        {
            var isInputDeviceAlreadyShownInItem = false;
            foreach(var item in InputDeviceSelectionItemsInternal)
            {
                if(item.InputDevice == inputDevice)
                {
                    // user just pressed join, no biggie
                    isInputDeviceAlreadyShownInItem = true;
                }
            }

            if(!isInputDeviceAlreadyShownInItem)
            {
                // find the first empty
                var firstEmpty = InputDeviceSelectionItemsInternal.Find(item => item.InputDevice == null);

                if(firstEmpty != null)
                {
                    firstEmpty.InputDevice = inputDevice;
                }
            }
        }
    }
}