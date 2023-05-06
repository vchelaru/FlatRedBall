using FlatRedBall.Glue.MVVM;

namespace TutorialPlugin.ViewModels
{
    public class MainControlViewModel : ViewModel
    {
        public string FileNameDisplay
        {
            get => Get<string>();
            set => Set(value);
        }

        public string WriteTimeDisplay
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
