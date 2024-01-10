using FlatRedBall.Glue.MVVM;
using FlatRedBall.PlatformerPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using TopDownPlugin.ViewModels;

namespace EntityInputMovementPlugin.ViewModels
{
    #region Enums

    public enum MovementType
    {
        None,
        TopDown,
        Platformer,
        Racing
    }

    public enum InputDevice
    {
        GamepadWithKeyboardFallback,
        None,
        ZeroInputDevice
    }
    #endregion

    public class MainViewModel : PropertyListContainerViewModel
    {
        public bool IsNoneRadioChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value)
                {
                    TopDownViewModel.IsTopDown = false;
                    PlatformerViewModel.IsPlatformer = false;
                }

            }
        }

        public bool CanUserSelectMovementType
        {
            get => Get<bool>();
            set => Set(value);
        }

        #region Input Device

        [SyncedProperty]
        [DefaultValue((int)InputDevice.GamepadWithKeyboardFallback)]
        public InputDevice InputDevice
        {
            get => (InputDevice)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(InputDevice))]
        public bool IsGamepadWithKeyboardFallbackInputDevice
        {
            get => InputDevice == InputDevice.GamepadWithKeyboardFallback;
            set
            {
                if(value)
                {
                    InputDevice = InputDevice.GamepadWithKeyboardFallback;
                }
            }
        }

        [DependsOn(nameof(InputDevice))]
        public bool IsZeroInputDevice
        {
            get => InputDevice == InputDevice.ZeroInputDevice;
            set
            {
                if(value)
                {
                    InputDevice = InputDevice.ZeroInputDevice;
                }
            }
        }

        [DependsOn(nameof(InputDevice))]
        public bool IsNoneInputDevice
        {
            get => InputDevice == InputDevice.None;
            set
            {
                if (value)
                {
                    InputDevice = InputDevice.None;
                }
            }
        }

        [DependsOn(nameof(IsTopDownRadioChecked))]
        [DependsOn(nameof(IsPlatformerRadioChecked))]
        public Visibility InputDeviceVisibility =>
            (IsTopDownRadioChecked || IsPlatformerRadioChecked).ToVisibility();

        #endregion

        #region Top down 
        public bool IsTopDownRadioChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value))
                {
                    TopDownViewModel.IsTopDown = value;
                }
            }
        }

        public TopDownEntityViewModel TopDownViewModel 
        {
            get => Get<TopDownEntityViewModel>();
            set
            {
                if (TopDownViewModel != null && TopDownViewModel != value)
                {
                    TopDownViewModel.PropertyChanged -= HandleTopDownPropertyChanged;
                }
                if(Set(value))
                {
                    if(value != null)
                    {
                        value.PropertyChanged += HandleTopDownPropertyChanged;
                    }
                }
            }
        }

        private void HandleTopDownPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(TopDownEntityViewModel.IsTopDown):
                    RefreshRadioButtonValues();
                    break;
                case nameof(TopDownEntityViewModel.InheritsFromTopDown):
                    RefreshRadioButtonValues();
                    break;
            }


        }

        [DependsOn(nameof(IsTopDownRadioChecked))]
        public Visibility TopDownUiVisibility => IsTopDownRadioChecked.ToVisibility();

        #endregion

        #region Platformer

        public bool IsPlatformerRadioChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    PlatformerViewModel.IsPlatformer = value;
                }
            }
        }

        public PlatformerEntityViewModel PlatformerViewModel
        {
            get => Get<PlatformerEntityViewModel>();
            set
            {
                if (PlatformerViewModel != null && PlatformerViewModel != value)
                {
                    PlatformerViewModel.PropertyChanged -= HandlePlatformerPropertyChanged;
                }
                if (Set(value))
                {
                    if (value != null)
                    {
                        value.PropertyChanged += HandlePlatformerPropertyChanged;
                    }
                }
            }
        }

        private void HandlePlatformerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlatformerEntityViewModel.IsPlatformer):
                    RefreshRadioButtonValues();
                    break;
                // todo - support inheritance
                case nameof(TopDownEntityViewModel.InheritsFromTopDown):
                    RefreshRadioButtonValues();
                    break;
            }


        }

        [DependsOn(nameof(IsPlatformerRadioChecked))]
        public Visibility PlatformerUiVisibility => IsPlatformerRadioChecked.ToVisibility();

        #endregion

        public void RefreshRadioButtonValues()
        {
            IsNoneRadioChecked = 
                !TopDownViewModel.InheritsFromTopDown && !TopDownViewModel.IsTopDown && 
                !PlatformerViewModel.InheritsFromPlatformer && !PlatformerViewModel.IsPlatformer;

            IsTopDownRadioChecked = TopDownViewModel.InheritsFromTopDown || TopDownViewModel.IsTopDown;
            IsPlatformerRadioChecked = PlatformerViewModel.InheritsFromPlatformer || PlatformerViewModel.IsPlatformer ; 

            CanUserSelectMovementType = 
                TopDownViewModel.InheritsFromTopDown == false && PlatformerViewModel.InheritsFromPlatformer == false;

        }

    }
}
