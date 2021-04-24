using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPluginsCore.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace OfficialPluginsCore.Wizard.Models
{
    #region Enums

    public enum GameType
    {
        Platformer,
        Topdown,
        None
    }

    public enum CollisionType
    {
        Rectangle,
        Circle,
        None
    }

    #endregion

    public class WizardData : ViewModel
    {
        #region Runtime (ignored) stuff like Configuration

        [JsonIgnore]
        [XmlIgnore]
        public string WizardConfiguration
        {
            get;
            set;
        }

        [JsonIgnore]
        public List<TaskItemViewModel> Tasks
        {
            get => Get<List<TaskItemViewModel>>();
            set => Set(value);
        }

        #endregion

        #region GameScreen

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

        #endregion

        #region Player Entity

        public bool AddPlayerEntity
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddPlayerEntity))]
        [DependsOn(nameof(AddCloudCollision))]
        [DependsOn(nameof(PlayerControlType))]
        public bool ShowPlayerVsCloudCollision => AddPlayerEntity && AddCloudCollision && PlayerControlType == GameType.Platformer;

        [DependsOn(nameof(AddPlayerEntity))]
        [DependsOn(nameof(AddSolidCollision))]
        public bool ShowPlayerVsSolidCollision => AddPlayerEntity && AddSolidCollision;

        public GameType PlayerControlType
        {
            get => Get<GameType>();
            set => Set(value);
        }

        [DependsOn(nameof(AddPlayerEntity))]
        public bool ShowOffsetPositionUi =>
            // Shouldn't be tied specifically to platformer, since borders can be added
            // no matter what kind of movement we have.
            AddPlayerEntity;
        //PlayerControlType == GameType.Platformer;

        public bool OffsetPlayerPosition
        {
            get => Get<bool>();
            set => Set(value);
        }

        public CollisionType PlayerCollisionType
        {
            get => Get<CollisionType>();
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

        public bool AddPlayerSprite
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Levels

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
        public bool IncludeCollisionBorderInLevels
        {
            get => Get<bool>();
            set => Set(value);
        }
        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(IncludeGameplayLayerInLevels))]
        [DependsOn(nameof(IncludStandardTilesetInLevels))]
        public bool ShowBorderCollisionCheckBox => AddGameScreen && IncludeGameplayLayerInLevels && IncludStandardTilesetInLevels;

        #endregion

        #region Gum/UI

        public bool AddGum {
            get => Get<bool>();
            set => Set(value);
        }
        public bool AddFlatRedBallForms
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Camera

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

        #endregion

        #region Additional Entities

        public List<string> ElementImportUrls
        {
            get; set;
        } = new List<string>();

        #endregion

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

            AddPlayerSprite = true;
            OffsetPlayerPosition = true;

            CreateLevels = true;
            NumberOfLevels = 2;
            IncludStandardTilesetInLevels = true;
            IncludeGameplayLayerInLevels = true;
            IncludeCollisionBorderInLevels = true;

            AddGum = true;
            AddFlatRedBallForms = true;

            AddCameraController = true;
            FollowPlayersWithCamera = true;
            KeepCameraInMap = true;
        }


    }
}
