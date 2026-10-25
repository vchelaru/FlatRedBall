#nullable enable
using System;
using FlatRedBall.IO;

namespace FlatRedBall
{
    public static partial class FlatRedBallServices
    {
        /// <summary>
        /// Save and load game data by key, async, same code on every platform. Memory-only until
        /// <see cref="InitializeFlatRedBall(Microsoft.Xna.Framework.Game, Microsoft.Xna.Framework.GraphicsDeviceManager)"/>
        /// runs and picks a persistent backend (or until a persistent <see cref="GameStorage.Backend"/> is assigned).
        /// Set <see cref="StorageName"/> before initializing; without it the first storage call throws.
        /// </summary>
        /// <example>
        /// <code>
        /// // Game1 constructor, before InitializeFlatRedBall:
        /// FlatRedBallServices.StorageName = "MyGame";
        ///
        /// // anywhere after initialization:
        /// await FlatRedBallServices.Storage.WriteTextAsync("saves/slot1.json", json);
        /// string? loaded = await FlatRedBallServices.Storage.ReadTextAsync("saves/slot1.json");
        /// </code>
        /// </example>
        public static GameStorage Storage { get; } = new GameStorage(new InMemoryStorageBackend());

        /// <summary>
        /// A stable name for this game's saved data, required to use <see cref="Storage"/>: it names the save
        /// folder on desktop and the key namespace in browser storage on web. Leaving it unset makes the first
        /// storage call throw <see cref="InvalidOperationException"/>, on every platform. Letters, digits, space,
        /// '-', '_', '.'; at most 64 characters. Renaming it later orphans existing saves.
        /// </summary>
        /// <remarks>
        /// Read when <c>InitializeFlatRedBall</c> runs, so set it before that call. Initialize replaces
        /// <see cref="Storage"/>'s backend, so assign a custom <see cref="GameStorage.Backend"/> after
        /// Initialize, not before.
        /// </remarks>
        public static string? StorageName { get; set; }

        /// <summary>
        /// Desktop only: the exact folder to keep saves in, instead of the default per-user location
        /// (<see cref="FileStorageBackend.DefaultRoot"/>). <see cref="StorageName"/> is still required
        /// and is not appended. Ignored on web. Set it before <c>InitializeFlatRedBall</c>.
        /// </summary>
        public static string? StorageRootDirectory { get; set; }

        /// <summary>
        /// For host integrations: builds the storage backend from <see cref="StorageName"/> when
        /// <c>InitializeFlatRedBall</c> runs, replacing the platform default (files on desktop, browser
        /// <c>localStorage</c> on web). Games do not need to set it. Set it before <c>InitializeFlatRedBall</c>.
        /// </summary>
        public static Func<string, IStorageBackend>? StorageBackendFactory { get; set; }

        // Picks the storage backend for a real boot. Before this runs Storage stays in-memory; only
        // FinishInitialization calls it.
        internal static void ConfigureStorage()
        {
            string? name = StorageName;
            if (name is null)
            {
                Storage.Backend = new UnconfiguredStorageBackend();
                return;
            }

            StorageKey.ValidateStorageName(name, nameof(StorageName));
            var factory = StorageBackendFactory;
            if (factory != null)
            {
                Storage.Backend = factory(name);
                return;
            }

#if WEB
            // A browser has no usable file system; localStorage survives reloads. If the browser blocks it,
            // the backend falls back to memory and Storage.IsPersistent is false.
            Storage.Backend = new BrowserStorageBackend(name, new LocalStorageApi());
#else
            Storage.Backend = new FileStorageBackend(StorageRootDirectory ?? FileStorageBackend.DefaultRoot(name));
#endif
        }
    }
}
