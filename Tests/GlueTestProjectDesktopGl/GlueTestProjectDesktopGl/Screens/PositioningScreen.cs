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
using FlatRedBall.Instructions;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Math;
using GlueTestProject.Entities;
using GlueTestProject.Factories;
#endif

namespace GlueTestProject.Screens
{
	public partial class PositioningScreen
	{
        int mNumberOfCreations = 0;
        int mNumberOfDestroys = 0;

		void CustomInitialize()
		{
            PositionedEntityInstance.ForceUpdateDependencies();
            if (PositionedEntityInstance.X == 0)
            {
                throw new Exception("Entities that set their own position through variables are not positioned properly in Screens when they are used as instances");
            }

            AttachedToCameraInstance.ForceUpdateDependencies();
            if (AttachedToCameraInstance.Z != 0)
            {
                throw new Exception("The AttachedToCamera instance should be at Z=0 but it's at Z=" + AttachedToCameraInstance.Z);
            }

            AttachedToCameraCopyInstance.ForceUpdateDependencies();
            if (MathFunctions.RoundToInt(AttachedToCameraCopyInstance.X) != 40)
            {
                throw new Exception("Entities attached to Cameras that set their exposed position in the entity itself are not positioned correctly");

            }

            ZSetAttachedToCamera.ForceUpdateDependencies();
            if (MathFunctions.RoundToInt(ZSetAttachedToCamera.X) != 0)
            {
                throw new Exception("Entity instances which explicitly set their X and attach to the Camera do not use a proper X value");
            }
            if (MathFunctions.RoundToInt(ZSetAttachedToCamera.Y) != 0)
            {
                throw new Exception("Entity instances which explicitly set their Y and attach to the Camera do not use a proper Y value");
            }
            if (MathFunctions.RoundToInt(ZSetAttachedToCamera.Z) != 0)
            {
                throw new Exception("Entity instances which explicitly set their Z and attach to the Camera do not use a proper Z value");
            }

            this.Call(CreateEntity).After(.2f);
            this.Call(DestroyEntity).After(.4f);


            this.Call(CreateEntity).After(.6f);
            this.Call(DestroyEntity).After(.8f);

            this.Set("IsActivityFinished").To(true).After(1.0f);

		}

		void CustomActivity(bool firstTimeCalled)
		{

		}

        void CreateEntity()
        {

            PositioningEntity pe = PositioningEntityFactory.CreateNew();

            float xPosition = 4;
            pe.X = xPosition;
            pe.ForceUpdateDependenciesDeep();

            string messageAboutCreation = null;
            if(mNumberOfCreations > 0)
            {
                messageAboutCreation = "This occurs on a recycled instance retrieved from a Factory.  ";
            }

            if (MathFunctions.RoundToInt(pe.EmitterInstance.Position.X) != MathFunctions.RoundToInt(xPosition + 1))
            {
                throw new Exception("There seems to be a problem with from-file object positioning.  " + messageAboutCreation);
            }
            if (MathFunctions.RoundToInt(pe.Circle.Position.X) != MathFunctions.RoundToInt(xPosition + 2))
            {
                throw new Exception("There seems to be a problem with Glue-created object positioning. " + messageAboutCreation + "Position should be " + (xPosition + 2) + " but it's " + pe.Circle.Position.X);
            }
            if (MathFunctions.RoundToInt(pe.ChildOfPositioningEntityInstance.Position.X) != MathFunctions.RoundToInt(xPosition + 3))
            {
                throw new Exception("There seems to be a problem with positioning related to Objects that have exposed variables, and those variables are set on the instance. " + messageAboutCreation +
                    "Position should be " + (xPosition + 3) + " but it's " + pe.ChildOfPositioningEntityInstance.Position.X);
            }
            if (pe.ChildOfPositioningEntityInstance.Position != pe.ChildOfPositioningEntityInstance.Rectangle.Position)
            {
                throw new Exception("The ChildOfPositioningEntityInstance is positioned at " + pe.ChildOfPositioningEntityInstance + " but its contained rectangle is at " +
                    pe.ChildOfPositioningEntityInstance.Rectangle.Position);
            }




            if (EntityList.Count != 1)
            {
                throw new Exception("There are an incorrect number of Entities: {EntityList.Count}, but expected to have 1. Did factory adding break on EntityList?");
            }

            // We move it to the right to see if velocity causes problems when recycling
            pe.XVelocity = 1;

            mNumberOfCreations++;

        }

        void DestroyEntity()
        {
            EntityList[0].Destroy();
            if (EntityList.Count != 0)
            {
                throw new Exception("There should be no Entities, but there are: " + EntityList.Count);
            }

            mNumberOfDestroys++;

        }

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
