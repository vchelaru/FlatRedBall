using AnimationEditor.Core.Rendering;
using SkiaSharp;

namespace AnimationEditor.App.Controls;

/// <summary>
/// Flood-fill opaque-region finder using SkiaSharp.
/// Port of FlatRedBall.AnimationEditorForms.Textures.InspectableTexture.
/// </summary>
public sealed class InspectableImage
{
    private readonly SKBitmap _bitmap;

    public InspectableImage(SKBitmap bitmap)
    {
        _bitmap = bitmap;
    }

    /// <summary>
    /// BFS flood fill from (<paramref name="startX"/>, <paramref name="startY"/>)
    /// across contiguous opaque pixels. Returns the bounding box in texture pixel coords.
    /// If the start pixel is transparent (or out of bounds) all out params are
    /// <c>int.MinValue / int.MaxValue</c> sentinels — check <c>maxX >= minX</c>.
    /// </summary>
    public void GetOpaqueWandBounds(int startX, int startY,
        out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = int.MaxValue; minY = int.MaxValue;
        maxX = int.MinValue; maxY = int.MinValue;

        if (_bitmap.IsEmpty) return;

        FloodFillBoundsCalculator.TryGetBounds(
            startX, startY,
            _bitmap.Width, _bitmap.Height,
            (x, y) => _bitmap.GetPixel(x, y).Alpha > 0,
            out minX, out minY, out maxX, out maxY);
    }
}
