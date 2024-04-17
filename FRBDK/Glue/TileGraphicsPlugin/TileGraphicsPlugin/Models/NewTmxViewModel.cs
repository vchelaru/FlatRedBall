using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace TiledPluginCore.Models
{
    enum WithVisualType
    {
        WithVisuals,
        NoVisuals
    }


    class NewTmxViewModel : ViewModel
    {
        public WithVisualType WithVisualType
        {
            get => Get<WithVisualType>();
            set => Set(value);
        }

        [DependsOn(nameof(WithVisualType))]
        public bool IsWithVisualsChecked
        {
            get => WithVisualType == WithVisualType.WithVisuals;
            set
            {
                if(value)
                {
                    WithVisualType = WithVisualType.WithVisuals;
                }
            }
        }

        [DependsOn(nameof(WithVisualType))]
        public bool IsNoVisualsChecked
        {
            get => WithVisualType == WithVisualType.NoVisuals;
            set
            {
                if(value)
                {
                    WithVisualType = WithVisualType.NoVisuals;
                }
            }
        }

        [DependsOn(nameof(WithVisualType))]
        public Visibility NoVisualsUiVisibility => (WithVisualType == WithVisualType.NoVisuals).ToVisibility();

        [DependsOn(nameof(WithVisualType))]
        public Visibility WithVisualsUiVisibility => (WithVisualType == WithVisualType.WithVisuals).ToVisibility();

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
