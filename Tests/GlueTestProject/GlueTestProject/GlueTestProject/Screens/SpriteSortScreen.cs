
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
	public partial class SpriteSortScreen
	{

		void CustomInitialize()
		{
            for(int i = 0; i < 3; i++)
            {
                var sprite = new Sprite();
                SpriteList.Add(sprite);
                SpriteManager.AddToLayer(sprite, LayerInstance);
            }

            SpriteList[0].Y = 0;
            SpriteList[1].Y = -10;
            SpriteList[2].Y = 10;

		}

		void CustomActivity(bool firstTimeCalled)
		{
            if(this.HasDrawBeenCalled)
            {
                if(LayerInstance.Sprites[1].Y > LayerInstance.Sprites[0].Y)
                {
                    throw new Exception("Sorting on layers is not performing proper secondary Y sorting");
                }
                if (LayerInstance.Sprites[2].Y > LayerInstance.Sprites[1].Y)
                {
                    throw new Exception("Sorting on layers is not performing proper secondary Y sorting");
                }

                this.IsActivityFinished = true;
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
