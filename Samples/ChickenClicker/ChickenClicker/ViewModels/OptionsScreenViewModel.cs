using FlatRedBall.Forms.MVVM;

namespace ChickenClicker.ViewModels
{
    internal class OptionsScreenViewModel : ViewModel
    {
        public bool IsUsingRounding
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
