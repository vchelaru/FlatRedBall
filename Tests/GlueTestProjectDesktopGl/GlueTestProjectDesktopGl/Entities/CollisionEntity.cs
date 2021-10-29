#region Usings

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
using FlatRedBall.Math.Splines;
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using GlueTestProject.TestFramework;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace GlueTestProject.Entities
{
	public partial class CollisionEntity
	{
		private void CustomInitialize()
		{
            // Just to make sure this compiles:
            this.Collidable1.CollideAgainst(this.CollidableEntityList);

            if (this.SingleInstance1.CollideAgainst(this.SingleInstanceList))
            {
                throw new Exception("ICollidable objects are colliding with each other.  They shouldn't");
            }

            this.Collision.Polygons.Contains(this.PolygonInShapeCollectionShouldBeInICollidableToo)
                .ShouldBe(true,
                    "because polygons inside of other shape collections should still show up in the main ICollidable shape collection");

            this.Collision.Circles.Contains(this.Circle1)
                .ShouldBe(true,
                    "because circles in a list should still be considered part of the ICollidable. The list may be used for organization");
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
