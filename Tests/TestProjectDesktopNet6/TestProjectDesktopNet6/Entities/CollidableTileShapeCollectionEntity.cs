using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using GlueTestProject.TestFramework;

namespace GlueTestProject.Entities
{
    public partial class CollidableTileShapeCollectionEntity
    {
        /// <summary>
        /// Initialization logic which is executed only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {
            TileShapeCollection_Included.Rectangles.Count.ShouldBe(16);
            TileShapeCollection_Excluded.Rectangles.Count.ShouldBe(16);

            Collision.AxisAlignedRectangles.Count.ShouldBe(16, "because only the rectangles in the included should be included here, and there are only 16 of them");

            // for debugging:
            for (int i = 0; i < TileShapeCollection_Excluded.Rectangles.Count; i++)
            {
                TileShapeCollection_Excluded.Rectangles[i].Name = $"Included rect {i}";
            }

            for (int i = 0; i < TileShapeCollection_Included.Rectangles.Count; i++)
            {
                TileShapeCollection_Included.Rectangles[i].Name = $"Included rect {i}";
            }
        }

        private void CustomActivity()
        {
            
        }

        private void CustomDestroy()
        {
            
        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {
            
        }
    }
}
