using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Graphics.Model;
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

namespace PlatformerSample.Screens
{
	public partial class CollisionTestScreen
	{
        AxisAlignedRectangle mStatic;
        AxisAlignedRectangle mFalling;

		void CustomInitialize()
		{
            mStatic = new AxisAlignedRectangle();
            mFalling = new AxisAlignedRectangle();
            ShapeManager.AddAxisAlignedRectangle(mStatic);
            ShapeManager.AddAxisAlignedRectangle(mFalling);

            mStatic.ScaleX = 32;
            mStatic.ScaleY = 32;

            mFalling.ScaleX = 16;
            mFalling.ScaleY = 16;
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (InputManager.Keyboard.KeyPushed(Keys.Space))
            {
                mFalling.X = 88;
                mFalling.Y = 80;
                mFalling.YAcceleration = -42;
                mFalling.YVelocity = 0;

            }
            
            mFalling.XVelocity = -24;

            float velocityBefore = mFalling.YVelocity;



            mFalling.CollideAgainstBounce(mStatic, 0, 1, 0);
            float velocityAfter = mFalling.YVelocity;

            if (velocityAfter > velocityBefore)
            {
                // ERROR!
                int m = 3;
            }
            FlatRedBall.Debugging.Debugger.Write(mFalling.YVelocity + ", " + mFalling.Y);
		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
