#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>Thrown by the first storage call when <see cref="FlatRedBallServices.StorageName"/> was never set.</summary>
internal sealed class StorageNotConfiguredException() : InvalidOperationException(
    "Storage is not configured. Set FlatRedBallServices.StorageName (a stable name for this game, " +
    "such as \"MyGame\") before calling FlatRedBallServices.InitializeFlatRedBall; it names the save folder on desktop and the " +
    "key namespace in browser storage on web.");

/// <summary>
/// Installed by <c>InitializeFlatRedBall</c> when no <see cref="FlatRedBallServices.StorageName"/> is set: every
/// operation throws, so a forgotten name fails the first save instead of silently losing data.
/// </summary>
internal sealed class UnconfiguredStorageBackend : IStorageBackend
{
    public bool IsPersistent => false;

    public Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken) => throw new StorageNotConfiguredException();
    public Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => throw new StorageNotConfiguredException();
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken) => throw new StorageNotConfiguredException();
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => throw new StorageNotConfiguredException();
    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken) => throw new StorageNotConfiguredException();
}
