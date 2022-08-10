using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using FlatRedBall.Instructions;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
using FlatRedBall.ManagedSpriteGroups;
#endif

namespace GlueTestProject.Screens
{
	public partial class FlatRedBallTypeScreen
    {
        #region Fields

        bool mHasCheckedX = false;
        bool mHasCheckedPosition = false;
        bool mHasCheckedTextInterpolation = false;

        SpriteFrame layeredSpriteFrameInstantiatedInCode;
        SpriteFrame unlayeredSpriteFrameInstantiatedInCode;

        Sprite managedInvisiblTestSprite1;

        #endregion

        void CustomInitialize()
		{

            if (!SpriteManager.OrderedSprites.Contains(this.SpriteObject))
            {
                throw new Exception("The Sprite object is not being added to the managers but it should be.");
            }

            if (ShapeCollectionFile.AxisAlignedRectangles.Contains(this.InvisibleRectangle) == false)
            {
                throw new Exception("The ShapeCollection does not contain the rectangle - possibly because it's being improperly cloned");
            }

            if (this.InvisibleRectangle.Visible)
            {
                throw new Exception("Rectangles that come from files that have their Visible set to false are still visible");
            }

            this.SceneInstanceSetFromFileAtRuntime = SceneFile;

            SpriteWithInstructions.Set("X").To(4.0f).After(.1f);
            SpriteWithInstructions.Set("Position").To(Vector3.One).After(.25);

            TestingTextInterpolationInstance.CurrentTextValuesState = InterpolationEntity.TextValues.Transparent;
            TestingTextInterpolationInstance.InterpolateToState(InterpolationEntity.TextValues.Opaque, 1);

            this.DynamicallyAssignedSceneSourceFile = SceneOption1;
            if(this.DynamicallyAssignedSceneSourceFile != SceneOption1)
            {
                throw new Exception("Setting the source file does not do anything");
            }

            // The CameraModifyingEntity sets the Z.  This should persist
            if (SpriteManager.Camera.Z != CameraModifyingEntity.CameraZToSet)
            {
                throw new Exception("The CameraModifyingEntity should modify the Camera's Z, but it's not!");
            }

            ManuallyUpdatedEntityInstance.ConvertToManuallyUpdated();

            // let's make sure that this thing actually has a collision function:
            bool didCollide = CollisionEntityInstance.CollideAgainst(CollisionEntityInstance2);
            // And that it worked
            if (!didCollide)
            {
                throw new Exception("ICollidable entities aren't properly detecting collisions");
            }

            CollisionEntityInstance.CollideAgainstMove(CollisionEntityInstance2, 1, 0);
            CollisionEntityInstance.CollideAgainstBounce(CollisionEntityInstance2, 1, 0, 1);

            
            if (this.LayerInstance.Sprites.Contains(SpriteFrameInheritingEntityInstanceLayered.CenterSprite) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities do not get put on Layers properly");
            }
            if (this.LayerInstance.Sprites.Contains(this.SpriteInheritingEntityInstanceLayered) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities do not get put on Layers properly");
            }
            if (this.LayerInstance.Texts.Contains(TextInheritingEntityInstanceLayered) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities do not get put on Layers properly");
            }

            SpriteFrameInheritingEntityInstanceLayered.MoveToLayer(LayerInstance2);
            SpriteInheritingEntityInstanceLayered.MoveToLayer(LayerInstance2);
            TextInheritingEntityInstanceLayered.MoveToLayer(LayerInstance2);
            // Make sure that entities with objects that are not instantiated an be moved to layers:
            NoInstantiationForMoveToLayer.MoveToLayer(LayerInstance2);

            if (this.LayerInstance2.Sprites.Contains(SpriteFrameInheritingEntityInstanceLayered.CenterSprite) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities aren't moved to other layers properly");
            }
            if (this.LayerInstance2.Sprites.Contains(this.SpriteInheritingEntityInstanceLayered) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities aren't moved to other layers properly");
            }
            if (this.LayerInstance2.Texts.Contains(TextInheritingEntityInstanceLayered) == false)
            {
                throw new Exception("SpriteFrame-inheriting entities aren't moved to other layers properly");
            }

            SetToInvisibleTestTextVisibility.Visible = false;
            if (SetToInvisibleTestTextVisibility.TextInstance.Visible)
            {
                throw new Exception("Setting an Entity that inherits from a FRB type to Invisible should set its IsContainer NOS to invisible too");
            }

            SpriteManager.AddToLayer(UnlayeredSpriteFrameNotAllSidesMoveToLayer, this.LayerInstance);

            // Now let's test SpriteFrames that we instantiate and add to layer right away
            layeredSpriteFrameInstantiatedInCode = new SpriteFrame(Aura, SpriteFrame.BorderSides.Left);
            SpriteManager.AddToLayer(layeredSpriteFrameInstantiatedInCode, LayerInstance);

            unlayeredSpriteFrameInstantiatedInCode = new SpriteFrame(Aura, SpriteFrame.BorderSides.Left);
            SpriteManager.AddToLayer(unlayeredSpriteFrameInstantiatedInCode, null);

            // Make sure AARects have the "all" reposition direction initially:
            var rectangle = new AxisAlignedRectangle();
            if(rectangle.RepositionDirections != RepositionDirections.All)
            {
                throw new Exception("AARects should have the All reposition direction, but they don't.");
            }


            managedInvisiblTestSprite1 = new Sprite();
            managedInvisiblTestSprite1.Name = nameof(managedInvisiblTestSprite1) + nameof(FlatRedBallTypeScreen);
            // January 25, 2015
            // A FRB user found a
            // crash bug occurring
            // if adding a Sprite as
            // a ManagedInvisibleSprite,
            // then later adding it to a Layer.
            // I was able to reproduce it in the
            // test project. Before the bug was fixed
            // the engine would crash internally.
            SpriteManager.AddManagedInvisibleSprite(managedInvisiblTestSprite1);
            SpriteManager.AddToLayer(managedInvisiblTestSprite1, this.LayerInstance);

            // The IDrawableBatchEntity depends on draw calls happening, and we
            // want to make sure that they don't get skipped on platforms like iOS
            // where performance may be lower than PC.  We're going to force draw calls
            // to happen after every activity.
            FlatRedBallServices.Game.IsFixedTimeStep = false;



		}

		void CustomActivity(bool firstTimeCalled)
		{
            // We need this screen to survive a while to make sure the emitter is emitting properly
            //if (!firstTimeCalled)
            //{
            //    IsActivityFinished = true;
            //}

            if(!firstTimeCalled)
            {


                if (!mHasCheckedX && this.PauseAdjustedSecondsSince(0) > .21f)
                {
                    if (SpriteWithInstructions.X != 4.0f)
                    {
                        throw new Exception("Property instructions are not working");
                    }
                    mHasCheckedX = true;
                }

                if (!mHasCheckedPosition && this.PauseAdjustedSecondsSince(0) > .51f)
                {
                    if (SpriteWithInstructions.Position != Vector3.One)
                    {
                        throw new Exception("Field instructions are not working");
                    }
                    mHasCheckedPosition = true;
                }

                if (!mHasCheckedTextInterpolation && this.PauseAdjustedSecondsSince(0) > .5f)
                {
                    if (TestingTextInterpolationInstance.TextInstanceX == 0)
                    {
                        throw new Exception("Text position interpolation over time doesn't work. This should be greater than 1 because InterpolateToState is called on this in CustomInitialize.");
                    }

                    if (TestingTextInterpolationInstance.TextInstanceAlpha == 0)
                    {
                        throw new Exception("Text alpha interpolation over time doesn't work");
                    }

                    mHasCheckedTextInterpolation = true;

                }

                const float secondsToLast = .66f;

                if (this.PauseAdjustedSecondsSince(0) > secondsToLast && this.IDrawableBatchEntityInstance.HasFinishedTests)
                {
                    IsActivityFinished = true;
                }
            }

		}

		void CustomDestroy()
		{
            SpriteManager.RemoveSpriteFrame(layeredSpriteFrameInstantiatedInCode);
            SpriteManager.RemoveSpriteFrame(unlayeredSpriteFrameInstantiatedInCode);

            SpriteManager.RemoveSprite(managedInvisiblTestSprite1);

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
