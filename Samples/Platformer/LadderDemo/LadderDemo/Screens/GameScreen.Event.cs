using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using LadderDemo.Entities;
using LadderDemo.Screens;
using System.Linq;

namespace LadderDemo.Screens
{
    public partial class GameScreen
    {
        void OnPlayerListVsLadderCollisionCollisionOccurred (Entities.Player player, FlatRedBall.TileCollisions.TileShapeCollection second) 
        {
            player.LastCollisionLadderRectange = second.LastCollisionAxisAlignedRectangles.First();

            // a little inefficient, could use caching to save a little calculation but it won't be too bad:
            var topRectangle = player.LastCollisionLadderRectange;

            var rectangleAbove = second.GetRectangleAtPosition(topRectangle.X, topRectangle.Y + second.GridSize);

            while(rectangleAbove != null)
            {
                topRectangle = rectangleAbove;
                rectangleAbove = second.GetRectangleAtPosition(topRectangle.X, topRectangle.Y + second.GridSize);
            }

            player.TopOfLadderY = topRectangle.Bottom;
        }

    }
}
