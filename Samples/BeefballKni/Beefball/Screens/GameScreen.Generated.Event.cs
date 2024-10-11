using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using Beefball.Entities;
using Beefball.Screens;
namespace Beefball.Screens
{
    public partial class GameScreen
    {
        void OnPuckVsGoalCollidedTunnel (Entities.Puck puck, Entities.Goal goal) 
        {
            if (this.PuckVsGoalCollided != null)
            {
                PuckVsGoalCollided(puck, goal);
            }
        }
    }
}
