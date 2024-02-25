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
using FlatRedBall.Forms.Managers;




namespace FormsSampleProject.Screens
{
    public partial class InputJoinScreen
    {

        void CustomInitialize()
        {

            FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.Games.InputDeviceSelectionItem)] = 
                typeof(FormsSampleProject.GumRuntimes.Controls.InputDeviceSelectionItemRuntime);


            var inputDeviceSelector = new FlatRedBall.Forms.Controls.Games.InputDeviceSelector(
                GumScreen.InputDeviceSelectorInstance);

            inputDeviceSelector.MaxPlayers = 3;

            FrameworkElementManager.Self.AddFrameworkElement(inputDeviceSelector);
        }

        void CustomActivity(bool firstTimeCalled)
        {


        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
