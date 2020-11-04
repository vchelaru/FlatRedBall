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
        #region Screen Type
        public AddScreenType AddScreenType
        {
            get => Get<AddScreenType>();
            set => Set(value);
        }

        public bool CanAddLevelScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanAddBaseLevelScreen
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

        [DependsOn(nameof(AddScreenType))]
        public Visibility LevelScreenUiVisibility => IsLevelScreen.ToVisibility();
        [DependsOn(nameof(AddScreenType))]
        public Visibility BaseLevelScreenUiVisibility => IsBaseLevelScreen.ToVisibility();

        public bool IsAddStandardTmxChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

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
    }
}
