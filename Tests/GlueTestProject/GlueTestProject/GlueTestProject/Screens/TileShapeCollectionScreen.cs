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
using FlatRedBall.Localization;
using GlueTestProject.TestFramework;

namespace GlueTestProject.Screens
{
	public partial class TileShapeCollectionScreen
	{

		void CustomInitialize()
		{
            EmptyTileShapeCollection.Rectangles.Count().ShouldBe(0);

            FillCompletelyTileShapeCollection.Rectangles.Count().ShouldBe(9);

            BorderOutlineTileShapeCollection.Rectangles.Count().ShouldBe(36);

            FromPropertyTileShapeCollection.Rectangles.Count().ShouldBe(5);

            FromTypeTileShapeCollection.Rectangles.Count().ShouldBe(6);
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
