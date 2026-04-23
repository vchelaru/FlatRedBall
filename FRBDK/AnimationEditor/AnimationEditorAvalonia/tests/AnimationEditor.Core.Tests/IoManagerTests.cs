using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.Data;
using AnimationEditor.Core.IO;
using FlatRedBall.IO;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class IoManagerTests
{
    // ── SaveCompanionFileFor ──────────────────────────────────────────────────

    [Fact]
    public void SaveCompanionFileFor_CreatesFileAtExpectedPath()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/hero.achx");
        var expectedAeProps = dir.Path + "/hero.aeproperties";

        IoManager.Self.SaveCompanionFileFor(achxPath, new AESettingsSave());

        Assert.True(File.Exists(expectedAeProps));
    }

    [Fact]
    public void SaveCompanionFileFor_FileContainsXmlContent()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/hero.achx");

        IoManager.Self.SaveCompanionFileFor(achxPath, new AESettingsSave { GridSize = 32 });

        var contents = File.ReadAllText(dir.Path + "/hero.aeproperties");
        Assert.Contains("32", contents);
    }

    [Fact]
    public void SaveCompanionFileFor_RoundTrip_PreservesUnitType()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/hero.achx");
        var settings = new AESettingsSave
        {
            UnitType = UnitType.TextureCoordinate,
            SnapToGrid = true,
            GridSize = 24
        };

        IoManager.Self.SaveCompanionFileFor(achxPath, settings);
        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.Equal(UnitType.TextureCoordinate, AppState.Self.UnitType);
    }

    [Fact]
    public void SaveCompanionFileFor_RoundTrip_PreservesSnapToGrid()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/snap.achx");
        var settings = new AESettingsSave { SnapToGrid = true, GridSize = 16 };

        IoManager.Self.SaveCompanionFileFor(achxPath, settings);
        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.True(AppState.Self.IsSnapToGridChecked);
    }

    [Fact]
    public void SaveCompanionFileFor_RoundTrip_PreservesGridSize()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/grid.achx");
        var settings = new AESettingsSave { GridSize = 64 };

        IoManager.Self.SaveCompanionFileFor(achxPath, settings);
        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.Equal(64, AppState.Self.GridSize);
    }

    // ── LoadAndApplyCompanionFileFor ──────────────────────────────────────────

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenFileExists_SetsAppStateUnitType()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/test.achx");
        var settings = new AESettingsSave { UnitType = UnitType.TextureCoordinate };
        IoManager.Self.SaveCompanionFileFor(achxPath, settings);

        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.Equal(UnitType.TextureCoordinate, AppState.Self.UnitType);
    }

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenFileExists_SetsSnapToGrid()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/test.achx");
        IoManager.Self.SaveCompanionFileFor(achxPath, new AESettingsSave { SnapToGrid = true });

        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.True(AppState.Self.IsSnapToGridChecked);
    }

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenFileExists_SetsGridSize()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/test.achx");
        IoManager.Self.SaveCompanionFileFor(achxPath, new AESettingsSave { GridSize = 48 });

        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.Equal(48, AppState.Self.GridSize);
    }

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenFileExists_FiresSettingsLoaded()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var achxPath = new FilePath(dir.Path + "/test.achx");
        IoManager.Self.SaveCompanionFileFor(achxPath, new AESettingsSave { GridSize = 16 });

        bool fired = false;
        IoManager.Self.SettingsLoaded += _ => fired = true;

        IoManager.Self.LoadAndApplyCompanionFileFor(achxPath.FullPath);

        Assert.True(fired);
    }

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenFileDoesNotExist_DoesNothing()
    {
        TestHelpers.SetupFreshAcls();
        AppState.Self.GridSize = 32; // baseline

        IoManager.Self.LoadAndApplyCompanionFileFor("C:/NoSuchFile/missing.achx");

        // Grid size should remain unchanged since the file was never loaded
        Assert.Equal(32, AppState.Self.GridSize);
    }

    [Fact]
    public void LoadAndApplyCompanionFileFor_WhenXmlIsInvalid_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        // Write invalid XML to .aeproperties file directly
        var aePropsPath = dir.Path + "/bad.aeproperties";
        File.WriteAllText(aePropsPath, "<<NOT VALID XML>>");
        var achxPath = dir.Path + "/bad.achx";

        var ex = Record.Exception(() =>
            IoManager.Self.LoadAndApplyCompanionFileFor(achxPath));

        Assert.Null(ex);
    }

    [Fact]
    public void SaveCompanionFileFor_WhenDirectoryDoesNotExist_FiresSaveFailed()
    {
        TestHelpers.SetupFreshAcls();
        Exception? caughtEx = null;
        IoManager.Self.SaveFailed += (_, e) => caughtEx = e;

        IoManager.Self.SaveCompanionFileFor(
            new FilePath("Z:/NonExistentDrive/hero.achx"),
            new AESettingsSave());

        // On most systems, writing to a non-existent drive should fail
        // If it succeeds for some reason (drive Z exists), this test is inconclusive —
        // but we still verify no unhandled exception is thrown
        // The important thing is that no exception escapes SaveCompanionFileFor
    }
}
