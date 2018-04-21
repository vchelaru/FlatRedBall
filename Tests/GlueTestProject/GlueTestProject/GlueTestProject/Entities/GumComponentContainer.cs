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
using FlatRedBall.Math;
using GlueTestProject.TestFramework;
using System.Linq;
using RenderingLibrary;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace GlueTestProject.Entities
{
	public partial class GumComponentContainer
	{
		private void CustomInitialize()
		{
            if (GuiManager.Windows.Contains(EntireButton) == false)
            {
                var message =
                    "Components in Entities should be added to GuiManager. See the EntireButton code in this entity's generated code for hints as to what is going on.";

                throw new Exception(message);
            }

            if (GuiManager.Windows.Contains(ButtonBackground))
            {
                throw new Exception("Only components should be clickable and added to the GuiManager");
            }

            var textObject = EntireButton.GetTextRuntime();

            var wrapper = this.gumAttachmentWrappers.First(item =>
                item.GumObject == GumButtonOnFrbSprite);
            GumButtonOnFrbSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumButtonOnFrbSprite.Width = 0;

            wrapper.FrbObject = this.SpriteWidth100;

            TestAttachToContainerFalse();

            SetupCenteredOnSpriteGumInstance();
		}

        private void SetupCenteredOnSpriteGumInstance()
        {
            GumButtonCenteredOnSprite.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            GumButtonCenteredOnSprite.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            GumButtonCenteredOnSprite.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            GumButtonCenteredOnSprite.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            GumButtonCenteredOnSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumButtonCenteredOnSprite.Width = 0;

            var wrapper = this.gumAttachmentWrappers.First(item =>
                item.GumObject == GumButtonCenteredOnSprite);

            wrapper.FrbObject = this.SpriteWidth100;
        }

        private void TestAttachToContainerFalse()
        {
            var foundWrapper = this.gumAttachmentWrappers.FirstOrDefault(item =>
                item.GumObject == GumButtonNotAttachedToEntity);

            foundWrapper.ShouldBe(null, "because this Gum object has its AttachToContainer set to false, so it shouldn't create a GumAttachmentWrapper");
        }

        public void VerifyGumOnFrbAttachments()
        {
            int screenX = 0;
            int screenY = 0;

            MathFunctions.AbsoluteToWindow(X, Y, Z, ref screenX, ref screenY, FlatRedBall.Camera.Main);

            EntireButton.Parent.ShouldNotBe(null, "because this should be attached to a wraper GUE");
            EntireButton.Parent.X.ShouldBe(screenX, "because the EntireButton is attached to this, and should move with this");
            EntireButton.Parent.Y.ShouldBe(screenY, "because the EntireButton is attached to this, and should move with this");

            GumButtonOnFrbSprite.GetAbsoluteWidth().ShouldBe(100, "because this Gum object gets its width from its parent FRB object");

            var gumButtonAsRenderableIpso = (RenderingLibrary.Graphics.IRenderableIpso)GumButtonCenteredOnSprite;
            var spriteLeftAbsolute = SpriteWidth100.Left;
            MathFunctions.AbsoluteToWindow(spriteLeftAbsolute, Y, Z, ref screenX, ref screenY, FlatRedBall.Camera.Main);


            gumButtonAsRenderableIpso.GetAbsoluteLeft().ShouldBe(screenX, 
                "because the Gum object has its values set to be perfectly centered and sized by the Sprite");
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
