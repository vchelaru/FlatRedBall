using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ExampleNamedObjectPlugin
{
    internal class ExampleViewModel : PropertyListContainerViewModel
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
