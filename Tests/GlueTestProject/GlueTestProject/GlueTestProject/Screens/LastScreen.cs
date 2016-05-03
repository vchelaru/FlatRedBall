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
using FlatRedBall.Graphics;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace GlueTestProject.Screens
{
	public partial class LastScreen
	{
        Text mText;
		void CustomInitialize()
		{
            mText = TextManager.AddText("");
            mText.SetPixelPerfectScale(SpriteManager.Camera);

            
		}

		void CustomActivity(bool firstTimeCalled)
		{
            mText.DisplayText = (ExitTimeAfterScreenCreation - this.PauseAdjustedSecondsSince(0)).ToString();

            if (this.PauseAdjustedSecondsSince(0) > ExitTimeAfterScreenCreation)
            {
#if !IOS && !WINDOWS_8
                FlatRedBallServices.Game.Exit();
#else
                mText.DisplayText = "Success!  All tests passed!";
#endif
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
