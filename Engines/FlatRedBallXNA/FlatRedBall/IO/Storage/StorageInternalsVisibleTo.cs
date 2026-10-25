using System.Runtime.CompilerServices;

// The storage backends' test seams (GameStorage's constructor, FileStorageBackend.BeforeReplace,
// FlatRedBallServices.ConfigureStorage) are internal; the engine unit tests need them.
[assembly: InternalsVisibleTo("EngineUnitTests")]
