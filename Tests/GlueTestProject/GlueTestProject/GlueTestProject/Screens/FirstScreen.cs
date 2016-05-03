#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
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
using FlatRedBall.IO;
#endif
#endregion

namespace GlueTestProject.Screens
{
	public partial class FirstScreen
	{

		void CustomInitialize()
		{
			string startingPrefix;

			#if WINDOWS
			startingPrefix = "c:\\";
			#else
			startingPrefix = "./";
			#endif

			string fileWithDotDot = startingPrefix + @"directory\subdirectory\..\file.png";

            string afterRemoval = FileManager.RemoveDotDotSlash(fileWithDotDot);
			string expected = FileManager.Standardize (startingPrefix + @"directory\file.png");

			if(afterRemoval != expected)
            {
				throw new Exception(@"Expected " + expected + " but got " + afterRemoval);
            }

			fileWithDotDot = startingPrefix + @"directory\subdirectory\.\file.png";

			afterRemoval = FileManager.Standardize (FileManager.RemoveDotDotSlash(fileWithDotDot));
			expected = FileManager.Standardize (startingPrefix + @"directory\subdirectory\file.png");

			if (afterRemoval != expected)
            {
                throw new Exception("Removal of ./ is not working.  Expected " + expected + " but got " + afterRemoval);
            }

		}

		void CustomActivity(bool firstTimeCalled)
		{
            IsActivityFinished = true;

		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
