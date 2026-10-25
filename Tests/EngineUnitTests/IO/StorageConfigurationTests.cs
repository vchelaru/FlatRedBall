using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.IO;
using Shouldly;

namespace EngineUnitTests.IO;

// What InitializeFlatRedBall does with FlatRedBallServices.StorageName / StorageRootDirectory, via the
// internal ConfigureStorage it calls (a real InitializeFlatRedBall needs a window). FlatRedBallServices.Storage
// is process-wide, so these share the non-parallel collection and put everything back in Dispose.
[Collection(nameof(EngineStatefulTestCollection))]
public class StorageConfigurationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "frb-storage-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        FlatRedBallServices.StorageName = null;
        FlatRedBallServices.StorageRootDirectory = null;
        FlatRedBallServices.StorageBackendFactory = null;
        FlatRedBallServices.Storage.Backend = new InMemoryStorageBackend();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Storage_BeforeInitialize_IsInMemoryAndNotPersistent()
    {
        // Every test in this class restores an in-memory backend in Dispose, so this holds in any order.
        FlatRedBallServices.Storage.Backend.ShouldBeOfType<InMemoryStorageBackend>();
        FlatRedBallServices.Storage.IsPersistent.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigureStorage_NoName_FirstCallThrowsNamingTheSetting()
    {
        FlatRedBallServices.StorageRootDirectory = _root;

        FlatRedBallServices.ConfigureStorage();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => FlatRedBallServices.Storage.ExistsAsync("a"));
        ex.Message.ShouldContain("StorageName");
    }

    [Fact]
    public async Task ConfigureStorage_NoName_EveryOperationThrowsInvalidOperation()
    {
        FlatRedBallServices.StorageRootDirectory = _root;
        FlatRedBallServices.ConfigureStorage();
        var storage = FlatRedBallServices.Storage;

        await Should.ThrowAsync<InvalidOperationException>(() => storage.ReadTextAsync("a"));
        await Should.ThrowAsync<InvalidOperationException>(() => storage.WriteTextAsync("a", "x"));
        await Should.ThrowAsync<InvalidOperationException>(() => storage.OpenReadAsync("a"));
        await Should.ThrowAsync<InvalidOperationException>(() => storage.DeleteAsync("a"));
        await Should.ThrowAsync<InvalidOperationException>(() => storage.ListAsync());
        Directory.Exists(_root).ShouldBeFalse();
    }

    [Fact]
    public void ConfigureStorage_NoName_IsNotPersistentAndDoesNotThrow()
    {
        FlatRedBallServices.ConfigureStorage();

        FlatRedBallServices.Storage.IsPersistent.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigureStorage_NameAndRootOverride_StoresUnderTheOverride()
    {
        FlatRedBallServices.StorageName = "my-game";
        FlatRedBallServices.StorageRootDirectory = _root;
        FlatRedBallServices.ConfigureStorage();

        await FlatRedBallServices.Storage.WriteTextAsync("saves/one.json", "x");

        FlatRedBallServices.Storage.IsPersistent.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_root, "saves", "one.json")).ShouldBe("x");
    }

    [Fact]
    public void ConfigureStorage_NameOnly_UsesDefaultRootWithoutTouchingDisk()
    {
        FlatRedBallServices.StorageName = "frb-test-never-created";

        FlatRedBallServices.ConfigureStorage();

        FlatRedBallServices.Storage.IsPersistent.ShouldBeTrue();
        Directory.Exists(FileStorageBackend.DefaultRoot("frb-test-never-created")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("bad:name")]
    [InlineData(" leading")]
    [InlineData("trailing.")]
    [InlineData("con")]
    public void ConfigureStorage_InvalidName_Throws(string name)
    {
        FlatRedBallServices.StorageName = name;

        Should.Throw<ArgumentException>(() => FlatRedBallServices.ConfigureStorage());
    }

    [Theory]
    [InlineData("MyGame")]
    [InlineData("My Game 2")]
    [InlineData("my-game_v1.0")]
    public void ConfigureStorage_ValidName_Succeeds(string name)
    {
        FlatRedBallServices.StorageName = name;
        FlatRedBallServices.StorageRootDirectory = _root;

        FlatRedBallServices.ConfigureStorage();

        FlatRedBallServices.Storage.IsPersistent.ShouldBeTrue();
    }

    [Fact]
    public void ConfigureStorage_FactoryProvided_UsesItWithTheStorageName()
    {
        string? receivedName = null;
        var backend = new InMemoryStorageBackend();
        FlatRedBallServices.StorageBackendFactory = name => { receivedName = name; return backend; };
        FlatRedBallServices.StorageName = "my-game";

        FlatRedBallServices.ConfigureStorage();

        receivedName.ShouldBe("my-game");
        FlatRedBallServices.Storage.Backend.ShouldBeSameAs(backend);
    }

    [Fact]
    public async Task ConfigureStorage_FactoryProvidedButNoName_StillThrowsOnFirstCall()
    {
        FlatRedBallServices.StorageBackendFactory = _ => new InMemoryStorageBackend();

        FlatRedBallServices.ConfigureStorage();

        await Should.ThrowAsync<InvalidOperationException>(() => FlatRedBallServices.Storage.ExistsAsync("a"));
    }
}
