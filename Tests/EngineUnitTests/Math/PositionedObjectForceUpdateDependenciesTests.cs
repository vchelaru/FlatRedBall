using FlatRedBall;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Math;

public class PositionedObjectForceUpdateDependenciesTests
{
    const float Tolerance = .0001f;

    [Fact]
    public void ForceUpdateDependencies_ShouldApplyRelativeRotationZ_WhenParentIsUnrotated()
    {
        var parent = new PositionedObject();
        var child = new PositionedObject();
        child.AttachTo(parent, false);

        child.RelativeRotationZ = MathHelper.PiOver4;
        child.ForceUpdateDependencies();

        child.RotationZ.ShouldBe(MathHelper.PiOver4, Tolerance);
    }

    [Fact]
    public void ForceUpdateDependencies_ShouldAddRelativeRotationZToParentRotationZ()
    {
        var parent = new PositionedObject();
        parent.RotationZ = MathHelper.PiOver4;
        var child = new PositionedObject();
        child.AttachTo(parent, false);

        child.RelativeRotationZ = MathHelper.PiOver4;
        child.ForceUpdateDependencies();

        child.RotationZ.ShouldBe(MathHelper.PiOver2, Tolerance);
    }

    [Fact]
    public void ForceUpdateDependencies_ShouldKeepRelativeRotationX_WhenParentHasNoXOrYRotation()
    {
        var parent = new PositionedObject();
        var child = new PositionedObject();
        child.AttachTo(parent, false);

        child.RelativeRotationX = MathHelper.PiOver4;
        child.ForceUpdateDependencies();

        child.RotationX.ShouldBe(MathHelper.PiOver4, Tolerance);
    }

    [Fact]
    public void ForceUpdateDependencies_ShouldMatchUpdateDependencies()
    {
        var parent = new PositionedObject();
        parent.RotationZ = .3f;
        var child = new PositionedObject();
        child.AttachTo(parent, false);
        child.RelativeRotationZ = .5f;

        child.ForceUpdateDependencies();
        var forcedRotationZ = child.RotationZ;

        child.UpdateDependencies(1);

        forcedRotationZ.ShouldBe(child.RotationZ, Tolerance);
    }
}
