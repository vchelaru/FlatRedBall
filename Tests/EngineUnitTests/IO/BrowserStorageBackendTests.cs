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

// An in-memory stand-in for window.localStorage: string-only, insertion-ordered like a real one,
// with an optional size cap and an "unusable" switch for the blocked-storage case.
internal sealed class FakeBrowserStorage : IBrowserStorageApi
{
    private readonly List<KeyValuePair<string, string>> _items = new();

    public int? QuotaChars { get; set; }
    public bool Blocked { get; set; }
    public int SetCalls { get; private set; }

    public IReadOnlyList<KeyValuePair<string, string>> Items => _items;

    public int Length { get { ThrowIfBlocked(); return _items.Count; } }

    public string? Key(int index) { ThrowIfBlocked(); return index < _items.Count ? _items[index].Key : null; }

    public string? GetItem(string key)
    {
        ThrowIfBlocked();
        int i = _items.FindIndex(p => p.Key == key);
        return i < 0 ? null : _items[i].Value;
    }

    public void SetItem(string key, string value)
    {
        ThrowIfBlocked();
        SetCalls++;
        int i = _items.FindIndex(p => p.Key == key);
        int used = _items.Sum(p => p.Key.Length + p.Value.Length) - (i < 0 ? 0 : _items[i].Key.Length + _items[i].Value.Length);
        if (QuotaChars is int quota && used + key.Length + value.Length > quota)
            throw new IOException("QuotaExceededError");
        if (i < 0)
            _items.Add(new KeyValuePair<string, string>(key, value));
        else
            _items[i] = new KeyValuePair<string, string>(key, value);
    }

    public void RemoveItem(string key)
    {
        ThrowIfBlocked();
        _items.RemoveAll(p => p.Key == key);
    }

    private void ThrowIfBlocked()
    {
        if (Blocked)
            throw new IOException("SecurityError: localStorage is blocked");
    }
}

// The shared contract, against browser storage backed by the fake.
public class BrowserStorageContractTests : StorageBackendContractTests
{
    protected override IStorageBackend CreateBackend() => new BrowserStorageBackend("contract-game", new FakeBrowserStorage());
}

public class BrowserStorageBackendTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void IsPersistent_StorageUsable_IsTrue()
    {
        new BrowserStorageBackend("game", new FakeBrowserStorage()).IsPersistent.ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_BinaryData_IsStoredAsBase64AndRoundTrips()
    {
        var fake = new FakeBrowserStorage();
        var backend = new BrowserStorageBackend("game", fake);
        byte[] data = { 0xFF, 0xFE, 0x00, 0x80, 0xC3, 0x28 }; // not valid UTF-8

        await backend.WriteAsync("bin", data, default);

        fake.Items.Single(p => p.Key.EndsWith("/bin")).Value.ShouldBe(Convert.ToBase64String(data));
        (await backend.ReadAsync("bin", default)).ShouldBe(data);
    }

    [Fact]
    public async Task WriteAsync_EmptyData_RoundTripsAsEmptyNotMissing()
    {
        var backend = new BrowserStorageBackend("game", new FakeBrowserStorage());

        await backend.WriteAsync("empty", ReadOnlyMemory<byte>.Empty, default);

        (await backend.ReadAsync("empty", default)).ShouldBe(Array.Empty<byte>());
        (await backend.ExistsAsync("empty", default)).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_KeysAreNamespacedByStorageName()
    {
        var fake = new FakeBrowserStorage();
        var backend = new BrowserStorageBackend("my game", fake);

        await backend.WriteAsync("saves/one", Bytes("x"), default);

        fake.Items.Select(p => p.Key).ShouldBe(new[] { "frb2:my game/saves/one" });
    }

    [Fact]
    public async Task TwoGamesOnOneOrigin_DoNotSeeOrDeleteEachOthersKeys()
    {
        var fake = new FakeBrowserStorage();
        var first = new BrowserStorageBackend("game", fake);
        var second = new BrowserStorageBackend("game2", fake);
        await first.WriteAsync("save", Bytes("one"), default);
        await second.WriteAsync("save", Bytes("two"), default);
        await second.WriteAsync("other", Bytes("x"), default);

        (await first.ReadAsync("save", default)).ShouldBe(Bytes("one"));
        (await first.ListAsync("", default)).ToArray().ShouldBe(new[] { "save" });
        (await second.ListAsync("", default)).ToArray().ShouldBe(new[] { "other", "save" });

        await first.DeleteAsync("save", default);
        (await second.ReadAsync("save", default)).ShouldBe(Bytes("two"));
    }

    [Fact]
    public async Task ListAsync_IgnoresForeignAndMalformedEntries()
    {
        var fake = new FakeBrowserStorage();
        fake.SetItem("someone-elses-key", "x");
        fake.SetItem("game/looks-like-ours", "x"); // missing the frb2: namespace
        fake.SetItem("frb2:game/UPPERCASE", "x"); // ours, but not a valid key
        var backend = new BrowserStorageBackend("game", fake);
        await backend.WriteAsync("mine", Bytes("x"), default);

        (await backend.ListAsync("", default)).ToArray().ShouldBe(new[] { "mine" });
    }

    [Fact]
    public async Task ListAsync_Prefix_FiltersWithinTheGame()
    {
        var backend = new BrowserStorageBackend("game", new FakeBrowserStorage());
        await backend.WriteAsync("saves/b", Bytes("x"), default);
        await backend.WriteAsync("saves/a", Bytes("x"), default);
        await backend.WriteAsync("settings", Bytes("x"), default);

        (await backend.ListAsync("saves/", default)).ToArray().ShouldBe(new[] { "saves/a", "saves/b" });
    }

    [Fact]
    public async Task WriteTextAsync_QuotaExceeded_ThrowsStorageExceptionAndOldValueSurvives()
    {
        var fake = new FakeBrowserStorage();
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = new BrowserStorageBackend("game", fake);
        await storage.WriteTextAsync("save", "old");
        fake.QuotaChars = fake.Items.Sum(p => p.Key.Length + p.Value.Length) + 4;

        var ex = await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("save", new string('x', 1000)));

        ex.InnerException.ShouldBeOfType<IOException>();
        (await storage.ReadTextAsync("save")).ShouldBe("old");
    }

    [Fact]
    public async Task OpenWriteAsync_QuotaExceededOnCommit_ThrowsStorageExceptionAndOldValueSurvives()
    {
        var fake = new FakeBrowserStorage();
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = new BrowserStorageBackend("game", fake);
        await storage.WriteTextAsync("save", "old");
        fake.QuotaChars = fake.Items.Sum(p => p.Key.Length + p.Value.Length) + 4;
        await using var stream = await storage.OpenWriteAsync("save");
        await stream.WriteAsync(new byte[1000]);

        await Should.ThrowAsync<StorageException>(() => stream.CommitAsync());

        (await storage.ReadTextAsync("save")).ShouldBe("old");
    }

    [Fact]
    public async Task Constructor_StorageBlocked_FallsBackToMemoryAndIsNotPersistent()
    {
        var fake = new FakeBrowserStorage { Blocked = true };

        var backend = new BrowserStorageBackend("game", fake);

        backend.IsPersistent.ShouldBeFalse();
        await backend.WriteAsync("a", Bytes("x"), default);
        (await backend.ReadAsync("a", default)).ShouldBe(Bytes("x"));
        (await backend.ListAsync("", default)).ToArray().ShouldBe(new[] { "a" });
        (await backend.DeleteAsync("a", default)).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_QuotaZero_FallsBackToMemory()
    {
        // Old private-browsing modes allow reads but throw on every write.
        var fake = new FakeBrowserStorage { QuotaChars = 0 };

        new BrowserStorageBackend("game", fake).IsPersistent.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_Usable_LeavesNoProbeKeyBehind()
    {
        var fake = new FakeBrowserStorage();

        new BrowserStorageBackend("game", fake);

        fake.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task StorageBlockedAfterStartup_OperationsThrowStorageException()
    {
        var fake = new FakeBrowserStorage();
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = new BrowserStorageBackend("game", fake);
        fake.Blocked = true;

        await Should.ThrowAsync<StorageException>(() => storage.WriteTextAsync("a", "x"));
        await Should.ThrowAsync<StorageException>(() => storage.ReadTextAsync("a"));
        await Should.ThrowAsync<StorageException>(() => storage.ListAsync());
    }

    [Fact]
    public async Task ReadAsync_CorruptBase64_ThrowsStorageException()
    {
        var fake = new FakeBrowserStorage();
        var storage = new GameStorage(new InMemoryStorageBackend());
        storage.Backend = new BrowserStorageBackend("game", fake);
        fake.SetItem("frb2:game/bad", "not base64!!");

        await Should.ThrowAsync<StorageException>(() => storage.ReadTextAsync("bad"));
    }

    [Fact]
    public async Task WriteAsync_CancelledBeforeWrite_StoresNothing()
    {
        var fake = new FakeBrowserStorage();
        var backend = new BrowserStorageBackend("game", fake);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => backend.WriteAsync("a", Bytes("x"), cts.Token));

        fake.Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a/b")]
    [InlineData("a:b")]
    public void Constructor_InvalidStorageName_Throws(string name)
    {
        Should.Throw<ArgumentException>(() => new BrowserStorageBackend(name, new FakeBrowserStorage()));
    }
}
