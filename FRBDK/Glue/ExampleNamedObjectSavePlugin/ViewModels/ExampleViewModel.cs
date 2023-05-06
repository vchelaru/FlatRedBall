using FlatRedBall.Glue.MVVM;

namespace ExampleNamedObjectSavePlugin.ViewModels
{
    public class ExampleViewModel : PropertyListContainerViewModel
    {
        [SyncedProperty]
        public string StringProperty
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool BoolProperty
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }
    }
}
