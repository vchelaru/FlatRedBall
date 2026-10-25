#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.IO;

/// <summary>
/// A write-only buffer for one storage key, from <see cref="GameStorage.OpenWriteAsync"/>. Nothing
/// reaches the backend until <see cref="CommitAsync"/>; disposing without committing (including
/// because an exception skipped the commit) discards the data and leaves the stored value as it was.
/// <para>
/// Gotchas: after <see cref="CommitAsync"/> the stream is read-only and cannot commit again. A
/// commit that throws (quota, disk full, cancellation) leaves it uncommitted and writable, so the
/// call can be retried. Disposing never throws and never commits.
/// </para>
/// </summary>
/// <example>
/// <code>
/// await using var s = await FlatRedBallServices.Storage.OpenWriteAsync("saves/slot1.json");
/// await JsonSerializer.SerializeAsync(s, saveData);
/// await s.CommitAsync();
/// </code>
/// </example>
public sealed class StorageWriteStream : Stream
{
    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _commit;
    private readonly MemoryStream _buffer = new();
    private bool _committed;
    private bool _disposed;

    internal StorageWriteStream(Func<ReadOnlyMemory<byte>, CancellationToken, Task> commit)
    {
        _commit = commit;
    }

    /// <summary>
    /// Stores the buffered bytes under the key, replacing any existing value in one step. Throws
    /// <see cref="InvalidOperationException"/> if already committed, <see cref="ObjectDisposedException"/>
    /// if disposed, and <see cref="StorageException"/> if the backend fails (the old value survives).
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_committed)
            throw new InvalidOperationException("This storage write stream was already committed.");
        cancellationToken.ThrowIfCancellationRequested();

        await _commit(new ReadOnlyMemory<byte>(_buffer.GetBuffer(), 0, (int)_buffer.Length), cancellationToken).ConfigureAwait(false);
        _committed = true;
    }

    /// <inheritdoc />
    public override bool CanRead => false;
    /// <inheritdoc />
    public override bool CanSeek => !_disposed;
    /// <inheritdoc />
    public override bool CanWrite => !_disposed && !_committed;
    /// <inheritdoc />
    public override long Length { get { ThrowIfDisposed(); return _buffer.Length; } }
    /// <inheritdoc />
    public override long Position
    {
        get { ThrowIfDisposed(); return _buffer.Position; }
        set { ThrowIfDisposed(); _buffer.Position = value; }
    }

    /// <inheritdoc />
    public override void Flush() { }
    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("A storage write stream is write-only.");
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) { ThrowIfDisposed(); return _buffer.Seek(offset, origin); }
    /// <inheritdoc />
    public override void SetLength(long value) { ThrowIfNotWritable(); _buffer.SetLength(value); }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) { ThrowIfNotWritable(); _buffer.Write(buffer, offset, count); }
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) { ThrowIfNotWritable(); _buffer.Write(buffer); }
    /// <inheritdoc />
    public override void WriteByte(byte value) { ThrowIfNotWritable(); _buffer.WriteByte(value); }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfNotWritable();
        return _buffer.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfNotWritable();
        return _buffer.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _buffer.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void ThrowIfNotWritable()
    {
        ThrowIfDisposed();
        if (_committed)
            throw new NotSupportedException("This storage write stream was already committed and is read-only.");
    }
}
