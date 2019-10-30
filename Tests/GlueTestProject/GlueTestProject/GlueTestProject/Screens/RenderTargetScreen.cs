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



namespace GlueTestProject.Screens
{
    public partial class RenderTargetScreen
    {

        void CustomInitialize()
        {


        }

        void CustomActivity(bool firstTimeCalled)
        {

            if(this.HasDrawBeenCalled)
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
