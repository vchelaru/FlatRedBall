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
#endif

namespace GlueTestProject.Screens
{
	public partial class CustomVariableScreen
	{

		void CustomInitialize()
		{
            this.VariableEntityInstance.TextInstanceDisplayTextVelocity = 1;

            // This will make it interpolate to the second state which should use the underlying velocity object
            this.CurrentState = VariableState.FirstState;
            this.InterpolateToState(VariableState.SecondState, 3);


            if (this.VariableEntityInstance.VariableThatCreatesVelocityVelocity == 0)
            {
                throw new Exception("Interpolating to states which set tunneled variables that have velocities are not properly setting velocity on those variables");

            }
		}

		void CustomActivity(bool firstTimeCalled)
		{

            if (ActivityCallCount > 50)
            {
                if (this.VariableEntityInstance.TextInstanceDisplayText < .1f)
                {
                    throw new Exception("Velocity values are not applying properly to variables that use overriding types.");
                }

                if (this.VariableEntityInstance.VariableThatCreatesVelocity == 0)
                {
                    throw new Exception("It looks like variables that have custom velocity are not properly being set through container states that tunnel in to the variable");
                }
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
