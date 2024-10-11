using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using GlueTestProject.TestFramework;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
using GlueTestProject.Factories;
using FlatRedBall.Math;

namespace GlueTestProject.Screens
{
	public partial class FactoryScreen
	{

		void CustomInitialize()
        {
            if (PooledDontInheritFromThisInstance.AxisAlignedRectangleInstance.RelativeX != 5)
            {
                throw new Exception("Pooled values aren't getting proper relative values set.");
            }

            this.ContainerOfFactoryEntityListInstance.Destroy();

            TestResetVariables();

            TestReaddToManagers();

            TestBaseNotPooled();

            TestDerivedPooledFromNotPooled();

            BasePooledEntityFactory.CreateNew();
            DerivedPooledFromPooledFactory.CreateNew().Destroy();
            // If this throws an exception, that means that the derived Destroy method is modifying the base entity factory.
            // This should run in debug to throw:
            BasePooledEntityFactory.CreateNew();

            TestPooledAttachment();

            TestPooledSpriteInheritingCollisionAttachment();

            TestBaseChildGrandchildListAdditions();

            TestFactoriesNotRequiringInitialize();

            TestRecyclingCollidables();
        }


        private void TestDerivedPooledFromNotPooled()
        {
            // According this bug:
            // http://www.hostedredmine.com/issues/413966
            // This may break:
            DerivedPooledFromNotPooledFactory.Initialize(ContentManagerName);
            var pooled = DerivedPooledFromNotPooledFactory.CreateNew();
            if (!SpriteManager.ManagedPositionedObjects.Contains(pooled))
            {
                throw new Exception("Derived entities with pooling from base entities without are not being added to the engine on creation");
            }
            // Now try to destroy:
            pooled.Destroy();
            if (SpriteManager.ManagedPositionedObjects.Contains(pooled))
            {
                throw new Exception("Derived entities with pooling from base entities without are not being removed from the engine on destroy");
            }
        }

        private void TestBaseNotPooled()
        {
            BaseNotPooledFactory.Initialize(ContentManagerName);
            BaseNotPooled notPooled = BaseNotPooledFactory.CreateNew();
            notPooled.Destroy();
        }

        private static void TestReaddToManagers()
        {
            // Let's try addition/removal:
            RecyclableEntity recyclableInstance = new RecyclableEntity();
            recyclableInstance.Destroy();
            // I believe this has been unified:
            recyclableInstance.AddToManagers(null);
            //recyclableInstance.ReAddToManagers(null);

            recyclableInstance.Destroy();
        }

        private void TestResetVariables()
        {
            FactoryEntityDerivedFactory.Initialize(ContentManagerName);
            FactoryEntityDerived instance = FactoryEntityDerivedFactory.CreateNew();
            if (instance.AxisAlignedRectangleInstance.RelativeX != 5.0f)
            {
                throw new Exception("Pooled values aren't getting proper relative values set on derived.");
            }

            instance.AxisAlignedRectangleInstance.RelativeX = 10;

            instance.Destroy();

            instance = FactoryEntityDerivedFactory.CreateNew();
            if (instance.AxisAlignedRectangleInstance.RelativeX != 5.0f)
            {
                throw new Exception("Reset varaibles aren't working");
            }
            instance.Destroy();
        }

        private void TestFactoriesNotRequiringInitialize()
        {
            // This makes sure that initialization is not needed for entities that are not pooled
            var toDestroy = FactoryEntityWithNoListFactory.CreateNew();
            toDestroy.Destroy(); 
        }

        private void TestRecyclingCollidables()
        {
            var instance = Factories.PooledCollidableFactory.CreateNew();
            instance.Collision.Circles.Count.ShouldNotBe(0, "because this object should have a circle");
            instance.Destroy();
            var newInstance = Factories.PooledCollidableFactory.CreateNew();
            newInstance.ShouldBe(instance, "because pooling should recycle");
            newInstance.Collision.Circles.Count.ShouldNotBe(0, "because recycled collidables should get their lists filled again");
        }

        private void TestBaseChildGrandchildListAdditions()
        {
            var newInstance = Factories.GrandchildFactoryEntityFactory.CreateNew();

            BaseFactoryEntityList.Contains(newInstance).ShouldBe(true);
            ChildFactoryEntityList.Contains(newInstance).ShouldBe(true);
            GrandchildFactoryEntityList.Contains(newInstance).ShouldBe(true);
        }

        private void TestPooledAttachment()
        {
            int numberToCreate = 50; // enough to actually pool:
            for (int i = 0; i < numberToCreate; i++)
            {
                Factories.PooledEntityContainingEntityFactory.CreateNew();
            }

            while (this.PooledEntityContainingEntityList.Count != 0)
            {
                this.PooledEntityContainingEntityList.Last.Destroy();
            }

            var pooledInstance = Factories.PooledEntityContainingEntityFactory.CreateNew();

            if (pooledInstance.CircleContainerInstance.Parent == null)
            {
                throw new Exception("If an Entity sprite is destroyed, then its attached entities children get detached and that shouldn't happen!");
            }
        }

        private void TestPooledSpriteInheritingCollisionAttachment()
        {
            int numberToCreate = 50; // enough to actually pool:
            for(int i = 0; i < numberToCreate; i++)
            {
                Factories.PooledEntityInheritFromSpriteFactory.CreateNew();
            }

            while(this.PooledEntityInheritFromSpriteList.Count != 0)
            {
                this.PooledEntityInheritFromSpriteList.Last.Destroy();
            }

            var pooledInstance = Factories.PooledEntityInheritFromSpriteFactory.CreateNew();

            if(pooledInstance.CircleInstance.Parent == null)
            {
                throw new Exception("If an Entity which inherits from a FRB sprite is destroyed, then its attached children (like Circle) also get detached and that shouldn't happen!");
            }
            if(pooledInstance.CircleContainerInstance.Parent == null)
            {
                throw new Exception("If an Entity which inherits from a FRB sprite is destroyed, then its attached entities get detached and not reattached and that shouldn't happen!");
            }
        }

        void CustomActivity(bool firstTimeCalled)
		{
            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
