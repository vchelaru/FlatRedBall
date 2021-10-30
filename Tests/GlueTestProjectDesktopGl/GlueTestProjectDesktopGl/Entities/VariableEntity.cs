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
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using GlueTestProject.TestFramework;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;


#endif

namespace GlueTestProject.Entities
{
	public partial class VariableEntity
	{
		private void CustomInitialize()
		{
            if (VariableEntity.SharedPropertyVariable != 16)
            {
                throw new Exception("Variables that are shared and are properties are not properly getting their default values set");
            }

            // Verify the value exists
            float temp = this.FloatWithNullValueInGlue;

            if (this.TimeTextInstance.DisplayText != "2:03.40")
            {
                throw new Exception("Minutes:Seconds.Hundreths isn't working properly");
            }

            if (this.TextInheritingEntityInstance.CircleRadius != 250)
            {
                throw new Exception("Tunneling variables are not properly overriding base variables");
            }

            var newState = new VariableEntity.VariableState();
            newState.TimeTextInstanceDisplayText = 1.0f; // this should be a float because it references a float variable


            if (this.TextInheritingEntityInstance2.CircleRadius != 300)
            {
                throw new Exception("Instance variables are not properly overriding base variables");
            }

            this.Score1.ShouldBe(66);
            this.Score1 = 1234;
            TextWithIntDisplayText.DisplayText.ShouldBe("1234");
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
