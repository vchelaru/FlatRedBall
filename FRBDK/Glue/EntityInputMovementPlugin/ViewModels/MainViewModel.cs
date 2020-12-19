using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TopDownPlugin.ViewModels;

namespace EntityInputMovementPlugin.ViewModels
{
    public enum MovementType
    {
        None,
        TopDown,
        Platformer,
        Racing
    }

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

                }

            }
        }


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


        public bool CanUserSelectMovementType
        {
            get => Get<bool>();
            set => Set(value);
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


        public void RefreshRadioButtonValues()
        {
            IsNoneRadioChecked = !TopDownViewModel.InheritsFromTopDown && !TopDownViewModel.IsTopDown;
            IsTopDownRadioChecked = TopDownViewModel.InheritsFromTopDown || TopDownViewModel.IsTopDown;

            CanUserSelectMovementType = TopDownViewModel.InheritsFromTopDown == false;

        }

    }
}
