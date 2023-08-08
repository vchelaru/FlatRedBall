using ChickenClicker.Models;
using ChickenClicker.ViewModels;
using System;
using System.ComponentModel;

namespace ChickenClicker.Screens
{
    public partial class OptionsScreen
    {
        void CustomInitialize()
        {
            SetupMvvmBindings();
            SetupEventHandlers();
            LoadCurrentOptions();
        }

        private void SetupEventHandlers()
        {
            Forms.ExitButton.Click += ExitOptions;
            ViewModel.PropertyChanged += PropertyChanged;
        }

        private void ExitOptions(object sender, EventArgs e)
        {
            MoveToScreen(nameof(MenuScreen));
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ViewModel.IsUsingRounding)))
            {
                OptionsModel.IsUsingRounding = ViewModel.IsUsingRounding;
            }
        }

        private void SetupMvvmBindings()
        {
            ViewModel = new OptionsScreenViewModel();
            Forms.BindingContext = ViewModel;

            Forms.RoundingCheckBox.SetBinding(
                nameof(Forms.RoundingCheckBox.IsChecked),
                nameof(OptionsScreenViewModel.IsUsingRounding));
        }

        private void LoadCurrentOptions()
        {
            ViewModel.IsUsingRounding = OptionsModel.IsUsingRounding;
        }

        void CustomActivity(bool firstTimeCalled)
        {

        }

        void CustomDestroy()
        {

        }

        static void CustomLoadStaticContent(string contentManagerName)
        {

        }

        private OptionsScreenViewModel ViewModel;
    }
}
