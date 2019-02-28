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
	public partial class AnimationChainScreen
	{
        AnimationController animationController;
        AnimationLayer fastAnimation;

		void CustomInitialize()
		{
            animationController = new AnimationController(this.SpriteInstance);

            var slow = new AnimationLayer();
            slow.EveryFrameAction = () => "LongAnimation";
            animationController.Layers.Add(slow);

            fastAnimation = new AnimationLayer();
            animationController.Layers.Add(fastAnimation);
		}

        double? timePlayDurationStarted;
        double? timePlayLoopStarted;

		void CustomActivity(bool firstTimeCalled)
		{
            animationController.Activity();

            const double timeToPlay = .1f;

            if(ActivityCallCount == 1)
            {
                SpriteInstance.CurrentChainName.ShouldBe("LongAnimation",
                    "because the fast animation isn't yet playing, so the controller should " +
                    "always fall back on the slow one.");

                fastAnimation.PlayDuration("ShortAnimation", timeToPlay);

                timePlayDurationStarted = PauseAdjustedCurrentTime;
            }
            if(ActivityCallCount == 2)
            {
                SpriteInstance.CurrentChainName.ShouldBe("ShortAnimation",
                    "because this layer is now playing");
            }
            if(ActivityCallCount == 3)
            {
                SpriteInstance.CurrentChainName.ShouldBe("ShortAnimation",
                "because this layer should still be playing");
            }

            if(timePlayDurationStarted != null && 
                PauseAdjustedSecondsSince(timePlayDurationStarted.Value)  > timeToPlay)
            {
                IsActivityFinished = true;

                SpriteInstance.CurrentChainName.ShouldBe("LongAnimation",
                    "becasue the short animation should be done playing by now");

                timePlayLoopStarted = PauseAdjustedCurrentTime;
                fastAnimation.PlayLoop("ShortAnimation", 1);
            }

            if(timePlayLoopStarted != null && 
                PauseAdjustedSecondsSince(timePlayLoopStarted.Value) > AnimationChainListFile["ShortAnimation"].TotalLength )
            {
                SpriteInstance.CurrentChainName.ShouldBe("LongAnimation",
                    "because the short animation should have looped by now");
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
