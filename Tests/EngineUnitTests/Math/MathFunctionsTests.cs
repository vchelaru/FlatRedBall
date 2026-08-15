using FlatRedBall.Math;
using Shouldly;

namespace EngineUnitTests.Math;

public class MathFunctionsTests
{
    [Fact]
    public void DivideOrDefault_ShouldDivide_WhenTimeIsPositive()
    {
        MathFunctions.DivideOrDefault(10, 2, fallbackIfTimeIsZeroOrLess: 999).ShouldBe(5);
    }

    [Fact]
    public void DivideOrDefault_ShouldReturnFallback_WhenTimeIsZero()
    {
        // Reproduces issue #2091: a Platformer entity's Ground -> Slow Down Time (DecelerationTimeX) of 0
        // used to divide straight through to NaN/Infinity, which PositionedObject.XAcceleration's setter
        // then rejected with an ArgumentException, crashing the game on startup.
        MathFunctions.DivideOrDefault(10, 0, fallbackIfTimeIsZeroOrLess: 42).ShouldBe(42);
    }

    [Fact]
    public void DivideOrDefault_ShouldReturnFallback_WhenTimeIsNegative()
    {
        MathFunctions.DivideOrDefault(10, -1, fallbackIfTimeIsZeroOrLess: 42).ShouldBe(42);
    }
}
