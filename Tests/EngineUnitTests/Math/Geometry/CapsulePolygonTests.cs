using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Geometry;

// Pins #2087: drag-resizing a CapsulePolygon down to 0 height/width in Live Edit crashed the game
// with an unhandled ArgumentException, because SelectionMarker.ChangeSizeBy clamps the dragged size
// to Math.Max(0, ...) and then assigns it through IScalable.ScaleX/ScaleY - the same path exercised here.
public class CapsulePolygonTests
{
    [Fact]
    public void ScaleY_SetToZero_ShouldNotThrow()
    {
        var capsule = new CapsulePolygon();
        var asScalable = (IScalable)capsule;

        Should.NotThrow(() => asScalable.ScaleY = 0);

        capsule.Height.ShouldBe(0);
    }

    [Fact]
    public void ScaleX_SetToZero_ShouldNotThrow()
    {
        var capsule = new CapsulePolygon();
        var asScalable = (IScalable)capsule;

        Should.NotThrow(() => asScalable.ScaleX = 0);

        capsule.Width.ShouldBe(0);
    }

    [Fact]
    public void Height_SetToNegative_ShouldStillThrow()
    {
        var capsule = new CapsulePolygon();

        Should.Throw<System.ArgumentException>(() => capsule.Height = -1);
    }

    [Fact]
    public void Width_SetToNegative_ShouldStillThrow()
    {
        var capsule = new CapsulePolygon();

        Should.Throw<System.ArgumentException>(() => capsule.Width = -1);
    }
}
