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

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;


#endif

namespace GlueTestProject.Entities
{
	public partial class StateEntityContainer
	{
		private void CustomInitialize()
		{
            this.InterpolateBetween(VariableState.First, VariableState.Second, 0);
            if (this.StateEntityInstance.RelativeX != 8)
            {
                throw new Exception("InterpolateBetween is not working for setting object states");
            }

            this.InterpolateBetween(VariableState.First, VariableState.Second, 1);
            if (this.StateEntityInstance.RelativeX != 4)
            {
                throw new Exception("InterpolateBetween is not working for setting object states");
            }

            this.InterpolateBetween(VariableState.Second, VariableState.First, 1);
            if (this.StateEntityInstance.RelativeX != 8)
            {
                throw new Exception("InterpolateBetween is not working for setting object states");
            }

            this.InterpolateBetween(VariableState.Second, VariableState.First, 0);
            if (this.StateEntityInstance.RelativeX != 4)
            {
                throw new Exception("InterpolateBetween is not working for setting object states");
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
