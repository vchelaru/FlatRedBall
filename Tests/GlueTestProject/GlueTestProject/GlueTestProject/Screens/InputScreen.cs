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
using GlueTestProject.TestFramework;


namespace GlueTestProject.Screens
{
	public partial class InputScreen
	{

		void CustomInitialize()
		{
            TestKeyboardInputDevice();

            TestXboxGamePadInputDevice();
		}

        private void TestKeyboardInputDevice()
        {
            IInputDevice inputDevice = InputManager.Keyboard;

            inputDevice.Default2DInput            .ShouldNotBe(null);
            inputDevice.DefaultHorizontalInput    .ShouldNotBe(null);
            inputDevice.DefaultVerticalInput.ShouldNotBe(null);
            inputDevice.DefaultPrimaryActionInput .ShouldNotBe(null);
            inputDevice.DefaultConfirmInput       .ShouldNotBe(null);
            inputDevice.DefaultJoinInput          .ShouldNotBe(null);
            inputDevice.DefaultPauseInput         .ShouldNotBe(null);
            inputDevice.DefaultBackInput.ShouldNotBe(null);
        }

        private void TestXboxGamePadInputDevice()
        {
            IInputDevice inputDevice = InputManager.Xbox360GamePads[0];


            inputDevice.Default2DInput.ShouldNotBe(null);
            inputDevice.DefaultHorizontalInput.ShouldNotBe(null);
            inputDevice.DefaultVerticalInput.ShouldNotBe(null);
            inputDevice.DefaultPrimaryActionInput.ShouldNotBe(null);
            inputDevice.DefaultConfirmInput.ShouldNotBe(null);
            inputDevice.DefaultJoinInput.ShouldNotBe(null);
            inputDevice.DefaultPauseInput.ShouldNotBe(null);
            inputDevice.DefaultBackInput.ShouldNotBe(null);
        }

        void CustomActivity(bool firstTimeCalled)
		{
            if(firstTimeCalled == false)
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
