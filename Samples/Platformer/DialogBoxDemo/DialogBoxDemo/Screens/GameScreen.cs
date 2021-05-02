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



namespace DialogBoxDemo.Screens
{
    public partial class GameScreen
    {

        void CustomInitialize()
        {
            LocalizationManager.CurrentLanguage = 1;
            Map.Z = -3; 
        }

        async void CustomActivity(bool firstTimeCalled)
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
