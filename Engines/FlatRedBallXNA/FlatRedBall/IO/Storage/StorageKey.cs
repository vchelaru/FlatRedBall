#nullable enable
using System;

namespace FlatRedBall.IO;

/// <summary>
/// The key rules every backend shares, enforced in <see cref="GameStorage"/> so a name that works on
/// one platform cannot fail on another. Deliberately the intersection of what Windows, macOS,
/// Linux, and browser storage accept. Lowercase only, because Windows and macOS file systems are
/// case-insensitive and "Save" and "save" would be one file there but two keys elsewhere.
/// </summary>
internal static class StorageKey
{
    internal const int MaxKeyLength = 256;
    internal const int MaxSegmentLength = 128;

    // Windows refuses these as file names (with or without an extension), so a file backend would
    // fail on a key the in-memory and browser backends accept. Lowercase because keys are.
    private static readonly string[] ReservedNames =
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    /// <summary>Throws <see cref="ArgumentException"/> unless <paramref name="key"/> is a valid storage key.</summary>
    internal static void ValidateKey(string key, string paramName)
    {
        ArgumentNullException.ThrowIfNull(key, paramName);
        if (key.Length == 0)
            throw Invalid(paramName, key, "is empty");
        if (key.Length > MaxKeyLength)
            throw Invalid(paramName, key, $"is longer than {MaxKeyLength} characters");

        int start = 0;
        while (true)
        {
            int slash = key.IndexOf('/', start);
            int end = slash < 0 ? key.Length : slash;
            ValidateSegment(key, key.AsSpan(start, end - start), paramName, isPrefixTail: false);
            if (slash < 0)
                break;
            start = slash + 1;
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> unless <paramref name="prefix"/> is empty or the start of a
    /// valid key: complete segments joined by '/', optionally ending in a partial segment or a trailing '/'.
    /// </summary>
    internal static void ValidatePrefix(string prefix, string paramName)
    {
        ArgumentNullException.ThrowIfNull(prefix, paramName);
        if (prefix.Length > MaxKeyLength)
            throw Invalid(paramName, prefix, $"is longer than {MaxKeyLength} characters");

        int start = 0;
        while (true)
        {
            int slash = prefix.IndexOf('/', start);
            bool isTail = slash < 0;
            int end = isTail ? prefix.Length : slash;
            ValidateSegment(prefix, prefix.AsSpan(start, end - start), paramName, isPrefixTail: isTail);
            if (isTail)
                break;
            start = slash + 1;
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> unless <paramref name="name"/> is usable as a game's storage
    /// name: a folder name on desktop and a database name on web. Letters (either case), digits, space,
    /// '-', '_', '.'; at most 64 characters; no leading/trailing space, no trailing '.'.
    /// </summary>
    internal static void ValidateStorageName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The storage name is empty.", paramName);
        if (name.Length > 64)
            throw new ArgumentException($"Storage name \"{name}\" is longer than 64 characters.", paramName);
        foreach (char c in name)
        {
            bool ok = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '.' or ' ';
            if (!ok)
                throw new ArgumentException(
                    $"Storage name \"{name}\" contains '{c}'; only letters, digits, space, '-', '_', and '.' are allowed.", paramName);
        }
        if (name[0] == ' ' || name[^1] == ' ' || name[^1] == '.')
            throw new ArgumentException($"Storage name \"{name}\" cannot start or end with a space or end with '.'.", paramName);

        int dot = name.IndexOf('.');
        string stem = dot < 0 ? name : name[..dot];
        foreach (var reserved in ReservedNames)
        {
            if (stem.Equals(reserved, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Storage name \"{name}\" uses '{reserved}', a reserved file name on Windows.", paramName);
        }
    }

    private static void ValidateSegment(string whole, ReadOnlySpan<char> segment, string paramName, bool isPrefixTail)
    {
        if (segment.Length == 0)
        {
            // A prefix may end in '/' or be empty; nothing else may have an empty segment.
            if (isPrefixTail)
                return;
            throw Invalid(paramName, whole, "has an empty path segment (leading, trailing, or doubled '/')");
        }
        if (segment.Length > MaxSegmentLength)
            throw Invalid(paramName, whole, $"has a path segment longer than {MaxSegmentLength} characters");

        foreach (char c in segment)
        {
            bool ok = c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_' or '.';
            if (!ok)
                throw Invalid(paramName, whole, $"contains '{c}'; only lowercase letters, digits, '-', '_', '.', and '/' are allowed");
        }

        if (segment.SequenceEqual(".") || segment.SequenceEqual(".."))
            throw Invalid(paramName, whole, "contains a '.' or '..' path segment");

        if (isPrefixTail)
            return; // may be a partial name: "trailing." or "con" can still grow into a valid segment

        if (segment[^1] == '.')
            throw Invalid(paramName, whole, "has a path segment ending in '.' (Windows drops it)");

        int dot = segment.IndexOf('.');
        var stem = dot < 0 ? segment : segment[..dot];
        foreach (var reserved in ReservedNames)
        {
            if (stem.SequenceEqual(reserved))
                throw Invalid(paramName, whole, $"uses '{reserved}', a reserved file name on Windows");
        }
    }

    private static ArgumentException Invalid(string paramName, string key, string reason) =>
        new($"Storage key \"{key}\" {reason}. Keys are relative, '/'-separated names.", paramName);
}
