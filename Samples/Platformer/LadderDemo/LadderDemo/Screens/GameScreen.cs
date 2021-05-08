using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;



namespace LadderDemo.Screens
{
    public partial class GameScreen
    {

        void CustomInitialize()
        {
            Map.Z = -3;

        }

        void CustomActivity(bool firstTimeCalled)
        {
            DoCollisionActivity();

        }

        private void DoCollisionActivity()
        {
            // first we reset the collision...
            foreach(var player in PlayerList)
            {
                player.LastCollisionLadderRectange = null;
            }
            // Then we do the collision which sets IsCollidingWithLadder if a collision happens
            PlayerListVsLadderCollision.DoCollisions();
        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
