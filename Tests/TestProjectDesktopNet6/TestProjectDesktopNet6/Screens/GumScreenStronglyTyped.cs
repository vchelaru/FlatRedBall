using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;

using GlueTestProject.Entities;


namespace GlueTestProject.Screens
{
    public partial class GumScreenStronglyTyped
    {
        private void CustomInitialize()
        {
            
        }

        int framesToWait = 60;
        bool hasAnimationChangedFramesBeforePause = false;
        bool hasPaused = false;
        bool hasAnimationChangedAfterPaused = false;


        private void CustomActivity(bool firstTimeCalled)
        {
            DoGumAnimatedSpriteTestActivity();

            if (this.ActivityCallCount >= framesToWait)
            {
                DoMoveToNextScreenLogic();
            }
        }

        private void DoGumAnimatedSpriteTestActivity()
        {
            if (GumScreen.AnimatedSpriteInstance.TextureLeft != 0)
            {
                if(!hasPaused)
                {
                    hasAnimationChangedFramesBeforePause = true;
                }
                else
                {
                    hasAnimationChangedAfterPaused = true;
                }
            }

            if(hasAnimationChangedFramesBeforePause && !hasPaused)
            {
                PauseThisScreen();
                UnpauseThisScreen();
                hasPaused = true;

            }
        }

        private void DoMoveToNextScreenLogic()
        {
            if(!hasAnimationChangedFramesBeforePause)
            {
                throw new Exception("The animated Gum sprite has not changed" +
                    "its LeftTeture, which means animations haven't played");
            }
            if(!hasAnimationChangedAfterPaused)
            {
                throw new Exception("The animated Gum sprite has not changed" +
                    "its LeftTeture after pausing, which means animations haven't played");
            }

            IsActivityFinished = true;
        }

        private void CustomDestroy()
        {
            
        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {
            
        }
    }
}
