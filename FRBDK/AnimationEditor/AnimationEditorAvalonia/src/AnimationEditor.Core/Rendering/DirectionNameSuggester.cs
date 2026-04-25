namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Suggests a mirrored direction name when duplicating an animation chain.
/// Given "WalkLeft" → "WalkRight"; given "JumpUp" → "JumpDown", etc.
/// Case-variants (lower, UPPER, Title) are all handled.
/// Returns <c>null</c> when the name contains no recognised direction token,
/// or when all possible substitutions still produce the original name.
/// </summary>
public static class DirectionNameSuggester
{
    private static readonly (string token, string mirror)[] _pairs =
    [
        ("Left",  "Right"),
        ("Right", "Left"),
        ("Up",    "Down"),
        ("Down",  "Up"),
    ];

    /// <summary>
    /// Returns the suggested mirrored name for <paramref name="chainName"/>,
    /// or <c>null</c> if no direction token is found.
    /// </summary>
    public static string? SuggestHorizontalMirror(string? chainName)
        => Suggest(chainName, "Left", "Right");

    /// <summary>
    /// Returns the suggested vertically-mirrored name for <paramref name="chainName"/>,
    /// or <c>null</c> if no direction token is found.
    /// </summary>
    public static string? SuggestVerticalMirror(string? chainName)
        => Suggest(chainName, "Up", "Down");

    private static string? Suggest(string? name, string tokenA, string tokenB)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // Try Title, lower, UPPER for both token directions
        string? result = TryReplace(name, tokenA, tokenB)
                      ?? TryReplace(name, tokenB, tokenA);

        // Only return the suggestion if it actually differs from the original
        return result == name ? null : result;
    }

    /// <summary>
    /// Replaces the first occurrence of <paramref name="from"/> (in any casing variant)
    /// inside <paramref name="name"/> with the correspondingly-cased <paramref name="to"/>.
    /// Returns <c>null</c> if <paramref name="from"/> does not appear in any casing variant.
    /// </summary>
    private static string? TryReplace(string name, string from, string to)
    {
        // Title-case: "Left" → "Right"
        if (name.Contains(from, StringComparison.Ordinal))
            return name.Replace(from, to, StringComparison.Ordinal);

        // lower-case: "left" → "right"
        var fromLower = from.ToLowerInvariant();
        var toLower   = to.ToLowerInvariant();
        if (name.Contains(fromLower, StringComparison.Ordinal))
            return name.Replace(fromLower, toLower, StringComparison.Ordinal);

        // UPPER-case: "LEFT" → "RIGHT"
        var fromUpper = from.ToUpperInvariant();
        var toUpper   = to.ToUpperInvariant();
        if (name.Contains(fromUpper, StringComparison.Ordinal))
            return name.Replace(fromUpper, toUpper, StringComparison.Ordinal);

        return null;
    }
}
