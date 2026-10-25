#nullable enable
using System.IO;

namespace FlatRedBall.IO;

/// <summary>
/// The slice of the browser's <c>localStorage</c> that <see cref="BrowserStorageBackend"/> needs:
/// synchronous and string-only, mirroring the Web Storage API. A seam so the backend runs under
/// plain .NET in tests; the real implementation is a thin JavaScript-interop wrapper in
/// the FlatRedBall web (KniWeb) engine.
/// <para>
/// Any failure (storage blocked, quota exceeded) must surface as an <see cref="IOException"/>, with
/// the failed <see cref="SetItem"/> leaving the previous value in place.
/// </para>
/// </summary>
public interface IBrowserStorageApi
{
    /// <summary>Number of stored entries.</summary>
    int Length { get; }

    /// <summary>The key at <paramref name="index"/>, or <c>null</c> when out of range.</summary>
    string? Key(int index);

    /// <summary>The value for <paramref name="key"/>, or <c>null</c> if absent.</summary>
    string? GetItem(string key);

    /// <summary>Stores <paramref name="value"/>. Throws <see cref="IOException"/> on quota or access failure.</summary>
    void SetItem(string key, string value);

    /// <summary>Removes <paramref name="key"/>; does nothing if absent.</summary>
    void RemoveItem(string key);
}
