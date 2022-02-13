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
using GlueTestProject.TestFramework;

namespace GlueTestProject.Screens
{
    public partial class DerivedOfDerivedScreen
    {

        void CustomInitialize()
        {
            this.ContentManagerName.ShouldBe(nameof(DerivedOfDerivedScreen));

        }

        void CustomActivity(bool firstTimeCalled)
        {
            if(!firstTimeCalled)
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
