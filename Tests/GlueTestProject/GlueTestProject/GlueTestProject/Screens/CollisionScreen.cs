
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
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace GlueTestProject.Screens
{
	public partial class CollisionScreen
	{

		void CustomInitialize()
		{
            MovableRectangle.X = 1;

            MovableRectangle.CollideAgainstMove(ImmovableRectangle, 0, 1);

            if(MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if(ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object when colliding against a 0 mass object");
            }

            // try positive infinity:
            MovableRectangle.X = 1;
            MovableRectangle.CollideAgainstMove(ImmovableRectangle, 1, float.PositiveInfinity);
            if (MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if (ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object with positive infinity");
            }




            // Try positive infinity, call with the immovable:
            MovableRectangle.X = 1;
            ImmovableRectangle.CollideAgainstMove(MovableRectangle, float.PositiveInfinity, 1);
            if (MovableRectangle.X == 1)
            {
                throw new Exception("CollideAgainstMove didn't move the movable rectangle");
            }
            if (ImmovableRectangle.X != 0)
            {
                throw new Exception("CollideAgainstMove moved an object with positive infinity");
            }

        }

        void CustomActivity(bool firstTimeCalled)
		{

            IsActivityFinished = true;
		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
