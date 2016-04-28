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

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
using GlueTestProject.Factories;
using FlatRedBall.Math;
#endif

namespace GlueTestProject.Screens
{
	public partial class FactoryScreen
	{

		void CustomInitialize()
		{
            // Just to make sure the syntax works:
            FactoryEntityFactory.ScreenListReference = FactoryEntityFactory.ScreenListReference;


            if (PooledDontInheritFromThisInstance.AxisAlignedRectangleInstance.RelativeX != 5)
            {
                throw new Exception("Pooled values aren't getting proper relative values set.");
            }

            this.ContainerOfFactoryEntityListInstance.Destroy();

            try
            {
                FactoryEntity factoryEntity = FactoryEntityFactory.CreateNew();
            }
            catch(Exception e)
            {
                throw new Exception("Destroying Entities also destroys factories if the Entity contains a list of a pooled type.  This shouldn't happen.");
            }


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

            // Let's try addition/removal:
            RecyclableEntity recyclableInstance = new RecyclableEntity();
            recyclableInstance.Destroy();
            recyclableInstance.ReAddToManagers(null);

            recyclableInstance.Destroy();

            BaseNotPooledFactory.Initialize(null, ContentManagerName);
            BaseNotPooled notPooled = BaseNotPooledFactory.CreateNew();
            notPooled.Destroy();


            // According this bug:
            // http://www.hostedredmine.com/issues/413966
            // This may break:
            DerivedPooledFromNotPooledFactory.Initialize((PositionedObjectList<DerivedPooledFromNotPooled>)null, ContentManagerName);
            var pooled = DerivedPooledFromNotPooledFactory.CreateNew();
            if (!SpriteManager.ManagedPositionedObjects.Contains(pooled))
            {
                throw new Exception("Derived entities with pooling from base entities without are not being added to the engine on creation");
            }
            // Now try to destroy:
            pooled.Destroy();
            if(SpriteManager.ManagedPositionedObjects.Contains(pooled))
            {
                throw new Exception("Derived entities with pooling from base entities without are not being removed from the engine on destroy");
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
