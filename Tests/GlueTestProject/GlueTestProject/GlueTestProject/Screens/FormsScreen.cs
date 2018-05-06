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
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using GlueTestProject.Forms.Controls;

namespace GlueTestProject.Screens
{
	public partial class FormsScreen
	{
        CustomUserControl control;
        void CustomInitialize()
		{
            // Test if derived controls automatically get visuals from their base if the derived doesn't exist...
            control = new CustomUserControl();
            control.Visual.AddToManagers();

		}

		void CustomActivity(bool firstTimeCalled)
		{
            IsActivityFinished = true;

		}

		void CustomDestroy()
		{
            control.Visual.RemoveFromManagers();

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
