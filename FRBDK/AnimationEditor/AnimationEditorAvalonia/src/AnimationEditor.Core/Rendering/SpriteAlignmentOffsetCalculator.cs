using AnimationEditor.Core.Data;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure math for placing the preview sprite relative to the FlatRedBall origin (0,0).
///
/// Port of the relevant snippet in the WinForms <c>PreviewManager</c>:
/// <code>
/// if (SpriteAlignment == Center)
/// {
///     sprite.X = (-sprite.Width) / 2f + animX;
///     sprite.Y = (-sprite.Height) / 2f - animY;
/// }
/// else  // TopLeft
/// {
///     sprite.X = 0 + animX;
///     sprite.Y = 0 - animY;
/// }
/// </code>
///
/// Only the alignment contribution is modelled here.
/// Offset (animX/animY) is applied separately by the rendering layer.
/// </summary>
public static class SpriteAlignmentOffsetCalculator
{
    /// <summary>
    /// Returns the X offset that the chosen alignment contributes to the sprite position.
    /// </summary>
    /// <param name="alignment">Selected alignment mode.</param>
    /// <param name="spriteWidth">Width of the rendered sprite in world units (same as pixel width at 100% zoom).</param>
    public static float GetXOffset(SpriteAlignment alignment, float spriteWidth)
        => alignment == SpriteAlignment.Center ? -spriteWidth / 2f : 0f;

    /// <summary>
    /// Returns the Y offset that the chosen alignment contributes to the sprite position.
    /// Note: in the WinForms renderer Y increases downward, so center Y = -height/2.
    /// </summary>
    /// <param name="alignment">Selected alignment mode.</param>
    /// <param name="spriteHeight">Height of the rendered sprite in world units.</param>
    public static float GetYOffset(SpriteAlignment alignment, float spriteHeight)
        => alignment == SpriteAlignment.Center ? -spriteHeight / 2f : 0f;

    /// <summary>
    /// Returns both alignment offsets in one call.
    /// </summary>
    public static (float x, float y) GetOffset(SpriteAlignment alignment, float spriteWidth, float spriteHeight)
        => (GetXOffset(alignment, spriteWidth), GetYOffset(alignment, spriteHeight));
}
