using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure-math helper for editing <c>AnimationFrameSave</c> UV coordinates when
/// the property inspector is in <b>Pixel</b> mode.
///
/// Mirrors the logic in the WinForms <c>AnimationFrameDisplayer.CoordinateChange</c>:
/// <list type="bullet">
///   <item>SetX  — shifts <em>both</em> LeftCoordinate and RightCoordinate by the pixel delta (preserves width).</item>
///   <item>SetY  — shifts <em>both</em> TopCoordinate and BottomCoordinate by the pixel delta (preserves height).</item>
///   <item>SetWidth  — adjusts RightCoordinate = LeftCoordinate + newWidth/textureWidth (keeps Left fixed).</item>
///   <item>SetHeight — adjusts BottomCoordinate = TopCoordinate + newHeight/textureHeight (keeps Top fixed).</item>
/// </list>
/// All coordinates are rounded to the nearest pixel after editing
/// (<c>round(coord * dimension) / dimension</c>) to avoid floating-point drift.
/// </summary>
public static class PixelFrameEditor
{
    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the frame horizontally to the given pixel X position, preserving
    /// the current width.  Both Left and Right are shifted by the same delta.
    /// </summary>
    public static void SetX(AnimationFrameSave frame, int newXPixels, int textureWidth)
    {
        if (frame        == null) throw new ArgumentNullException(nameof(frame));
        if (textureWidth <= 0)    throw new ArgumentOutOfRangeException(nameof(textureWidth), "Must be > 0");

        int oldLeft = RoundToPixel(frame.LeftCoordinate, textureWidth);
        int delta   = newXPixels - oldLeft;

        float deltaCoord = delta / (float)textureWidth;
        frame.LeftCoordinate  = Round(frame.LeftCoordinate  + deltaCoord, textureWidth);
        frame.RightCoordinate = Round(frame.RightCoordinate + deltaCoord, textureWidth);
    }

    /// <summary>
    /// Moves the frame vertically to the given pixel Y position, preserving
    /// the current height.  Both Top and Bottom are shifted by the same delta.
    /// </summary>
    public static void SetY(AnimationFrameSave frame, int newYPixels, int textureHeight)
    {
        if (frame         == null) throw new ArgumentNullException(nameof(frame));
        if (textureHeight <= 0)    throw new ArgumentOutOfRangeException(nameof(textureHeight), "Must be > 0");

        int oldTop = RoundToPixel(frame.TopCoordinate, textureHeight);
        int delta  = newYPixels - oldTop;

        float deltaCoord = delta / (float)textureHeight;
        frame.TopCoordinate    = Round(frame.TopCoordinate    + deltaCoord, textureHeight);
        frame.BottomCoordinate = Round(frame.BottomCoordinate + deltaCoord, textureHeight);
    }

    /// <summary>
    /// Sets the frame's width in pixels.  LeftCoordinate is unchanged;
    /// RightCoordinate = LeftCoordinate + newWidth / textureWidth.
    /// </summary>
    public static void SetWidth(AnimationFrameSave frame, int newWidthPixels, int textureWidth)
    {
        if (frame        == null) throw new ArgumentNullException(nameof(frame));
        if (textureWidth <= 0)    throw new ArgumentOutOfRangeException(nameof(textureWidth), "Must be > 0");

        frame.RightCoordinate = frame.LeftCoordinate + newWidthPixels / (float)textureWidth;
    }

    /// <summary>
    /// Sets the frame's height in pixels.  TopCoordinate is unchanged;
    /// BottomCoordinate = TopCoordinate + newHeight / textureHeight.
    /// </summary>
    public static void SetHeight(AnimationFrameSave frame, int newHeightPixels, int textureHeight)
    {
        if (frame         == null) throw new ArgumentNullException(nameof(frame));
        if (textureHeight <= 0)    throw new ArgumentOutOfRangeException(nameof(textureHeight), "Must be > 0");

        frame.BottomCoordinate = frame.TopCoordinate + newHeightPixels / (float)textureHeight;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Rounds a UV coordinate to the nearest pixel boundary.</summary>
    public static float Round(float coord, int dimension)
        => (float)Math.Round(coord * dimension) / dimension;

    private static int RoundToPixel(float coord, int dimension)
        => (int)Math.Round(coord * dimension);
}
