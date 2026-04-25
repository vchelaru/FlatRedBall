namespace AnimationEditor.Core.Data;

/// <summary>
/// Controls where the origin of the preview sprite is placed relative to its texture.
/// Mirrors the WinForms <c>SpriteAlignment</c> enum.
/// </summary>
public enum SpriteAlignment
{
    /// <summary>The sprite's top-left corner is placed at (0, 0).</summary>
    TopLeft,

    /// <summary>The sprite is centred at (0, 0) — the FlatRedBall default.</summary>
    Center
}
