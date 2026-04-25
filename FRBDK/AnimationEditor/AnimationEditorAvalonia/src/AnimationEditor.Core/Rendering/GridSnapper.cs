namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure snap-to-grid math. No Avalonia or SkiaSharp references.
/// </summary>
public static class GridSnapper
{
    /// <summary>
    /// Snaps <paramref name="value"/> to the nearest lower multiple of
    /// <paramref name="gridSize"/> (floor snap).
    /// When <paramref name="gridSize"/> is ≤ 0 the raw integer cast of
    /// <paramref name="value"/> is returned unchanged.
    /// </summary>
    /// <remarks>
    /// Matches the WireframeControl grid-click and grid-hover behaviour:
    /// <code>int gx = _gridSize * ((int)world.X / _gridSize);</code>
    /// </remarks>
    public static int Snap(float value, int gridSize)
    {
        if (gridSize <= 0) return (int)value;
        int iv = (int)value;
        // For negative coords, C# integer division truncates toward zero;
        // we want floor behaviour so we adjust.
        if (iv < 0 && iv % gridSize != 0)
            return gridSize * (iv / gridSize - 1);
        return gridSize * (iv / gridSize);
    }
}
