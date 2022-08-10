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

            TextWithDefaultBitmapFont.BitmapFont.ShouldNotBe(null, "Becuause texts with default fonts should fall back to the default BitmapFont object");

            var buttonBitmapFont = this.TopButton.GetTextRuntime().BitmapFont;
            buttonBitmapFont.ShouldNotBe(null, "because Texts should have their BitmapFont assigned, but seem to not be");

            this.NineSliceInstance.InternalNineSlice.BottomLeftTexture.ShouldNotBe(this.NineSliceInstance.InternalNineSlice.CenterTexture,
                "because NineSliceInstance should use the texture pattern");

            // Make sure that categories run fine...
            EntireGumScreen.CurrentState = GumRuntimes.TestScreenRuntime.StateCategory1.On;

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

            TestWrappingChangingContainerHeight();

            TestAddChildSetsParent();

            TestHeightDependsOnChildrenWithCenteredChildren();

            TestTextWidth();

            TestStronglyTypedGenericContainers();

            TestResolutionChangeForcesLayout();

        }

        private void TestResolutionChangeForcesLayout()
        {
            var oldWidth = CameraSetup.Data.ResolutionWidth;
            var rectangle = new ColoredRectangleRuntime();
            rectangle.X = 0;
            rectangle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            rectangle.AddToManagers();
            rectangle.UpdateLayout();
            rectangle.AbsoluteX.ShouldBe(oldWidth);


            CameraSetup.Data.ResolutionWidth = 1234;
            CameraSetup.ResetCamera();
            rectangle.AbsoluteX.ShouldBe(1234);


            CameraSetup.Data.ResolutionWidth = oldWidth;
            CameraSetup.ResetCamera();

        }

        private void TestStronglyTypedGenericContainers()
        {
            var stronglyTypedContainer = EntireGumScreen.StronglyTypedContainerInstance;

            stronglyTypedContainer.ShouldNotBe(null, "because generic typed lists should be propery instantiated");

            var isRightGenericType = stronglyTypedContainer.GetType() == typeof(ContainerRuntime<NineSliceButtonRuntime>);

            isRightGenericType.ShouldBe(true, "because the type of the container is NineSliceButton");
        }

        private void TestHeightDependsOnChildrenWithCenteredChildren()
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
            var parentContainer = new ContainerRuntime();
            parentContainer.Height = 4;
            parentContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            var child = new ContainerRuntime();
            child.YOrigin = VerticalAlignment.Center;
            child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            child.Height = 10;
            child.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            child.Y = 0;

            parentContainer.Children.Add(child);
            GraphicalUiElement.IsAllLayoutSuspended = false;

            parentContainer.UpdateLayout();

            var parentAbsoluteHeight = parentContainer.GetAbsoluteHeight();
            
            parentAbsoluteHeight.ShouldBe(child.Height + parentContainer.Height, "because the parent container should be the height of its child plus the padding");

            var childAbsoluteY = child.GetAbsoluteY();

            childAbsoluteY.ShouldBe(2, "because this child should be centered");
        }

        private void TestTextWidth()
        {
            var text = new TextRuntime();
            text.AddToManagers();

            text.Width = 0;
            text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            text.Text = null;
            text.GetAbsoluteWidth().ShouldBe(0, "because this has no text and its width is based on its 'children' which means its contained text");

            text.Text = "Now this is some longer text";
            text.GetAbsoluteWidth().ShouldBeGreaterThan(1, "because setting the text should automatically make this wider");

            var widthFromText = text.GetAbsoluteWidth();

            text.Width = 10;

            var newTextWidth = text.GetAbsoluteWidth();
            newTextWidth.ShouldBe(widthFromText + 10, "because the text should be 10 units larger than its children, which is the measured width");

            text.Text = "";
            (text.RenderableComponent as Text).WrappedText.Count.ShouldBe(1);
            (text.RenderableComponent as Text).WrappedText[0].ShouldBe("");

            text.GetAbsoluteWidth().ShouldBe(10, "because this has 10 + length of text, but there is no text");

            text.Width = 9;
            text.Text = "This is some much longer text. It should not wrap because the width units is relative to children.";

            var widthAt9Plus = text.GetAbsoluteWidth();
            widthAt9Plus.ShouldBeGreaterThan(400, "because this is some really long text with a width units of RelativeToChildren");

            text.RemoveFromManagers();
        }

        private void TestAddChildSetsParent()
        {
            var first = new ContainerRuntime();
            var second = new ContainerRuntime();

            first.Children.Add(second);

            second.Parent.ShouldBe(first, "because adding to children should set the child's parent");

            first.Children.Remove(second);

            second.Parent.ShouldBe(null, "because removing the child should set the parent to null");
        }

        private void TestWrappingChangingContainerHeight()
        {
            ToResizeInCode.ShouldNotBe(null);

            ToResizeInCode.Width.ShouldBeGreaterThan(400, "because it is set to be a wider object in Gum");

            var oldHeight = ToResizeInCode.GetAbsoluteHeight() ;

            ToResizeInCode.Width = 40;

            ToResizeInCode.GetAbsoluteHeight().ShouldBeGreaterThan(oldHeight, "because resizing the width should make the contained Text word wrap, and this height is based on the contained object");
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
            if(this.ActivityCallCount == 4)
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
