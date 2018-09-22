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
using Microsoft.Xna.Framework.Graphics;
using FlatRedBallWpf.ScriptLoading;
using FlatRedBall.Screens;
using GlueView2.Commands;

namespace FlatRedBallWpf.Screens
{
	public partial class MainScreen
	{
     
        void CustomInitialize()
		{
            //GlueViewCommands.Self.LoadProject(
            //        @"C:\Users\Victor\Documents\FlatRedBallProjects\GViewTargetProject\GViewTargetProject\GViewTargetProject.glux");
        }

        void CustomActivity(bool firstTimeCalled)
		{
            //if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            //{
            //    GlueViewCommands.Self.ShowScreen("GViewTargetProject.Screens.GameScreen");
            //}
        }

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
