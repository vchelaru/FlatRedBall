using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.Wizard.Models
{
    enum GameType
    {
        Platformer,
        Topdown,
        None
    }
    class WizardData
    {

        public bool AddGameScreen { get; set; } = true;
        public bool AddTiledMap { get; set; } = true;
        public bool AddSolidCollision { get; set; } = true;
        public bool AddCloudCollision { get; set; } = true;

        public bool AddPlayerEntity { get; set; } = true;

        public GameType PlayerControlType { get; set; } = GameType.Topdown;
        public bool AddPlayerListToGameScreen { get; set; } = true;
        public bool AddPlayerToList { get; set; } = true;

        public bool CollideAgainstSolidCollision { get; set; } = true;
        public bool CollideAgainstCloudCollision { get; set; } = true;

        public bool CreateLevels { get; set; } = true;
        public int NumberOfLevels { get; set; } = 2;
        public bool IncludStandardTilesetInLevels { get; set; } = true;
        public bool IncludeGameplayLayerInLevels { get; set; } = true;

        public bool AddGum { get; set; } = true;
        public bool AddFlatRedBallForms { get; set; } = true;

        public bool AddCameraController { get; set; } = true;
        public bool FollowPlayersWithCamera { get; set; } = true;
        public bool KeepCameraInMap { get; set; } = true;
    }
}
