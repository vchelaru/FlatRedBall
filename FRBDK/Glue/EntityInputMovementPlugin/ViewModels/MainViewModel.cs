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
    #endregion

    class MainViewModel : ViewModel
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

        #region Top down 
        public bool IsTopDownRadioChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value)
                {
                    TopDownViewModel.IsTopDown = true;
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
                if (Set(value) && value)
                {
                    PlatformerViewModel.IsPlatformer = true;
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
                //case nameof(TopDownEntityViewModel.InheritsFromTopDown):
                //    RefreshRadioButtonValues();
                //    break;
            }


        }

        [DependsOn(nameof(IsPlatformerRadioChecked))]
        public Visibility PlatformerUiVisibility => IsPlatformerRadioChecked.ToVisibility();

        #endregion

        public void RefreshRadioButtonValues()
        {
            IsNoneRadioChecked = !TopDownViewModel.InheritsFromTopDown && !TopDownViewModel.IsTopDown 
                && !PlatformerViewModel.IsPlatformer // add inheritance eventually

                ;
            IsTopDownRadioChecked = TopDownViewModel.InheritsFromTopDown || TopDownViewModel.IsTopDown;
            IsPlatformerRadioChecked = PlatformerViewModel.IsPlatformer ; // eventually add inheritance

            CanUserSelectMovementType = TopDownViewModel.InheritsFromTopDown == false; // eventually add inheritance for platformer

        }

    }
}
