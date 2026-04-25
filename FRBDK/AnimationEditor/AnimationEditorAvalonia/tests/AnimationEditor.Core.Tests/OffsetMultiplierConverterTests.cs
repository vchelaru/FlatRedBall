using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class OffsetMultiplierConverterTests
{
    // ── ToDisplay ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1f,   2f,   2f)]
    [InlineData(3f,   1f,   3f)]
    [InlineData(0f,   5f,   0f)]
    [InlineData(-2f,  3f,  -6f)]
    [InlineData(1.5f, 2f,   3f)]
    public void ToDisplay_MultipliesStoredByMultiplier(float stored, float multiplier, float expected)
    {
        float result = OffsetMultiplierConverter.ToDisplay(stored, multiplier);
        Assert.Equal(expected, result, precision: 4);
    }

    [Fact]
    public void ToDisplay_ZeroMultiplier_TreatsAsOne()
    {
        // Guard: multiplier=0 should not produce NaN; treat as ×1
        float result = OffsetMultiplierConverter.ToDisplay(7f, 0f);
        Assert.Equal(7f, result, precision: 4);
    }

    // ── FromDisplay ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(4f,   2f,   2f)]
    [InlineData(3f,   1f,   3f)]
    [InlineData(0f,   5f,   0f)]
    [InlineData(-6f,  3f,  -2f)]
    [InlineData(3f,   2f,   1.5f)]
    public void FromDisplay_DividesDisplayByMultiplier(float display, float multiplier, float expected)
    {
        float result = OffsetMultiplierConverter.FromDisplay(display, multiplier);
        Assert.Equal(expected, result, precision: 4);
    }

    [Fact]
    public void FromDisplay_ZeroMultiplier_TreatsAsOne()
    {
        float result = OffsetMultiplierConverter.FromDisplay(9f, 0f);
        Assert.Equal(9f, result, precision: 4);
    }

    // ── Round-trip ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(3.14f, 2f)]
    [InlineData(-7.5f, 4f)]
    [InlineData(0f,    1f)]
    [InlineData(1f,    0.5f)]
    public void RoundTrip_PreservesStoredValue(float stored, float multiplier)
    {
        float display = OffsetMultiplierConverter.ToDisplay(stored, multiplier);
        float roundTripped = OffsetMultiplierConverter.FromDisplay(display, multiplier);
        Assert.Equal(stored, roundTripped, precision: 3);
    }
}
