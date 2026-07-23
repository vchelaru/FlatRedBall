using System;
using System.IO;
using EditorObjects.IoC;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Services;
using GlueFormsCore.ViewModels;
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
///    this is CSV-driven and needs no plugins, unlike the per-plugin ATIs below. Guarded on
///    <c>AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets == null</c>, not on
///    <c>CommonAtis == null</c>: when running the full test suite, something upstream of this bootstrap
///    (outside test code - not yet root-caused) already leaves <c>CommonAtis</c> non-null the first time
///    this method runs, which made the old <c>CommonAtis == null</c> guard skip calling
///    <c>Initialize</c> on <c>Self</c> entirely, leaving <c>Self.AdditionalExtensionsToTreatAsAssets</c>
///    permanently null - surfaced as a NullReferenceException deep in
///    <c>FileCommands.IsContent</c>/<c>GluxCommands.AddSingleFileTo</c>, which only a real "add a csv
///    ReferencedFileSave" call chain reaches. Guarding on the exact field this bootstrap needs to have set,
///    rather than a same-method side effect that can apparently already be true from elsewhere, is correct
///    regardless of that upstream cause.
///  - <see cref="FlatRedBall.Glue.Reflection.ExposedVariableManager"/>.Initialize populates the reflected
///    list of PositionedObject members (X/Y/Z/Velocity/etc.) that code generation uses to decide whether a
///    CustomVariable is "exposing" an already-existing base member - a plain, UI-free reflection pass over
///    <c>typeof(PositionedObject)</c>, the same call MainGlueWindow.cs makes. Without it,
///    CustomVariableCodeGenerator NREs (ArgumentNullException on a null source list) the first time code
///    generation runs for *any* entity - only reachable once something in a test drives real entity code
///    generation (e.g. WizardProjectLogic.HandleAddPlayerEntity's AddEntityAsync), which is why this went
///    unnoticed until the first end-to-end player-entity test.
///  - <see cref="MainGlueWindow"/>.Self is given a <see cref="FakeMainGlueWindow"/> (only Program.cs ever
///    constructs the real WinForms window, which this test host never does) so any code path reading
///    MainGlueWindow.Self - PropertyGrid, HasErrorOccurred, Invoke/BeginInvoke, etc. - gets a harmless
///    object instead of NRE-ing.
///  - <see cref="GlueState.Find"/> is given a <see cref="FakeFindManager"/> (only ever set by
///    MainTreeViewPlugin.StartUp, which this test host never runs) so tree-node-resolving setters like
///    GlueState.CurrentNamedObjectSave don't NRE.
///  - <see cref="PluginManager.TabControlViewModel"/> is given a real (not fake) <c>TabControlViewModel</c>
///    - only ever set by MainPanelControl.xaml.cs's real WPF startup, which this test host never runs.
///    It's a plain MVVM view model (ObservableCollection-backed tab containers, no live Dispatcher/message
///    loop needed to construct), so any code path reading it - DialogCommands.FocusTab,
///    GlueState.CurrentFocusedTabs, GlueCommands.RestoreTabs - gets a real, just permanently empty (no
///    tabs ever added, since nothing here runs the real WPF tab UI) TabControlViewModel instead of NREing.
///    Searching for a tab that was never added is not an error case for that code (e.g. FocusTab just
///    finds nothing and returns).
///  - The legacy <see cref="Container"/> is also given a real <c>EntitySaveSetPropertyLogic</c> (only ever
///    set by MainGlueWindow.cs's real startup) so <c>ElementCommands.ReactToPropertyChanged</c> - the path
///    any "flip an Implements* flag on an EntitySave" call goes through, e.g. ImplementsICollidable - can
///    resolve it instead of throwing "container does not contain an entry".
///  - The legacy <see cref="Container"/> is also given a real
///    <see cref="FlatRedBall.Glue.SetVariable.NamedObjectSetVariableLogic"/> (only ever set by
///    MainGlueWindow.cs's real startup) so any "a NamedObjectSave's ExposedInDerived/instance name/property
///    changed" path - e.g. GluxCommands.AddNewNamedObjectToAsync's ExposedInDerived notification - can
///    resolve it instead of throwing "container does not contain an entry". Its constructor only reads
///    from the Builder DI container (already built above), no live UI needed.
///  - The legacy <see cref="Container"/> is also given a real
///    <see cref="FlatRedBall.Glue.Errors.GlueErrorManager"/> (only ever set by MainGlueWindow.cs's real
///    startup) so <c>PluginBase.AddErrorReporter</c> - called from a real plugin's <c>StartUp</c>, e.g.
///    <see cref="PluginManager.RegisterPluginForTesting"/> - can resolve it instead of throwing "container
///    does not contain an entry". A plain in-memory list, no live UI needed.
///  - <see cref="TaskManager.SynchronousMode"/> is forced on, process-wide, for the lifetime of the test
///    run (set here, first, before anything else touches <see cref="TaskManager.Self"/>). Individual tests
///    still flip it in their own constructor/Dispose for documentation purposes, but if it were left at its
///    default of false at first access, TaskManager's constructor spins up its real dedicated background
///    thread, which then runs for the entire process - any *other*, unrelated test whose production code
///    path enqueues work while SynchronousMode happens to be transiently false (e.g. between two tests that
///    each toggle it locally) leaves that work to be picked up by this still-live background thread later,
///    racing against whatever a completely different, currently-running test is doing on the main thread.
///    This was observed as an intermittent NullReferenceException inside a shared, non-thread-safe
///    List&lt;string&gt; (AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets) when running the
///    full suite - not a bug in the plugin/production code under test, but in ever letting that background
///    thread exist at all in a test host. Forcing it here means the thread is never spun up in the first
///    place, for any test in the assembly.
///
/// Any test that drives production code through GlueCommands.Self/GlueState.Self (rather than calling a
/// pure static method directly) needs this. Call <see cref="EnsureInitialized"/> once per test.
/// </summary>
internal static class GlueTestBootstrap
{
    static readonly object _lock = new();
    static bool _initialized;
    static bool _collisionPluginAssetTypesRegistered;
    static bool _collisionPluginRegisteredWithPluginManager;
    static bool _gumPluginStandardElementsInitialized;
    static bool _gumPluginCodeGeneratorsInitialized;

    public static void EnsureInitialized()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            // Must run before any other line here (or in any test): see the SynchronousMode bullet above.
            TaskManager.SynchronousMode = true;

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

            if (AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets == null)
            {
                AvailableAssetTypes.Self.Initialize(FindGlueStartupPath());
            }

            if (FlatRedBall.Glue.Reflection.ExposedVariableManager.PositionedObjectMembers == null)
            {
                FlatRedBall.Glue.Reflection.ExposedVariableManager.Initialize();
            }

            MainGlueWindow.Self ??= new FakeMainGlueWindow();
            GlueState.Self.Find ??= new FakeFindManager();

            if (PluginManager.TabControlViewModel == null)
            {
                PluginManager.SetTabs(new TabControlViewModel());
            }

            Container.Set(new FlatRedBall.Glue.SetVariable.EntitySaveSetPropertyLogic());
            Container.Set(new FlatRedBall.Glue.SetVariable.NamedObjectSetVariableLogic());
            Container.Set(new FlatRedBall.Glue.Errors.GlueErrorManager());
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

    /// <summary>
    /// Opt-in, separate from <see cref="EnsureInitialized"/> and <see cref="EnsureCollisionPluginAssetTypesRegistered"/>:
    /// constructs a real <see cref="MainCollisionPlugin"/> and registers it with
    /// <see cref="PluginManager.RegisterPluginForTesting"/>, running its real <c>StartUp</c> (asset types,
    /// code generators, view model, and - the piece this exists for - wiring
    /// <see cref="FlatRedBall.Glue.Plugins.PluginBase.ReactToCreateCollisionRelationshipsBetween"/> to the
    /// real <c>CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects</c> handler).
    /// Call this (instead of <see cref="EnsureCollisionPluginAssetTypesRegistered"/> - StartUp already does
    /// what that does, and MainCollisionPlugin.RegisterAssetTypes is idempotent if a test calls both) when
    /// a test needs the Collision Plugin's real plugin-event behavior, not just its asset type registered -
    /// e.g. driving WizardProjectLogic.HandleAddPlayerInstance end-to-end, which creates collision
    /// relationships via PluginManager.ReactToCreateCollisionRelationshipsBetween, a static event that has
    /// no subscriber (and silently no-ops) unless a real MainCollisionPlugin has run this.
    /// </summary>
    public static void EnsureCollisionPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_collisionPluginRegisteredWithPluginManager)
            {
                return;
            }
            _collisionPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPluginForTesting(new MainCollisionPlugin());
        }
    }

    /// <summary>
    /// Opt-in, separate from <see cref="EnsureInitialized"/>: registers the Gum Plugin's real
    /// <see cref="Gum.Managers.StandardElementsManager"/> defaults and <see cref="GumPlugin.Managers.AssetTypeInfoManager"/>
    /// common ATIs, the same two calls <see cref="MainGumPlugin.StartUp"/> makes. Needed by any test that adds
    /// a Gum Forms component (<c>GumPluginCommands.AddComponent</c> - e.g. via
    /// <c>NewGumProjectCreationLogic.CreateGumProjectInternal(shouldAlsoAddForms: true, ...)</c>) since
    /// <c>StandardElementsManager.GetDefaultStateFor</c> throws "You must first call Initialize" without it.
    /// Kept separate from the base bootstrap so tests that don't touch Gum Forms components don't pay for it.
    /// </summary>
    public static void EnsureGumPluginStandardElementsInitialized()
    {
        lock (_lock)
        {
            if (_gumPluginStandardElementsInitialized)
            {
                return;
            }
            _gumPluginStandardElementsInitialized = true;

            Gum.Managers.StandardElementsManager.Self.Initialize();
            GumPlugin.Managers.AssetTypeInfoManager.Self.AddCommonAtis();
        }
    }

    /// <summary>
    /// Opt-in: wires up <see cref="GumPlugin.CodeGeneration.StandardsCodeGenerator"/> and
    /// <see cref="GumPlugin.CodeGeneration.StateCodeGenerator"/> with their real per-type generators (the
    /// same construction <see cref="MainGumPlugin.StartUp"/> performs), then runs the same
    /// Refresh*SkipFor* calls <c>MainGumPlugin</c> runs on glux load. Without this,
    /// <c>StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor</c> NREs inside
    /// <c>AddAdditionalInheritance</c> (its per-type generator fields are never assigned), which
    /// <c>CodeGeneratorManager.GenerateAllElements</c> swallows into its own error list - so standard-element
    /// runtimes (e.g. RectangleRuntime.Generated.cs) silently never get written instead of throwing. Needed
    /// by any test that drives real standard-element (not just Forms-component) code generation, e.g. via
    /// <c>CodeGeneratorManager.GenerateDerivedGueRuntimesAsync</c>.
    /// </summary>
    public static void EnsureGumPluginCodeGeneratorsInitialized()
    {
        EnsureGumPluginStandardElementsInitialized();

        lock (_lock)
        {
            if (!_gumPluginCodeGeneratorsInitialized)
            {
                _gumPluginCodeGeneratorsInitialized = true;

                var textCodeGenerator = new GumPlugin.CodeGeneration.TextCodeGenerator();
                var containerCodeGenerator = new GumPlugin.CodeGeneration.ContainerCodeGenerator(GlueState.Self);
                var nineSliceCodeGenerator = new GumPlugin.CodeGeneration.NineSliceCodeGenerator(GlueState.Self);
                var spriteCodeGenerator = new GumPlugin.CodeGeneration.SpriteCodeGenerator(GlueState.Self);
                var polygonCodeGenerator = new GumPlugin.CodeGeneration.PolygonCodeGenerator(GlueState.Self);

                GumPlugin.CodeGeneration.StandardsCodeGenerator.Self.Initialize(
                    spriteCodeGenerator,
                    textCodeGenerator,
                    containerCodeGenerator,
                    nineSliceCodeGenerator,
                    polygonCodeGenerator);
                GumPlugin.CodeGeneration.StateCodeGenerator.Self.Initialize(textCodeGenerator, containerCodeGenerator);
            }
        }

        // Version-dependent (reads GlueState.Self.CurrentGlueProject.FileVersion), and cheap - re-run every
        // call rather than caching, since callers may set up a different CurrentGlueProject per test.
        GumPlugin.CodeGeneration.StateCodeGenerator.Self.RefreshVariablesToSkipForStates();
        GumPlugin.CodeGeneration.StateCodeGenerator.Self.RefreshVariableNamesToSkipBasedOnGlueVersion();
        GumPlugin.CodeGeneration.StandardsCodeGenerator.Self.RefreshVariableNamesToSkipForProperties();
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
