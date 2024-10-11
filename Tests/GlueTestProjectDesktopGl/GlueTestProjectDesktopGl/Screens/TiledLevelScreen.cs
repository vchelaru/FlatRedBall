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
using System.Linq;
using FlatRedBall.TileGraphics;
using TMXGlueLib.DataTypes;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

using GlueTestProject.TestFramework;
#endregion

namespace GlueTestProject.Screens
{
	public partial class TiledLevelScreen
	{

		void CustomInitialize()
		{
            ReducedTileMapInfo.FastCreateFromTmx = true;

            //InitializeLevel("Level1");

            if (CreatedByTiledList.Count == 0)
            {
                throw new Exception("Entities created from tiled are not appearing in the screen's list");
            }

            if(CreatedByTiledList.Count(item=>item.Z == 2) == 0)
            {
                throw new Exception("Entities created from tiled object layers are not appearing in the screen's list with the right Z.");
            }
            //CurrentTileMap.ShapeCollections.Count.ShouldNotBe(0, "Because this TMX file contains shapes");

            TestEntitiesCreatedFromShapes();

            TestTypeEntityCreation();

            TestSettingStatesFromVariables();
		}

        private void TestSettingStatesFromVariables()
        {
            var entity = CreatedByTiledTypeList
                .FirstOrDefault(item => item.Name == "SetsTopOrBottom");

            entity.ShouldNotBe(null);
            entity.TopOrBottom.ShouldBe(Entities.StateEntity.TopOrBottom.Top);
        }

        private void TestEntitiesCreatedFromShapes()
        {
            CollidableShapesFromTmxList.Count.ShouldNotBe(0, "because this TMX contains shapes which create entities");

            var rectEntityToCreate = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "RectEntityToCreate");
            rectEntityToCreate.ShouldNotBe(null, "because entity creation from Rect with EntityToCreate should work");

            var circleEntityToCreate = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "CircleEntityToCreate");
            circleEntityToCreate.ShouldNotBe(null, "because entity creation from Circle with EntityToCreate should work");


            var polyEntityToCreate = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "PolyEntityToCreate");
            polyEntityToCreate.ShouldNotBe(null, "because entity creation from Polygon with EntityToCreate should work");


            //var polyType = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "PolyType");
            //var circleType = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "CircleType");
            var rectType = CollidableShapesFromTmxList.FirstOrDefault(item => item.Name == "RectType");
            // Vic says - we'll support these later
            //rectType.ShouldNotBe(null, "because entity creation from Rect with Type should work");


        }

        private void TestTypeEntityCreation()
        {
            CreatedByTiledTypeList.Count.ShouldNotBe(0, "because setting a Tiled object's Type should result in that entity being created");

            var first = CreatedByTiledTypeList[0];
            first.SetByDefaultInt.ShouldBe(4, "because entities should have default values set from the tileset");
        }

        void CustomActivity(bool firstTimeCalled)
		{
            if(!firstTimeCalled)
            {
                IsActivityFinished = true;
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
