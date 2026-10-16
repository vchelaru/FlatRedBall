using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Graphics.Animation;
using Shouldly;
using Point = FlatRedBall.Math.Geometry.Point;

namespace EngineUnitTests.Graphics.Animation;

/// <summary>
/// Shape keyframe interpolation: shapes match across frames by name and kind, and Offset, Size and Points
/// are independent groups with their own skip flag and curve. Mirrors FlatRedBall2's shape keyframe evaluator
/// tests so both engines agree on the same file.
/// </summary>
public class ShapeKeyframeEvaluatorTests
{
    static AxisAlignedRectangleSave Rect(string name, float x = 0, float y = 0, float scaleX = 1, float scaleY = 1,
        string? offsetType = null, string? offsetEasing = null, bool skipOffset = false,
        string? sizeType = null, bool skipSize = false)
        => new()
        {
            Name = name, X = x, Y = y, ScaleX = scaleX, ScaleY = scaleY,
            OffsetInterpolation = offsetType, OffsetEasing = offsetEasing, SkipOffsetKeyframe = skipOffset,
            SizeInterpolation = sizeType, SkipSizeKeyframe = skipSize,
        };

    static CircleSave Circle(string name, float x = 0, float radius = 1, string? sizeType = null)
        => new() { Name = name, X = x, Radius = radius, SizeInterpolation = sizeType };

    static PolygonSave Polygon(string name, float x = 0, string? offsetType = null,
        string? pointsType = null, bool skipPoints = false, params (double X, double Y)[] points)
        => new()
        {
            Name = name, X = x, OffsetInterpolation = offsetType,
            PointsInterpolation = pointsType, SkipPointsKeyframe = skipPoints,
            Points = points.Select(p => new Point(p.X, p.Y)).ToArray(),
        };

    static AnimationFrame Frame(float length, params object[] shapes)
    {
        var collection = new ShapeCollectionSave();
        foreach (object shape in shapes)
        {
            switch (shape)
            {
                case AxisAlignedRectangleSave r: collection.AxisAlignedRectangleSaves.Add(r); break;
                case CircleSave c: collection.CircleSaves.Add(c); break;
                case PolygonSave p: collection.PolygonSaves.Add(p); break;
            }
        }
        return new AnimationFrame { FrameLength = length, ShapeCollectionSave = collection };
    }

    static AnimationChain Chain(params AnimationFrame[] frames)
    {
        var chain = new AnimationChain { Name = "c" };
        chain.AddRange(frames);
        return chain;
    }

    // ---- Offset ----

    [Fact]
    public void EvaluateShape_DefaultShapes_ReturnOwnValuesAtAnyElapsed()
    {
        var chain = Chain(Frame(1, Rect("A", x: 10, y: 20, scaleX: 3)), Frame(1, Rect("A", x: 50, y: 60, scaleX: 9)));

        foreach (double elapsed in new[] { 0.0, 0.5, 1.0 })
        {
            var value = KeyframeEvaluator.EvaluateShape(chain, true, 0, elapsed, (AxisAlignedRectangleSave)chain[0].ShapeCollectionSave.AxisAlignedRectangleSaves[0]);
            value.X.ShouldBe(10f);
            value.Y.ShouldBe(20f);
            value.SizeX.ShouldBe(3f);
        }
    }

    [Fact]
    public void EvaluateShape_LinearOffset_ReturnsMidpointAtHalfway()
    {
        var first = Rect("A", x: 0, y: 10, offsetType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Rect("A", x: 100, y: 30)));

        var value = KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first);

        value.X.ShouldBe(50f, 0.001f);
        value.Y.ShouldBe(20f, 0.001f);
    }

    [Fact]
    public void EvaluateShape_SkippedMiddleFrame_GlideSpansAcrossIt()
    {
        var first = Rect("A", x: 0, offsetType: "Linear");
        var middle = Rect("A", x: 999, skipOffset: true);
        var chain = Chain(Frame(1, first), Frame(1, middle), Frame(2, Rect("A", x: 100)));

        // The keyframes are 2 seconds apart (frames 0 and 1); the start of frame 1 is halfway.
        var value = KeyframeEvaluator.EvaluateShape(chain, false, 1, 0.0, middle);

        value.X.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void EvaluateShape_LastKeyframeOfNonLoopingChain_Holds()
    {
        var last = Rect("A", x: 100, offsetType: "Linear");
        var chain = Chain(Frame(1, Rect("A", x: 0)), Frame(1, last));

        KeyframeEvaluator.EvaluateShape(chain, false, 1, 0.9, last).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateShape_LoopingChain_LastKeyframeGlidesBackToFirst()
    {
        var last = Rect("A", x: 100, offsetType: "Linear");
        var chain = Chain(Frame(1, Rect("A", x: 0)), Frame(1, last));

        KeyframeEvaluator.EvaluateShape(chain, true, 1, 0.5, last).X.ShouldBe(50f, 0.001f);
    }

    // ---- Size ----

    [Fact]
    public void EvaluateShape_RectangleSize_GlidesIndependentlyOfOffset()
    {
        var first = Rect("A", x: 0, scaleX: 2, scaleY: 4, sizeType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Rect("A", x: 100, scaleX: 6, scaleY: 8)));

        var value = KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first);

        value.SizeX.ShouldBe(4f, 0.001f);
        value.SizeY.ShouldBe(6f, 0.001f);
        value.X.ShouldBe(0f); // offset curve is snap
    }

    [Fact]
    public void EvaluateShape_CircleRadius_GlidesInSizeX()
    {
        var first = Circle("C", radius: 10, sizeType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Circle("C", radius: 30)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).SizeX.ShouldBe(20f, 0.001f);
    }

    [Fact]
    public void EvaluateShape_SkipSizeOnly_OffsetStillGlides()
    {
        var first = Rect("A", x: 0, scaleX: 2, offsetType: "Linear", sizeType: "Linear");
        var second = Rect("A", x: 100, scaleX: 999, skipSize: true);
        var chain = Chain(Frame(1, first), Frame(1, second), Frame(1, Rect("A", x: 200, scaleX: 6)));

        var value = KeyframeEvaluator.EvaluateShape(chain, false, 1, 0.0, second);

        value.SizeX.ShouldBe(4f, 0.001f); // size glides 2 -> 6 across the skipped frame
        value.X.ShouldBe(100f, 0.001f);   // offset is a keyframe here, but second's own curve is snap
    }

    // ---- Matching ----

    [Fact]
    public void EvaluateShape_NameMissingFromNextFrame_Snaps()
    {
        var first = Rect("A", x: 0, offsetType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Rect("B", x: 100)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateShape_NameChangesKind_Snaps()
    {
        var first = Rect("A", x: 0, offsetType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Circle("A", x: 100)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateShape_UnnamedShape_Snaps()
    {
        var first = Rect("", x: 0, offsetType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Rect("", x: 100)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateShape_NameDuplicatedInNextFrame_Snaps()
    {
        var first = Rect("A", x: 0, offsetType: "Linear");
        var chain = Chain(Frame(1, first), Frame(1, Rect("A", x: 100), Circle("A", x: 5)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateShape_ShapeNotInItsFrame_ReturnsOwnValues()
    {
        var stray = Rect("A", x: 42, scaleX: 7);
        var chain = Chain(Frame(1, Rect("B", x: 1)), Frame(1));

        var value = KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, stray);

        value.X.ShouldBe(42f);
        value.SizeX.ShouldBe(7f);
    }

    [Fact]
    public void EvaluateShape_FirstFrameOfARun_IsAlwaysAKeyframe()
    {
        // B appears in frame 1 flagged skip; since frame 0 lacks B, frame 1 starts the run and still controls the glide.
        var b1 = Rect("B", x: 0, offsetType: "Linear", skipOffset: true);
        var chain = Chain(Frame(1, Rect("A")), Frame(1, b1), Frame(1, Rect("B", x: 100)));

        KeyframeEvaluator.EvaluateShape(chain, false, 1, 0.5, b1).X.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void EvaluateShape_Polygon_OffsetGlides()
    {
        var first = Polygon("P", x: 0, offsetType: "Linear", points: new[] { (0.0, 0.0) });
        var chain = Chain(Frame(1, first), Frame(1, Polygon("P", x: 100, points: new[] { (0.0, 0.0) })));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void EvaluateShape_BackCurve_SwingsPastEndpoints()
    {
        var first = Rect("A", x: 0, offsetType: "Back", offsetEasing: "In");
        var chain = Chain(Frame(1, first), Frame(1, Rect("A", x: 100)));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.2, first).X.ShouldBeLessThan(0f);
    }

    // ---- Points ----

    [Fact]
    public void EvaluateShapePoints_Linear_GlidesEachPoint()
    {
        var first = Polygon("P", pointsType: "Linear", points: new[] { (0.0, 0.0), (10.0, 0.0) });
        var chain = Chain(Frame(1, first), Frame(1, Polygon("P", points: new[] { (0.0, 20.0), (30.0, 0.0) })));

        var points = KeyframeEvaluator.EvaluateShapePoints(chain, true, 0, 0.5, first);

        points.Length.ShouldBe(2);
        points[0].Y.ShouldBe(10.0, 0.001);
        points[1].X.ShouldBe(20.0, 0.001);
    }

    [Fact]
    public void EvaluateShapePoints_DifferentCounts_HoldsEarlierPoints()
    {
        var first = Polygon("P", pointsType: "Linear", points: new[] { (0.0, 0.0), (10.0, 0.0), (10.0, 10.0) });
        var chain = Chain(Frame(1, first), Frame(1, Polygon("P", points: new[] { (0.0, 0.0), (99.0, 0.0) })));

        var points = KeyframeEvaluator.EvaluateShapePoints(chain, true, 0, 0.5, first);

        points.ShouldBeSameAs(first.Points);
    }

    [Fact]
    public void EvaluateShapePoints_SnapCurve_ReturnsKeyframeOwnArray()
    {
        var first = Polygon("P", points: new[] { (0.0, 0.0), (10.0, 0.0) });
        var chain = Chain(Frame(1, first), Frame(1, Polygon("P", points: new[] { (5.0, 5.0), (6.0, 6.0) })));

        KeyframeEvaluator.EvaluateShapePoints(chain, true, 0, 0.5, first).ShouldBeSameAs(first.Points);
    }

    [Fact]
    public void EvaluateShapePoints_SkippedMiddleFrame_GlideSpansAcrossIt()
    {
        var first = Polygon("P", pointsType: "Linear", points: new[] { (0.0, 0.0) });
        var middle = Polygon("P", skipPoints: true, points: new[] { (999.0, 999.0) });
        var chain = Chain(Frame(1, first), Frame(1, middle), Frame(2, Polygon("P", points: new[] { (100.0, 0.0) })));

        var points = KeyframeEvaluator.EvaluateShapePoints(chain, false, 1, 0.0, middle);

        points[0].X.ShouldBe(50.0, 0.001);
    }

    [Fact]
    public void EvaluateShapePoints_OffsetAndPoints_AreIndependentGroups()
    {
        var first = Polygon("P", x: 0, offsetType: "Linear", pointsType: null, points: new[] { (0.0, 0.0) });
        var chain = Chain(Frame(1, first), Frame(1, Polygon("P", x: 100, points: new[] { (50.0, 0.0) })));

        KeyframeEvaluator.EvaluateShape(chain, true, 0, 0.5, first).X.ShouldBe(50f, 0.001f);
        KeyframeEvaluator.EvaluateShapePoints(chain, true, 0, 0.5, first)[0].X.ShouldBe(0.0);
    }
}
