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
	public partial class PositioningEntity
	{
		private void CustomInitialize()
		{


		}

		private void CustomActivity()
		{
            this.EmitterInstance.TimedEmit();

		}

		private void CustomDestroy()
		{
            if (this.EmitterInstance.Parent != this)
            {
                throw new Exception("Emitters which are part of entities are not remaining attached when the Entity is destroyed");
            }
            if (this.Children.Contains(EmitterInstance) == false)
            {
                throw new Exception("Emitters are not remaining as children of their parent Entities when their parent Entity is being destroyed");
            }
            if (this.Children.Contains(this.Circle) == false)
            {
                throw new Exception("Circles are not remaining as children of their parent Entities when their parent Entity is being destroyed");
            }

            if (ShapeManager.VisibleCircles.Contains(this.Circle))
            {
                throw new Exception("Circles aren't being removed from being rendered in the ShapeManager when recycled Entities are destroyed");
            }

            if(ShapeManager.VisibleRectangles.Contains(this.AxisAlignedRectangleInstance))
            {
                throw new Exception("Rectangles aren't being removed from being rendered in the ShapeManager when recycled Entities are destroyed");

            }
		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
