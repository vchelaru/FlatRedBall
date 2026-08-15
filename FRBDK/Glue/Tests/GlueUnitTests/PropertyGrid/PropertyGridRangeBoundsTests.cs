using System.Linq;
using FlatRedBall.Glue.Elements;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

// Pins ContentTypes.csv's MinValue/MaxValue for variables whose runtime setter throws outside a range,
// so the property grid clamps instead of crashing the live game (#2003, #2004): Camera.FieldOfView
// throws outside (0, PI) exclusive, AxisAlignedRectangle/CapsulePolygon Width/Height throw on a
// too-small scale. These variables had no MinValue/MaxValue before, so any of these throws for real
// through Glue's live-edit variable-set path.
public class PropertyGridRangeBoundsTests
{
    public PropertyGridRangeBoundsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    VariableDefinition GetVariable(AssetTypeInfo ati, string name) =>
        ati.VariableDefinitions.First(v => v.Name == name);

    [Fact]
    public void Camera_FieldOfView_ShouldHaveMinAndMaxValue_WithinExclusiveEngineRange()
    {
        var variable = GetVariable(AvailableAssetTypes.CommonAtis.Camera, "FieldOfView");

        // Camera.FieldOfView throws for value <= 0 and value >= PI (Camera.cs), so the bounds must
        // sit strictly inside that range, not on the boundary.
        variable.MinValue.ShouldNotBeNull();
        variable.MinValue.Value.ShouldBeGreaterThan(0);
        variable.MaxValue.ShouldNotBeNull();
        variable.MaxValue.Value.ShouldBeLessThan(System.Math.PI);
    }

    [Fact]
    public void AxisAlignedRectangle_WidthAndHeight_ShouldHaveMinValueZero()
    {
        // AxisAlignedRectangle.ScaleX/ScaleY (backing Width/Height) throw for value < 0.
        GetVariable(AvailableAssetTypes.CommonAtis.AxisAlignedRectangle, "Width").MinValue.ShouldBe(0);
        GetVariable(AvailableAssetTypes.CommonAtis.AxisAlignedRectangle, "Height").MinValue.ShouldBe(0);
    }

    [Fact]
    public void CapsulePolygon_WidthAndHeight_ShouldHaveMinValueOne()
    {
        // CapsulePolygon.Width/Height only throw for negative values now (#2087), but the property
        // grid still clamps at 1 - a positive value is required to render a non-degenerate capsule.
        GetVariable(AvailableAssetTypes.CommonAtis.CapsulePolygon, "Width").MinValue.ShouldBe(1);
        GetVariable(AvailableAssetTypes.CommonAtis.CapsulePolygon, "Height").MinValue.ShouldBe(1);
    }
}
