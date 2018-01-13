
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

using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.TestFramework;

namespace GlueTestProject.Screens
{
	public partial class CollisionScreen
	{
		void CustomInitialize()
		{
            MovableRectangle.X = 1;

            MovableRectangle.CollideAgainstMove(ImmovableRectangle, 0, 1);

            if(MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if(ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object when colliding against a 0 mass object");
            }

            // try positive infinity:
            MovableRectangle.X = 1;
            MovableRectangle.CollideAgainstMove(ImmovableRectangle, 1, float.PositiveInfinity);
            if (MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if (ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object with positive infinity");
            }




            // Try positive infinity, call with the immovable:
            MovableRectangle.X = 1;
            ImmovableRectangle.CollideAgainstMove(MovableRectangle, float.PositiveInfinity, 1);
            if (MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if (ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object with positive infinity");
            }

            Test_L_RepositonDirection();

            CreateCollisionRelationships();
        }

        private void CreateCollisionRelationships()
        {
            var subcollision = CollisionManager.Self.CreateRelationship(PlayerList, ShipList);


        }

        private void Test_L_RepositonDirection()
        {
            ShapeCollection rectangles = new ShapeCollection();

            // This tests the following bug:

            // https://trello.com/c/twwOTKFz/411-l-shaped-corners-can-cause-entities-to-teleport-through-despite-using-proper-reposition-directions

            // make corner first so it is tested first
            var corner = new AxisAlignedRectangle();
            corner.X = 0;
            corner.Y = 0;
            corner.Width = 32;
            corner.Height = 32;
            corner.RepositionDirections = RepositionDirections.Left | RepositionDirections.Down;
            rectangles.AxisAlignedRectangles.Add(corner);

            var top = new AxisAlignedRectangle();
            top.X = 0;
            top.Y = 32;
            top.Width = 32;
            top.Height = 32;
            top.RepositionDirections = RepositionDirections.Left | RepositionDirections.Right;
            rectangles.AxisAlignedRectangles.Add(top);

            var right = new AxisAlignedRectangle();
            right.X = 32;
            right.Y = 0;
            right.Width = 32;
            right.Height = 32;
            right.RepositionDirections = RepositionDirections.Up | RepositionDirections.Down;
            rectangles.AxisAlignedRectangles.Add(right);

            var movable = new AxisAlignedRectangle();
            movable.X = 32 - 1;
            movable.Y = 32 - 1;
            movable.Width = 32;
            movable.Height = 32;

            movable.CollideAgainstMove(rectangles, 0, 1);

            movable.X.ShouldBeGreaterThan(31);
            movable.Y.ShouldBeGreaterThan(31);
        }

        void CustomActivity(bool firstTimeCalled)
        {
            var shouldContinue = this.ActivityCallCount > 5;

            // give the screen a chance to call 

            if (shouldContinue)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{
            CollisionManager.Self.Relationships.Count.ShouldBe(0);
        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
