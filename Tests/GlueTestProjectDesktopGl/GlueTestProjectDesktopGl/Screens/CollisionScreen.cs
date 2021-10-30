
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
using FlatRedBall.Math;

namespace GlueTestProject.Screens
{
	public partial class CollisionScreen
	{
		void CustomInitialize()
        {
            MovableRectangle.X = 1;

            MovableRectangle.CollideAgainstMove(ImmovableRectangle, 0, 1);

            if (MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if (ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object when colliding against a 0 mass object");
            }

            TestPositiveInfinitySecondCollisionMass();

            TestPositiveInfinityFirstObjectCollisionMass();

            Test_L_RepositonDirection();

            CreateCollisionRelationships();

            TestEntityListVsShapeCollection();

            TestEntityVsShapeCollection();

            TestNullSubcollision();

            TestCollidedThisFrame();

            TestCollisionRelationshipPreventDoubleCollision();

        }

        private void TestCollisionRelationshipPreventDoubleCollision()
        {
            PositionedObjectList<Entities.CollidableEntity> bulletList = new PositionedObjectList<Entities.CollidableEntity>();
            bulletList.Add(new Entities.CollidableEntity());

            PositionedObjectList<Entities.CollidableEntity> enemyList = new PositionedObjectList<Entities.CollidableEntity>();
            enemyList.Add(new Entities.CollidableEntity());
            enemyList.Add(new Entities.CollidableEntity());

            var collisionRelationship = CollisionManager.Self.CreateRelationship(bulletList, enemyList);

            int numberOfHits = 0;

            collisionRelationship.CollisionOccurred += (bullet, enemy) =>
            {
                numberOfHits++;
                bullet.Destroy();
            };

            collisionRelationship.DoCollisions();

            numberOfHits.ShouldBe(1, "because the bullet is destroyed on the first collision, so it shouldn't hit the second enemy");

            while(enemyList.Count > 0)
            {
                enemyList[0].Destroy();
            }

        }

        private void TestPositiveInfinitySecondCollisionMass()
        {
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
        }

        private void TestPositiveInfinityFirstObjectCollisionMass()
        {
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
        }

        private void TestCollidedThisFrame()
        {
            var firstEntity = new Entities.CollidableEntity();
            var secondEntity = new Entities.CollidableEntity();

            var relationship = CollisionManager.Self.CreateRelationship(firstEntity, secondEntity);

            relationship.CollidedThisFrame.ShouldBe(false);

            var collided = relationship.DoCollisions();

            collided.ShouldBe(true);

            relationship.CollidedThisFrame.ShouldBe(true);


            firstEntity.X += 10000;
            firstEntity.ForceUpdateDependenciesDeep();

            collided = relationship.DoCollisions();

            collided.ShouldBe(false);

            relationship.CollidedThisFrame.ShouldBe(false);

            firstEntity.Destroy();
            secondEntity.Destroy();
        }

        private void TestEntityVsShapeCollection()
        {
            var singleEntity = new Entities.CollidableEntity();

            // for now just testing that the method exists:
            var relationship = CollisionManager.Self.CreateRelationship(singleEntity, ShapeCollectionInstance);



            CollisionManager.Self.Relationships.Remove(relationship);
            singleEntity.Destroy();
        }

        private void TestNullSubcollision()
        {
            CollisionEntityList[0].Collision.ShouldNotBe(null);

            // event collision, both null
            var relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetFirstSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.CollisionOccurred += (first, second) => { };
            relationship.DoCollisions();

            // Move collision, both null
            relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetFirstSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetMoveCollision(1, 1);
            relationship.DoCollisions();

            // Bounce collision, both null
            relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetFirstSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetBounceCollision(1, 1, 1);
            relationship.DoCollisions();

            // event collision, second null
            relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.CollisionOccurred += (first, second) => { };
            relationship.DoCollisions();

            // Move collision, second null
            relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetMoveCollision(1, 1);
            relationship.DoCollisions();

            // Bounce collision, second null
            relationship = CollisionManager.Self.CreateRelationship(CollisionEntityList, CollisionEntityList);
            relationship.SetSecondSubCollision(item => item.NullCollidableEntityInstance);
            relationship.SetBounceCollision(1, 1, 1);
            relationship.DoCollisions();
        }

        private void CreateCollisionRelationships()
        {
            var subcollision = CollisionManager.Self.CreateRelationship(PlayerList, ShipList);

            var selfCollidingRelationship = CollisionManager.Self.CreateRelationship(SelfCollisionList, SelfCollisionList);
            selfCollidingRelationship.SetMoveCollision(1, 1);

            var fullVsEmptyRelationship = CollisionManager.Self.CreateRelationship(PlayerList, EmptyList1);

            var emptyVsFullRelationship = CollisionManager.Self.CreateRelationship(EmptyList1, PlayerList);

            var emptyVsEmptyRelationship = CollisionManager.Self.CreateRelationship(EmptyList1, EmptyList2);

            var emptyVsSameEmptyRelationship = CollisionManager.Self.CreateRelationship(EmptyList1, EmptyList1);


        }

        private void TestEntityListVsShapeCollection()
        {
            var entityVsShapeCollection = CollisionManager.Self.CreateRelationship(CollidableList, ShapeCollectionInstance);

            List<Entities.CollidableEntity> collidedEntities = new List<Entities.CollidableEntity>();

            entityVsShapeCollection.CollisionOccurred += (entity, shapeCollection) =>
            {
                collidedEntities.Add(entity);
            };

            entityVsShapeCollection.DoCollisions();

            collidedEntities.Contains(At0).ShouldBe(true);
            collidedEntities.Contains(At100).ShouldBe(true);
            collidedEntities.Contains(At200).ShouldBe(true);
            collidedEntities.Contains(At400).ShouldBe(false);
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
            corner.RepositionHalfSize = true;
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

            if(!firstTimeCalled)
            {
                var areAllDifferent = C1.X != C2.X && C2.X != C3.X && C3.X != C4.X;
                areAllDifferent.ShouldBe(true, "because all items in this list should self move collide");
            }

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
