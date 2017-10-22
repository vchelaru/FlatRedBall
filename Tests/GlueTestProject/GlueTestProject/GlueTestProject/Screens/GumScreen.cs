#region Usings

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

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif
#endregion

namespace GlueTestProject.Screens
{
	public partial class GumScreen
	{

		void CustomInitialize()
		{
            if(this.TopButton.Width < 149 || this.TopButton.Width > 151)
            {
                throw new Exception("Width values from Gum fles are not being assigned.  Expected width: " + 150 + " but got width " + TopButton.Width);
            }

            this.TopButton.GetTextRuntime().GetBitmapFont().ShouldNotBe(null, "because Texts should having their BitmapFont assigned, but seem to not be");

            this.NineSliceInstance.InternalNineSlice.BottomLeftTexture.ShouldNotBe(this.NineSliceInstance.InternalNineSlice.CenterTexture,
                "because NineSliceInstance should use the texture pattern");

            // Make sure that categories run fine...
            EntireGumScreen.CurrentStateCategory1State = GumRuntimes.TestScreenRuntime.StateCategory1.On;

            this.StateComponentInstance.CurrentVariableState.ShouldBe(GumRuntimes.StateComponentRuntime.VariableState.NonDefaultState, 
                "because setting a state on an instance in a screen should result in the state being set on the runtime.");

            var outlineBitmapFont = (OutlineTextInstance.RenderableComponent as RenderingLibrary.Graphics.Text).BitmapFont;
            outlineBitmapFont.ShouldNotBe(null, "because outlined text objects should have their fonts set.");

            this.TestRectangleInstance.Visible = false;
            bool isAbsoluteVisible = 
                (this.TestRectangleInstance.Sprite as RenderingLibrary.Graphics.IVisible).AbsoluteVisible;

            isAbsoluteVisible.ShouldNotBe(true, "because making a component invisible should make its contained objects that are attached to containers also invisible.");

            // The following test all variable assignment to make sure it comes over okay:
            ColoredRectSetsEverything.X.ShouldBe(64);
            ColoredRectSetsEverything.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
            ColoredRectSetsEverything.Y.ShouldBe(96);
            ColoredRectSetsEverything.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
            ColoredRectSetsEverything.XOrigin.ShouldBe(RenderingLibrary.Graphics.HorizontalAlignment.Center);
            ColoredRectSetsEverything.YOrigin.ShouldBe(RenderingLibrary.Graphics.VerticalAlignment.Center);
            ColoredRectSetsEverything.Width.ShouldBe(60);
            ColoredRectSetsEverything.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToContainer);
            ColoredRectSetsEverything.Height.ShouldBe(256);
            ColoredRectSetsEverything.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.PercentageOfOtherDimension);
            ColoredRectSetsEverything.Parent.Name.ShouldBe("NameWith-Dash");
            ColoredRectSetsEverything.Visible.ShouldBe(true);
            ColoredRectSetsEverything.Rotation.ShouldBe(32);
            ColoredRectSetsEverything.Alpha.ShouldBe(200);
            ColoredRectSetsEverything.Red.ShouldBe(0);
            ColoredRectSetsEverything.Green.ShouldBe(0);
            ColoredRectSetsEverything.Blue.ShouldBe(139);
            ColoredRectSetsEverything.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);

            ExternalFontText.BitmapFont.ShouldNotBe(null, "because custom fonts referenced outside of the Gum project should be found and loaded correctly");

        }

		void CustomActivity(bool firstTimeCalled)
		{

            if (this.ActivityCallCount > 5)
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
