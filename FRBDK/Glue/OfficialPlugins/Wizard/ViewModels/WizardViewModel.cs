using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPluginsCore.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace OfficialPluginsCore.Wizard.Models
{
    #region Enums

    public enum GameType
    {
        Platformer,
        TopDown,
        None,
    }

    public enum CollisionType
    {
        Rectangle,
        Circle,
        None
    }

    public enum PlayerCreationType
    {
        SelectOptions,
        ImportEntity
    }

    public enum WithVisualType
    {
        WithVisuals,
        NoVisuals
    }

    public enum CameraResolution
    {
        _256x224,
        _360x240,
        _480x360,
        _640x480,
        _800x600,
        _1024x768,
        _1920x1080
    }

    public static class CameraResolutionExtensions
    {
        public static int ResolutionWidth(this CameraResolution cameraResolution)
        {
            switch (cameraResolution)
            {
                case CameraResolution._256x224: return 256;
                case CameraResolution._360x240: return 360;
                case CameraResolution._480x360: return 480;
                case CameraResolution._640x480: return 640;
                case CameraResolution._800x600: return 800;
                case CameraResolution._1024x768: return 1024;
                case CameraResolution._1920x1080: return 1920;
            }
            return 0;
        }

        public static int ResolutionHeight(this CameraResolution cameraResolution)
        {
            switch (cameraResolution)
            {
                case CameraResolution._256x224: return 224;
                case CameraResolution._360x240: return 240;
                case CameraResolution._480x360: return 360;
                case CameraResolution._640x480: return 480;
                case CameraResolution._800x600: return 600;
                case CameraResolution._1024x768: return 768;
                case CameraResolution._1920x1080: return 1080;
            }
            return 0;
        }
    }

    #endregion

    public class WizardViewModel : ViewModel
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

        public bool AddHudLayer
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

        public PlayerCreationType PlayerCreationType
        {
            get => Get<PlayerCreationType>();
            set => Set(value);
        }


        [DependsOn(nameof(PlayerCreationType))]
        [DependsOn(nameof(AddPlayerEntity))]
        public bool IsPlayerCreationSelectingOptions => 
            PlayerCreationType == PlayerCreationType.SelectOptions &&
            AddPlayerEntity;


        [DependsOn(nameof(AddPlayerEntity))]
        [DependsOn(nameof(PlayerCreationType))]
        public Visibility PlayerEntityImportUiVisibility =>
            (AddPlayerEntity && PlayerCreationType == PlayerCreationType.ImportEntity).ToVisibility();

        public string PlayerEntityImportUrlOrFile
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(AddPlayerEntity))]
        [DependsOn(nameof(AddCloudCollision))]
        [DependsOn(nameof(PlayerControlType))]
        [DependsOn(nameof(PlayerCreationType))]
        public bool ShowPlayerVsCloudCollision => 
            AddPlayerEntity && 
            (AddCloudCollision && PlayerControlType == GameType.Platformer || PlayerCreationType == PlayerCreationType.ImportEntity);

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

        #region Platformer Animations

        public bool AddPlayerSpritePlatformerAnimations
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddPlayerSprite))]
        [DependsOn(nameof(PlayerControlType))]
        public bool ShowAddPlayerSpritePlatformerAnimations =>
            AddPlayerSprite && PlayerControlType == GameType.Platformer;

        public bool AddPlatformerAnimationController
        {
            get => Get<bool>();
            set => Set(value);
        }
        [DependsOn(nameof(ShowAddPlayerSpritePlatformerAnimations))]
        [DependsOn(nameof(AddPlayerSpritePlatformerAnimations))]
        public bool ShowAddPlatformAnimatorController =>
            ShowAddPlayerSpritePlatformerAnimations && AddPlayerSpritePlatformerAnimations;

        #endregion

        #region Top-Down Animations

        public bool AddPlayerSpriteTopDownAnimations
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AddPlayerSprite))]
        [DependsOn(nameof(PlayerControlType))]
        public bool ShowAddPlayerSpriteTopDownAnimations =>
            AddPlayerSprite && PlayerControlType == GameType.TopDown;

        public bool AddTopDownAnimationController
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(ShowAddPlayerSpriteTopDownAnimations))]
        [DependsOn(nameof(AddPlayerSpriteTopDownAnimations))]
        public bool ShowAddTopDownAnimatorController =>
            ShowAddPlayerSpriteTopDownAnimations && AddPlayerSpriteTopDownAnimations;

        #endregion

        public bool IsPlayerDamageableChecked
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

        [DependsOn(nameof(PlayerControlType))]
        [DependsOn(nameof(AddGameScreen))]
        public bool IsWithVisualsVisible => 
            AddGameScreen &&
            PlayerControlType == GameType.Platformer;

        public WithVisualType WithVisualType
        {
            get
            {
                if(PlayerControlType == GameType.Platformer)
                {
                    return Get<WithVisualType>();
                }
                else
                {
                    // top down doesn't yet support visuals
                    return WithVisualType.NoVisuals;
                }
            }
            set => Set(value);
        }

        public bool IncludePlatformerVisualLayers
        {
            get => WithVisualType == WithVisualType.WithVisuals;
            set => WithVisualType = value ? WithVisualType.WithVisuals : WithVisualType.NoVisuals;
        }

        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(WithVisualType))]
        public bool IsIncludeStandardTilesetUiVisible =>
            AddGameScreen && WithVisualType == WithVisualType.NoVisuals;

        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(WithVisualType))]
        public bool IsIncludeGameplayLayerVisibile =>
            AddGameScreen && WithVisualType == WithVisualType.NoVisuals;


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
        [DependsOn(nameof(WithVisualType))]
        public bool ShowBorderCollisionCheckBox => 
            AddGameScreen && 
            IncludeGameplayLayerInLevels && 
            IncludStandardTilesetInLevels &&
            WithVisualType == WithVisualType.NoVisuals;

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

        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(AddHudLayer))]
        [DependsOn(nameof(AddGum))]
        public bool IsAddGumScreenToLayerVisible => AddGameScreen && AddHudLayer && AddGum;

        public bool AddGameScreenGumToHudLayer
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

        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(AddPlayerEntity))]
        [DependsOn(nameof(AddPlayerListToGameScreen))]
        public bool FollowPlayersWithCameraVisibility => AddGameScreen && AddPlayerEntity && AddPlayerListToGameScreen;

        public bool KeepCameraInMap
        {
            get => Get<bool>();
            set => Set(value);
        }
        [DependsOn(nameof(AddGameScreen))]
        [DependsOn(nameof(AddTiledMap))]
        public bool KeepCameraInMapVisibility => AddGameScreen && AddTiledMap;

        public CameraResolution SelectedCameraResolution
        {
            get => Get<CameraResolution>();
            set => Set(value);
        }

        public int ScalePercent
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedCameraResolution))]
        [DependsOn(nameof(ScalePercent))]
        public string EffectiveCameraResolutionText => $"Game Display Size: {SelectedCameraResolution.ResolutionWidth() * ScalePercent/100}x{SelectedCameraResolution.ResolutionHeight() * ScalePercent/100}";

        #endregion

        #region Additional Entities

        public List<string> ElementImportUrls
        {
            get; set;
        } = new List<string>();

        #endregion

        // Vic says - this is easy to do, but do we want to support more than this like having entire serialized screens?
        public List<string> AdditionalNonGameScreens { get; set; } = new List<string>();

        //public Dictionary<string, NamedObjectSave> NamedObjectSaves
        //{
        //    get; set;
        //} = new Dictionary<string, NamedObjectSave>();

        // Deserializes to a Dictionary<string, NamedObjectSave> where the string key is the name of the screen
        // that contains the NamedObject.
        public string NamedObjectSavesSerialized
        {
            get; set;
        }

        #region Constructor/Init

        public WizardViewModel()
        {

        }

        public void ApplyDefaults()
        { 
            AddGameScreen = true;
            AddTiledMap = true;
            AddSolidCollision = true;
            AddCloudCollision = true;
            AddHudLayer = true;

            AddPlayerEntity = true;

            PlayerControlType = GameType.TopDown;
            AddPlayerListToGameScreen = true;
            AddPlayerToList = true;

            CollideAgainstSolidCollision = true;
            CollideAgainstCloudCollision = true;

            AddPlayerSprite = true;
            OffsetPlayerPosition = true;

            IsPlayerDamageableChecked = true;

            CreateLevels = true;
            WithVisualType = WithVisualType.WithVisuals;
            NumberOfLevels = 2;
            IncludStandardTilesetInLevels = true;
            IncludeGameplayLayerInLevels = true;
            IncludeCollisionBorderInLevels = true;

            AddGum = true;
            AddFlatRedBallForms = true;
            AddGameScreenGumToHudLayer = true;

            AddCameraController = true;
            FollowPlayersWithCamera = true;
            KeepCameraInMap = true;

            SelectedCameraResolution = CameraResolution._480x360;
            ScalePercent = 200;
        }

        #endregion

    }
}
