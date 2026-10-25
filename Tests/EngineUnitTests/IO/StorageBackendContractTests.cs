using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace EngineUnitTests.IO;

// Behavior every backend must show through GameStorage: round trips, atomic writes, key rules,
// cancellation. Each backend gets a concrete subclass supplying a fresh, isolated backend.
public abstract class StorageBackendContractTests : IDisposable
{
    protected abstract IStorageBackend CreateBackend();

    public virtual void Dispose()
    {
    }

    protected GameStorage NewStorage()
    {
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = CreateBackend();
        return storage;
    }

    // Throws the given exception from every operation, like a full disk or a revoked browser quota.
    private sealed class FailingBackend(Exception failure) : IStorageBackend
    {
        public bool IsPersistent => true;
        public Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken) => throw failure;
        public Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => throw failure;
        public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken) => throw failure;
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => throw failure;
        public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken) => throw failure;
    }

    [Fact]
    public async Task WriteTextAsync_ThenReadTextAsync_RoundTrips()
    {
        var storage = NewStorage();

        await storage.WriteTextAsync("saves/slot1.json", "{\"level\":3}");

        (await storage.ReadTextAsync("saves/slot1.json")).ShouldBe("{\"level\":3}");
    }

    [Fact]
    public async Task ReadTextAsync_MissingKey_ReturnsNull()
    {
        (await NewStorage().ReadTextAsync("missing")).ShouldBeNull();
    }

    [Fact]
    public async Task WriteTextAsync_ExistingKey_Overwrites()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "first");

        await storage.WriteTextAsync("a", "second");

        (await storage.ReadTextAsync("a")).ShouldBe("second");
    }

    [Fact]
    public async Task WriteTextAsync_NonAscii_RoundTripsAsUtf8WithoutBom()
    {
        var storage = NewStorage();

        await storage.WriteTextAsync("a", "h\u00e9llo \u2713");

        (await storage.ReadTextAsync("a")).ShouldBe("h\u00e9llo \u2713");
        await using var stream = (await storage.OpenReadAsync("a"))!;
        stream.ReadByte().ShouldBe((int)'h');
    }

    [Fact]
    public async Task ExistsAsync_AfterWriteAndDelete_TracksKey()
    {
        var storage = NewStorage();
        (await storage.ExistsAsync("a")).ShouldBeFalse();

        await storage.WriteTextAsync("a", "x");
        (await storage.ExistsAsync("a")).ShouldBeTrue();

        (await storage.DeleteAsync("a")).ShouldBeTrue();
        (await storage.ExistsAsync("a")).ShouldBeFalse();
        (await storage.DeleteAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task OpenWriteAsync_CommitAsync_MakesDataVisible()
    {
        var storage = NewStorage();

        await using (var stream = await storage.OpenWriteAsync("bin"))
        {
            await stream.WriteAsync(new byte[] { 1, 2, 3 });
            (await storage.ExistsAsync("bin")).ShouldBeFalse(); // nothing visible until commit
            await stream.CommitAsync();
            (await storage.ExistsAsync("bin")).ShouldBeTrue();
        }

        await using var read = (await storage.OpenReadAsync("bin"))!;
        var buffer = new byte[3];
        (await read.ReadAsync(buffer)).ShouldBe(3);
        buffer.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task OpenWriteAsync_DisposeWithoutCommit_KeepsOldValue()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "old");

        await using (var stream = await storage.OpenWriteAsync("a"))
            await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"));

        (await storage.ReadTextAsync("a")).ShouldBe("old");
    }

    [Fact]
    public async Task OpenWriteAsync_SyncDisposeWithoutCommit_StoresNothingAndDoesNotThrow()
    {
        var storage = NewStorage();

        using (var stream = await storage.OpenWriteAsync("a"))
            stream.WriteByte(7);

        (await storage.ExistsAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task OpenWriteAsync_NeverDisposed_LeavesOldValueUntouched()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "old");

        var stream = await storage.OpenWriteAsync("a");
        await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"));

        (await storage.ReadTextAsync("a")).ShouldBe("old");
    }

    [Fact]
    public async Task OpenWriteAsync_ExceptionBeforeCommit_KeepsOldValue()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "old");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await using var stream = await storage.OpenWriteAsync("a");
            await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"));
            throw new InvalidOperationException("serializer blew up");
        });

        (await storage.ReadTextAsync("a")).ShouldBe("old");
    }

    [Fact]
    public async Task CommitAsync_Twice_Throws()
    {
        var storage = NewStorage();
        await using var stream = await storage.OpenWriteAsync("a");
        stream.WriteByte(1);
        await stream.CommitAsync();

        await Should.ThrowAsync<InvalidOperationException>(() => stream.CommitAsync());
    }

    [Fact]
    public async Task Write_AfterCommit_ThrowsAndStoredValueUnchanged()
    {
        var storage = NewStorage();
        await using var stream = await storage.OpenWriteAsync("a");
        stream.WriteByte(1);
        await stream.CommitAsync();

        stream.CanWrite.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => stream.WriteByte(2));

        await using var read = (await storage.OpenReadAsync("a"))!;
        read.Length.ShouldBe(1);
    }

    [Fact]
    public async Task CommitAsync_AfterDispose_Throws()
    {
        var storage = NewStorage();
        var stream = await storage.OpenWriteAsync("a");
        stream.WriteByte(1);
        await stream.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(() => stream.CommitAsync());
        (await storage.ExistsAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task CommitAsync_CancelledToken_ThrowsAndStoresNothing()
    {
        var storage = NewStorage();
        await using var stream = await storage.OpenWriteAsync("a");
        stream.WriteByte(1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => stream.CommitAsync(cts.Token));

        (await storage.ExistsAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task CommitAsync_BackendFails_ThrowsStorageExceptionAndOldValueSurvives()
    {
        var inner = new InMemoryStorageBackend();
        var failing = new FailOnWriteBackend(inner);
        var storage = NewStorage();
        storage.Backend = failing;
        await storage.WriteTextAsync("a", "old");
        await using var stream = await storage.OpenWriteAsync("a");
        stream.WriteByte((byte)'n');
        failing.Fail = true;

        var ex = await Should.ThrowAsync<StorageException>(() => stream.CommitAsync());

        ex.InnerException.ShouldBeOfType<IOException>();
        (await storage.ReadTextAsync("a")).ShouldBe("old");

        // A failed commit leaves the stream uncommitted, so the caller can retry.
        failing.Fail = false;
        await stream.CommitAsync();
        (await storage.ReadTextAsync("a")).ShouldBe("n");
    }

    [Fact]
    public async Task WriteTextAsync_BackendFails_OldValueSurvives()
    {
        var inner = new InMemoryStorageBackend();
        var failing = new FailOnWriteBackend(inner);
        var storage = NewStorage();
        storage.Backend = failing;
        await storage.WriteTextAsync("a", "old");
        failing.Fail = true;

        await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("a", "new"));

        (await storage.ReadTextAsync("a")).ShouldBe("old");
    }

    [Fact]
    public async Task ReadBytesAsync_NonUtf8Bytes_RoundTripsExactly()
    {
        var storage = NewStorage();
        byte[] data = { 0xFF, 0xFE, 0x00, 0x80, 0xC3, 0x28 }; // invalid as UTF-8

        await storage.WriteBytesAsync("bin", data);

        (await storage.ReadBytesAsync("bin")).ShouldBe(data);
    }

    [Fact]
    public async Task ReadBytesAsync_MissingKey_ReturnsNull()
    {
        (await NewStorage().ReadBytesAsync("missing")).ShouldBeNull();
    }

    [Fact]
    public async Task ReadBytesAsync_MutatingResult_DoesNotChangeStorage()
    {
        var storage = NewStorage();
        await storage.WriteBytesAsync("bin", new byte[] { 1, 2, 3 });

        var first = (await storage.ReadBytesAsync("bin"))!;
        first[0] = 99;

        (await storage.ReadBytesAsync("bin")).ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task WriteBytesAsync_RoundTripsThroughOpenReadAsync()
    {
        var storage = NewStorage();

        await storage.WriteBytesAsync("bin", new byte[] { 9, 8, 7 });

        await using var read = (await storage.OpenReadAsync("bin"))!;
        var buffer = new byte[3];
        (await read.ReadAsync(buffer)).ShouldBe(3);
        buffer.ShouldBe(new byte[] { 9, 8, 7 });
    }

    // Delegates to a real in-memory store but fails writes on demand, to prove a failed write leaves
    // the old value alone.
    private sealed class FailOnWriteBackend(InMemoryStorageBackend inner) : IStorageBackend
    {
        public bool Fail { get; set; }
        public bool IsPersistent => true;
        public Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken) => inner.ReadAsync(key, cancellationToken);
        public Task WriteAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
            Fail ? throw new IOException("disk full") : inner.WriteAsync(key, data, cancellationToken);
        public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken) => inner.DeleteAsync(key, cancellationToken);
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => inner.ExistsAsync(key, cancellationToken);
        public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken) => inner.ListAsync(prefix, cancellationToken);
    }


    [Fact]
    public async Task OpenReadAsync_ThenOverwrite_StreamKeepsOldSnapshot()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "old");
        await using var stream = (await storage.OpenReadAsync("a"))!;

        await storage.WriteTextAsync("a", "new!");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).ShouldBe("old");
    }

    [Fact]
    public async Task OpenReadAsync_MissingKey_ReturnsNull()
    {
        (await NewStorage().OpenReadAsync("missing")).ShouldBeNull();
    }

    [Fact]
    public async Task OpenReadAsync_ReturnedStream_IsNotWritable()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "x");
        await using var stream = (await storage.OpenReadAsync("a"))!;

        stream.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public async Task ListAsync_Prefix_ReturnsMatchingKeysSorted()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("saves/b", "");
        await storage.WriteTextAsync("saves/a", "");
        await storage.WriteTextAsync("settings", "");

        (await storage.ListAsync("saves/")).ToArray().ShouldBe(new[] { "saves/a", "saves/b" });
        (await storage.ListAsync()).ToArray().ShouldBe(new[] { "saves/a", "saves/b", "settings" });
        (await storage.ListAsync("sa")).ToArray().ShouldBe(new[] { "saves/a", "saves/b" });
        (await storage.ListAsync("nothing/")).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/rooted")]
    [InlineData("C:/rooted")]
    [InlineData("a/../b")]
    [InlineData("..")]
    [InlineData("./a")]
    [InlineData("a//b")]
    [InlineData("a/")]
    [InlineData("back\\slash")]
    [InlineData("has space")]
    [InlineData("colon:name")]
    [InlineData("star*")]
    [InlineData("trailing.")]
    [InlineData("con")]
    [InlineData("nul.txt")]
    [InlineData("Save")]
    [InlineData("saves/Slot1.json")]
    [InlineData("saveS")]
    public async Task EveryOperation_InvalidKey_ThrowsArgumentException(string key)
    {
        var storage = NewStorage();

        await Should.ThrowAsync<ArgumentException>(() => storage.ExistsAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => storage.ReadTextAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => storage.ReadBytesAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => storage.WriteTextAsync(key, "x"));
        await Should.ThrowAsync<ArgumentException>(() => storage.OpenReadAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => storage.OpenWriteAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => storage.WriteBytesAsync(key, new byte[1]));
        await Should.ThrowAsync<ArgumentException>(() => storage.DeleteAsync(key));
    }

    [Fact]
    public async Task EveryOperation_NullKey_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => NewStorage().ReadTextAsync(null!));
    }

    [Fact]
    public async Task WriteTextAsync_KeyTooLong_ThrowsArgumentException()
    {
        var storage = NewStorage();
        string tooLongTotal = string.Join("/", Enumerable.Repeat(new string('a', 100), 3)); // 302 chars
        string tooLongSegment = new string('a', 129);

        await Should.ThrowAsync<ArgumentException>(() => storage.WriteTextAsync(tooLongTotal, "x"));
        await Should.ThrowAsync<ArgumentException>(() => storage.WriteTextAsync(tooLongSegment, "x"));
    }

    [Theory]
    [InlineData("/saves")]
    [InlineData("a/../")]
    [InlineData("a//")]
    [InlineData("bad char")]
    [InlineData("Saves/")]
    [InlineData("saves/Sl")]
    public async Task ListAsync_InvalidPrefix_ThrowsArgumentException(string prefix)
    {
        await Should.ThrowAsync<ArgumentException>(() => NewStorage().ListAsync(prefix));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("saves/slot-1_final.v2.json")]
    [InlineData("a/b/c")]
    [InlineData(".hidden")]
    [InlineData("console")]
    public async Task WriteTextAsync_ValidKey_Succeeds(string key)
    {
        var storage = NewStorage();

        await storage.WriteTextAsync(key, "x");

        (await storage.ReadTextAsync(key)).ShouldBe("x");
    }

    [Fact]
    public async Task EveryOperation_FailingBackend_ThrowsStorageExceptionWithInner()
    {
        var storage = NewStorage();
        var failure = new IOException("disk full");
        storage.Backend = new FailingBackend(failure);

        var ex = await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("a", "x"));
        ex.InnerException.ShouldBeSameAs(failure);
        await Should.ThrowAsync<StorageException>(() => storage.ReadTextAsync("a"));
        await Should.ThrowAsync<StorageException>(() => storage.OpenReadAsync("a"));
        await Should.ThrowAsync<StorageException>(() => storage.ExistsAsync("a"));
        await Should.ThrowAsync<StorageException>(() => storage.DeleteAsync("a"));
        await Should.ThrowAsync<StorageException>(() => storage.ListAsync());
    }

    [Fact]
    public async Task WriteTextAsync_BackendThrowsNonIoException_ThrowsStorageException()
    {
        var storage = NewStorage();
        storage.Backend = new FailingBackend(new UnauthorizedAccessException());

        await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("a", "x"));
    }

    [Fact]
    public async Task WriteTextAsync_CancelledToken_ThrowsAndStoresNothing()
    {
        var storage = NewStorage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => storage.WriteTextAsync("a", "x", cts.Token));

        (await storage.ExistsAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task EveryOperation_CancelledToken_ThrowsOperationCanceled()
    {
        var storage = NewStorage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => storage.ReadTextAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.ReadBytesAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.OpenReadAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.OpenWriteAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.ExistsAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.DeleteAsync("a", cts.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => storage.ListAsync("", cts.Token));
    }

    [Fact]
    public void Backend_SetToNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() => NewStorage().Backend = null!);
    }

    [Fact]
    public async Task Backend_Replaced_UsesNewBackend()
    {
        var storage = NewStorage();
        await storage.WriteTextAsync("a", "x");

        storage.Backend = new InMemoryStorageBackend();

        (await storage.ExistsAsync("a")).ShouldBeFalse();
    }

    [Fact]
    public async Task Storage_TwoEngines_AreIndependent()
    {
        var first = NewStorage();
        var second = NewStorage();

        await first.WriteTextAsync("a", "x");

        (await second.ExistsAsync("a")).ShouldBeFalse();
    }
}
