namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Converts a sprite-sheet tile index into UV texture coordinates for an
/// <c>AnimationFrameSave</c>.  Mirrors the logic in the WinForms
/// <c>AnimationFrameDisplayer.SetTileX / SetTileY</c>.
/// </summary>
public static class TileCoordinateCalculator
{
    /// <summary>
    /// Given a zero-based tile column index, tile pixel width, and texture pixel width,
    /// returns the left and right UV texture coordinates.
    /// </summary>
    /// <param name="tileX">Zero-based column index in the sprite sheet.</param>
    /// <param name="tileWidth">Pixel width of a single tile.</param>
    /// <param name="textureWidth">Total pixel width of the texture.</param>
    /// <returns>
    /// (leftCoord, rightCoord) — both in the range [0, 1].
    /// </returns>
    public static (float Left, float Right) GetLeftRight(int tileX, int tileWidth, int textureWidth)
    {
        if (textureWidth <= 0) throw new ArgumentOutOfRangeException(nameof(textureWidth), "Must be > 0");
        if (tileWidth   <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth),    "Must be > 0");

        float left  = (tileX * tileWidth) / (float)textureWidth;
        float right = left + tileWidth / (float)textureWidth;
        return (left, right);
    }

    /// <summary>
    /// Given a zero-based tile row index, tile pixel height, and texture pixel height,
    /// returns the top and bottom UV texture coordinates.
    /// </summary>
    /// <param name="tileY">Zero-based row index in the sprite sheet.</param>
    /// <param name="tileHeight">Pixel height of a single tile.</param>
    /// <param name="textureHeight">Total pixel height of the texture.</param>
    /// <returns>
    /// (topCoord, bottomCoord) — both in the range [0, 1].
    /// </returns>
    public static (float Top, float Bottom) GetTopBottom(int tileY, int tileHeight, int textureHeight)
    {
        if (textureHeight <= 0) throw new ArgumentOutOfRangeException(nameof(textureHeight), "Must be > 0");
        if (tileHeight    <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight),    "Must be > 0");

        float top    = (tileY * tileHeight) / (float)textureHeight;
        float bottom = top + tileHeight / (float)textureHeight;
        return (top, bottom);
    }

    /// <summary>
    /// Derives the pixel size of a single tile given how many tiles fit across a
    /// texture dimension.  Mirrors the WinForms “Set the cell height to N cells”
    /// workflow where the editor auto-computes the pixel size from the count.
    /// </summary>
    /// <param name="cellCount">Number of evenly-spaced tiles along the dimension (≥1).</param>
    /// <param name="textureSize">Total pixel size of the texture along that dimension (≥1).</param>
    /// <returns>Pixel size of one tile (integer division, floor).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either argument is ≤0.</exception>
    public static int CellSizeFromCount(int cellCount, int textureSize)
    {
        if (cellCount  <= 0) throw new ArgumentOutOfRangeException(nameof(cellCount),  "Must be ≥1");
        if (textureSize <= 0) throw new ArgumentOutOfRangeException(nameof(textureSize), "Must be ≥1");
        return textureSize / cellCount;
    }
}
