#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// Saves and loads game data (settings, progress, high scores) under string keys, with the same
/// game code on desktop and web. Reached as <c>FlatRedBallServices.Storage</c>.
/// <para>
/// Everything is async because browser storage is. Keys are relative, '/'-separated names such as
/// <c>"saves/slot1.json"</c>: lowercase letters, digits, '-', '_', '.', at most 256 characters, no '..',
/// no leading '/'. Uppercase is rejected on every platform because Windows and macOS file systems
/// are case-insensitive, so <c>"Save"</c> and <c>"save"</c> would be one file there but two keys
/// elsewhere. Keys are validated the same way on every backend, so a key that works in a desktop
/// build works in the web build. An invalid key throws <see cref="ArgumentException"/>; an
/// I/O or quota failure throws <see cref="StorageException"/>.
/// </para>
/// <para>
/// Until <c>InitializeFlatRedBall</c> runs, storage is memory-only (<see cref="IsPersistent"/> is <c>false</c>), so
/// tests never touch disk. <c>InitializeFlatRedBall</c> switches to files on desktop (browser storage on web) when
/// <see cref="FlatRedBallServices.StorageName"/> is set, and makes every storage call throw
/// <see cref="InvalidOperationException"/> when it is not. Assign <see cref="Backend"/> to choose another.
/// </para>
/// </summary>
public sealed class GameStorage
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private IStorageBackend _backend;

    internal GameStorage(IStorageBackend backend)
    {
        _backend = backend;
    }

    /// <summary>
    /// The backend that holds the data. Replacing it does not move existing data across; set it
    /// before the first read or write.
    /// </summary>
    public IStorageBackend Backend
    {
        get => _backend;
        set => _backend = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Whether stored data survives the process ending. <c>false</c> means saves are lost on exit.</summary>
    public bool IsPersistent => _backend.IsPersistent;

    /// <summary>Whether <paramref name="key"/> has stored data.</summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.ValidateKey(key, nameof(key));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        return await RunAsync("check", key, () => backend.ExistsAsync(key, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Reads <paramref name="key"/> as UTF-8 text. Returns <c>null</c> if it does not exist.</summary>
    public async Task<string?> ReadTextAsync(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(key, cancellationToken).ConfigureAwait(false);
        if (bytes == null)
            return null;
        using var reader = new StreamReader(new MemoryStream(bytes, writable: false), Utf8NoBom, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Stores <paramref name="text"/> as UTF-8 (no byte-order mark), replacing any existing value.
    /// All-or-nothing: if it throws, the previous value is intact.
    /// </summary>
    public async Task WriteTextAsync(string key, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        await WriteBytesAsync(key, Utf8NoBom.GetBytes(text), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stores <paramref name="data"/>, replacing any existing value. All-or-nothing: if it throws,
    /// the previous value is intact.
    /// </summary>
    public async Task WriteBytesAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        StorageKey.ValidateKey(key, nameof(key));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        await RunAsync("write", key, async () =>
        {
            await backend.WriteAsync(key, data, cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a read-only snapshot of <paramref name="key"/>. Later writes to the key do not change
    /// what this stream returns. Returns <c>null</c> if the key does not exist.
    /// </summary>
    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(key, cancellationToken).ConfigureAwait(false);
        return bytes == null ? null : new MemoryStream(bytes, writable: false);
    }

    /// <summary>
    /// Opens a write buffer for <paramref name="key"/>. The stored value changes only when you call
    /// <see cref="StorageWriteStream.CommitAsync"/>, replacing it in one step; disposing without
    /// committing (including when an exception skips the commit) discards the data and keeps the
    /// old value, so a partial save is never stored.
    /// <para>
    /// <paramref name="cancellationToken"/> covers opening only; pass a token to
    /// <see cref="StorageWriteStream.CommitAsync"/> to cancel the commit.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// await using var s = await FlatRedBallServices.Storage.OpenWriteAsync("saves/slot1.json");
    /// await JsonSerializer.SerializeAsync(s, saveData);
    /// await s.CommitAsync();
    /// </code>
    /// </example>
    public async Task<StorageWriteStream> OpenWriteAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.ValidateKey(key, nameof(key));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        await Task.CompletedTask.ConfigureAwait(false);
        return new StorageWriteStream((data, ct) => RunAsync("write", key, async () =>
        {
            await backend.WriteAsync(key, data, ct).ConfigureAwait(false);
            return true;
        }));
    }

    /// <summary>Removes <paramref name="key"/>. Returns <c>false</c> if it did not exist.</summary>
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.ValidateKey(key, nameof(key));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        return await RunAsync("delete", key, () => backend.DeleteAsync(key, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Keys that start with <paramref name="prefix"/>, sorted ordinally. The prefix is a plain string
    /// match, not a folder: <c>"saves/"</c> lists a folder's keys, <c>"saves/slot"</c> a partial name.
    /// Empty lists everything.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListAsync(string prefix = "", CancellationToken cancellationToken = default)
    {
        StorageKey.ValidatePrefix(prefix, nameof(prefix));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        return await RunAsync("list", prefix, () => backend.ListAsync(prefix, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the raw bytes of <paramref name="key"/>. Returns <c>null</c> if it does not exist. The array
    /// is yours; changing it does not change what is stored.
    /// </summary>
    public async Task<byte[]?> ReadBytesAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.ValidateKey(key, nameof(key));
        cancellationToken.ThrowIfCancellationRequested();
        var backend = _backend;
        return await RunAsync("read", key, () => backend.ReadAsync(key, cancellationToken)).ConfigureAwait(false);
    }

    // Wraps any backend failure (except cancellation) so game code catches one exception type.
    private static async Task<T> RunAsync<T>(string operation, string key, Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException and not StorageNotConfiguredException)
        {
            throw new StorageException($"Storage {operation} of \"{key}\" failed: {ex.Message}", ex);
        }
    }
}
