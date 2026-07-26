using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Graphics;

public class VertexColorPackerTests
{
    [Fact]
    public void Pack_AddSubtract_RedMinusOne_EncodesToByteZero()
    {
        var packed = VertexColorPacker.Pack(new Vector4(-1, 0, 0, 1), ColorOperation.AddSubtract);

        (packed & 0xFF).ShouldBe(0u);
    }

    [Fact]
    public void Pack_AddSubtract_RedZero_EncodesToNeutralByte127()
    {
        // (0 * 0.5 + 0.5) * 255 = 127.5, truncated to 127 - consistent with the existing
        // (uint)(255 * value) truncating cast used everywhere else in this packing step.
        var packed = VertexColorPacker.Pack(new Vector4(0, 0, 0, 1), ColorOperation.AddSubtract);

        (packed & 0xFF).ShouldBe(127u);
    }

    [Fact]
    public void Pack_AddSubtract_RedOne_EncodesToByte255()
    {
        var packed = VertexColorPacker.Pack(new Vector4(1, 0, 0, 1), ColorOperation.AddSubtract);

        (packed & 0xFF).ShouldBe(255u);
    }

    [Fact]
    public void Pack_AddSubtract_AlphaChannel_PacksUnbiased()
    {
        var packed = VertexColorPacker.Pack(new Vector4(0, 0, 0, 1), ColorOperation.AddSubtract);

        ((packed >> 24) & 0xFF).ShouldBe(255u);
    }

    [Fact]
    public void Pack_Add_NegativeRed_KeepsExistingUnbiasedPacking_RegressionGuard()
    {
        // ColorOperation.Add must keep its existing packing math untouched: (uint)(255 * -0.5f)
        // truncates to -127 and is reinterpreted as a uint32, so the low byte wraps to 129 on this
        // (.NET 8 / x64) target - it does not cleanly clamp to 0 and it does not subtract. Either
        // way, it does not produce a genuine subtraction; AddSubtract is the new op for that.
        var packed = VertexColorPacker.Pack(new Vector4(-0.5f, 0, 0, 1), ColorOperation.Add);

        (packed & 0xFF).ShouldBe(129u);
    }
}
