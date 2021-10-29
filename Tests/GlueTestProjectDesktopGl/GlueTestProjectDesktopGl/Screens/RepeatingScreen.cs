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
	public partial class RepeatingScreen
	{
        static int NumberOfVisits = 0;
        static string NextScreenOriginal;
		void CustomInitialize()
		{
            if(NumberOfVisits == 0)
            {
                NextScreenOriginal = NextScreen;
            }
            NumberOfVisits++;
            UnloadsContentManagerWhenDestroyed = true;
            if (EmitterListFile1.Count == 0)
            {
                throw new Exception("EmitterListFile1 is being cleared out - probably when the Screen resets.  It shouldn't");
            }
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
                if (NumberOfVisits < 3)
                {
                    NextScreen = this.GetType().FullName;
                    UnloadsContentManagerWhenDestroyed = false;
                }
                else
                {
                    NextScreen = NextScreenOriginal;
                }

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
