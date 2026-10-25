using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace EngineUnitTests.IO;

public class InMemoryStorageContractTests : StorageBackendContractTests
{
    protected override IStorageBackend CreateBackend() => new InMemoryStorageBackend();

    [Fact]
    public void NewEngine_DefaultStorage_IsInMemoryAndNotPersistent()
    {
        var storage = new GameStorage(new InMemoryStorageBackend());

        storage.Backend.ShouldBeOfType<InMemoryStorageBackend>();
        storage.IsPersistent.ShouldBeFalse();
    }
}
