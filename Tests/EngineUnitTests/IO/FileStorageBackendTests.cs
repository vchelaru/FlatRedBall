using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace EngineUnitTests.IO;

// Runs the shared contract against real files in a throwaway folder.
public class FileStorageContractTests : StorageBackendContractTests
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "frb-storage-" + Guid.NewGuid().ToString("N"));

    protected override IStorageBackend CreateBackend() =>
        new FileStorageBackend(Path.Combine(_root, Guid.NewGuid().ToString("N")));

    public override void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}

public class FileStorageBackendTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "frb-storage-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void IsPersistent_IsTrue()
    {
        new FileStorageBackend(_root).IsPersistent.ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_NestedKey_CreatesDirectoriesOnDemand()
    {
        var backend = new FileStorageBackend(_root);
        Directory.Exists(_root).ShouldBeFalse(); // constructing touches nothing

        await backend.WriteAsync("saves/slots/one.json", Bytes("x"), default);

        File.ReadAllText(Path.Combine(_root, "saves", "slots", "one.json")).ShouldBe("x");
    }

    [Fact]
    public async Task ReadAsync_ReturnedArray_CanBeMutatedWithoutChangingFile()
    {
        var backend = new FileStorageBackend(_root);
        await backend.WriteAsync("a", new byte[] { 1, 2 }, default);

        (await backend.ReadAsync("a", default))![0] = 9;

        (await backend.ReadAsync("a", default)).ShouldBe(new byte[] { 1, 2 });
    }

    [Fact]
    public async Task ReadAsync_MissingKeyOrMissingRoot_ReturnsNull()
    {
        var backend = new FileStorageBackend(_root);

        (await backend.ReadAsync("nothing/here", default)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_IgnoresFilesThatAreNotValidKeys()
    {
        var backend = new FileStorageBackend(_root);
        await backend.WriteAsync("saves/a", Bytes("x"), default);
        File.WriteAllText(Path.Combine(_root, "saves", "Foreign File.txt"), "not ours");

        (await backend.ListAsync("", default)).ToArray().ShouldBe(new[] { "saves/a" });
    }

    [Fact]
    public async Task WriteAsync_AfterSuccess_LeavesNoTempFiles()
    {
        var backend = new FileStorageBackend(_root);

        await backend.WriteAsync("a", Bytes("x"), default);
        await backend.WriteAsync("a", Bytes("y"), default);

        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToArray().ShouldBe(new[] { "a" });
    }

    [Fact]
    public async Task WriteAsync_FailsBeforeReplace_OldValueSurvivesAndTempIsRemoved()
    {
        var backend = new FileStorageBackend(_root);
        await backend.WriteAsync("a", Bytes("old"), default);
        backend.BeforeReplace = () => throw new IOException("disk full");

        await Should.ThrowAsync<IOException>(() => backend.WriteAsync("a", Bytes("new"), default));

        File.ReadAllText(Path.Combine(_root, "a")).ShouldBe("old");
        Directory.GetFiles(_root).Length.ShouldBe(1);
    }

    [Fact]
    public async Task WriteAsync_CancelledBeforeReplace_OldValueSurvivesAndTempIsRemoved()
    {
        var backend = new FileStorageBackend(_root);
        await backend.WriteAsync("a", Bytes("old"), default);
        using var cts = new CancellationTokenSource();
        backend.BeforeReplace = cts.Cancel; // cancelled after the temp file is fully written

        await Should.ThrowAsync<OperationCanceledException>(() => backend.WriteAsync("a", Bytes("new"), cts.Token));

        File.ReadAllText(Path.Combine(_root, "a")).ShouldBe("old");
        Directory.GetFiles(_root).Length.ShouldBe(1);
    }

    [Fact]
    public async Task WriteAsync_CancelledNewKey_StoresNothing()
    {
        var backend = new FileStorageBackend(_root);
        using var cts = new CancellationTokenSource();
        backend.BeforeReplace = cts.Cancel;

        await Should.ThrowAsync<OperationCanceledException>(() => backend.WriteAsync("a", Bytes("new"), cts.Token));

        (await backend.ExistsAsync("a", default)).ShouldBeFalse();
    }

    [Fact]
    public async Task WriteAsync_OldOrphanedTempFile_IsCleanedUp()
    {
        var backend = new FileStorageBackend(_root);
        await backend.WriteAsync("a", Bytes("x"), default);
        string orphan = Path.Combine(_root, "a.deadbeef.~tmp");
        File.WriteAllText(orphan, "left by a crash");
        File.SetLastWriteTimeUtc(orphan, DateTime.UtcNow.AddDays(-1));
        string recent = Path.Combine(_root, "b.cafe.~tmp");
        File.WriteAllText(recent, "maybe another process mid-write");

        await new FileStorageBackend(_root).WriteAsync("c", Bytes("x"), default);

        File.Exists(orphan).ShouldBeFalse();
        File.Exists(recent).ShouldBeTrue();
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/../../escape")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/windows/x")]
    [InlineData("a\\..\\b")]
    public async Task EveryOperation_TraversalKey_ThrowsAndTouchesNothingOutsideRoot(string key)
    {
        string outside = Path.Combine(Path.GetDirectoryName(_root)!, "escape");
        var backend = new FileStorageBackend(_root);

        await Should.ThrowAsync<ArgumentException>(() => backend.WriteAsync(key, Bytes("x"), default));
        await Should.ThrowAsync<ArgumentException>(() => backend.ReadAsync(key, default));
        await Should.ThrowAsync<ArgumentException>(() => backend.DeleteAsync(key, default));
        await Should.ThrowAsync<ArgumentException>(() => backend.ExistsAsync(key, default));

        File.Exists(outside).ShouldBeFalse();
    }

    [Fact]
    public async Task WriteTextAsync_KeyConflictsWithExistingFolder_ThrowsStorageException()
    {
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = new FileStorageBackend(_root);
        await storage.WriteTextAsync("a/b", "x");

        await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("a", "y"));
        (await storage.ReadTextAsync("a/b")).ShouldBe("x");
        Directory.GetFiles(_root, "*.~tmp", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void DefaultRoot_IsAppFolderUnderLocalApplicationData()
    {
        FileStorageBackend.DefaultRoot("my-game").ShouldBe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "my-game"));
    }

    [Fact]
    public void Constructor_EmptyRoot_Throws()
    {
        Should.Throw<ArgumentException>(() => new FileStorageBackend(""));
    }
}
