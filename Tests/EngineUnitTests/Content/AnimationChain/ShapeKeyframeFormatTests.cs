using EngineUnitTests.TestSupport;
using FlatRedBall.Content.AnimationChain;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>
/// File-format side of per-shape keyframe interpolation (FlatRedBall2 #1478, #1484): the Offset, Size and Points
/// fields on a frame's shapes load from .achx and .achj, round-trip through .achx unchanged (including names this
/// engine doesn't understand), and are omitted when unset so existing files stay byte-identical.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class ShapeKeyframeFormatTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "achx-shape-keyframe-" + Guid.NewGuid());

    public ShapeKeyframeFormatTests()
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

    const string FullyPopulatedAchx = """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <FileRelativeTextures>true</FileRelativeTextures>
          <TimeMeasurementUnit>Second</TimeMeasurementUnit>
          <CoordinateType>UV</CoordinateType>
          <AnimationChain>
            <Name>c</Name>
            <Frame>
              <TextureName></TextureName>
              <FrameLength>0.1</FrameLength>
              <LeftCoordinate>0</LeftCoordinate>
              <RightCoordinate>1</RightCoordinate>
              <TopCoordinate>0</TopCoordinate>
              <BottomCoordinate>1</BottomCoordinate>
              <ShapeCollectionSave>
                <AxisAlignedRectangleSaves>
                  <AxisAlignedRectangleSave>
                    <X>1</X><Y>2</Y><Z>0</Z><ScaleX>8</ScaleX><ScaleY>4</ScaleY><Name>R</Name>
                    <SkipOffsetKeyframe>true</SkipOffsetKeyframe>
                    <OffsetInterpolation>Cubic</OffsetInterpolation>
                    <OffsetEasing>InOut</OffsetEasing>
                    <SkipSizeKeyframe>true</SkipSizeKeyframe>
                    <SizeInterpolation>FutureCurve</SizeInterpolation>
                    <SizeEasing>Sideways</SizeEasing>
                  </AxisAlignedRectangleSave>
                </AxisAlignedRectangleSaves>
                <PolygonSaves>
                  <PolygonSave>
                    <Name>P</Name><X>0</X><Y>0</Y><Z>0</Z>
                    <Points><Point><X>0</X><Y>0</Y></Point><Point><X>4</X><Y>0</Y></Point><Point><X>4</X><Y>4</Y></Point></Points>
                    <SkipOffsetKeyframe>true</SkipOffsetKeyframe>
                    <OffsetInterpolation>Linear</OffsetInterpolation>
                    <OffsetEasing>Out</OffsetEasing>
                    <SkipPointsKeyframe>true</SkipPointsKeyframe>
                    <PointsInterpolation>Bounce</PointsInterpolation>
                    <PointsEasing>In</PointsEasing>
                  </PolygonSave>
                </PolygonSaves>
                <CircleSaves>
                  <CircleSave>
                    <X>0</X><Y>0</Y><Z>0</Z><Radius>6</Radius><Name>C</Name>
                    <SkipOffsetKeyframe>true</SkipOffsetKeyframe>
                    <OffsetInterpolation>Back</OffsetInterpolation>
                    <OffsetEasing>In</OffsetEasing>
                    <SkipSizeKeyframe>true</SkipSizeKeyframe>
                    <SizeInterpolation>Bounce</SizeInterpolation>
                    <SizeEasing>Out</SizeEasing>
                  </CircleSave>
                </CircleSaves>
              </ShapeCollectionSave>
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """;

    const string FullyPopulatedAchj = """
        { "animationChains": [ { "name": "c", "frames": [ {
            "textureName": "", "frameLength": 0.1,
            "shapes": {
                "rectangles": [ {
                    "name": "R", "x": 1, "y": 2, "scaleX": 8, "scaleY": 4,
                    "skipOffsetKeyframe": true, "offsetInterpolation": "Cubic", "offsetEasing": "InOut",
                    "skipSizeKeyframe": true, "sizeInterpolation": "FutureCurve", "sizeEasing": "Sideways"
                } ],
                "polygons": [ {
                    "name": "P", "points": [ { "x": 0, "y": 0 }, { "x": 4, "y": 0 }, { "x": 4, "y": 4 } ],
                    "skipOffsetKeyframe": true, "offsetInterpolation": "Linear", "offsetEasing": "Out",
                    "skipPointsKeyframe": true, "pointsInterpolation": "Bounce", "pointsEasing": "In"
                } ],
                "circles": [ {
                    "name": "C", "radius": 6,
                    "skipOffsetKeyframe": true, "offsetInterpolation": "Back", "offsetEasing": "In",
                    "skipSizeKeyframe": true, "sizeInterpolation": "Bounce", "sizeEasing": "Out"
                } ]
            }
        } ] } ] }
        """;

    static void AssertPreserved(AnimationChainListSave save)
    {
        var shapes = save.AnimationChains[0].Frames[0].ShapeCollectionSave;

        var rectangle = shapes.AxisAlignedRectangleSaves.Single();
        rectangle.SkipOffsetKeyframe.ShouldBeTrue();
        rectangle.OffsetInterpolation.ShouldBe("Cubic");
        rectangle.OffsetEasing.ShouldBe("InOut");
        rectangle.SkipSizeKeyframe.ShouldBeTrue();
        rectangle.SizeInterpolation.ShouldBe("FutureCurve");
        rectangle.SizeEasing.ShouldBe("Sideways");
        rectangle.ScaleX.ShouldBe(8f);

        var circle = shapes.CircleSaves.Single();
        circle.SkipOffsetKeyframe.ShouldBeTrue();
        circle.OffsetInterpolation.ShouldBe("Back");
        circle.OffsetEasing.ShouldBe("In");
        circle.SkipSizeKeyframe.ShouldBeTrue();
        circle.SizeInterpolation.ShouldBe("Bounce");
        circle.SizeEasing.ShouldBe("Out");
        circle.Radius.ShouldBe(6f);

        var polygon = shapes.PolygonSaves.Single();
        polygon.SkipOffsetKeyframe.ShouldBeTrue();
        polygon.OffsetInterpolation.ShouldBe("Linear");
        polygon.OffsetEasing.ShouldBe("Out");
        polygon.SkipPointsKeyframe.ShouldBeTrue();
        polygon.PointsInterpolation.ShouldBe("Bounce");
        polygon.PointsEasing.ShouldBe("In");
        polygon.Points.Length.ShouldBe(3);
    }

    // ---- Loading ----

    [Fact]
    public void FromFile_Achx_ReadsShapeKeyframeFields()
    {
        AssertPreserved(AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx)));
    }

    [Fact]
    public void FromFile_Achj_ReadsShapeKeyframeFields()
    {
        AssertPreserved(AnimationChainListSave.FromFile(Write("a.achj", FullyPopulatedAchj)));
    }

    [Fact]
    public void FromFile_AchjNonStringCurveName_IsIgnoredWithoutThrowing()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "c", "frames": [ {
                "textureName": "", "frameLength": 1,
                "shapes": { "circles": [ { "name": "C", "offsetInterpolation": 5, "sizeEasing": { "a": 1 } } ] }
            } ] } ] }
            """);

        var circle = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0].ShapeCollectionSave.CircleSaves.Single();

        circle.OffsetInterpolation.ShouldBeNull();
        circle.SizeEasing.ShouldBeNull();
    }

    [Fact]
    public void FromFile_OldShapesWithoutFields_LoadWithDefaults()
    {
        string path = Write("a.achx", """
            <?xml version="1.0" encoding="utf-8"?>
            <AnimationChainArraySave>
              <AnimationChain>
                <Name>c</Name>
                <Frame>
                  <TextureName></TextureName>
                  <FrameLength>0.1</FrameLength>
                  <ShapeCollectionSave>
                    <CircleSaves><CircleSave><X>0</X><Y>0</Y><Z>0</Z><Radius>6</Radius><Name>C</Name></CircleSave></CircleSaves>
                  </ShapeCollectionSave>
                </Frame>
              </AnimationChain>
            </AnimationChainArraySave>
            """);

        var circle = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0].ShapeCollectionSave.CircleSaves.Single();

        circle.SkipOffsetKeyframe.ShouldBeFalse();
        circle.SkipSizeKeyframe.ShouldBeFalse();
        circle.OffsetInterpolation.ShouldBeNull();
        circle.SizeInterpolation.ShouldBeNull();
    }

    // ---- Saving ----

    [Fact]
    public void Save_PreservesEveryValueIncludingUnknownNames()
    {
        var loaded = AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx));
        string savedPath = Path.Combine(_directory, "saved.achx");

        loaded.Save(savedPath);

        AssertPreserved(AnimationChainListSave.FromFile(savedPath));
    }

    [Fact]
    public void Save_FromAchj_PreservesEveryValueInAchx()
    {
        string savedPath = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(Write("a.achj", FullyPopulatedAchj)).Save(savedPath);

        AssertPreserved(AnimationChainListSave.FromFile(savedPath));
    }

    [Fact]
    public void Save_UsesDocumentedElementNames()
    {
        string savedPath = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx)).Save(savedPath);
        string xml = File.ReadAllText(savedPath);

        xml.ShouldContain("<SkipOffsetKeyframe>true</SkipOffsetKeyframe>");
        xml.ShouldContain("<OffsetInterpolation>Cubic</OffsetInterpolation>");
        xml.ShouldContain("<OffsetEasing>InOut</OffsetEasing>");
        xml.ShouldContain("<SkipSizeKeyframe>true</SkipSizeKeyframe>");
        xml.ShouldContain("<SizeInterpolation>FutureCurve</SizeInterpolation>");
        xml.ShouldContain("<SizeEasing>Sideways</SizeEasing>");
        xml.ShouldContain("<SkipPointsKeyframe>true</SkipPointsKeyframe>");
        xml.ShouldContain("<PointsInterpolation>Bounce</PointsInterpolation>");
        xml.ShouldContain("<PointsEasing>In</PointsEasing>");
    }

    [Fact]
    public void Save_ShapesWithoutKeyframeData_WriteNoKeyframeElements()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "c", "frames": [ {
                "textureName": "", "frameLength": 1,
                "shapes": {
                    "rectangles": [ { "name": "R" } ],
                    "circles": [ { "name": "C" } ],
                    "polygons": [ { "name": "P", "points": [ { "x": 0, "y": 0 } ] } ]
                }
            } ] } ] }
            """);
        string savedPath = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(path).Save(savedPath);
        string xml = File.ReadAllText(savedPath);

        xml.ShouldNotContain("Skip");
        xml.ShouldNotContain("Interpolation");
        xml.ShouldNotContain("Easing");
    }
}
