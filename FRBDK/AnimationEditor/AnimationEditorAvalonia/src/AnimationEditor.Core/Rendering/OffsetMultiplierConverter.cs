namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure conversions between the stored <c>RelativeX/Y</c> values and the
/// "display" values shown in the property panel when the offset multiplier (PL12) is set.
///
/// From the WinForms <c>AnimationFrameDisplayer</c>:
/// <code>
/// GetRelativeX  → frame.RelativeX * PreviewManager.Self.OffsetMultiplier
/// SetRelativeX  → frame.RelativeX = displayValue / PreviewManager.Self.OffsetMultiplier
/// </code>
/// </summary>
public static class OffsetMultiplierConverter
{
    /// <summary>
    /// Converts a stored offset value to a display value.
    /// <c>display = stored × multiplier</c>
    /// </summary>
    /// <param name="storedValue">The value stored in <c>AnimationFrameSave.RelativeX/Y</c>.</param>
    /// <param name="multiplier">The current offset multiplier from <see cref="AnimationEditor.Core.CommandsAndState.AppState.OffsetMultiplier"/>.</param>
    public static float ToDisplay(float storedValue, float multiplier)
    {
        if (multiplier == 0f) multiplier = 1f;
        return storedValue * multiplier;
    }

    /// <summary>
    /// Converts a display value back to the stored offset value.
    /// <c>stored = display / multiplier</c>
    /// </summary>
    /// <param name="displayValue">The value entered by the user in the property panel.</param>
    /// <param name="multiplier">The current offset multiplier.</param>
    public static float FromDisplay(float displayValue, float multiplier)
    {
        if (multiplier == 0f) multiplier = 1f;
        return displayValue / multiplier;
    }
}
