using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
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
using FlatRedBall.TileCollisions;
#endif

namespace DemoProject.Screens
{
	public partial class TutorialScreen
	{
        TileShapeCollection mCollision;

		void CustomInitialize()
		{
            mCollision = new TileShapeCollection();
            mCollision.GridSize = 32;
            mCollision.Visible = true;

            for (int i = 0; i < 10; i++)
            {
                mCollision.AddCollisionAtWorld(i * 32 + 16, 16);

            }

		}

        void CustomActivity(bool firstTimeCalled)
		{
            InputManager.Keyboard.ControlPositionedObject(Rectangle, 200);
            mCollision.CollideAgainstSolid(Rectangle);

		}

		void CustomDestroy()
		{

            mCollision.RemoveFromManagers();
		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
