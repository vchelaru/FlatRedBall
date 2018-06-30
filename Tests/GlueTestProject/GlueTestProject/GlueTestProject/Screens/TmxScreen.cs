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

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Content;
using FlatRedBall.Debugging;
using FlatRedBall.Content.Scene;
#endif

namespace GlueTestProject.Screens
{
	public partial class TmxScreen
	{
        Scene scene;
		void CustomInitialize()
		{
            SpriteManager.Camera.UsePixelCoordinates();
            SpriteManager.Camera.X = 129;
            SpriteManager.Camera.Y = -216;

            FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;

			SpriteEditorScene ses = SceneSave.FromFile("Content/Screens/TmxScreen/FinalFantasyScene.scnx".ToLowerInvariant());

            scene = new Scene();
            ses.SetScene<Sprite>(ContentManagerName, scene, SceneSettingOptions.ConvertZSeparatedSpritesIntoSpriteGrids);
            scene.Shift(new Vector3(0, 0, 0));
            scene.AddToManagers();

            TestLevel2.X -= 530;

            if (string.IsNullOrEmpty(TilbTest.MapLayers[0].Name))
            {
                throw new Exception("Map layers names are not coming through");
            }


            // Layer 0 should be called "LayerWithStuff"
            if (TmxWithEmptyLayers.MapLayers[0].Name != "LayerWithStuff")
            {
                throw new Exception("Having empty layers can screw up Layer names.  " +
                    "This layer should be named \"LayerWithStuff\" but it is instead named " + TmxWithEmptyLayers.MapLayers[0].Name);
            }

            TestRotatedTiles();

            TestTileSizeOnMapWithObjects();

            // Make the shapes visible, to make sure that they get removed when the screen is destroyed:
            foreach(var shapeCollection in TmxWithShapes.ShapeCollections)
            {
                shapeCollection.Visible = true;
                shapeCollection.AddToManagers();
            }
		}

        private void TestTileSizeOnMapWithObjects()
        {
            var expectedWidth = 32 * 16;
            var actualWidth = WithLargeObjectOnFirstLayer.Width;

            if(expectedWidth != actualWidth)
            {
                throw new Exception("Maps with objects on the first layer that are not the same size as tiles report the wrong width and height");
            }
        }

        private void TestRotatedTiles()
        {
            RotatedTileTmx.Y += 100;
            
        }

		void CustomActivity(bool firstTimeCalled)
		{
            const bool progressImmediately = true;

            InputManager.Keyboard.ControlPositionedObject(SpriteManager.Camera, 100);
            if (scene != null)
            {
                scene.ManageAll();
            }
            Debugger.Write(SpriteManager.OrderedSprites.Count);

            if (!firstTimeCalled && progressImmediately)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{
            scene.RemoveFromManagers();


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
