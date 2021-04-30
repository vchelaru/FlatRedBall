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
using MultiplayerPlatformerDemo.ViewModels;
using Microsoft.Xna.Framework;

namespace MultiplayerPlatformerDemo.Screens
{
    public partial class CharacterJoiningScreen
    {
        CharacterJoiningScreenViewModel ViewModel => CharacterJoiningScreenGum.BindingContext as
            CharacterJoiningScreenViewModel;

        void CustomInitialize()
        {
            var gum = CharacterJoiningScreenGum;

            var vm = new CharacterJoiningScreenViewModel();

            for(int i = 0; i < vm.IndividualJoinViewModels.Length; i++)
            {
                if(InputManager.Xbox360GamePads[i].IsConnected)
                {
                    if(GameScreen.PlayerJoinStates[i] == GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.Joined)
                    {
                        vm.IndividualJoinViewModels[i].JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.Joined;
                    }
                    else
                    {
                        vm.IndividualJoinViewModels[i].JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.PluggedInNotJoined;
                    }
                }
            }

            gum.BindingContext = vm;

            gum.IndividualJoinComponentInstance.BindingContext = vm.IndividualJoinViewModels[0];
            gum.IndividualJoinComponentInstance1.BindingContext = vm.IndividualJoinViewModels[1];
            gum.IndividualJoinComponentInstance2.BindingContext = vm.IndividualJoinViewModels[2];
            gum.IndividualJoinComponentInstance3.BindingContext = vm.IndividualJoinViewModels[3];


            InputManager.ControllerConnectionEvent += HandleControllerConnectionEvent;
        }

        private void HandleControllerConnectionEvent(object sender, InputManager.ControllerConnectionEventArgs e)
        {
            if(e.Connected)
            {
                if(ViewModel.IndividualJoinViewModels[e.PlayerIndex].JoinState == GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.NotPluggedIn)
                {
                    ViewModel.IndividualJoinViewModels[e.PlayerIndex].JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.PluggedInNotJoined;
                }
            }
            else // disconnected
            {
                ViewModel.IndividualJoinViewModels[e.PlayerIndex].JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.NotPluggedIn;
            }

        }

        void CustomActivity(bool firstTimeCalled)
        {
            var gamepads = InputManager.Xbox360GamePads;

            for(int i = 0; i < gamepads.Length; i++)
            {
                var gamePad = gamepads[i];
                var viewModel = ViewModel.IndividualJoinViewModels[i];
                if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    if(viewModel.JoinState == GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.PluggedInNotJoined)
                    {
                        viewModel.JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.Joined;
                    }
                }
                if(gamePad.ButtonPushed(Xbox360GamePad.Button.B))
                {
                    if (viewModel.JoinState == GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.Joined)
                    {
                        viewModel.JoinState = GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.PluggedInNotJoined;
                    }
                }
                if(gamePad.ButtonPushed(Xbox360GamePad.Button.Start))
                {
                    if(viewModel.JoinState == GumRuntimes.IndividualJoinComponentRuntime.JoinCategory.Joined)
                    {
                        StartLevel();
                    }
                }
            }

        }

        private void StartLevel()
        {
            for(int i = 0; i < ViewModel.IndividualJoinViewModels.Length; i++)
            {
                GameScreen.PlayerJoinStates[i] = ViewModel.IndividualJoinViewModels[i].JoinState;
            }

            MoveToScreen(typeof(Level1));
        }

        void CustomDestroy()
        {
            InputManager.ControllerConnectionEvent -= HandleControllerConnectionEvent;
        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
