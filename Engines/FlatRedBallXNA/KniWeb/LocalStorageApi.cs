#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;

// This assembly only ever runs in a browser (the KniWeb engine), so the "browser only" platform-compat
// analyzer has nothing to warn about.
#pragma warning disable CA1416

namespace FlatRedBall.IO;

/// <summary>
/// The browser's <c>window.localStorage</c> as an <see cref="IBrowserStorageApi"/>. All logic lives in
/// <see cref="BrowserStorageBackend"/>; this only forwards calls, turning JavaScript errors
/// (<c>SecurityError</c> when storage is blocked, <c>QuotaExceededError</c> when full) into
/// <see cref="IOException"/>.
/// </summary>
/// <remarks>
/// Synchronous <c>[JSImport]</c> with the <c>globalThis.</c> prefix needs no JavaScript file. Do not
/// replace these with <c>IJSRuntime</c> calls: the game loop runs inside a JS animation frame, and
/// blocking on an async interop call from there can report failure for a write that landed.
/// </remarks>
internal sealed partial class LocalStorageApi : IBrowserStorageApi
{
    /// <inheritdoc />
    public int Length => Guard(() => JSHost.GlobalThis.GetPropertyAsJSObject("localStorage")!.GetPropertyAsInt32("length"));

    /// <inheritdoc />
    public string? Key(int index) => Guard(() => KeyJs(index));

    /// <inheritdoc />
    public string? GetItem(string key) => Guard(() => GetItemJs(key));

    /// <inheritdoc />
    public void SetItem(string key, string value) => Guard(() => { SetItemJs(key, value); return 0; });

    /// <inheritdoc />
    public void RemoveItem(string key) => Guard(() => { RemoveItemJs(key); return 0; });

    [JSImport("globalThis.localStorage.key")]
    [return: JSMarshalAs<JSType.String>]
    private static partial string? KeyJs(int index);

    [JSImport("globalThis.localStorage.getItem")]
    [return: JSMarshalAs<JSType.String>]
    private static partial string? GetItemJs(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItemJs(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void RemoveItemJs(string key);

    private static T Guard<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (JSException ex)
        {
            throw new IOException($"Browser storage failed: {ex.Message}", ex);
        }
    }
}
