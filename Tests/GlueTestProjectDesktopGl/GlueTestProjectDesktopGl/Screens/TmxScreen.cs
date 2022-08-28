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
using GlueTestProject.TestFramework;
using FlatRedBall.TileEntities;

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

            //TestLevel2.X -= 530;

            if (string.IsNullOrEmpty(TilbTest.MapLayers[0].Name))
            {
                throw new Exception("Map layers names are not coming through");
            }


            // Layer 0 should be called "LayerWithStuff"
            TmxWithEmptyLayers.MapLayers[0].Name.ShouldBe("EmptyLayer", "because even empty layers should be included in the loaded TMX");
            TmxWithEmptyLayers.MapLayers[1].Name.ShouldBe("LayerWithStuff", "because even empty layers should be included in the loaded TMX");


            TestRotatedTiles();

            TestTileSizeOnMapWithObjects();

            TestEntitiesInFolders();

            TestFromPropertyCollision();

            // Make the shapes visible, to make sure that they get removed when the screen is destroyed:
            foreach(var shapeCollection in TmxWithShapes.ShapeCollections)
            {
                shapeCollection.Visible = true;
                shapeCollection.AddToManagers();
            }

            TestParallax();
		}
        private void TestFromPropertyCollision()
        {
            WaterTypeCollisionLayer.ShouldNotBe(null);
        }

        private void TestEntitiesInFolders()
        {
            this.TiledEntityInFolderList.Count
                .ShouldBe(0, "because entities haven't yet been created");
            TileEntityInstantiator.CreateEntitiesFrom(TmxWithEntities);

            this.TiledEntityInFolderList.Count
                .ShouldBe(2, "because there are 2 tile instances creating this entity, one with a forward slash, one with a back slash");
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

        private void TestParallax()
        {
            var parallaxLayer = MapWithParallax.MapLayers.FindByName("ParallaxLayerXY");
            parallaxLayer.ShouldNotBe(null);
            // This doesn't carry over exactly due to floating point, so we do greater than/less than
            parallaxLayer.ParallaxMultiplierX.ShouldBeGreaterThan(.199f);
            parallaxLayer.ParallaxMultiplierX.ShouldBeLessThan(.201f);
            parallaxLayer.ParallaxMultiplierY.ShouldBeGreaterThan(.299f);
            parallaxLayer.ParallaxMultiplierY.ShouldBeLessThan(.301f);
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
