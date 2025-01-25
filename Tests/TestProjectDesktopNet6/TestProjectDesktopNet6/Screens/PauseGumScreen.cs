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
using GlueTestProject.TestFramework;
using System.Threading.Tasks;


namespace GlueTestProject.Screens;

enum PauseGumScreenState
{
    NotStarted,
    FirstAnimation,
    Paused,
    SecondAnimation
}
public partial class PauseGumScreen
{
    PauseGumScreenState PauseGumScreenState = PauseGumScreenState.NotStarted;

    private void CustomInitialize()
    {
        
    }

    double gameTimePaused;
    private void CustomActivity(bool firstTimeCalled)
    {
        if(PauseGumScreenState == PauseGumScreenState.NotStarted)
        {
            PauseGumScreenState = PauseGumScreenState.FirstAnimation;
            GumScreen.GreenAnimationAnimation.Play();
        }
        if(PauseGumScreenState == PauseGumScreenState.FirstAnimation && TimeManager.CurrentScreenTime > .3)
        {
            this.PauseThisScreen();
            PauseGumScreenState = PauseGumScreenState.Paused;
            GumScreen.GreenAnimationAnimation.IsPlaying().ShouldBe(false);
            gameTimePaused = TimeManager.CurrentTime;
        }
        if(PauseGumScreenState == PauseGumScreenState.Paused && TimeManager.CurrentTime - gameTimePaused > .25)
        {
            PlayAnimationAsync();
        }

    }

    private async void PlayAnimationAsync()
    {
        PauseGumScreenState = PauseGumScreenState.SecondAnimation;


        //await GumScreen.BlueAnimationAnimation.PlayAsync();
        var animationTask = GumScreen.BlueAnimationAnimation.PlayAsync();
        var timeoutTask = Task.Delay((int)(GumScreen.BlueAnimationAnimation.Length * 1000 + 1000));

        var completedTask = await Task.WhenAny(animationTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Awaiting a PlayAsync should finish even if a screen is paused. It is not...");
        }


        // wait until it finishes, then unpause:

        UnpauseThisScreen();

        IsActivityFinished = true;
    }

    private void CustomDestroy()
    {
        
    }

    private static void CustomLoadStaticContent(string contentManagerName)
    {
        
    }
}
