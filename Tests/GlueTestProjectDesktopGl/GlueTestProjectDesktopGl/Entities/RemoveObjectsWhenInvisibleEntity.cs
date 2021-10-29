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
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Graphics;

#endif
#endregion

namespace GlueTestProject.Entities
{
	public partial class RemoveObjectsWhenInvisibleEntity
	{
		private void CustomInitialize()
		{
            if (!SpriteManager.OrderedSprites.Contains(this.StartingVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects aren't being removed despite being invisible.");
            }
            StartingVisibleVisible = false;
            if (SpriteManager.OrderedSprites.Contains(this.StartingVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects are not removed when set to invisible.");
            }
            // Now let's test double-add
            StartingVisibleVisible = true;
            StartingVisibleVisible = true;
            if (!SpriteManager.OrderedSprites.Contains(this.StartingVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects aren't being removed despite being invisible.");
            }

            // ... and double-remove
            StartingVisibleVisible = false;
            StartingVisibleVisible = false;
            if (SpriteManager.OrderedSprites.Contains(this.StartingVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects are not removed when set to invisible.");
            }
            

            //-------------------------------------------------------------------------------------------

            if (SpriteManager.OrderedSprites.Contains(this.StartingInvisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects aren't working when defaulting to invisible");
            }

            //-------------------------------------------------------------------------------------------

            if (SpriteManager.OrderedSprites.Contains(this.StartingInvisibleMadeVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects aren't being removed despite being invisible");
            }

            StartingInvisibleMadeVisible.Blue = 0f;

            StartingInvisibleMadeVisibleVisible = true;
            // Make sure that setting visible to true doesn't change any variables, only 
            if (StartingInvisibleMadeVisible.Blue != 0f)
            {
                throw new Exception("Setting visible to true on a RemoveFromEngineWhenInvisible sets properties and it shouldn't");
            }

            if (!SpriteManager.OrderedSprites.Contains(this.StartingInvisibleMadeVisible))
            {
                throw new Exception("RemoveFromEngineWhenInvisible objects being added to the engine when made visible");
            }


            //-------------------------------------------------------------------------------------------

            if (this.SetInvisibleOnlyByTunneledVariable.Visible)
            {
                throw new Exception("Items removed from managers when invisible are not set to invisible if starting invisible by tunneled variable");
            }

            //-------------------------------------------------------------------------------------------

            if (TextManager.AutomaticallyUpdatedTexts.Contains(this.TextInstanceStartingInvisible))
            {
                throw new Exception("Text objects that start invisible and are set to RemoveFromEngineWhenInvisible are being added, but shouldn't be");
            }

            //-------------------------------------------------------------------------------------------
            this.TextInstanceSetInvisibleInCustomCodeVisible = false;
            if (TextManager.AutomaticallyUpdatedTexts.Contains(this.TextInstanceSetInvisibleInCustomCode))
            {
                throw new Exception("Text objects that are set to invisible in custom code and are set to RemoveFromEngineWhenInvisible arent't removed from the engine.");
            }

            //-------------------------------------------------------------------------------------------

            this.TextInheritingEntityInstanceVisible = false;
            if (TextInheritingEntityInstance.Parent != this)
            {
                throw new Exception("Making a RemoveFromEngineWhenInvisible entity instance detaches it, and it shouldn't");
            }
            const string changedText = "Changed";
            this.TextInheritingEntityInstance.DisplayText = changedText;
            const float alphaToSet = .5f;
            this.TextInheritingEntityInstance.ThisAsTextAlpha = alphaToSet;
            this.TextInheritingEntityInstanceVisible = true;
            if (this.TextInheritingEntityInstance.DisplayText != "Changed")
            {
                throw new Exception("Setting visible on/off seems to re-initialize objects which are RemoveFromEngineWhenInvisible");
            }
            if (this.TextInheritingEntityInstance.ThisAsTextAlpha != alphaToSet)
            {
                throw new Exception("Setting visible on/off seems to re-initialize CustomVariables which are on RemoveFromEngineWhenInvisible");
            }
            if (!TextManager.AutomaticallyUpdatedTexts.Contains(TextInheritingEntityInstance))
            {
                throw new Exception(
                    "Entity instances which inherit from FRB types and are removed when invisible are not re-added to the engine when made visible");

            }

            //-------------------------------------------------------------------------------------------
            if (SetVisibleInCustomCode.Red != .5f)
            {
                throw new Exception("Invisible objects which are removed when invisible do not get their custom variables set initially");
            }

            //-------------------------------------------------------------------------------------------

            if (this.TextInheritingEntityInstanceStartingInvisible.CircleContainerSetCircleInCustomVariablesInstance.CircleInstanceRadius != 200)
            {
                throw new Exception("Entity instances contained in entities which are not initially added to managers because of visibility do not get thier custom variables set in generated code");
            }
            //-------------------------------------------------------------------------------------------

            if (this.TextInheritingEntityInstanceStartingInvisibleCustomRadius.CircleContainerSetCircleInCustomVariablesInstance.CircleInstanceRadius != 250)
            {
                throw new Exception("Custom variables set on instances that also set their own variables internally are not overriding the base values.");
            }
            //-------------------------------------------------------------------------------------------
            
            // Make sure attachments are preserved:
            InstanceContainingOtherEntityInstanceVisible = false;
            if (InstanceContainingOtherEntityInstance.CircleContainerSetCircleInCustomVariablesInstance.Parent == null)
            {
                throw new Exception(
                    "Making entity instances invisible when they're removed when invisible detaches their children entity instances and it shouldn't");
            }

		}

		private void CustomActivity()
		{


		}

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
