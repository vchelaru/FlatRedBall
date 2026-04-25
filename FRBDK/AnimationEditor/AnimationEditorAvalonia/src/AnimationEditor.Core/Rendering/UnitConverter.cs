using AnimationEditor.Core.Data;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Converts normalised UV coordinates (0–1) to the value displayed in the
/// coordinate inspector for the current <see cref="UnitType"/>.
/// Extracted from the wireframe/inspector UI so the conversion math can be
/// unit-tested without any rendering infrastructure.
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// Convert a UV coordinate to the appropriate display value.
    /// </summary>
    /// <param name="uvCoord">A normalised texture coordinate in [0, 1].</param>
    /// <param name="unitType">The active display unit mode.</param>
    /// <param name="textureSizePx">Texture width or height in pixels (used for Pixel / SpriteSheet modes).</param>
    /// <returns>
    /// <c>Pixel</c>             → <c>uvCoord × textureSizePx</c><br/>
    /// <c>TextureCoordinate</c> → <c>uvCoord</c> unchanged<br/>
    /// <c>SpriteSheet</c>       → <c>uvCoord × textureSizePx</c> (raw pixel position; sprite-cell
    ///                            subdivision is handled by the display layer)
    /// </returns>
    public static float ToDisplay(float uvCoord, UnitType unitType, int textureSizePx)
        => unitType switch
        {
            UnitType.Pixel             => uvCoord * textureSizePx,
            UnitType.TextureCoordinate => uvCoord,
            UnitType.SpriteSheet       => uvCoord * textureSizePx,
            _                          => uvCoord,
        };

    /// <summary>
    /// Convert a display value back to a normalised UV coordinate.
    /// </summary>
    public static float FromDisplay(float displayValue, UnitType unitType, int textureSizePx)
    {
        if (unitType == UnitType.TextureCoordinate || textureSizePx <= 0)
            return displayValue;
        return displayValue / textureSizePx;
    }
}
