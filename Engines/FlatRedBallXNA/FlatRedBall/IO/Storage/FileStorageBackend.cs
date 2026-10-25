#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// Stores each key as a file under a root folder (<c>"saves/slot1.json"</c> is
/// <c>&lt;root&gt;/saves/slot1.json</c>). Desktop only; the browser build uses browser storage instead.
/// <para>
/// Writes go to a temp file in the same folder and are then renamed over the target, so a failed or
/// cancelled write leaves the old value. Cancelling is "nothing changed": the token is honored until
/// the rename, and not after. Folders are created on first write. Files in the root that are not valid
/// keys (other tools' files, uppercase names, leftover temp files) are ignored by listing.
/// </para>
/// </summary>
public sealed class FileStorageBackend : IStorageBackend
{
    // '~' is not a valid key character, so a temp file can never be mistaken for a stored key.
    private const string TempSuffix = ".~tmp";
    private static readonly TimeSpan OrphanAge = TimeSpan.FromHours(1);

    private readonly string _root;
    private bool _orphansSwept;

    /// <summary>
    /// Stores files under <paramref name="rootDirectory"/>, created on first write. Use
    /// <see cref="DefaultRoot"/> for the per-user location.
    /// </summary>
    public FileStorageBackend(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _root = Path.GetFullPath(rootDirectory);
    }

    /// <summary>
    /// The per-user folder for <paramref name="storageName"/>: <c>%LocalAppData%\name</c> on Windows,
    /// and .NET's <c>LocalApplicationData</c> folder (<c>~/.local/share/name</c> on Linux,
    /// <c>~/Library/Application Support/name</c> on macOS) elsewhere.
    /// </summary>
    public static string DefaultRoot(string storageName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), storageName);

    /// <inheritdoc />
    public bool IsPersistent => true;

    // Test seam: runs after the temp file is complete and before it replaces the target.
    internal Action? BeforeReplace { get; set; }

    /// <inheritdoc />
    public async Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        string path = PathFor(key);
        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        string path = PathFor(key);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        SweepOrphanedTempFiles();

        string temp = $"{path}.{Guid.NewGuid():N}{TempSuffix}";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            BeforeReplace?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        string path = PathFor(key);
        if (!File.Exists(path))
            return Task.FromResult(false);
        File.Delete(path);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(File.Exists(PathFor(key)));

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        if (Directory.Exists(_root))
        {
            foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                string key = Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/');
                if (key.StartsWith(prefix, StringComparison.Ordinal) && IsValidKey(key))
                    keys.Add(key);
            }
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

    // The validation is repeated here so a backend used directly is as safe as one behind GameStorage;
    // the containment check is a second line of defense should the key rules ever loosen.
    private string PathFor(string key)
    {
        StorageKey.ValidateKey(key, nameof(key));
        string path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException($"Storage key \"{key}\" resolves outside the storage folder.", nameof(key));
        return path;
    }

    // A crash between writing the temp file and renaming it leaves the temp behind. Only old ones are
    // removed, since a recent one may belong to another process mid-write. Once per backend instance.
    private void SweepOrphanedTempFiles()
    {
        if (_orphansSwept)
            return;
        _orphansSwept = true;
        try
        {
            foreach (string file in Directory.EnumerateFiles(_root, "*" + TempSuffix, SearchOption.AllDirectories))
            {
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > OrphanAge)
                    TryDelete(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best effort; never fail a save because of it.
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
