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
            if(this.TopButton.GetTextRuntime().GetBitmapFont() == null)
            {
                throw new Exception("Texts are not having their BitmapFont assigned");
            }

            if(this.NineSliceInstance.InternalNineSlice.BottomLeftTexture == 
                this.NineSliceInstance.InternalNineSlice.CenterTexture)
            {
                throw new Exception("The nine slice isn't using the texture pattern and it should.");
            }

            // Make sure that categories run fine...
            EntireGumScreen.CurrentStateCategory1State = GumRuntimes.TestScreenRuntime.StateCategory1.On;

            if(this.StateComponentInstance.CurrentVariableState != GumRuntimes.StateComponentRuntime.VariableState.NonDefaultState)
            {
                throw new Exception("Setting a state on an instance in a screen does not result in the state being set on the runtime.");
            }

            if( (OutlineTextInstance.RenderableComponent as RenderingLibrary.Graphics.Text).BitmapFont == null)
            {
                throw new Exception("Outlined text objects are not getting their fonts set.");
            }

            this.TestRectangleInstance.Visible = false;
            bool isAbsoluteVisible = 
                (this.TestRectangleInstance.Sprite as RenderingLibrary.Graphics.IVisible).AbsoluteVisible;
            if (isAbsoluteVisible)
            {
                throw new Exception("Making a component invisible is not making its contained objects that are attahe to containers also invisible.");
            }

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
