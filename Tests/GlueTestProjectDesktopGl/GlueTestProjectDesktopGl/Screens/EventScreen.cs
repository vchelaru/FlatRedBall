using System;
using System.Collections.Generic;
using System.Linq;
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
#endif

namespace GlueTestProject.Screens
{
	public partial class EventScreen
	{

		void CustomInitialize()
		{
            // Setting the CircleX should set the Y of this
            this.EventEntityInstance.CircleX = 4;
            if (FlatRedBall.Math.MathFunctions.RoundToInt(EventEntityInstance.Y) != 4)
            {
                throw new Exception("Events seem to not be working on the EventEntityInstance");
            }

            // There was once a code gen bug where events only fired if the object had a parent, so let's detach it and make sure it works
            this.EventEntityInstance.Circle.Detach();
            this.EventEntityInstance.CircleX = 5;
            if (FlatRedBall.Math.MathFunctions.RoundToInt(EventEntityInstance.Y) != 5)
            {
                throw new Exception("Events seem to not be working on the EventEntityInstance when the Circle is detached");
            }

            // There is a script on this which sets the Entity's X to 5
            this.EventEntityInstance.CurrentCategory1State = Entities.EventEntity.Category1.State1;
            if (FlatRedBall.Math.MathFunctions.RoundToInt(EventEntityInstance.X) != 5)
            {
                throw new Exception("Events on States seem to not be working");
            }

            DerivedEventEntity derivedEventEntity = new DerivedEventEntity();
            derivedEventEntity.AfterExposedInDerivedSet += delegate { derivedEventEntity.X = 100; };

            var asBase = derivedEventEntity as EventEntity;
            // this should set X to 100
            asBase.ExposedInDerived = 10;
            if(derivedEventEntity.X == 0)
            {
                throw new Exception("Setting events on the derived, but then assigning the variable on the base doesn't work and it should." +
                    "This is probably happening because base and derived are generating their own events instead of derived using base's");
            }

            var derivedEventEntityType = typeof(DerivedEventEntity);
            var derivedEvents = derivedEventEntityType.GetEvents();

            if (derivedEvents.Any(item => item.Name == "AfterExposedInDerivedSet" && item.DeclaringType.Name == "DerivedEventEntity"))
            {
                throw new Exception("Events for variables defined in base types and set by derived " + 
                    "are being declared in derived types and they shouldn't be.");
            }

            derivedEventEntity.Destroy();
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
