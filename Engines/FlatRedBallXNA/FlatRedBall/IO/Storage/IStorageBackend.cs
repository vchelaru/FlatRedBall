#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// Where <see cref="GameStorage"/> actually keeps bytes: memory, a folder, browser storage.
/// Implementations are async-only because browser storage (IndexedDB) is; ones that can finish
/// synchronously return an already-completed task.
/// <para>
/// <see cref="GameStorage"/> validates every key and prefix before calling in, so a backend can
/// treat them as safe, slash-separated names. Writes must be all-or-nothing: a failed or
/// interrupted <see cref="WriteAsync"/> leaves the previous value intact. Throw on failure (usually
/// <see cref="System.IO.IOException"/>); <see cref="GameStorage"/> wraps it in <see cref="StorageException"/>.
/// </para>
/// </summary>
public interface IStorageBackend
{
    /// <summary>
    /// Whether data outlives the process. <c>false</c> for memory-only backends; a game can use it
    /// to warn that progress will not be saved.
    /// </summary>
    bool IsPersistent { get; }

    /// <summary>
    /// The stored bytes, or <c>null</c> if <paramref name="key"/> does not exist. Return an array the
    /// caller may keep and mutate, not your internal storage.
    /// </summary>
    Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores a copy of <paramref name="data"/> under <paramref name="key"/>, replacing any existing value.</summary>
    Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>Removes <paramref name="key"/>. Returns <c>false</c> if it did not exist.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>Whether <paramref name="key"/> exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Every key that starts with <paramref name="prefix"/> (ordinal, case-sensitive; empty matches all),
    /// sorted ordinally.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken);
}
