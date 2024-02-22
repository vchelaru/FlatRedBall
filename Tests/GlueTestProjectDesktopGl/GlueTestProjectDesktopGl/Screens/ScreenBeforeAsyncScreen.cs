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
	public partial class ScreenBeforeAsyncScreen
	{

		void CustomInitialize()
		{


		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (AsyncLoadingState == FlatRedBall.Screens.AsyncLoadingState.NotStarted &&
                !firstTimeCalled)
            {
                // FRB monogame does not support async loading:
                //StartAsyncLoad(typeof(ScreenToLoadAsync).FullName);
                MoveToScreen(typeof(ScreenToLoadAsync).FullName);

            }
            else if (AsyncLoadingState == FlatRedBall.Screens.AsyncLoadingState.Done)
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
