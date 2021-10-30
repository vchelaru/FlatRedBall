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


#endif

namespace GlueTestProject.Entities
{
	public partial class InterpolationEntity
	{
		private void CustomInitialize()
		{
            CurrentSizeCategoryState = InterpolationEntity.SizeCategory.Small;
            InterpolateToState(
                InterpolationEntity.SizeCategory.Small, InterpolationEntity.SizeCategory.Big,
                .5, FlatRedBall.Glue.StateInterpolation.InterpolationType.Exponential, FlatRedBall.Glue.StateInterpolation.Easing.Out);

		}

		private void CustomActivity()
		{
            // make sure that the sub object is interpolating too

            if (this.mSizeCategoryTweener.Position != 0 &&
                this.InterpolationEntitySubInstance.CircleInstanceRadius <= 5)
            {
                throw new Exception("Advanced interpolation isn't impacting sub-entities that have advanced interpolation");
            }

		}

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
