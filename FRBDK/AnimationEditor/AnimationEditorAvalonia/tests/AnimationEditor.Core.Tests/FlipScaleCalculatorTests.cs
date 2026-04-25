using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

// ── FlipScaleCalculator ───────────────────────────────────────────────────────
// Pure tests — no collection attribute needed (no singletons touched).

public class FlipScaleCalculatorTests
{
    // Compute —————————————————————————————————————————————————————————————————

    [Fact]
    public void Compute_NoFlip_ReturnsPlusOneForBoth()
    {
        var (sx, sy) = FlipScaleCalculator.Compute(false, false);
        Assert.Equal(1f, sx);
        Assert.Equal(1f, sy);
    }

    [Fact]
    public void Compute_FlipHorizontalOnly_ReturnsNegativeXPlusOneY()
    {
        var (sx, sy) = FlipScaleCalculator.Compute(true, false);
        Assert.Equal(-1f, sx);
        Assert.Equal(1f, sy);
    }

    [Fact]
    public void Compute_FlipVerticalOnly_ReturnsPlusOneXNegativeY()
    {
        var (sx, sy) = FlipScaleCalculator.Compute(false, true);
        Assert.Equal(1f, sx);
        Assert.Equal(-1f, sy);
    }

    [Fact]
    public void Compute_BothFlipped_ReturnsNegativeForBoth()
    {
        var (sx, sy) = FlipScaleCalculator.Compute(true, true);
        Assert.Equal(-1f, sx);
        Assert.Equal(-1f, sy);
    }

    // IsFlipped ———————————————————————————————————————————————————————————————

    [Fact]
    public void IsFlipped_NoFlags_ReturnsFalse()
        => Assert.False(FlipScaleCalculator.IsFlipped(false, false));

    [Fact]
    public void IsFlipped_FlipH_ReturnsTrue()
        => Assert.True(FlipScaleCalculator.IsFlipped(true, false));

    [Fact]
    public void IsFlipped_FlipV_ReturnsTrue()
        => Assert.True(FlipScaleCalculator.IsFlipped(false, true));

    [Fact]
    public void IsFlipped_BothSet_ReturnsTrue()
        => Assert.True(FlipScaleCalculator.IsFlipped(true, true));

    // Scale values are exactly +1 / -1 (no float drift) ——————————————————————

    [Theory]
    [InlineData(false, false,  1f,  1f)]
    [InlineData(true,  false, -1f,  1f)]
    [InlineData(false, true,   1f, -1f)]
    [InlineData(true,  true,  -1f, -1f)]
    public void Compute_Theory(bool flipH, bool flipV, float expX, float expY)
    {
        var (sx, sy) = FlipScaleCalculator.Compute(flipH, flipV);
        Assert.Equal(expX, sx);
        Assert.Equal(expY, sy);
    }
}
