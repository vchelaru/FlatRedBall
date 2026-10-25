#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// Keeps data in the browser's <c>localStorage</c> (through <see cref="IBrowserStorageApi"/>), which
/// survives reloads and browser restarts. Installed automatically by the FlatRedBall web (KniWeb) engine.
/// <para>
/// Gotchas: <c>localStorage</c> holds strings only, so bytes are stored as base64 (about 33% larger),
/// and the browser caps it at roughly 5 MiB per origin, shared by every page on that origin. A write
/// past the cap throws <see cref="StorageException"/> (through <see cref="GameStorage"/>) and keeps the
/// old value. Entries live under <c>frb2:&lt;storage name&gt;/&lt;key&gt;</c> (the same prefix FlatRedBall 2 uses, so saves
/// carry over between the engines), so two games on one origin do not collide. Users can clear site data at any time.
/// </para>
/// <para>
/// If <c>localStorage</c> is unusable when the backend is created (blocked by the browser, disabled
/// cookies, a private mode that refuses writes), it falls back to memory only: everything works for the
/// session, <see cref="IsPersistent"/> is <c>false</c>, and nothing survives a reload. Check
/// <see cref="GameStorage.IsPersistent"/> to warn the player. It does not throw, so a blocked browser
/// does not break the game, but it also cannot tell a full quota from blocked storage at startup.
/// </para>
/// </summary>
public sealed class BrowserStorageBackend : IStorageBackend
{
    private const string Namespace = "frb2:";
    // Outside every game's "frb2:<name>/" prefix, so a leftover probe is never listed as a key.
    private const string ProbeKey = "frb2:~probe";

    private readonly IBrowserStorageApi _api;
    private readonly string _prefix;
    private readonly InMemoryStorageBackend? _fallback;

    /// <summary>
    /// Creates the backend for the game named <paramref name="storageName"/>
    /// (<see cref="FlatRedBallServices.StorageName"/>), probing whether storage is writable.
    /// </summary>
    public BrowserStorageBackend(string storageName, IBrowserStorageApi api)
    {
        ArgumentNullException.ThrowIfNull(api);
        StorageKey.ValidateStorageName(storageName, nameof(storageName));
        _api = api;
        _prefix = $"{Namespace}{storageName}/";

        try
        {
            api.SetItem(ProbeKey, "1");
            api.RemoveItem(ProbeKey);
        }
        catch (Exception)
        {
            _fallback = new InMemoryStorageBackend();
        }
    }

    /// <inheritdoc />
    public bool IsPersistent => _fallback == null;

    /// <inheritdoc />
    public Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        if (_fallback != null)
            return _fallback.ReadAsync(key, cancellationToken);
        string? value = _api.GetItem(_prefix + key);
        return Task.FromResult(value == null ? null : Convert.FromBase64String(value));
    }

    /// <inheritdoc />
    public Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_fallback != null)
            return _fallback.WriteAsync(key, data, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        // One setItem is atomic: it either stores the whole value or throws and keeps the old one.
        _api.SetItem(_prefix + key, Convert.ToBase64String(data.Span));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        if (_fallback != null)
            return _fallback.DeleteAsync(key, cancellationToken);
        string storageKey = _prefix + key;
        bool existed = _api.GetItem(storageKey) != null;
        if (existed)
            _api.RemoveItem(storageKey);
        return Task.FromResult(existed);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        if (_fallback != null)
            return _fallback.ExistsAsync(key, cancellationToken);
        return Task.FromResult(_api.GetItem(_prefix + key) != null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken)
    {
        if (_fallback != null)
            return _fallback.ListAsync(prefix, cancellationToken);

        var keys = new List<string>();
        int count = _api.Length;
        for (int i = 0; i < count; i++)
        {
            string? storageKey = _api.Key(i);
            if (storageKey == null || !storageKey.StartsWith(_prefix, StringComparison.Ordinal))
                continue;
            string key = storageKey.Substring(_prefix.Length);
            if (key.StartsWith(prefix, StringComparison.Ordinal) && IsValidKey(key))
                keys.Add(key);
        }
        keys.Sort(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<string>>(keys);
    }

    private static bool IsValidKey(string key)
    {
        try
        {
            StorageKey.ValidateKey(key, nameof(key));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
