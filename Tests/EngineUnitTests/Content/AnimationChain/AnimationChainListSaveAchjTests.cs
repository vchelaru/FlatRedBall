using System;
using System.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

public class AnimationChainListSaveAchjTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "achj-" + Guid.NewGuid());

    public AnimationChainListSaveAchjTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    string Write(string fileName, string contents)
    {
        string path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void FromFile_Achj_ShouldReadListSettings()
    {
        string path = Write("a.achj", """
            {
              "fileRelativeTextures": false,
              "timeMeasurementUnit": "Millisecond",
              "coordinateType": "Pixel",
              "projectFile": "../Game.gluj",
              "animationChains": []
            }
            """);

        var save = AnimationChainListSave.FromFile(path);

        save.FileRelativeTextures.ShouldBeFalse();
        save.TimeMeasurementUnit.ShouldBe(FlatRedBall.TimeMeasurementUnit.Millisecond);
        save.CoordinateType.ShouldBe(FlatRedBall.Graphics.TextureCoordinateType.Pixel);
        save.ProjectFile.ShouldBe("../Game.gluj");
        save.FileName.ShouldEndWith("a.achj");
    }

    [Fact]
    public void FromFile_AchjMissingSettings_ShouldUseXmlDefaults()
    {
        string path = Write("a.achj", """{ "animationChains": [ { "name": "Walk" } ] }""");

        var save = AnimationChainListSave.FromFile(path);

        save.FileRelativeTextures.ShouldBeTrue();
        save.CoordinateType.ShouldBe(FlatRedBall.Graphics.TextureCoordinateType.UV);
        save.AnimationChains.Count.ShouldBe(1);
        save.AnimationChains[0].Name.ShouldBe("Walk");
        save.AnimationChains[0].Frames.Count.ShouldBe(0);
    }

    [Fact]
    public void FromFile_Achj_ShouldReadFrameFields()
    {
        string path = Write("a.achj", """
            {
              "animationChains": [
                { "name": "Walk", "frames": [
                  {
                    "textureName": "sheet.png", "frameLength": 0.25,
                    "leftCoordinate": 0.1, "rightCoordinate": 0.2, "topCoordinate": 0.3, "bottomCoordinate": 0.4,
                    "flipHorizontal": true, "flipVertical": true, "flipDiagonal": true,
                    "relativeX": 3.5, "relativeY": -2
                  },
                  { "textureName": "sheet.png", "frameLength": 1 }
                ] }
              ]
            }
            """);

        var frames = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames;

        frames.Count.ShouldBe(2);
        var first = frames[0];
        first.TextureName.ShouldBe("sheet.png");
        first.FrameLength.ShouldBe(0.25f);
        first.LeftCoordinate.ShouldBe(0.1f);
        first.RightCoordinate.ShouldBe(0.2f);
        first.TopCoordinate.ShouldBe(0.3f);
        first.BottomCoordinate.ShouldBe(0.4f);
        first.FlipHorizontal.ShouldBeTrue();
        first.FlipVertical.ShouldBeTrue();
        first.FlipDiagonal.ShouldBeTrue();
        first.RelativeX.ShouldBe(3.5f);
        first.RelativeY.ShouldBe(-2f);

        // Omitted coordinates default to the full texture, same as the XML path.
        var second = frames[1];
        second.LeftCoordinate.ShouldBe(0f);
        second.TopCoordinate.ShouldBe(0f);
        second.RightCoordinate.ShouldBe(1f);
        second.BottomCoordinate.ShouldBe(1f);
        second.FlipHorizontal.ShouldBeFalse();
        second.Red.ShouldBeNull();
        second.ColorOperation.ShouldBeNull();
    }

    [Fact]
    public void FromFile_Achj_ShouldReadColorChannelsAndOperation()
    {
        string path = Write("a.achj", """
            {
              "animationChains": [
                { "name": "Flash", "frames": [
                  { "textureName": "", "frameLength": 1, "red": -255, "green": 128, "blue": 255, "alpha": 0, "colorOperation": "Add" }
                ] }
              ]
            }
            """);

        var frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

        frame.Red.ShouldBe(-255);
        frame.Green.ShouldBe(128);
        frame.Blue.ShouldBe(255);
        frame.Alpha.ShouldBe(0);
        frame.ColorOperation.ShouldBe(AnimationFrameColorOperation.Add);
    }

    [Fact]
    public void ToAnimationChainList_AchjNegativeColor_ShouldPreserveNegativeValues()
    {
        string path = Write("a.achj", """
            {
              "animationChains": [
                { "name": "Flash", "frames": [
                  { "textureName": "", "frameLength": 1, "red": -255, "green": -51, "blue": 255, "colorOperation": "Add" }
                ] }
              ]
            }
            """);

        var list = AnimationChainListSave.FromFile(path).ToAnimationChainList("Test");

        var frame = list[0][0];
        frame.Red.ShouldBe(-1f);
        frame.Green!.Value.ShouldBe(-0.2f, 0.0001f);
        frame.Blue.ShouldBe(1f);
        frame.ColorOperation.ShouldBe(FlatRedBall.Graphics.ColorOperation.AddSubtract);
    }

    [Fact]
    public void FromFile_Achj_ShouldReadShapes()
    {
        string path = Write("a.achj", """
            {
              "animationChains": [
                { "name": "Attack", "frames": [
                  { "textureName": "", "frameLength": 1,
                    "shapes": {
                      "rectangles": [ { "name": "Hit", "x": 1, "y": 2, "scaleX": 8, "scaleY": 4, "z": 5, "alpha": 0.5, "red": 0.25, "green": 0.5, "blue": 0.75 } ],
                      "circles": [ { "name": "Round", "x": 3, "y": 4, "radius": 6 } ],
                      "polygons": [ { "name": "Poly", "x": 7, "y": 8, "points": [ { "x": 0, "y": 0 }, { "x": 10, "y": 0 }, { "x": 10, "y": 10 } ] } ]
                    } },
                  { "textureName": "", "frameLength": 1 }
                ] }
              ]
            }
            """);

        var frames = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames;

        var shapes = frames[0].ShapeCollectionSave;
        shapes.ShouldNotBeNull();

        var rect = shapes.AxisAlignedRectangleSaves.ShouldHaveSingleItem();
        rect.Name.ShouldBe("Hit");
        rect.X.ShouldBe(1f);
        rect.Y.ShouldBe(2f);
        rect.ScaleX.ShouldBe(8f);
        rect.ScaleY.ShouldBe(4f);
        rect.Z.ShouldBe(5f);
        rect.Alpha.ShouldBe(0.5f);
        rect.Red.ShouldBe(0.25f);
        rect.Green.ShouldBe(0.5f);
        rect.Blue.ShouldBe(0.75f);

        var circle = shapes.CircleSaves.ShouldHaveSingleItem();
        circle.Name.ShouldBe("Round");
        circle.X.ShouldBe(3f);
        circle.Y.ShouldBe(4f);
        circle.Radius.ShouldBe(6f);
        // Omitted tint falls back to the FRB1 default of 1.
        circle.Alpha.ShouldBe(1f);
        circle.Red.ShouldBe(1f);

        var polygon = shapes.PolygonSaves.ShouldHaveSingleItem();
        polygon.Name.ShouldBe("Poly");
        polygon.X.ShouldBe(7f);
        polygon.Y.ShouldBe(8f);
        polygon.Points.Length.ShouldBe(3);
        polygon.Points[1].X.ShouldBe(10);
        polygon.Points[2].Y.ShouldBe(10);

        frames[1].ShapeCollectionSave.ShouldBeNull();
    }

    [Fact]
    public void FromFile_AchjWithLoopLockedAndEvents_ShouldIgnoreThem()
    {
        string path = Write("a.achj", """
            {
              "animationChains": [
                { "name": "Walk", "loop": false, "locked": true, "frames": [
                  { "textureName": "", "frameLength": 1, "events": [ { "name": "Footstep", "data": "left" } ] }
                ] }
              ]
            }
            """);

        var save = AnimationChainListSave.FromFile(path);

        save.AnimationChains[0].Name.ShouldBe("Walk");
        save.AnimationChains[0].Frames.Count.ShouldBe(1);
    }

    [Fact]
    public void FromFile_AchjUpperCaseExtension_ShouldParseAsJson()
    {
        string path = Write("A.ACHJ", """{ "animationChains": [ { "name": "Walk" } ] }""");

        AnimationChainListSave.FromFile(path).AnimationChains[0].Name.ShouldBe("Walk");
    }

    [Fact]
    public void FromFile_AchjWithUtf8Bom_ShouldParse()
    {
        string path = Path.Combine(_directory, "bom.achj");
        File.WriteAllText(path, """{ "animationChains": [ { "name": "Walk" } ] }""", new System.Text.UTF8Encoding(true));

        AnimationChainListSave.FromFile(path).AnimationChains[0].Name.ShouldBe("Walk");
    }

    [Fact]
    public void ToAnimationChainList_AchjAndAchxOfSameData_ShouldProduceEqualChains()
    {
        string achx = Write("a.achx", """
            <?xml version="1.0" encoding="utf-8"?>
            <AnimationChainArraySave>
              <FileRelativeTextures>true</FileRelativeTextures>
              <TimeMeasurementUnit>Millisecond</TimeMeasurementUnit>
              <CoordinateType>UV</CoordinateType>
              <AnimationChain>
                <Name>Walk</Name>
                <Frame>
                  <FlipHorizontal>true</FlipHorizontal>
                  <TextureName></TextureName>
                  <FrameLength>250</FrameLength>
                  <LeftCoordinate>0.1</LeftCoordinate>
                  <RightCoordinate>0.2</RightCoordinate>
                  <TopCoordinate>0.3</TopCoordinate>
                  <BottomCoordinate>0.4</BottomCoordinate>
                  <RelativeX>3</RelativeX>
                  <Red>-100</Red>
                  <Alpha>200</Alpha>
                  <ColorOperation>Add</ColorOperation>
                </Frame>
              </AnimationChain>
            </AnimationChainArraySave>
            """);
        string achj = Write("a.achj", """
            {
              "fileRelativeTextures": true,
              "timeMeasurementUnit": "Millisecond",
              "coordinateType": "UV",
              "animationChains": [
                { "name": "Walk", "frames": [
                  { "textureName": "", "frameLength": 250, "flipHorizontal": true,
                    "leftCoordinate": 0.1, "rightCoordinate": 0.2, "topCoordinate": 0.3, "bottomCoordinate": 0.4,
                    "relativeX": 3, "red": -100, "alpha": 200, "colorOperation": "Add" }
                ] }
              ]
            }
            """);

        var fromXml = AnimationChainListSave.FromFile(achx).ToAnimationChainList("Test");
        var fromJson = AnimationChainListSave.FromFile(achj).ToAnimationChainList("Test");

        fromJson.Count.ShouldBe(fromXml.Count);
        fromJson.TimeMeasurementUnit.ShouldBe(fromXml.TimeMeasurementUnit);
        fromJson[0].Name.ShouldBe(fromXml[0].Name);
        fromJson[0].Count.ShouldBe(fromXml[0].Count);

        var expected = fromXml[0][0];
        var actual = fromJson[0][0];
        actual.FrameLength.ShouldBe(expected.FrameLength);
        actual.LeftCoordinate.ShouldBe(expected.LeftCoordinate);
        actual.RightCoordinate.ShouldBe(expected.RightCoordinate);
        actual.TopCoordinate.ShouldBe(expected.TopCoordinate);
        actual.BottomCoordinate.ShouldBe(expected.BottomCoordinate);
        actual.FlipHorizontal.ShouldBe(expected.FlipHorizontal);
        actual.RelativeX.ShouldBe(expected.RelativeX);
        actual.Red.ShouldBe(expected.Red);
        actual.Alpha.ShouldBe(expected.Alpha);
        actual.ColorOperation.ShouldBe(expected.ColorOperation);
    }

    [Fact]
    public void ContentManagerLoad_Achj_ShouldReturnAnimationChainList()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "Walk", "frames": [ { "textureName": "", "frameLength": 1 } ] } ] }
            """);
        var contentManager = new FlatRedBall.Content.ContentManager("Test", new NullServiceProvider());

        var loaded = contentManager.Load<AnimationChainList>(path);

        loaded.Count.ShouldBe(1);
        loaded[0].Name.ShouldBe("Walk");
        loaded[0].Count.ShouldBe(1);
    }
}
