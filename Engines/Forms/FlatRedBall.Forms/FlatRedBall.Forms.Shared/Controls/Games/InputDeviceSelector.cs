using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FlatRedBall.Forms.Controls.Games
{
    public class InputDeviceSelector : FrameworkElement
    {
        GraphicalUiElement InputDeviceContainerInstance;

        public List<IInputDevice> AllConnectedInputDevices { get; private set; }

        public ObservableArray<IInputDevice> JoinedInputDevices { get; private set; }

        int maxPlayers = 0;
        bool hasAssignedMaxPlayers = false;
        public int MaxPlayers
        {
            get => maxPlayers;
            set
            {
                maxPlayers = value;
                UpdateToJoinedInputDeviceCount();
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
            // by default we'll support all gamepads + keyboard. This can be changed by the user:
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
            InputDeviceContainerInstance.Children.Clear();

            if (!hasAssignedMaxPlayers)
            {
                hasAssignedMaxPlayers = true;
                MaxPlayers = 4;
            }
            else
            {
                UpdateInputDeviceSelectionItemsCount();
            }

            base.ReactToVisualChanged();
        }

        #endregion

        #region Update in response to changes

        private void UpdateToJoinedInputDeviceCount()
        {
            var oldArray = JoinedInputDevices;

            if(oldArray != null)
            {
                oldArray.CollectionChanged -= HandleJoinedInputDevicesChanged;
            }

            JoinedInputDevices = new ObservableArray<IInputDevice>(MaxPlayers);
            UpdateInputDeviceSelectionItemsCount();

            JoinedInputDevices.CollectionChanged += HandleJoinedInputDevicesChanged;

            if(oldArray != null)
            {
                for(int i = 0; i < oldArray.Length && i < JoinedInputDevices.Length; i++)
                {
                    JoinedInputDevices[i] = oldArray[i];
                }
            }
        }

        private void HandleJoinedInputDevicesChanged(object sender, ObservableArrayIndexChangeArgs e)
        {
            var index = e.Index;

            InputDeviceSelectionItemsInternal[index].InputDevice = JoinedInputDevices[index];
        }

        private void UpdateInputDeviceSelectionItemsCount()
        {
            while(InputDeviceSelectionItemsInternal.Count < JoinedInputDevices.Length)
            {
                var newItem = new InputDeviceSelectionItem();
                newItem.InputDeviceRemoved += (sender, args) =>
                {
                    int index = InputDeviceSelectionItemsInternal.IndexOf(newItem);
                    JoinedInputDevices[index] = null;
                };
                InputDeviceSelectionItemsInternal.Add(newItem);
                InputDeviceContainerInstance.Children.Add(newItem.Visual);
                newItem.InputDevice = JoinedInputDevices[InputDeviceSelectionItemsInternal.Count-1];
            }
            while(InputDeviceSelectionItemsInternal.Count > JoinedInputDevices.Length)
            {
                var lastItem = InputDeviceSelectionItemsInternal[InputDeviceSelectionItemsInternal.Count - 1];
                InputDeviceSelectionItemsInternal.Remove(lastItem);
                InputDeviceContainerInstance.Children.Remove(lastItem.Visual);
            }
        }


        #endregion

        List<IInputDevice> devicesUnjoinedThisFrame = new List<IInputDevice>();

        public override void Activity()
        {
            devicesUnjoinedThisFrame.Clear();

            bool DidUnjoin(IInputDevice inputDevice) =>
                inputDevice.DefaultCancelInput.WasJustPressed|| inputDevice.DefaultBackInput.WasJustPressed;

            foreach (var item in InputDeviceSelectionItemsInternal)
            {
                if(item.InputDevice == null)
                {
                    continue;
                }
                if(item.InputDevice.DefaultCancelInput.WasJustPressed == true || item.InputDevice.DefaultBackInput.WasJustPressed == true)
                {
                    devicesUnjoinedThisFrame.Add(item.InputDevice);
                    item.InputDevice = null;
                }
            }

            foreach(var inputDevice in AllConnectedInputDevices)
            {
                if(inputDevice.DefaultJoinInput.WasJustPressed)
                {
                    HandleJoin(inputDevice);
                }
                if(DidUnjoin(inputDevice))
                {
                    // cancel pressed = handle...
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

    public class ObservableArrayIndexChangeArgs
    {
        public int Index { get; private set; }

        public ObservableArrayIndexChangeArgs(int index)
        {
            Index = index;
        }
    }

    public class ObservableArray<T>
    {
        private T[] _array;
        public event Action<object, ObservableArrayIndexChangeArgs> CollectionChanged;

        public int Length => _array.Length;

        public ObservableArray(int size)
        {
            _array = new T[size];
        }

        public T this[int index]
        {
            get => _array[index];
            set
            {
                var oldItem = _array[index];
                _array[index] = value;
                OnIndexChanged(oldItem, index);
            }
        }

        void OnIndexChanged(T oldItem, int index)
        {
            CollectionChanged?.Invoke(this, new ObservableArrayIndexChangeArgs(index));
        }
    }
}