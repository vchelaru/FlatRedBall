using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace TiledPluginCore.ViewModels
{
    #region Enums

    enum WithVisualType
    {
        WithVisuals,
        NoVisuals
    }

    public enum TmxLevels
    {
        OverworldPlatformerA,
        OverworldPlatformerB,
        OverworldPlatformerC,
        OverworldTopDownA,
        OverworldTopDownB,
        OverworldTopDownC
    }

    #endregion

    class NewTmxViewModel : ViewModel
    {
        public WithVisualType WithVisualType
        {
            get => Get<WithVisualType>();
            set => Set(value);
        }

        #region With Visuals 

        [DependsOn(nameof(WithVisualType))]
        public Visibility WithVisualsUiVisibility => (WithVisualType == WithVisualType.WithVisuals).ToVisibility();

        public TmxLevels SelectedLevel
        {
            get => Get<TmxLevels>();
            set => Set(value);
        }

        [DependsOn(nameof(WithVisualType))]
        public bool IsWithVisualsChecked
        {
            get => WithVisualType == WithVisualType.WithVisuals;
            set
            {
                if (value)
                {
                    WithVisualType = WithVisualType.WithVisuals;
                }
            }
        }

        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldPlatformerASelected
        {
            get => SelectedLevel == TmxLevels.OverworldPlatformerA;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldPlatformerA;
                }
            }
        }

        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldPlatformerBSelected
        {
            get => SelectedLevel == TmxLevels.OverworldPlatformerB;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldPlatformerB;
                }
            }
        }

        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldPlatformerCSelected
        {
            get => SelectedLevel == TmxLevels.OverworldPlatformerC;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldPlatformerC;
                }
            }
        }


        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldTopDownASelected
        {
            get => SelectedLevel == TmxLevels.OverworldTopDownA;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldTopDownA;
                }
            }
        }
        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldTopDownBSelected
        {
            get => SelectedLevel == TmxLevels.OverworldTopDownB;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldTopDownB;
                }
            }
        }
        [DependsOn(nameof(SelectedLevel))]
        public bool IsOverworldTopDownCSelected
        {
            get => SelectedLevel == TmxLevels.OverworldTopDownC;
            set
            {
                if (value)
                {
                    SelectedLevel = TmxLevels.OverworldTopDownC;
                }
            }
        }

        public Visibility PlatformerLevelVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public Visibility TopDownLevelVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }


        #endregion

        #region No Visuals

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

        [DependsOn(nameof(WithVisualType))]
        public bool IsNoVisualsChecked
        {
            get => WithVisualType == WithVisualType.NoVisuals;
            set
            {
                if (value)
                {
                    WithVisualType = WithVisualType.NoVisuals;
                }
            }
        }

        [DependsOn(nameof(WithVisualType))]
        public Visibility NoVisualsUiVisibility => (WithVisualType == WithVisualType.NoVisuals).ToVisibility();


        #endregion

        public NewTmxViewModel()
        {
            // by default we will select visuals
            WithVisualType = WithVisualType.WithVisuals;

            // todo - we should have the creator of this look at the Player entity and select a top down or platformer level
            // according to the type of input movement type.
            SelectedLevel = TmxLevels.OverworldPlatformerA;
            IncludeDefaultTileset = true;
            IncludeGameplayLayer = true;
            IsSolidCollisionBorderChecked = true;
        }
    }
}
