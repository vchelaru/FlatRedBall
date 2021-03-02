using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.Wizard.Models
{
    public enum GameType
    {
        Platformer,
        Topdown,
        None
    }
    public class WizardData : ViewModel
    {

        public bool AddGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddGameScreen))]
        public bool NoGameScreen => AddGameScreen == false;

        public bool AddTiledMap
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool AddSolidCollision
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool AddCloudCollision
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool AddPlayerEntity
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GameType PlayerControlType
        {
            get => Get<GameType>();
            set => Set(value);
        }
        public bool AddPlayerListToGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool AddPlayerToList
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CollideAgainstSolidCollision
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool CollideAgainstCloudCollision
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CreateLevels
        {
            get => Get<bool>();
            set => Set(value);
        }
        public int NumberOfLevels
        {
            get => Get<int>();
            set => Set(value);
        }
        public bool IncludStandardTilesetInLevels
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IncludeGameplayLayerInLevels
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool AddGum {
            get => Get<bool>();
            set => Set(value);
        }
        public bool AddFlatRedBallForms
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool AddCameraController
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool FollowPlayersWithCamera
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool KeepCameraInMap
        {
            get => Get<bool>();
            set => Set(value);
        }


        public WizardData()
        {
            AddGameScreen = true;
            AddTiledMap = true;
            AddSolidCollision = true;
            AddCloudCollision = true;

            AddPlayerEntity = true;

            PlayerControlType = GameType.Topdown;
            AddPlayerListToGameScreen = true;
            AddPlayerToList = true;

            CollideAgainstSolidCollision = true;
            CollideAgainstCloudCollision = true;

            CreateLevels = true;
            NumberOfLevels = 2;
            IncludStandardTilesetInLevels = true;
            IncludeGameplayLayerInLevels = true;

            AddGum = true;
            AddFlatRedBallForms = true;

            AddCameraController = true;
            FollowPlayersWithCamera = true;
            KeepCameraInMap = true;
        }


    }
}
