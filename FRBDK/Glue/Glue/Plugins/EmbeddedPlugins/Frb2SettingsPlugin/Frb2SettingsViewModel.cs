using FlatRedBall.Glue.MVVM;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Frb2SettingsPlugin
{
    public class Frb2SettingsViewModel : ViewModel
    {
        public bool GenerateCode
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
