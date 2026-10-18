using FlatRedBall.Math.Paths;
using Shouldly;
using Path = FlatRedBall.Math.Paths.Path;

namespace EngineUnitTests.Math;

public class PathFromJsonTests
{
    [Fact]
    public void FromJson_ShouldCreateLineSegment_FromSerializedValues()
    {
        var path = new Path();

        path.FromJson("[{\"SegmentType\":0,\"IsRelative\":false,\"StartX\":0.0,\"StartY\":0.0,\"EndX\":3.0,\"EndY\":4.0," +
            "\"ArcAngle\":0.0,\"CircleCenter\":\"0, 0\",\"CalculatedLength\":0.0,\"AngleUnit\":0}]");

        path.Segments.Count.ShouldBe(1);
        path.Segments[0].SegmentType.ShouldBe(SegmentType.Line);
        path.Segments[0].EndX.ShouldBe(3f);
        path.Segments[0].EndY.ShouldBe(4f);
        path.TotalLength.ShouldBe(5f, 0.001f);
    }

    [Fact]
    public void FromJson_ShouldCreateArcSegment_WithAngleConvertedFromDegrees()
    {
        var path = new Path();

        path.FromJson("[{\"SegmentType\":1,\"IsRelative\":false,\"StartX\":0.0,\"StartY\":0.0,\"EndX\":60.0,\"EndY\":0.0," +
            "\"ArcAngle\":-180.0,\"CircleCenter\":\"0, 0\",\"CalculatedLength\":0.0,\"AngleUnit\":0}]");

        path.Segments.Count.ShouldBe(1);
        path.Segments[0].SegmentType.ShouldBe(SegmentType.Arc);
        path.Segments[0].EndX.ShouldBe(60f);
        path.Segments[0].EndY.ShouldBe(0f);
        path.Segments[0].ArcAngle.ShouldBe(-System.MathF.PI, 0.001f);
        path.Segments[0].AngleUnit.ShouldBe(AngleUnit.Radians);
    }

    [Fact]
    public void FromJson_ShouldReadAngleUnitRadiansAndMultipleSegments()
    {
        var path = new Path();

        path.FromJson("[{\"SegmentType\":0,\"EndX\":10.0,\"EndY\":0.0,\"ArcAngle\":0.0,\"AngleUnit\":0}," +
            "{\"SegmentType\":1,\"EndX\":0.0,\"EndY\":10.0,\"ArcAngle\":1.5,\"AngleUnit\":1}]");

        path.Segments.Count.ShouldBe(2);
        path.Segments[1].SegmentType.ShouldBe(SegmentType.Arc);
        path.Segments[1].ArcAngle.ShouldBe(1.5f, 0.001f);
        path.Segments[1].AngleUnit.ShouldBe(AngleUnit.Radians);
        // The second segment is relative to the end of the first.
        path.Segments[1].EndX.ShouldBe(10f);
        path.Segments[1].EndY.ShouldBe(10f);
    }
}
