using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall;
using FlatRedBall.Graphics;
using GlueSaveClasses;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class LayerTests
    {


        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();
        }

        [TestFixtureSetUp]
        public void TestLayerOrthoValues()
        {
            EntitySave entitySave = new EntitySave();
            entitySave.Name = "LayerTestTestLayerOrthoValuesEntity";

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Layer";
            nos.InstanceName = "Layer1";
            nos.IndependentOfCamera = true;
            nos.Is2D = true;
            nos.LayerCoordinateUnit = LayerCoordinateUnit.Pixel;
            nos.LayerCoordinateType = LayerCoordinateType.MatchCamera;
            entitySave.NamedObjects.Add(nos);

            nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Layer";
            nos.InstanceName = "Layer2";
            nos.IndependentOfCamera = true;
            nos.Is2D = true;
            nos.LayerCoordinateUnit = LayerCoordinateUnit.Pixel;
            nos.LayerCoordinateType = LayerCoordinateType.MatchCamera;
            nos.DestinationRectangle = new FloatRectangle(0, 0, 80, 64);
            entitySave.NamedObjects.Add(nos);

            SpriteManager.Camera.Orthogonal = true;
            SpriteManager.Camera.OrthogonalWidth = 800;
            SpriteManager.Camera.OrthogonalHeight = 640;


            ElementRuntime elementRuntime = new ElementRuntime(entitySave,
                null,
                null,
                null,
                null);

            Layer layer = (elementRuntime.ContainedElements[0].DirectObjectReference as Layer);

            if (layer.LayerCameraSettings.OrthogonalWidth != 800 ||
                layer.LayerCameraSettings.OrthogonalHeight != 640)
            {
                throw new Exception("A Layer using MatchCamera coordinate types is not matching the Camera's ortho values");
            }


            layer = (elementRuntime.ContainedElements[1].DirectObjectReference as Layer);

            if (layer.LayerCameraSettings.OrthogonalWidth != 80 ||
                layer.LayerCameraSettings.OrthogonalHeight != 64)
            {
                throw new Exception("A Layer using MatchCamera with a destination rectangle does not have proper coordinates");
            }
        }

        [TestFixtureSetUp]
        public void TestPercentageLayers()
        {
            EntitySave entitySave = new EntitySave();
            entitySave.Name = "LayerTestTestPercentageLayersEntity";

            NamedObjectSave nos = new NamedObjectSave();

            nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Layer";
            nos.InstanceName = "PercentageLayer";
            nos.IndependentOfCamera = true;
            nos.Is2D = true;
            nos.LayerCoordinateUnit = LayerCoordinateUnit.Percent;
            nos.LayerCoordinateType = LayerCoordinateType.MatchCamera;
            nos.DestinationRectangle = new FloatRectangle(0, 0, 10, 10);
            entitySave.NamedObjects.Add(nos);
            NamedObjectSave percentageLayerNos = nos;


            SpriteManager.Camera.Orthogonal = true;
            SpriteManager.Camera.OrthogonalWidth = 800;
            SpriteManager.Camera.OrthogonalHeight = 640;


            ElementRuntime elementRuntime = new ElementRuntime(entitySave,
                null,
                null,
                null,
                null);

            var layer = (elementRuntime.ContainedElements[0].DirectObjectReference as Layer);

            float destinationWidth = layer.LayerCameraSettings.RightDestination - layer.LayerCameraSettings.LeftDestination;

            if (System.Math.Abs(destinationWidth - (percentageLayerNos.DestinationRectangle.Value.Width * Camera.Main.DestinationRectangle.Width / 100.0f)) > .01)
            {
                throw new Exception("Percentage based layers are not properly setting the destination rectangle");
            }

            if (System.Math.Abs(layer.LayerCameraSettings.OrthogonalWidth - destinationWidth) > .01)
            {
                throw new Exception("Percentage based 2D layers are not properly setting their orthogonal values");
            }



        }

    }
}
