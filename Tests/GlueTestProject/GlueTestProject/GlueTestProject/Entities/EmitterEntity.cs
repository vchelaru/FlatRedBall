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
	public partial class EmitterEntity
	{
        double mTimeCreated;

        private void CustomInitialize()
		{
            mTimeCreated = TimeManager.CurrentTime;


            if(EmitterListFile1[0].EmissionSettings.Texture == null)
            {
                throw new Exception("Emitters are not loading their textures and they should.");
            }


		}

		private void CustomActivity()
		{
            if (TimeManager.SecondsSince(mTimeCreated) > .5f)
            {
                // There better be some particles!
                if (SpriteManager.ParticleCount == 0)
                {
                    throw new Exception("Emitters added to Entities should emit, but they are not!");
                }
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
