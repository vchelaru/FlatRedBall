using System;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Axis-aligned bounding rectangle expressed as four edge coordinates.
/// Used by <see cref="DragHandleApplier"/> to avoid a SkiaSharp dependency in Core.
/// </summary>
public readonly record struct BoundsRect(float Left, float Top, float Right, float Bottom);

/// <summary>
/// Pure, SkiaSharp-free drag-handle math for frame UV rectangles.
/// Computes new texture-pixel bounds after a handle drag and converts them to UV coords.
/// </summary>
public static class DragHandleApplier
{
    /// <summary>
    /// Returns the new texture-pixel bounds after dragging <paramref name="handle"/>
    /// by (<paramref name="dx"/>, <paramref name="dy"/>) from <paramref name="startBounds"/>.
    /// The result is clamped to [0, bitmapWidth] × [0, bitmapHeight] with a minimum
    /// dimension of 1 pixel on each axis.
    /// </summary>
    public static BoundsRect Apply(
        HandleKind handle,
        float dx, float dy,
        BoundsRect startBounds,
        float bitmapWidth, float bitmapHeight)
    {
        var b = startBounds;

        BoundsRect nb = handle switch
        {
            HandleKind.Move      => new(b.Left + dx, b.Top + dy, b.Right  + dx, b.Bottom + dy),
            HandleKind.TopLeft   => new(b.Left + dx, b.Top + dy, b.Right,        b.Bottom),
            HandleKind.TopCenter => new(b.Left,       b.Top + dy, b.Right,        b.Bottom),
            HandleKind.TopRight  => new(b.Left,       b.Top + dy, b.Right  + dx,  b.Bottom),
            HandleKind.MidLeft   => new(b.Left + dx,  b.Top,       b.Right,        b.Bottom),
            HandleKind.MidRight  => new(b.Left,        b.Top,       b.Right  + dx,  b.Bottom),
            HandleKind.BotLeft   => new(b.Left + dx,  b.Top,       b.Right,        b.Bottom + dy),
            HandleKind.BotCenter => new(b.Left,        b.Top,       b.Right,        b.Bottom + dy),
            HandleKind.BotRight  => new(b.Left,        b.Top,       b.Right  + dx,  b.Bottom + dy),
            _                    => b,
        };

        // Clamp to bitmap bounds and enforce minimum 1-pixel size on each axis
        float l = Math.Max(0f,          Math.Min(nb.Left,   nb.Right  - 1f));
        float t = Math.Max(0f,          Math.Min(nb.Top,    nb.Bottom - 1f));
        float r = Math.Min(bitmapWidth,  Math.Max(nb.Right,  nb.Left   + 1f));
        float bm = Math.Min(bitmapHeight, Math.Max(nb.Bottom, nb.Top    + 1f));

        return new(l, t, r, bm);
    }

    /// <summary>
    /// Converts texture-pixel bounds to UV coordinates (0…1 relative to bitmap dimensions).
    /// Returns (left, top, right, bottom) UV tuple.
    /// </summary>
    public static (float L, float T, float R, float B) ToUvCoords(
        BoundsRect bounds, float bitmapWidth, float bitmapHeight) =>
        (bounds.Left   / bitmapWidth,
         bounds.Top    / bitmapHeight,
         bounds.Right  / bitmapWidth,
         bounds.Bottom / bitmapHeight);
}
