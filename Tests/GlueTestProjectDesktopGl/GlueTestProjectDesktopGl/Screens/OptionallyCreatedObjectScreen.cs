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
	public partial class OptionallyCreatedObjectScreen
	{

		void CustomInitialize()
		{
            if (this.CircleInvalidSetByContainer == null)
            {
                throw new Exception("Objects which are SetByContainer in Screens should be instantiated since Screens can't contained");

            }

		}

		void CustomActivity(bool firstTimeCalled)
		{

            // Let's keep this thing around for a few frames so that the contained objects 
            // have time to do their logic.  
            if (this.ActivityCallCount > 10)
            {
                this.IsActivityFinished = true;

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
