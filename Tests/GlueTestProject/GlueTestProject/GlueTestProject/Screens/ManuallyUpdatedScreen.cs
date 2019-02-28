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
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using GlueTestProject.TestFramework;
using System.Runtime.CompilerServices;

namespace GlueTestProject.Screens
{
	public partial class ManuallyUpdatedScreen
	{
        Text manuallyUpdatedTextInCode;
        #region Initialize Methods

        void CustomInitialize()
		{
            CreateManuallyUpdatedText();

            SetDetachedManualUpdateVelocityVariables();

            SetDetachedManualUpdateAccelerationVariables();

            SetManualUpdateRelativeVelocityVariables();

            SetManualUpdateRelativeAccelerationVariables();

            SetAttachedVariables();

            SetManualUpdateSpriteInheritingVariables();

            SetManualUpdateTextInheritVariables();

            SetManuallyUpdatedInheritFromSpriteNoAttachInstanceVariables();

            SetEntityInListAttachedToObject();

            TestManuallyUpdatedEntityIneritingFromSprite();
        }

        private void SetDetachedManualUpdateVelocityVariables()
        {
            DetachedUpdateVelocityInCodeInstance.XVelocity = 100;
            DetachedUpdateVelocityInCodeInstance.YVelocity = 100;
            DetachedUpdateVelocityInCodeInstance.ZVelocity = 100;
            DetachedUpdateVelocityInCodeInstance.Drag = .1f;

            DetachedUpdateVelocityInCodeInstance.Call(item => item.X -= 1000).After(0);
        }

        private void SetDetachedManualUpdateAccelerationVariables()
        {
            DetachedUpdateAccelerationInCodeInstance.XAcceleration = -1;
            DetachedUpdateAccelerationInCodeInstance.YAcceleration = -2;
            DetachedUpdateAccelerationInCodeInstance.ZAcceleration = -3;
        }

        private void SetManualUpdateRelativeVelocityVariables()
        {
            UpdateRelativeVelocityInCodeInstance.RelativeXVelocity = 100;
            UpdateRelativeVelocityInCodeInstance.RelativeYVelocity = 100;
            UpdateRelativeVelocityInCodeInstance.RelativeZVelocity = 100;
        }

        private void SetManualUpdateRelativeAccelerationVariables()
        {
            UpdateRelativeAccelerationInCodeInstance.RelativeXAcceleration = -1;
            UpdateRelativeAccelerationInCodeInstance.RelativeYAcceleration = -2;
            UpdateRelativeAccelerationInCodeInstance.RelativeZAcceleration = -3;
        }

        private void SetAttachedVariables()
        {
            AttachedUpdatedInCodeInstance.AttachTo(ParentPositionedObject);
            ParentPositionedObject.X = 100;
            ParentPositionedObject.Y = 200;
        }

        private void CreateManuallyUpdatedText()
        {
            manuallyUpdatedTextInCode = new Text();
            manuallyUpdatedTextInCode.DisplayText = "Text from code";
            manuallyUpdatedTextInCode.SetPixelPerfectScale(Camera.Main);
            TextManager.AddManuallyUpdated(manuallyUpdatedTextInCode);
            manuallyUpdatedTextInCode.ForceUpdateDependencies();
        }

        private void SetManualUpdateSpriteInheritingVariables()
        {
            ManuallyUpdatedInheritFromSpriteInstance.ScaleXVelocity = 1;
            ManuallyUpdatedInheritFromSpriteInstance.ScaleYVelocity = 1;
            ManuallyUpdatedInheritFromSpriteInstance.ScaleX = 1;
            ManuallyUpdatedInheritFromSpriteInstance.ScaleY = 1;
            ManuallyUpdatedInheritFromSpriteInstance.TextureScale = 0;

            ManuallyUpdatedInheritFromSpriteInstance.Alpha = 0;
            ManuallyUpdatedInheritFromSpriteInstance.AlphaRate = 1;

            ManuallyUpdatedInheritFromSpriteInstance.Red = 0;
            ManuallyUpdatedInheritFromSpriteInstance.RedRate = 1;

            ManuallyUpdatedInheritFromSpriteInstance.Green = 0;
            ManuallyUpdatedInheritFromSpriteInstance.GreenRate = 1;

            ManuallyUpdatedInheritFromSpriteInstance.Blue = 0;
            ManuallyUpdatedInheritFromSpriteInstance.BlueRate = 1;
        }

        private void SetManualUpdateTextInheritVariables()
        {
            ManuallyUpdatedInheritFromTextInstance.Scale = 1;
            ManuallyUpdatedInheritFromTextInstance.ScaleVelocity = 1;

            ManuallyUpdatedInheritFromTextInstance.Spacing = 1;
            ManuallyUpdatedInheritFromTextInstance.SpacingVelocity = 1;

        }

        private void SetManualUpdateSpriteIsContainerVariables()
        {
            ManuallyUpdatedSpriteIsContainerInstance.TextureScale = 0;
            ManuallyUpdatedSpriteIsContainerInstance.ScaleX = 1;
            ManuallyUpdatedSpriteIsContainerInstance.ScaleY = 1;
            ManuallyUpdatedSpriteIsContainerInstance.ScaleXVelocity = 1 * 60;
            ManuallyUpdatedSpriteIsContainerInstance.ScaleYVelocity = 1 * 60;
        }

        private void SetManuallyUpdatedInheritFromSpriteNoAttachInstanceVariables()
        {
            ManuallyUpdatedInheritFromSpriteNoAttachInstance.ScaleXVelocity = 1;
            ManuallyUpdatedInheritFromSpriteNoAttachInstance.ScaleX = 1;
            ManuallyUpdatedInheritFromSpriteNoAttachInstance.TextureScale = 0;
        }

        const float inListRelativeXVelocity = 60;
        double timeAttachmentWasMade;

        private void SetEntityInListAttachedToObject()
        {
            PositionedObject parent = new PositionedObject();
            var entity = new Entities.ManuallyUpdateAllInCode();
            entity.AttachTo(parent);
            parent.X = 120;
            entity.RelativeXVelocity = inListRelativeXVelocity;
            timeAttachmentWasMade = TimeManager.CurrentTime;
            this.ManuallyUpdateAllInCodeList.Add(entity);

        }

        private void TestManuallyUpdatedEntityIneritingFromSprite()
        {
            var isDrawn = SpriteManager.OrderedSprites.Contains(ManuallyUpdatedIsSpriteInstance);
            isDrawn.ShouldBe(true, "because entities that are manually updated and inherit from Sprite should add themselves as drawn objects");
        }
        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            if (this.ActivityCallCount == 1)
            {
                TextManager.AutomaticallyUpdatedTexts.Contains(manuallyUpdatedTextInCode).ShouldBe(false);

                TextManager.AutomaticallyUpdatedTexts.Contains(ManuallyUpdatedText).ShouldBe(false);
                TextManager.AutomaticallyUpdatedTexts.Contains(LayeredManualText).ShouldBe(false);

                SpriteManager.AutomaticallyUpdatedSprites.Contains(ManuallyUpdatedSprite).ShouldBe(false);
                SpriteManager.OrderedSprites.Contains(ManuallyUpdatedSprite).ShouldBe(true);

                SpriteManager.AutomaticallyUpdatedSprites.Contains(LayeredManualSprite).ShouldBe(false);
                LayerInstance.Sprites.Contains(LayeredManualSprite).ShouldBe(true);

            }

            if (this.ActivityCallCount == 2)
            {
                SetManualUpdateSpriteIsContainerVariables();
            }
            if (this.ActivityCallCount == 3)
            {
                TextManualUpdateSpriteIsContainerVariables();
            }

            // Give us some time to do instructions and time-based checks
            if (this.ActivityCallCount == 8)
            {
                TestDetachedManualUpdateVelocityVariables();

                TestDetachedManualUpdateAccelerationVariables();

                TestAttachedManualUpdateVariables();

                TestManualUpdateRelativeVelocityVariables();

                TestManualUpdateRelativeAccelerationVariables();

                TestManualUpdateSpriteInheritingVariables();

                TestManualUpdateTextInheritingVariables();

                TestManuallyUpdatedInheritFromSpriteNoAttachInstanceVariables();


            }

            if (this.PauseAdjustedCurrentTime > .2f)
            {
                // UpdateDependencies is only called when there is a draw, 
                // and multiple draws may be skipped if fps is low enough
                // If this fails, increase numberOfFrames above
                TestEntityInListAttachedToObject();

                IsActivityFinished = true;
            }
        }
        
        private void TestAttachedManualUpdateVariables()
        {
            AttachedUpdatedInCodeInstance.X.ShouldBe(100, "because this entity is attached to a position object that has moved");
            AttachedUpdatedInCodeInstance.Y.ShouldBe(200, "because this entity is attached to a position object that has moved");
        }

        private void TestDetachedManualUpdateVelocityVariables()
        {
            DetachedUpdateVelocityInCodeInstance.X.ShouldNotBe(0);
            DetachedUpdateVelocityInCodeInstance.X.ShouldBeLessThan(0, "because the instruction should have shifted it to the left");
            DetachedUpdateVelocityInCodeInstance.X.ShouldBeGreaterThan(-1000, "because XVelocity should have moved the object to the right some");

            DetachedUpdateVelocityInCodeInstance.Y.ShouldBeGreaterThan(0);
            DetachedUpdateVelocityInCodeInstance.Z.ShouldBeGreaterThan(0);

            DetachedUpdateVelocityInCodeInstance.Drag.ShouldNotBe(0);

            DetachedUpdateVelocityInCodeInstance.XVelocity.ShouldBeGreaterThan(0, "because we assigned this value");
            DetachedUpdateVelocityInCodeInstance.XVelocity.ShouldBeLessThan(100, "because drag should have slowed this down");

            DetachedUpdateVelocityInCodeInstance.YVelocity.ShouldBeGreaterThan(0, "because we assigned this value");
            DetachedUpdateVelocityInCodeInstance.YVelocity.ShouldBeLessThan(100, "because drag should have slowed this down");

            DetachedUpdateVelocityInCodeInstance.ZVelocity.ShouldBeGreaterThan(0, "because we assigned this value");
            DetachedUpdateVelocityInCodeInstance.ZVelocity.ShouldBeLessThan(100, "because drag should have slowed this down");
        }

        private void TestManualUpdateRelativeVelocityVariables()
        {
            UpdateRelativeVelocityInCodeInstance.RelativeX.ShouldBeGreaterThan(0);
            UpdateRelativeVelocityInCodeInstance.RelativeY.ShouldBeGreaterThan(0);
            UpdateRelativeVelocityInCodeInstance.RelativeZ.ShouldBeGreaterThan(0);
        }

        private void TestManualUpdateRelativeAccelerationVariables()
        {
            UpdateRelativeAccelerationInCodeInstance.RelativeXVelocity.ShouldBeLessThan(0);
            UpdateRelativeAccelerationInCodeInstance.RelativeYVelocity.ShouldBeLessThan(0);
            UpdateRelativeAccelerationInCodeInstance.RelativeZVelocity.ShouldBeLessThan(0);
        }

        private void TestManualUpdateSpriteInheritingVariables()
        {
            ManuallyUpdatedInheritFromSpriteInstance.Alpha.ShouldBeGreaterThan(0);
            ManuallyUpdatedInheritFromSpriteInstance.Red.ShouldBeGreaterThan(0);
            ManuallyUpdatedInheritFromSpriteInstance.Green.ShouldBeGreaterThan(0);
            ManuallyUpdatedInheritFromSpriteInstance.Blue.ShouldBeGreaterThan(0);

            ManuallyUpdatedInheritFromSpriteInstance.ScaleX.ShouldBeGreaterThan(1);
            ManuallyUpdatedInheritFromSpriteInstance.ScaleY.ShouldBeGreaterThan(1);
        }

        private void TestDetachedManualUpdateAccelerationVariables()
        {
            DetachedUpdateAccelerationInCodeInstance.X.ShouldBeLessThan(0);
            DetachedUpdateAccelerationInCodeInstance.XVelocity.ShouldBeLessThan(0);


            DetachedUpdateAccelerationInCodeInstance.Y.ShouldBeLessThan(0);
            DetachedUpdateAccelerationInCodeInstance.YVelocity.ShouldBeLessThan(0);
            DetachedUpdateAccelerationInCodeInstance.YVelocity.ShouldBeLessThan(DetachedUpdateAccelerationInCodeInstance.XVelocity);


            DetachedUpdateAccelerationInCodeInstance.Z.ShouldBeLessThan(0);
            DetachedUpdateAccelerationInCodeInstance.ZVelocity.ShouldBeLessThan(0);
            DetachedUpdateAccelerationInCodeInstance.ZVelocity.ShouldBeLessThan(DetachedUpdateAccelerationInCodeInstance.YVelocity);
        }

        private void TestManualUpdateTextInheritingVariables()
        {
            ManuallyUpdatedInheritFromTextInstance.Scale.ShouldBeGreaterThan(1);
            ManuallyUpdatedInheritFromTextInstance.Spacing.ShouldBeGreaterThan(1);
        }

        private void TextManualUpdateSpriteIsContainerVariables()
        {
            ManuallyUpdatedSpriteIsContainerInstance.ScaleX.ShouldBeGreaterThan(1.5f, "because scale velocity should apply");
            ManuallyUpdatedSpriteIsContainerInstance.ScaleX.ShouldBeLessThan(2.5f, "because scale velocity should only apply once per frame and the velocity is set to not go much bigger than 2");


            ManuallyUpdatedSpriteIsContainerInstance.ScaleY.ShouldBeGreaterThan(1.5f, "because scale velocity should apply");
            ManuallyUpdatedSpriteIsContainerInstance.ScaleY.ShouldBeLessThan(2.5f, "because scale velocity should only apply once per frame and the velocity is set to not go much bigger than 2");
        }

        private void TestManuallyUpdatedInheritFromSpriteNoAttachInstanceVariables()
        {
            ManuallyUpdatedInheritFromSpriteNoAttachInstance.ScaleX.ShouldBeGreaterThan(1);

            ManuallyUpdatedInheritFromSpriteNoAttachInstance.VerticesForDrawing
                .ShouldNotBe(null, "because this entity has its ScaleXVelocity applied, so it should have manual update called too, which populates this array.");

            ManuallyUpdatedInheritFromSpriteNoAttachInstance.VerticesForDrawing.Length.ShouldBe(4);

            ManuallyUpdatedInheritFromSpriteNoAttachInstance
                .VerticesForDrawing[0].Position.X
                .ShouldBeLessThan(-1.0f);
        }


        private void TestEntityInListAttachedToObject()
        {
            var item = ManuallyUpdateAllInCodeList[0];

            item.Parent.ShouldNotBe(null);

            item.X.ShouldBeGreaterThan(item.Parent.X, "because RelativeXVelocity is applied, and the object is attached");

            var timePassed = TimeManager.SecondsSince(timeAttachmentWasMade);
            float desiredRelativeX = (float)(timePassed * inListRelativeXVelocity);

            item.RelativeX.ShouldBeGreaterThan(desiredRelativeX - .1f, "because RelativeXVelocity should be applied for items in lists.");
            item.RelativeX.ShouldBeLessThan(desiredRelativeX + .1f, "because RelativeXVelocity should be applied for items in lists.");
        }


        #endregion

        void CustomDestroy()
		{
            TextManager.RemoveText(manuallyUpdatedTextInCode);

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
