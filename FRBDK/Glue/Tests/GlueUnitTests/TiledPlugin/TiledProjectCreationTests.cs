using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;
using TiledPlugin.Managers;
using TMXGlueLib;

// Namespace is "TiledPluginTests", not "TiledPlugin": the real Tiled plugin project's root namespace is
// the bare (non-nested) "TiledPlugin" (see TiledPlugin.csproj), so nesting a same-named namespace under
// GlueUnitTests would shadow it for every file under GlueUnitTests.* - same reasoning as
// GlueUnitTests.GumPluginTests.
namespace GlueUnitTests.TiledPluginTests;

// Follow-up to GitHub issue #1894 (reopened): WizardProjectLogic's Tiled add-on steps (~lines 422-483) all
// go through PluginManager.CallPluginMethod("Tiled Plugin", ...), which silently no-ops in a test host (no
// plugin registered with PluginManager) - the same false-coverage risk PR #1901/#1902 fixed for the
// Collision and Gum plugins.
//
// All five call sites (SaveFileWithVisuals x2, SaveTilesetFilesToDisk, AddStandardTilesetOnCurrentFile,
// AddGameplayLayerToCurrentFile, AddCollisionBorderToCurrentFile) are thin forwarders on
// MainTiledPluginClass to public methods on TmxCreationManager.Self - same "thin forwarder to
// directly-callable logic" shape as MainCollisionPlugin.FixNamedObjectCollisionType and
// MainGumPlugin.CreateGumProjectWithForms/NoForms. TmxCreationManager itself only depends on
// GlueState.Self.ContentDirectory / GlueCommands.Self.FileCommands / GlueCommands.Self.GetAbsoluteFilePath
// and plain file I/O (TiledMapSave, embedded-resource copies) - no MessageBox.Show anywhere in this call
// chain (verified by inspection of TmxCreationManager.cs and TMXGlueLib) and no live-window dependency.
// These tests call TmxCreationManager.Self's methods directly, bypassing both MainTiledPluginClass and
// PluginManager.CallPluginMethod entirely - real production logic, not a fake.
//
// AddStandardTilesetOnCurrentFile/AddGameplayLayerToCurrentFile/AddCollisionBorderToCurrentFile add one
// more layer of indirection in production (they read GlueState.Self.CurrentReferencedFileSave and check
// IsTmx(rfs.GetAssetTypeInfo()) before forwarding to TmxCreationManager) - that guard is a trivial
// extension check, not exercised here, same judgment call as the WizardProjectLogic.HandleAddPlayerInstance
// vs. CollisionRelationshipViewModelController.TryFixSourceClassType split in PR #1901.
[Collection(nameof(TaskManagerSequentialCollection))]
public class TiledProjectCreationTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly string _originalRelativeDirectory;
    private readonly string _tempProjectDirectory;

    public TiledProjectCreationTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalRelativeDirectory = FileManager.RelativeDirectory;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        FileManager.RelativeDirectory = _tempProjectDirectory + "\\";
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        FileManager.RelativeDirectory = _originalRelativeDirectory;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public void SaveTmxWithVisuals_ShouldWriteVisualTmxAndZoriaTileset_ForTopDownLevel()
    {
        // Mirrors WizardProjectLogic's PlayerControlType == GameType.TopDown branch (~line 422), which
        // calls SaveFileWithVisuals with a "OverworldTopDown*" level name.
        var newFile = new ReferencedFileSave { Name = "Levels/TestLevel/TestLevelMap.tmx" };

        TmxCreationManager.Self.SaveTmxWithVisuals(newFile, "OverworldTopDownA");

        var tmxPath = Path.Combine(_tempProjectDirectory, "Levels", "TestLevel", "TestLevelMap.tmx");
        File.Exists(tmxPath).ShouldBeTrue();

        // Real side effect: the TopDown-specific Zoria tileset (image + tsx) was copied from the plugin's
        // embedded resources to disk, alongside the always-copied standard tileset.
        File.Exists(Path.Combine(_tempProjectDirectory, "ZoriaOverworld.tsx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempProjectDirectory, "ZoriaOverworld.png")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempProjectDirectory, "StandardTileset.tsx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempProjectDirectory, "StandardTilesetIcons.png")).ShouldBeTrue();
        // The Platformer-only FrbVisualTiles tileset should NOT have been copied - proves the branch
        // actually selected Zoria, not just "copied everything".
        File.Exists(Path.Combine(_tempProjectDirectory, "FrbVisualTiles.tsx")).ShouldBeFalse();

        // Real side effect: the saved TMX is a real, loadable Tiled map (not just bytes) whose tileset
        // references were rewritten from the embedded template's placeholder paths to point at the newly
        // copied files, relative to the new TMX's own directory.
        var reloaded = TiledMapSave.FromFile(tmxPath);
        reloaded.Tilesets.ShouldContain(t => t.Source.EndsWith("ZoriaOverworld.tsx"));
        reloaded.Tilesets.ShouldContain(t => t.Source.EndsWith("StandardTileset.tsx"));
    }

    [Fact]
    public void SaveTmxWithVisuals_ShouldWriteVisualTmxAndFrbTileset_ForPlatformerLevel()
    {
        // Mirrors WizardProjectLogic's platformer (else) branch (~line 428).
        var newFile = new ReferencedFileSave { Name = "PlatformerMap.tmx" };

        TmxCreationManager.Self.SaveTmxWithVisuals(newFile, "OverworldPlatformerA");

        var tmxPath = Path.Combine(_tempProjectDirectory, "PlatformerMap.tmx");
        File.Exists(tmxPath).ShouldBeTrue();

        File.Exists(Path.Combine(_tempProjectDirectory, "FrbVisualTiles.tsx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempProjectDirectory, "FrbVisualTiles.png")).ShouldBeTrue();
        // The TopDown-only Zoria tileset should NOT have been copied for a platformer level.
        File.Exists(Path.Combine(_tempProjectDirectory, "ZoriaOverworld.tsx")).ShouldBeFalse();

        var reloaded = TiledMapSave.FromFile(tmxPath);
        reloaded.Tilesets.ShouldContain(t => t.Source.EndsWith("FrbVisualTiles.tsx"));
    }

    [Fact]
    public void SaveTilesetFilesToDisk_ShouldWriteStandardTilesetAssetsToContentDirectory()
    {
        // Mirrors WizardProjectLogic's "premade level" branch (~line 449).
        TmxCreationManager.Self.SaveTilesetFilesToDisk();

        var tsxPath = Path.Combine(_tempProjectDirectory, "StandardTileset.tsx");
        var pngPath = Path.Combine(_tempProjectDirectory, "StandardTilesetIcons.png");

        File.Exists(tsxPath).ShouldBeTrue();
        File.Exists(pngPath).ShouldBeTrue();
        // Real embedded-resource bytes, not empty placeholder files.
        new FileInfo(tsxPath).Length.ShouldBeGreaterThan(0);
        new FileInfo(pngPath).Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void IncludeDefaultTilesetOn_ShouldAppendStandardTilesetEntryAndWriteAssetsToDisk()
    {
        // Mirrors WizardProjectLogic's IncludStandardTilesetInLevels branch, which calls
        // AddStandardTilesetOnCurrentFile (~line 472) - forwards to IncludeDefaultTilesetOn.
        var newFile = SaveSeedTmx("SeedMap.tmx", width: 4, height: 4, tilesetSource: "Existing.tsx");
        var tmxPath = Path.Combine(_tempProjectDirectory, "SeedMap.tmx");
        var tilesetCountBefore = LoadTmxForVerification(tmxPath).Tilesets.Count;

        TmxCreationManager.Self.IncludeDefaultTilesetOn(newFile);

        File.Exists(Path.Combine(_tempProjectDirectory, "StandardTileset.tsx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempProjectDirectory, "StandardTilesetIcons.png")).ShouldBeTrue();

        var reloaded = LoadTmxForVerification(tmxPath);
        reloaded.Tilesets.Count.ShouldBe(tilesetCountBefore + 1);
        reloaded.Tilesets.Last().Source.ShouldEndWith("StandardTileset.tsx");
    }

    [Fact]
    public void IncludeGameplayLayerOn_ShouldReplaceAllLayersWithSingleGameplayLayerCopyingFirstLayerData()
    {
        // Mirrors WizardProjectLogic's IncludeGameplayLayerInLevels branch, which calls
        // AddGameplayLayerToCurrentFile (~line 477) - forwards to IncludeGameplayLayerOn.
        var newFile = SaveSeedTmx("SeedMap.tmx", width: 4, height: 4, tilesetSource: "Existing.tsx",
            firstLayerName: "Sky", secondLayerName: "Clouds");
        var tmxPath = Path.Combine(_tempProjectDirectory, "SeedMap.tmx");
        var originalFirstLayerTiles = LoadTmxForVerification(tmxPath).Layers[0].data[0].tiles;

        TmxCreationManager.Self.IncludeGameplayLayerOn(newFile);

        var reloaded = LoadTmxForVerification(tmxPath);
        // Real side effect: every prior layer (both "Sky" and "Clouds") was removed and replaced with a
        // single new layer, proving this isn't just an additive append.
        reloaded.MapLayers.Count.ShouldBe(1);
        reloaded.Layers[0].Name.ShouldBe("GameplayLayer");
        reloaded.Layers[0].width.ShouldBe(4);
        reloaded.Layers[0].height.ShouldBe(4);
        reloaded.Layers[0].data[0].tiles.ShouldBe(originalFirstLayerTiles);
    }

    [Fact]
    public void AddCollisionBorderOn_ShouldPaintBorderTilesOnFirstLayerWithinHardcoded32x32Region()
    {
        // Mirrors WizardProjectLogic's IncludeCollisionBorderInLevels branch, which calls
        // AddCollisionBorderToCurrentFile (~line 483) - forwards to AddCollisionBorderOn.
        //
        // AddCollisionBorderOn hardcodes a 32x32 loop regardless of the map's actual width/height (see
        // TmxCreationManager.cs) - this test seeds a 32x32 map to match that real (if map-size-agnostic)
        // production behavior, rather than papering over it.
        var newFile = SaveSeedTmx("SeedMap.tmx", width: 32, height: 32, tilesetSource: "Existing.tsx");
        var tmxPath = Path.Combine(_tempProjectDirectory, "SeedMap.tmx");

        TmxCreationManager.Self.AddCollisionBorderOn(newFile);

        var reloaded = LoadTmxForVerification(tmxPath);
        reloaded.Tilesets[0].Firstgid.ShouldBe(1u);

        var tiles = reloaded.Layers[0].data[0].tiles;
        // Border cells (edges of the hardcoded 32x32 region) were painted...
        tiles[0].ShouldBe(1u); // x=0, y=0
        tiles[31].ShouldBe(1u); // x=31, y=0
        tiles[31 * 32].ShouldBe(1u); // x=0, y=31
        tiles[31 + 31 * 32].ShouldBe(1u); // x=31, y=31
        tiles[5].ShouldBe(1u); // x=5, y=0 (top edge)
        // ...interior cells were left untouched.
        tiles[5 + 5 * 32].ShouldBe(0u); // x=5, y=5
    }

    /// <summary>
    /// Builds and saves a real, minimal Tiled map to the current temp content directory - not an embedded
    /// production template, but real TMXGlueLib types serialized through the same TiledMapSave.Save/FromFile
    /// path production uses. Needed because IncludeDefaultTilesetOn/IncludeGameplayLayerOn/AddCollisionBorderOn
    /// all load an existing TMX off disk (TiledMapSave.FromFile) rather than creating one - in production
    /// they run against a TMX already placed on disk by an earlier "add new file" step.
    /// </summary>
    private ReferencedFileSave SaveSeedTmx(string relativeName, int width, int height, string tilesetSource,
        string firstLayerName = "Layer1", string? secondLayerName = null)
    {
        var wasLoadingSource = Tileset.ShouldLoadValuesFromSource;
        Tileset.ShouldLoadValuesFromSource = false;
        try
        {
            var tiledMapSave = new TiledMapSave
            {
                Tilesets = new List<Tileset> { new Tileset { Source = tilesetSource } },
                MapLayers = new List<AbstractMapLayer> { CreateLayer(firstLayerName, width, height) }
            };

            if (secondLayerName != null)
            {
                tiledMapSave.MapLayers.Add(CreateLayer(secondLayerName, width, height));
            }

            var absolutePath = Path.Combine(_tempProjectDirectory, relativeName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            tiledMapSave.Save(absolutePath);

            return new ReferencedFileSave { Name = relativeName };
        }
        finally
        {
            Tileset.ShouldLoadValuesFromSource = wasLoadingSource;
        }
    }

    /// <summary>
    /// Reloads a TMX from disk for assertions, the same way TmxCreationManager's own methods do internally
    /// (they all wrap TiledMapSave.FromFile with Tileset.ShouldLoadValuesFromSource = false) - our seed
    /// tilesets reference a "tsx" file that doesn't really exist on disk (SaveSeedTmx's tilesetSource is a
    /// placeholder, not a real embedded tileset), so a real-loading reload would fail; production code
    /// tolerates this the same way here.
    /// </summary>
    private static TiledMapSave LoadTmxForVerification(string tmxPath)
    {
        var wasLoadingSource = Tileset.ShouldLoadValuesFromSource;
        Tileset.ShouldLoadValuesFromSource = false;
        try
        {
            return TiledMapSave.FromFile(tmxPath);
        }
        finally
        {
            Tileset.ShouldLoadValuesFromSource = wasLoadingSource;
        }
    }

    private static MapLayer CreateLayer(string name, int width, int height)
    {
        var layer = new MapLayer { Name = name, width = width, height = height };
        var tileCount = width * height;
        var data = new mapLayerData
        {
            encoding = "csv",
            Value = string.Join(",", Enumerable.Repeat("0", tileCount))
        };
        layer.data = new[] { data };
        return layer;
    }
}
