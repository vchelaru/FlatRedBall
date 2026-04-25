using AnimationEditor.Core.Data;
using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

// Pure tests — no singletons, no collection attribute.
public class UnitConverterTests
{
    // ── ToDisplay ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToDisplay_Pixel_MultipliesUvByTextureSize()
    {
        float result = UnitConverter.ToDisplay(0.5f, UnitType.Pixel, 256);
        Assert.Equal(128f, result);
    }

    [Fact]
    public void ToDisplay_Pixel_ZeroUv_ReturnsZero()
    {
        float result = UnitConverter.ToDisplay(0f, UnitType.Pixel, 512);
        Assert.Equal(0f, result);
    }

    [Fact]
    public void ToDisplay_Pixel_OneUv_ReturnsFullTextureSize()
    {
        float result = UnitConverter.ToDisplay(1f, UnitType.Pixel, 64);
        Assert.Equal(64f, result);
    }

    [Fact]
    public void ToDisplay_TextureCoordinate_ReturnsUvUnchanged()
    {
        float result = UnitConverter.ToDisplay(0.25f, UnitType.TextureCoordinate, 512);
        Assert.Equal(0.25f, result, 6);
    }

    [Fact]
    public void ToDisplay_TextureCoordinate_IgnoresTextureSize()
    {
        // Texture size must not affect result in UV mode
        Assert.Equal(UnitConverter.ToDisplay(0.75f, UnitType.TextureCoordinate, 100),
                     UnitConverter.ToDisplay(0.75f, UnitType.TextureCoordinate, 9999), 6);
    }

    [Fact]
    public void ToDisplay_SpriteSheet_MultipliesUvByTextureSize()
    {
        float result = UnitConverter.ToDisplay(0.25f, UnitType.SpriteSheet, 128);
        Assert.Equal(32f, result);
    }

    [Theory]
    [InlineData(0f,   256,   0f)]
    [InlineData(0.5f, 256, 128f)]
    [InlineData(1f,   256, 256f)]
    [InlineData(0.25f, 64,  16f)]
    public void ToDisplay_Pixel_Theory(float uv, int size, float expected)
        => Assert.Equal(expected, UnitConverter.ToDisplay(uv, UnitType.Pixel, size), 4);

    // ── FromDisplay ───────────────────────────────────────────────────────────

    [Fact]
    public void FromDisplay_Pixel_DividesValueByTextureSize()
    {
        float result = UnitConverter.FromDisplay(128f, UnitType.Pixel, 256);
        Assert.Equal(0.5f, result, 6);
    }

    [Fact]
    public void FromDisplay_TextureCoordinate_ReturnsValueUnchanged()
    {
        float result = UnitConverter.FromDisplay(0.75f, UnitType.TextureCoordinate, 512);
        Assert.Equal(0.75f, result, 6);
    }

    [Fact]
    public void FromDisplay_ZeroTextureSize_ReturnsValueUnchanged()
    {
        // Guard: dividing by zero must not happen
        float result = UnitConverter.FromDisplay(50f, UnitType.Pixel, 0);
        Assert.Equal(50f, result, 6);
    }

    [Fact]
    public void ToDisplay_ThenFromDisplay_RoundTrips()
    {
        float original = 0.375f;
        float display  = UnitConverter.ToDisplay(original, UnitType.Pixel, 128);
        float back     = UnitConverter.FromDisplay(display, UnitType.Pixel, 128);
        Assert.Equal(original, back, 5);
    }
}
