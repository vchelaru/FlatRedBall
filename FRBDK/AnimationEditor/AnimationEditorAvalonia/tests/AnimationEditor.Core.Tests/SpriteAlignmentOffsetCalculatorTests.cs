using AnimationEditor.Core.Data;
using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class SpriteAlignmentOffsetCalculatorTests
{
    // ── TopLeft ───────────────────────────────────────────────────────────

    [Fact]
    public void TopLeft_XOffset_IsZero()
    {
        float x = SpriteAlignmentOffsetCalculator.GetXOffset(SpriteAlignment.TopLeft, 64f);
        Assert.Equal(0f, x, precision: 4);
    }

    [Fact]
    public void TopLeft_YOffset_IsZero()
    {
        float y = SpriteAlignmentOffsetCalculator.GetYOffset(SpriteAlignment.TopLeft, 64f);
        Assert.Equal(0f, y, precision: 4);
    }

    [Fact]
    public void TopLeft_GetOffset_BothZero()
    {
        var (x, y) = SpriteAlignmentOffsetCalculator.GetOffset(SpriteAlignment.TopLeft, 128f, 64f);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(0f, y, precision: 4);
    }

    // ── Center ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(64f,  -32f)]
    [InlineData(128f, -64f)]
    [InlineData(1f,   -0.5f)]
    [InlineData(0f,    0f)]
    public void Center_XOffset_IsNegativeHalfWidth(float width, float expected)
    {
        float x = SpriteAlignmentOffsetCalculator.GetXOffset(SpriteAlignment.Center, width);
        Assert.Equal(expected, x, precision: 4);
    }

    [Theory]
    [InlineData(64f,  -32f)]
    [InlineData(128f, -64f)]
    [InlineData(1f,   -0.5f)]
    [InlineData(0f,    0f)]
    public void Center_YOffset_IsNegativeHalfHeight(float height, float expected)
    {
        float y = SpriteAlignmentOffsetCalculator.GetYOffset(SpriteAlignment.Center, height);
        Assert.Equal(expected, y, precision: 4);
    }

    [Fact]
    public void Center_GetOffset_BothNegativeHalf()
    {
        var (x, y) = SpriteAlignmentOffsetCalculator.GetOffset(SpriteAlignment.Center, 80f, 40f);
        Assert.Equal(-40f, x, precision: 4);
        Assert.Equal(-20f, y, precision: 4);
    }

    // ── Non-square sprite ─────────────────────────────────────────────────

    [Fact]
    public void Center_GetOffset_NonSquare()
    {
        // 200 × 50 sprite
        var (x, y) = SpriteAlignmentOffsetCalculator.GetOffset(SpriteAlignment.Center, 200f, 50f);
        Assert.Equal(-100f, x, precision: 4);
        Assert.Equal(-25f,  y, precision: 4);
    }
}
