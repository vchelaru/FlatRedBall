using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.ViewModels
{
    enum AddScreenType
    {
        LevelScreen,
        BaseLevelScreen,
        EmptyScreen
    }

    class AddScreenViewModel : ViewModel
    {
        public AddScreenType AddScreenType
        {
            get => Get<AddScreenType>();
            set => Set(value);
        }

        public bool HasGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        #region Level Screen

        public bool CanAddLevelScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddScreenType))]
        public bool IsLevelScreen
        {
            get => AddScreenType == AddScreenType.LevelScreen;
            set
            {
                if(value)
                {
                    AddScreenType = AddScreenType.LevelScreen;
                }
            }
        }

        [DependsOn(nameof(HasGameScreen))]
        public Visibility LevelScreenOptionUiVisibility => HasGameScreen.ToVisibility();


        [DependsOn(nameof(AddScreenType))]
        [DependsOn(nameof(HasGameScreen))]
        public Visibility LevelScreenUiVisibility => (HasGameScreen && IsLevelScreen).ToVisibility();


        public bool IsAddStandardTmxChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool InheritFromGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSetAsStartupChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Base Level Screen

        public bool CanAddBaseLevelScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddScreenType))]
        public bool IsBaseLevelScreen
        {
            get => AddScreenType == AddScreenType.BaseLevelScreen;
            set
            {
                if (value)
                {
                    AddScreenType = AddScreenType.BaseLevelScreen;
                }
            }
        }

        [DependsOn(nameof(AddScreenType))]
        public Visibility BaseLevelScreenUiVisibility => IsBaseLevelScreen.ToVisibility();

        public bool IsAddMapLayeredTileMapChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAddSolidCollisionShapeCollectionChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAddCloudCollisionShapeCollectionChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Empty Screen

        [DependsOn(nameof(AddScreenType))]
        public bool IsEmptyScreen
        {
            get => AddScreenType == AddScreenType.EmptyScreen;
            set
            {
                if (value)
                {
                    AddScreenType = AddScreenType.EmptyScreen;
                }
            }
        }


        #endregion

    }
}
