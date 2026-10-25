#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// Keeps stored data in memory only. The default backend, so tests and headless runs never touch
/// disk or browser storage. Everything is lost when the engine is discarded.
/// </summary>
public sealed class InMemoryStorageBackend : IStorageBackend
{
    private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <inheritdoc />
    public bool IsPersistent => false;

    /// <inheritdoc />
    public Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.TryGetValue(key, out var data) ? (byte[]?)data.Clone() : null);
        }
    }

    /// <inheritdoc />
    public Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var copy = data.ToArray();
        lock (_lock)
        {
            _entries[key] = copy;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.Remove(key));
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.ContainsKey(key));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        lock (_lock)
        {
            foreach (var key in _entries.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                    keys.Add(key);
            }
        }
        keys.Sort(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<string>>(keys);
    }
}
