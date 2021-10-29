#region Usings

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
using FlatRedBall.Screens;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Graphics;

#endif
#endregion

namespace GlueTestProject.Entities
{
	public partial class IDrawableBatchEntity
	{
        // Internal IDB used to test:
        ZBufferedDrawableBatch zBufferedDrawableBatch;
        int activityCallCount = 0;

        public bool HasFinishedTests
        {
            get;
            private set;
        }

		private void CustomInitialize()
		{

            zBufferedDrawableBatch = new ZBufferedDrawableBatch();
            SpriteManager.AddZBufferedDrawableBatch(zBufferedDrawableBatch);
		}

		private void CustomActivity()
		{
			if(ScreenManager.CurrentScreen != null && ScreenManager.CurrentScreen.HasDrawBeenCalled)
            {
                if(!zBufferedDrawableBatch.HasUpdated)
                {
                    throw new Exception("Z-buffered drawable batches are not getting their Update method called");
                }

                HasFinishedTests = true;
            }


            activityCallCount++;
		}

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}

    public class ZBufferedDrawableBatch : IDrawableBatch
    {
        public bool HasUpdated
        {
            get;
            private set;
        }
        public float X
        {
            get;
            set;
        }

        public float Y
        {
            get;
            set;
        }

        public float Z
        {
            get;
            set;
        }

        public bool UpdateEveryFrame
        {
            get { return true; }
        }

        public void Draw(Camera camera)
        {
        }

        public void Update()
        {
            HasUpdated = true;
        }

        public void Destroy()
        {
        }
    }




}
