using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using CheckpointAndLevelEndDemo.Entities;
using CheckpointAndLevelEndDemo.Screens;
namespace CheckpointAndLevelEndDemo.Screens
{
    public partial class GameScreen
    {
        void OnPlayerListVsPitCollisionCollisionOccurred (Entities.Player first, FlatRedBall.Math.Geometry.ShapeCollection second) 
        {
            this.RestartScreen(reloadContent:false);
        }
        void OnPlayerListVsCheckpointListCollisionOccurred (Entities.Player first, Entities.Checkpoint checkpoint) 
        {
            if(checkpoint.IsSpriteVisible)
            {
                // This is a checkpoint that you can actually touch and "turn on"
                checkpoint.MarkAsChecked();

                LastCheckpointName = checkpoint.Name;
            }
        }
        void OnPlayerListVsEndOfLevelListCollisionOccurred (Entities.Player first, Entities.EndOfLevel endOfLevel) 
        {
            GameScreen.LastCheckpointName = "LevelStart";
            MoveToScreen(endOfLevel.NextLevel);
        }
        
    }
}
