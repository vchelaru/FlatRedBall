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



namespace GlueTestProject.Screens
{
	public partial class GumScreenLayered
	{

		void CustomInitialize()
		{
            var gumLayer = LayerInstanceGum;
            if(gumLayer.Renderables.Count == 0)
            {
                throw new Exception("Moving an entire Gum screen to a FRB layer does not move contained objects within that screen. It should.");
            }
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if(!HasDrawBeenCalled)
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
