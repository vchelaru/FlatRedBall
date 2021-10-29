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
using Microsoft.Xna.Framework.Audio;
using FlatRedBall.Audio;
#endif

namespace GlueTestProject.Screens
{
	public partial class AudioScreen
	{


		void CustomInitialize()
		{
            if (BoomInstance2 == null || (BoomInstance2 is SoundEffectInstance) == false)
            {
                throw new Exception("SoundEffectInstances are not properly being created by Glue");
            }



            bool isSongPlayingBefore = AudioManager.CurrentlyPlayingSong != null;

            if (!isSongPlayingBefore)
            {
                throw new Exception("Song added to a Screen should play, but it's not");
            }

            AudioManager.AreSongsEnabled = false;

            bool isSongPlayingAfter = AudioManager.CurrentlyPlayingSong != null;
            if (isSongPlayingAfter)
            {
                throw new Exception("Setting AudioManager.AreSongsEnabled to false should stop any current songs");
            }

            AudioManager.AreSongsEnabled = true;
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (ActivityCallCount > 100)
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
