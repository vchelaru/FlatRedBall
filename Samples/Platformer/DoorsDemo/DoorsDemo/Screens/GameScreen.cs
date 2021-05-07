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
using Microsoft.Xna.Framework;

namespace DoorsDemo.Screens
{
    public partial class GameScreen
    {
        ShapeCollection boundsShapeCollection;

        void CustomInitialize()
        {
            // adjust the map Z so that the background layers are behind entities
            Map.Z = -3;

            // Adjust the doors so they're behind the player
            foreach(var door in DoorList)
            {
                door.Z = -.5f;
            }

            boundsShapeCollection = Map.ShapeCollections.First(item => item.Name == "CameraBoundsLayer");

            UpdateBoundsForPosition(Player1.Position);
        }

        private void UpdateBoundsForPosition(Vector3 position)
        {
            foreach(var rectangle in boundsShapeCollection.AxisAlignedRectangles)
            {
                if(rectangle.IsPointInside(position.X, position.Y))
                {
                    CameraControllingEntityInstance.Map = rectangle;
                    break;
                }
            }
        }

        void CustomActivity(bool firstTimeCalled)
        {


        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
