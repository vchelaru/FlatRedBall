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
using FlatRedBall.Debugging;
#endif

namespace DemoProject.Screens
{
	public partial class GameScreen
	{

        TileShapeCollection mTileSolidCollision;

		void CustomInitialize()
		{

            FlatRedBallServices.Game.IsMouseVisible = true;

            Camera.Main.OrthogonalHeight /= 2.0f;
            Camera.Main.FixAspectRatioYConstant();

            FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;

            Visuals.Shift(new Vector3(0, 400, 0));




            mTileSolidCollision = new TileShapeCollection();
            mTileSolidCollision.GridSize = 16;
            mTileSolidCollision.Visible = true;

            foreach (var sprite in Visuals.Sprites)
            {
                if (sprite.Name == "CollisionTile")
                {
                    mTileSolidCollision.AddCollisionAtWorld(sprite.X, sprite.Y);
                }
            }





            PlayerCharacterInstance.X = 32;
            PlayerCharacterInstance.Y = 80;

            Camera.Main.OrthogonalHeight *= 1;
            Camera.Main.FixAspectRatioYConstant();
            Camera.Main.Y = 100;
		}

        void CustomActivity(bool firstTimeCalled)
		{
            PlayerCharacterInstance.CollideAgainst(() => mTileSolidCollision.CollideAgainstSolid(PlayerCharacterInstance.Collision), false);

            //this.PlayerCharacterInstance.CollideAgainst(SolidCollisions);
            this.PlayerCharacterInstance.DetermineMovementValues();
            Camera.Main.X = PlayerCharacterInstance.X;


            Cursor cursor = GuiManager.Cursor;
            if (cursor.PrimaryClick)
            {
                float x = cursor.WorldXAt(0);
                float y = cursor.WorldYAt(0);

                if (mTileSolidCollision.GetTileAt(x, y) == null)
                {
                    mTileSolidCollision.AddCollisionAtWorld(x, y);
                }
                else
                {
                    mTileSolidCollision.RemoveCollisionAtWorld(x, y);
                }
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
