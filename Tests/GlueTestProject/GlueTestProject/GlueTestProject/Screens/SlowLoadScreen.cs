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
	public partial class SlowLoadScreen
	{

		void CustomInitialize()
		{
            // We're going to load slowly here, then make sure that our TimeManager never returns a slow time
#if WINDOWS_8
            System.Threading.Tasks.Task.Delay(1000);
#else
            System.Threading.Thread.Sleep(1000);
#endif
        }

		void CustomActivity(bool firstTimeCalled)
		{
            if (ActivityCallCount > 15)
            {
                IsActivityFinished = true;
            }

            if (TimeManager.SecondDifference > .1f)
            {
                throw new Exception("The frame " + ActivityCallCount + " took " + TimeManager.SecondDifference + " seconds, it shouldn't");
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
