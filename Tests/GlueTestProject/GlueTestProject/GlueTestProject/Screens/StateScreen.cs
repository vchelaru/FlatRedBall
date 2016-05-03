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
#endif

namespace GlueTestProject.Screens
{
	public partial class StateScreen
	{

		void CustomInitialize()
		{

            // These states should be set by generated code
            if (StateVariablesSetInEntity.CurrentState != StateEntity.VariableState.First)
            {
                throw new Exception("The uncategorized First state isn't set, but it is being set in Glue");
            }
            if (StateVariablesSetInEntity.CurrentTopOrBottomState != StateEntity.TopOrBottom.Top)
            {
                throw new Exception("The categorized Top state isn't set, but it is being set in Glue");
            }


            if (StateVariablesSetOnInstance.CurrentState != Entities.StateEntity.VariableState.Second)
            {
                throw new Exception("Setting uncategorized states on objects in Glue is not working properly");
            }

            if (StateVariablesSetOnInstance.CurrentTopOrBottomState != Entities.StateEntity.TopOrBottom.Bottom)
            {
                throw new Exception("Setting categorized states on objects in Glue is not working properly");
            }

            if (StateEntityWithoutCurrentStateVariableInstance.X != 64)
            {
                throw new Exception("The X value should be 64, but instead it's " + StateEntityWithoutCurrentStateVariableInstance.X + " which means CurrentState is set before variables.  It shouldn't be");
            }

            this.InstanceTestingVelocity.StartVelocityTesting(.2f);

            ChildEntity.CurrentState = StateEntityChild.VariableState.Fourth;
            if (ChildEntity.CurrentState != StateEntityChild.VariableState.Fourth)
            {
                throw new Exception("Setting child state in child entity isn't working.");
            }

		    StateEntity parentEntity = ChildEntity;
            if (parentEntity.CurrentState != StateEntity.VariableState.Unknown)
            {
                throw new Exception("Getting child state from parent entity isn't working.");
            }


            if (this.OverridingVariableStateEntityInstance.VariableToGetChangedByState != 4)
            {
                throw new Exception("States aren't properly overriding default values.");
            }

            this.CurrentTextSizeCategoryState = TextSizeCategory.InterpolationInstanceTextSmall;
            this.InterpolateToState(TextSizeCategory.InterpolationInstanceTextLarge, 1);
		}

		void CustomActivity(bool firstTimeCalled)
		{
            // We do some things over time so we
            // need to wait a little bit.
            if (this.ActivityCallCount > 10)
            {
                if (InterpolationEntityInstance.CircleInstanceRadius <= 16.4f)
                {
                    throw new Exception("Tunneled states which are set in other states do not properly interpolate.");
                }
            }


            if (this.ActivityCallCount > 30)
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
