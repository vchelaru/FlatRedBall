namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Identifies which resize handle (or the body) of a frame rectangle the user is
/// interacting with. Shared between <see cref="DragHandleHitTester"/>,
/// <see cref="DragHandleApplier"/>, and the Avalonia <c>WireframeControl</c>.
/// </summary>
public enum HandleKind
{
    None,
    Move,
    TopLeft,
    TopCenter,
    TopRight,
    MidLeft,
    MidRight,
    BotLeft,
    BotCenter,
    BotRight,
}
