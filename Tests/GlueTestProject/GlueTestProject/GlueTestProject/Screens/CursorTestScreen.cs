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

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif
#endregion

namespace GlueTestProject.Screens
{
	public partial class CursorTestScreen
	{

		void CustomInitialize()
		{
            Cursor cursor = GuiManager.Cursor;


            cursor.ScreenX = Camera.Main.DestinationRectangle.X;
            cursor.ScreenY = Camera.Main.DestinationRectangle.Y;

            float worldXAt = cursor.WorldXAt(0);
            float worldYAt = cursor.WorldYAt(0);

            // Unlayered
            if (System.Math.Abs(worldXAt - Camera.Main.AbsoluteLeftXEdgeAt(0)) > 1)
            {
                throw new Exception("WorldXAt isn't working");
            }

            worldXAt = cursor.WorldXAt(0, FullScreenLayer2D);
            worldYAt = cursor.WorldYAt(0, FullScreenLayer2D);
            // Full screen layer:
            if (System.Math.Abs(worldXAt - Camera.Main.AbsoluteLeftXEdgeAt(0)) > 1)
            {
                throw new Exception("WorldXAt isn't working");
            }


            if(LeftHalfLayer.LayerCameraSettings.LeftDestination != Camera.Main.LeftDestination)
            {
                throw new Exception("The LeftHalfLayer's left side should be aligned with the left of the Camera destination rectangle");
            }

            var expectedLeftHalfRightBound = Camera.Main.DestinationRectangle.Left + Camera.Main.DestinationRectangle.Width / 2;
            if(LeftHalfLayer.LayerCameraSettings.RightDestination != expectedLeftHalfRightBound)
            {
                string message = "The LeftHalfLayer should occupy half of the screen. On scaled displays this should scale with the display too." +
                    $"Expected:{expectedLeftHalfRightBound}, Actual:{LeftHalfLayer.LayerCameraSettings.RightDestination}";
                throw new Exception(message);
            }


            // Do a half-screen X Layer now:
            worldXAt = cursor.WorldXAt(0, LeftHalfLayer);
            worldYAt = cursor.WorldYAt(0, LeftHalfLayer);

            if(worldXAt != -400)
            {
                throw new Exception("WorldXAt should be -400");

            }

            var absoluteCameraLeftXEdge = Camera.Main.AbsoluteLeftXEdgeAt(0);
            // Full screen layer:
            if (System.Math.Abs(worldXAt - absoluteCameraLeftXEdge) > 1)
            {
                throw new Exception("WorldXAt isn't working when dealing with Layers that do not take up the full screen");
            }

            // Now a half screen Y layer:
            worldXAt = cursor.WorldXAt(0, TopHalfLayer);
            worldYAt = cursor.WorldYAt(0, TopHalfLayer);
            // Full screen layer:
            if (System.Math.Abs(worldYAt - Camera.Main.AbsoluteTopYEdgeAt(0)) > 1)
            {
                throw new Exception("WorldYAt isn't working when dealing with Layers that do not take up the full screen");
            }


            ZoomedLayer.LayerCameraSettings.OrthogonalWidth /= 2.0f;
            ZoomedLayer.LayerCameraSettings.OrthogonalHeight /= 2.0f;
            worldXAt = cursor.WorldXAt(0, ZoomedLayer);
            worldYAt = cursor.WorldYAt(0, ZoomedLayer);
            if (System.Math.Abs(worldXAt - Camera.Main.AbsoluteLeftXEdgeAt(0) / 2.0f) > 1)
            {
                throw new Exception("WorldXAt isn't working when dealing with zoomed Layers");
            }
            if (System.Math.Abs(worldYAt - Camera.Main.AbsoluteTopYEdgeAt(0) / 2.0f) > 1)
            {
                throw new Exception("WorldYAt isn't working when dealing with zoomed Layers");
            }

            Camera.Main.OrthogonalHeight /= 2;
            Camera.Main.OrthogonalWidth /= 2;
            worldXAt = cursor.WorldXAt(0, ZoomedLayer);
            worldYAt = cursor.WorldYAt(0, ZoomedLayer);
            if (System.Math.Abs(worldXAt - Camera.Main.AbsoluteLeftXEdgeAt(0)) > 1)
            {
                throw new Exception("WorldXAt isn't working when dealing with zoomed Layers");
            }
            if (System.Math.Abs(worldYAt - Camera.Main.AbsoluteTopYEdgeAt(0)) > 1)
            {
                throw new Exception("WorldYAt isn't working when dealing with zoomed Layers");
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
