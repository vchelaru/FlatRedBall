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
    public partial class ReloadContentScreen
    {
        private void CustomInitialize()
        {
            ReloadableTexture1.IsDisposed.ShouldBe(false);
            ReloadableAnimation1[0][0].Texture.ShouldBe(ReloadableTexture1);

            var textureBeforeReload = ReloadableTexture1;

            GlobalContent.Reload(GlobalContent.ReloadableTexture1);
            textureBeforeReload.IsDisposed.ShouldBe(true);
            ReloadableAnimation1[0][0].Texture.ShouldNotBe(textureBeforeReload);

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
