using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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

        [DependsOn(nameof(IncludeDefaultTileset))]
        [DependsOn(nameof(IncludeGameplayLayer))]
        public Visibility SolidCollisionCheckBoxVisibility =>
            (IncludeDefaultTileset && IncludeGameplayLayer).ToVisibility();

        public bool IsSolidCollisionBorderChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsSolidCollisionBorderChecked))]
        [DependsOn(nameof(SolidCollisionCheckBoxVisibility))]
        public bool ShouldAddCollisionBorder =>
            IsSolidCollisionBorderChecked &&
            SolidCollisionCheckBoxVisibility == Visibility.Visible;
    }
}
