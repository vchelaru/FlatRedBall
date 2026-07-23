using System;
using System.IO;
using EditorObjects.IoC;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Services;
using OfficialPlugins.CollisionPlugin;
using Glue;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// One-time, process-wide bootstrap that mirrors the handful of static/DI registrations Glue.exe's real
/// startup (Program.cs / MainGlueWindow.cs) performs before GlueCommands.Self/GlueState.Self are usable.
/// None of this is a fake or a mock - it is the same calls production makes, just run in-process for a
/// test host instead of a live WinForms app:
///  - <see cref="Builder"/> wires up the app's lightweight DI container (GluxCommands, ElementCommands,
///    etc. all resolve through it).
///  - The legacy <see cref="Container"/> (EditorObjects.IoC) is a second, older service locator that
///    several command classes (GenerateCodeCommands, VisualStudioProject, ...) still read from directly.
///  - <see cref="FlatRedBall.Glue.ProjectManager"/>.Initialize sets up CodeProjectHelper, used by any
///    code-file-adding path (e.g. AddScreen).
///  - <see cref="FileWatchManager"/>.Initialize sets up the self-save-suppression list that
///    IgnoreNextChangeOnFile writes to.
///  - <see cref="AvailableAssetTypes"/>.Initialize populates <see cref="AvailableAssetTypes.CommonAtis"/>
///    (Camera/Text/Sprite/Polygon/etc.) from the same Content/ContentTypes.csv that MainGlueWindow reads -
///    this is CSV-driven and needs no plugins, unlike the per-plugin ATIs below.
///  - <see cref="MainGlueWindow"/>.Self is given a <see cref="FakeMainGlueWindow"/> (only Program.cs ever
///    constructs the real WinForms window, which this test host never does) so any code path reading
///    MainGlueWindow.Self - PropertyGrid, HasErrorOccurred, Invoke/BeginInvoke, etc. - gets a harmless
///    object instead of NRE-ing.
///  - <see cref="GlueState.Find"/> is given a <see cref="FakeFindManager"/> (only ever set by
///    MainTreeViewPlugin.StartUp, which this test host never runs) so tree-node-resolving setters like
///    GlueState.CurrentNamedObjectSave don't NRE.
///
/// Any test that drives production code through GlueCommands.Self/GlueState.Self (rather than calling a
/// pure static method directly) needs this. Call <see cref="EnsureInitialized"/> once per test.
/// </summary>
internal static class GlueTestBootstrap
{
    static readonly object _lock = new();
    static bool _initialized;
    static bool _collisionPluginAssetTypesRegistered;

    public static void EnsureInitialized()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            if (Builder.App == null)
            {
                new Builder().Build();
            }

            Container.Set<IGlueCommands>(GlueCommands.Self);
            Container.Set<IGlueState>(GlueState.Self);

            if (FlatRedBall.Glue.ProjectManager.CodeProjectHelper == null)
            {
                FlatRedBall.Glue.ProjectManager.Initialize();
            }

            FileWatchManager.Initialize();

            if (AvailableAssetTypes.CommonAtis == null)
            {
                AvailableAssetTypes.Self.Initialize(FindGlueStartupPath());
            }

            MainGlueWindow.Self ??= new FakeMainGlueWindow();
            GlueState.Self.Find ??= new FakeFindManager();
        }
    }

    /// <summary>
    /// Opt-in, separate from <see cref="EnsureInitialized"/>: registers the Collision Plugin's
    /// <see cref="OfficialPlugins.CollisionPlugin.Managers.AssetTypeInfoManager.CollisionRelationshipAti"/>
    /// into <see cref="AvailableAssetTypes"/>, the same single call <see cref="MainCollisionPlugin.StartUp"/>
    /// makes - <see cref="MainCollisionPlugin.RegisterAssetTypes"/> is called directly here, bypassing
    /// PluginManager.LoadPlugins' reflection/directory-scan entirely (that machinery is unchanged and still
    /// the only way real/third-party plugins load). Kept separate from the base bootstrap so tests that
    /// don't touch collision types don't pay for it. Call <see cref="EnsureInitialized"/> first.
    /// </summary>
    public static void EnsureCollisionPluginAssetTypesRegistered()
    {
        lock (_lock)
        {
            if (_collisionPluginAssetTypesRegistered)
            {
                return;
            }
            _collisionPluginAssetTypesRegistered = true;

            MainCollisionPlugin.RegisterAssetTypes();
        }
    }

    // Walks up from the test assembly to the repo's Glue/Content/ContentTypes.csv - the same file
    // MainGlueWindow.cs reads via `FileManager.GetDirectory(assembly location) + "Content\ContentTypes.csv"`.
    // Test hosts run from a different output directory, so this can't be hardcoded or copied from
    // MainGlueWindow's own startupPath calculation.
    static string FindGlueStartupPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Glue", "Content", "ContentTypes.csv");
            if (File.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "Glue") + Path.DirectorySeparatorChar;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate the repo 'Glue/Content/ContentTypes.csv' above " + AppContext.BaseDirectory);
    }
}
