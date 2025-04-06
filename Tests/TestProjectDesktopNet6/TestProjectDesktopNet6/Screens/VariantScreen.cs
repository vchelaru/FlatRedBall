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


namespace GlueTestProject.Screens
{
    public partial class VariantScreen
    {
        private void CustomInitialize()
        {
            // This gluj version still uses the "Type" suffix.
            // If this project is upgraded, then this must be refactored:
            VariantEntityBaseType.VariantEntityBase.LoadStaticContent(ContentManagerName);
            VariantEntityBaseType.VariantEntityDerived.LoadStaticContent(ContentManagerName);

            VariantEntityBaseType.VariantEntityBase.GetFile("FileInBase").ShouldNotBe(null);
            VariantEntityBaseType.VariantEntityDerived.GetFile("FileInDerived").ShouldNotBe(null);
            VariantEntityBaseType.VariantEntityDerived.GetFile("FileInBase").ShouldNotBe(null);
        }

        private void CustomActivity(bool firstTimeCalled)
        {
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
