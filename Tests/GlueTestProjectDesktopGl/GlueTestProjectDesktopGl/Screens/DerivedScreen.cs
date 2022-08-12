using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using GlueTestProject.TestFramework;
using RenderingLibrary.Graphics;

#if FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace GlueTestProject.Screens
{
	public partial class DerivedScreen
	{

		void CustomInitialize()
		{
            base.LayerInstanceGum.Renderables.Contains(ToBeLayered.RenderableComponent as IRenderableIpso).ShouldBe(true);

            // This class is the base class for DerivedOfDerived. In that case we don't want to run this test
            if(this.GetType() == typeof(DerivedScreen))
            {
                GlueTestProject.GumRuntimes.BaseGlueScreenNotBaseGumRuntime.NumberOfTimesCustomInitializeCalled
                    .ShouldBe(2, "because the base screen comes before this, and that increments this value once");
            }

            // access the song to force a load:
            var songTemp = BaseScreenSongOnlyWhenReferenced;

        }

		void CustomActivity(bool firstTimeCalled)
		{
            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
            }

        }

		void CustomDestroy()
		{
            // at this point all content should be unloaded. Therefore, accessing the song should return a non-disposed song:
            var song = BaseScreenSongOnlyWhenReferenced;
            song.IsDisposed.ShouldBe(false);

            //Since that reloaded, unload ^^
            FlatRedBallServices.Unload("BaseScreen");

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
