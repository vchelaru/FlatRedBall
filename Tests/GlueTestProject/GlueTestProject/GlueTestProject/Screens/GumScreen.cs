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
using Gum.Wireframe;
using GlueTestProject.GumRuntimes;
using System.Linq;
using RenderingLibrary;
using RenderingLibrary.Graphics;

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
            if (this.TopButton.Width < 149 || this.TopButton.Width > 151)
            {
                throw new Exception("Width values from Gum fles are not being assigned.  Expected width: " + 150 + " but got width " + TopButton.Width);
            }

            var buttonBitmapFont = this.TopButton.GetTextRuntime().GetBitmapFont();
            buttonBitmapFont.ShouldNotBe(null, "because Texts should have their BitmapFont assigned, but seem to not be");

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

            TestColoredRectangleSettingAllValues();

            ExternalFontText.BitmapFont.ShouldNotBe(null, "because custom fonts referenced outside of the Gum project should be found and loaded correctly");

            ComponentWithCustomInitializeInstance.Width.ShouldBe(20, "because CustomInitialize should be called after default state is set.");

            PerformTopToBottomStackTest();

            PerformDynamicParentAssignmentTest();

            PerformDependsOnChildrenTest();

            // Move this to the right so it isn't at 0,0
            GumComponentContainer_ForAttachment.X = 100;

            TestRotation();

            TrailingSpacesTextInstance.GetAbsoluteWidth().ShouldBeGreaterThan(200,
                "because trailing spaces should not be removed, and should widen text objects that are sized by their children.");

        }

        private void TestRotation()
        {
            // any non-zero value
            GumRuntimeEntityInstanceForRotation.RotationZ = 1;
            GumRuntimeEntityInstanceForRotation.X = 200;
            GumRuntimeEntityInstanceForRotation.Y = 200;

        }

        private void PerformDependsOnChildrenTest()
        {
            // This control has children which have widths that depend on the parent, so they have dependent units on the X axis, not Y axis. The stack panel should still
            // auto-size itself based on the heights of the children.
            var stackPanel = TestScreen.GetGraphicalUiElementByName("StackWithChildrenWidthDependsOnParent");

            stackPanel.GetAbsoluteHeight().ShouldNotBe(0, "because the height of the container should be set even though the the width of the contained nine slice depends on its parent.");
        }

        private void TestColoredRectangleSettingAllValues()
        {
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
        }

        private void PerformTopToBottomStackTest()
        {
            var stackingContainer = TestScreen.GetGraphicalUiElementByName("TopToBottomStackTest");

            var firstChild = stackingContainer.Children[0] as GraphicalUiElement;
            var secondChild = stackingContainer.Children[1] as GraphicalUiElement;
            // first make sure the children are stacked correctly:
            var firstAbsolute = firstChild.AbsoluteY;
            var secondAbsolute = secondChild.AbsoluteY;

            secondAbsolute.ShouldBe(firstAbsolute + firstChild.GetAbsoluteHeight());

            // now shift the first child down
            firstChild.Y += 64;

            firstAbsolute = firstChild.AbsoluteY;
            secondAbsolute = secondChild.AbsoluteY;

            secondAbsolute.ShouldBe(firstAbsolute + firstChild.GetAbsoluteHeight());

            var lastChildBeforeDynamicallyAdded = stackingContainer.Children.Last() as GraphicalUiElement;

            // Add objects dynamically and see if they stack properly too
            var newNineSlice = new NineSliceRuntime();
            newNineSlice.Parent = stackingContainer;

            newNineSlice.AbsoluteY.ShouldBeGreaterThan(lastChildBeforeDynamicallyAdded.AbsoluteY, "because newly-added children of a stack panel should be positioned at the end of the stack");

            float lastAbsoluteYBeforeMove = newNineSlice.AbsoluteY;
            firstChild.Y += 64;
            newNineSlice.AbsoluteY.ShouldBeGreaterThan(lastAbsoluteYBeforeMove, "because moving the first child in a stack should also move dynamically-added children of the stack");
        }

        private void PerformDynamicParentAssignmentTest()
        {
            var circle = new GumRuntimes.CircleRuntime();
            circle.Parent = TestScreen.GetGraphicalUiElementByName("ContainerInstance") as GraphicalUiElement;

        }


        void CustomActivity(bool firstTimeCalled)
		{
            int numberOfFramesToWait = 5;

            // give it a few frames...
            if(this.ActivityCallCount == 2)
            {
                GumComponentContainer_ForAttachment.VerifyGumOnFrbAttachments();

                var textInstance = this.GumRuntimeEntityInstanceForRotation.TextRuntimeInstance.RenderableComponent
                    as IRenderableIpso;

                var rotation = textInstance.GetAbsoluteRotation();
                rotation.ShouldBeGreaterThan(0);

            }


            if (this.ActivityCallCount > numberOfFramesToWait)
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
