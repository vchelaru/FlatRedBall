using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace TiledPluginCore.Models
{
    class NewTmxViewModel : ViewModel
    {
        public bool IncludeDefaultTileset
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IncludeGameplayLayer
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
