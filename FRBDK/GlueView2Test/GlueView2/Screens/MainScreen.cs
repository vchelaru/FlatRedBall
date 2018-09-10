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
using GlueView2.ScriptLoading;
using FlatRedBall.Screens;

namespace GlueView2.Screens
{
	public partial class MainScreen
	{
        ScriptLoadingLogic scriptLoadingLogic;
        Screen dynamicallyLoadedScreen;

        void CustomInitialize()
		{
            scriptLoadingLogic = new ScriptLoadingLogic();

		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                if(dynamicallyLoadedScreen != null)
                {
                    dynamicallyLoadedScreen.Destroy();
                }


                var directory = 
                    @"C:\Users\Victor\Documents\FlatRedBallProjects\GViewTargetProject\GViewTargetProject\";
                var assembly = scriptLoadingLogic.LoadProjectCode(directory);

                FlatRedBall.IO.FileManager.RelativeDirectory = directory;
                dynamicallyLoadedScreen = (Screen)assembly.CreateObject("GViewTargetProject.Screens.GameScreen");


                dynamicallyLoadedScreen.Initialize(true);
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
