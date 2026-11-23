using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EditorObjects.IoC;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using GlueFormsCore.ViewModels;
using GumPlugin;
using EntityInputMovementPlugin;
using OfficialPlugins.CollisionPlugin;
using OfficialPlugins.ScreenPlugin;
using OfficialPlugins.MonoGameContent;
using TileGraphicsPlugin;
using FlatRedBall.Glue.Headless;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Test-side wrapper around <see cref="GlueHeadlessHost"/>, the shipped headless startup that runs the same
/// static/DI registrations Glue.exe's real startup (Program.cs / MainGlueWindow.cs) performs before
/// GlueCommands.Self/GlueState.Self are usable. <see cref="GlueHeadlessHost"/>'s doc comment lists what each
/// step sets up and why. This class adds only what is test-specific: locating the repo's
/// <c>Glue/Content/ContentTypes.csv</c> from the test output folder, recording dialog messages for tests
/// to assert on, the <see cref="ErrorRecordingPlugin"/>, and opt-in registration of individual official
/// plugins.
///
/// Any test that drives production code through GlueCommands.Self/GlueState.Self (rather than calling a
/// pure static method directly) needs this. Call <see cref="EnsureInitialized"/> once per test.
/// </summary>
internal static class GlueTestBootstrap
{
    static readonly object _lock = new();
    static bool _collisionPluginAssetTypesRegistered;
    static bool _collisionPluginRegisteredWithPluginManager;
    static bool _gumPluginStandardElementsInitialized;
    static bool _gumPluginCodeGeneratorsInitialized;
    static bool _gumPluginRegisteredWithPluginManager;
    static bool _tiledPluginRegisteredWithPluginManager;
    static bool _screenPluginRegisteredWithPluginManager;
    static bool _entityInputMovementPluginRegisteredWithPluginManager;
    static bool _axisAlignedRectanglePluginRegisteredWithPluginManager;
    static bool _circlePluginRegisteredWithPluginManager;
    static bool _cameraControllingEntityPluginRegisteredWithPluginManager;
    static bool _errorReportingPluginRegisteredWithPluginManager;
    static bool _propertyGridPluginRegisteredWithPluginManager;

    /// <summary>
    /// Every message production code sent through <see cref="DialogService"/> since
    /// <see cref="EnsureHeadlessProjectLoadReady"/> ran. A real project load should produce none - anything
    /// here is Glue reporting a problem it would have shown the user a dialog about, so tests assert this is
    /// empty rather than letting the load "succeed" with a swallowed error.
    /// </summary>
    public static List<string> RecordedDialogMessages { get; } = new();

    /// <summary>
    /// <see cref="GlueHeadlessHost.EnsureInitialized"/>, with Glue's startup path pointed at the repo's
    /// <c>Glue</c> folder, since the test output folder has no <c>Content/ContentTypes.csv</c>.
    /// </summary>
    public static void EnsureInitialized() => GlueHeadlessHost.EnsureInitialized(FindGlueStartupPath);

    /// <summary>
    /// Opt-in, on top of <see cref="EnsureInitialized"/>: <see cref="GlueHeadlessHost.EnsureProjectLoadReady"/>,
    /// so <see cref="FlatRedBall.Glue.IO.ProjectLoader"/>.<c>LoadProject</c> can run a real, checked-in game
    /// project end to end. Dialog messages and unhandled UI-thread exceptions go to
    /// <see cref="RecordedDialogMessages"/>, and an <see cref="ErrorRecordingPlugin"/> is registered before
    /// the embedded plugins so it captures anything they print while starting up.
    ///
    /// <b>Must be called from an STA thread</b> - use <c>[StaFact]</c>, not <c>[Fact]</c>. Plugin StartUp
    /// methods build real WPF toolbars (MainTiledPluginClass.CreateToolbar and friends), which throw
    /// "The calling thread must be STA" otherwise.
    /// </summary>
    public static void EnsureHeadlessProjectLoadReady()
    {
        EnsureInitialized();

        GlueHeadlessHost.EnsureProjectLoadReady(
            message => RecordedDialogMessages.Add(message),
            new ErrorRecordingPlugin());
    }

    /// <summary>
    /// <see cref="GlueHeadlessHost.EnsureMsBuildEnvironmentVariable"/>, running <see cref="EnsureInitialized"/>
    /// first only when the variable still needs setting. Call before anything touches MSBuild; see the
    /// host's comment for why it must precede the first evaluation and why it is safe to call repeatedly.
    /// </summary>
    public static void EnsureMsBuildEnvironmentVariable()
    {
        if (!GlueHeadlessHost.IsMsBuildEnvironmentVariableSet)
        {
            // SetMsBuildEnvironmentVariable reports through GlueCommands.Self, whose type initializer needs
            // the DI container, so the base bootstrap has to have run first.
            EnsureInitialized();
            GlueHeadlessHost.EnsureMsBuildEnvironmentVariable();
        }
    }

    /// <summary>
    /// Opt-in: constructs a real <see cref="MainGumPlugin"/> and registers it with
    /// <see cref="PluginManager.RegisterPlugin"/>, running its real <c>StartUp</c>. This is what
    /// assigns <c>GlueCommands.Self.GumCommands</c>; without it that is null and <c>HasGum</c> is never asked,
    /// which is what silently compiles out every <c>#if HasGum</c> branch of the GlueControl
    /// embedded code, so a compile-based test would pass with the bug present (GitHub issue #1973).
    ///
    /// Requires <see cref="EnsureHeadlessProjectLoadReady"/> first: MainGumPlugin's StartUp adds a menu item
    /// and builds a WPF toolbar, so it needs both the populated MenuStrip and an STA thread.
    /// </summary>
    public static void EnsureGumPluginRegisteredWithPluginManager()
    {
        EnsureHeadlessProjectLoadReady();

        lock (_lock)
        {
            if (_gumPluginRegisteredWithPluginManager)
            {
                return;
            }
            _gumPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new MainGumPlugin());
        }
    }

    /// <summary>
    /// Opt-in: the plugins a real game project needs for a full regeneration, registered by an explicit
    /// list rather than <see cref="PluginManagerBase.LoadPlugins"/>'s reflection/directory scan. The scan is
    /// the wrong tool here: <c>StartupPlugin</c> swallows a failing <c>StartUp</c> into a disabled plugin, so
    /// a plugin that cannot come up headless would leave the harness looking loaded while silently
    /// generating nothing. An explicit list fails loudly, one plugin at a time (GitHub issue #1973).
    ///
    /// Grow this list as gold projects need it. Each addition is a real StartUp running in the test host, so
    /// expect to have to feed it whatever UI seam it dereferences, the way
    /// <see cref="EnsureHeadlessProjectLoadReady"/> already does for the menu strip and toolbar tray.
    /// </summary>
    public static void EnsureGameProjectPluginsRegistered()
    {
        EnsureGumPluginRegisteredWithPluginManager();
        EnsureCollisionPluginRegisteredWithPluginManager();
        EnsureScreenPluginRegisteredWithPluginManager();
        EnsureEntityInputMovementPluginRegisteredWithPluginManager();
        EnsureAxisAlignedRectanglePluginRegisteredWithPluginManager();

        lock (_lock)
        {
            if (_tiledPluginRegisteredWithPluginManager)
            {
                return;
            }
            _tiledPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new MainTiledPluginClass());
            PluginManager.RegisterPlugin(new MainContentPipelinePlugin());
        }
    }

    /// <summary>
    /// Opt-in: constructs a real <see cref="OfficialPlugins.AxisAlignedRectanglePlugin.MainAxisAlignedRectanglePlugin"/>
    /// and registers it with <see cref="PluginManager.RegisterPlugin"/>, running its real
    /// <c>StartUp</c>. That StartUp adds the XVelocity/YVelocity/ZVelocity <c>VariableDefinition</c>s to the
    /// AxisAlignedRectangle ATI - without it, any entity with an AxisAlignedRectangle NamedObjectSave whose
    /// CustomVariables tunnel one of those (e.g. a collision-driven "bounce" entity) fails code generation
    /// with "Found the AssetTypeInfo AxisAlignedRectangle but missing the variable XVelocity".
    /// </summary>
    public static void EnsureAxisAlignedRectanglePluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_axisAlignedRectanglePluginRegisteredWithPluginManager)
            {
                return;
            }
            _axisAlignedRectanglePluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new OfficialPlugins.AxisAlignedRectanglePlugin.MainAxisAlignedRectanglePlugin());
        }
    }

    /// <summary>
    /// Opt-in: registers a real <see cref="OfficialPlugins.CirclePlugin.MainCirclePlugin"/>, whose StartUp
    /// adds the Circle ATI's <c>AddToManagersFunc</c>. Once only: the plugin adds its handler with <c>+=</c>.
    /// </summary>
    public static void EnsureCirclePluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_circlePluginRegisteredWithPluginManager)
            {
                return;
            }
            _circlePluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new OfficialPlugins.CirclePlugin.MainCirclePlugin());
        }
    }

    /// <summary>
    /// Opt-in: registers a real <see cref="OfficialPlugins.CameraControllingEntityPlugin.MainCameraControllingEntityPlugin"/>,
    /// whose StartUp adds the CameraControllingEntity ATI to <see cref="AvailableAssetTypes"/>.
    /// </summary>
    public static void EnsureCameraControllingEntityPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_cameraControllingEntityPluginRegisteredWithPluginManager)
            {
                return;
            }
            _cameraControllingEntityPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new OfficialPlugins.CameraControllingEntityPlugin.MainCameraControllingEntityPlugin());
        }
    }

    /// <summary>
    /// Opt-in: constructs a real <see cref="MainScreenPlugin"/> and registers it with
    /// <see cref="PluginManager.RegisterPlugin"/>, running its real <c>StartUp</c>.
    ///
    /// Its whole StartUp is one <c>VariableDefinition</c> added to the Screen ATI, which is easy to read as
    /// editor-only polish and is not: <c>DefaultLayer</c> is declared <c>UsesCustomCodeGeneration</c>, and
    /// that flag is what stops <c>CustomVariableCodeGenerator</c> from emitting a <c>public string
    /// DefaultLayer;</c> field shadowing the base <c>Screen.DefaultLayer</c> (a <c>Layer</c>). Without this
    /// plugin, any screen that sets DefaultLayer and holds a factory-created entity list regenerates into
    /// code that cannot compile - CS0029 on <c>Factories.XFactory.DefaultLayer = this.DefaultLayer;</c>.
    /// GitHub issue #1988.
    /// </summary>
    public static void EnsureScreenPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_screenPluginRegisteredWithPluginManager)
            {
                return;
            }
            _screenPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new MainScreenPlugin());
        }
    }

    /// <summary>
    /// Opt-in: constructs a real <see cref="MainEntityInputMovementPlugin"/> and registers it with
    /// <see cref="PluginManager.RegisterPlugin"/>, running its real <c>StartUp</c>. This is what
    /// writes a platformer/top-down project's <c>Platformer/*.Generated.cs</c> and <c>TopDown/*.Generated.cs</c>
    /// files - which the .csproj still lists, so a project regenerated without it fails to build with
    /// CS2001 on the missing sources rather than with anything naming a plugin.
    /// </summary>
    public static void EnsureEntityInputMovementPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_entityInputMovementPluginRegisteredWithPluginManager)
            {
                return;
            }
            _entityInputMovementPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new MainEntityInputMovementPlugin());
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
    /// <see cref="PluginManager.RegisterPlugin"/>, running its real <c>StartUp</c> (asset types,
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

            RegisteredCollisionPlugin = new MainCollisionPlugin();
            PluginManager.RegisterPlugin(RegisteredCollisionPlugin);
        }
    }

    /// <summary>
    /// Opt-in: registers a real <see cref="OfficialPlugins.VariableDisplay.MainPropertyGridPlugin"/>, which
    /// reacts to every selection. GlueCli tests register it so a command that selects its target sees the
    /// same selection side effects as the real exe (GitHub issue #2476). It decides what to show from the
    /// selected tree node, so a test also needs a <see cref="GlueState.Find"/> that returns one; see
    /// <see cref="GlueUnitTests.Cli.ElementTreeNodeFinder"/>.
    /// </summary>
    public static void EnsurePropertyGridPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_propertyGridPluginRegisteredWithPluginManager)
            {
                return;
            }
            _propertyGridPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new OfficialPlugins.VariableDisplay.MainPropertyGridPlugin());
        }
    }

    /// <summary>
    /// Opt-in: registers a real <see cref="OfficialPlugins.ErrorReportingPlugin.MainErrorReportingPlugin"/>,
    /// whose StartUp adds the NamedObjectSave, ReferencedFileSave, CustomVariable and element-inheritance
    /// error reporters - the ones that report a broken reference in a project's JSON.
    /// </summary>
    public static void EnsureErrorReportingPluginRegisteredWithPluginManager()
    {
        lock (_lock)
        {
            if (_errorReportingPluginRegisteredWithPluginManager)
            {
                return;
            }
            _errorReportingPluginRegisteredWithPluginManager = true;

            PluginManager.RegisterPlugin(new OfficialPlugins.ErrorReportingPlugin.MainErrorReportingPlugin());
        }
    }

    static bool _missingFileErrorReporterRegistered;

    /// <summary>
    /// Opt-in: adds the error reporter <see cref="OfficialPlugins.ErrorPlugin.MainErrorPlugin"/>'s StartUp adds,
    /// which reports referenced files missing from disk. The plugin itself is not registered: its StartUp builds
    /// a WPF ErrorWindow.
    /// </summary>
    public static void EnsureMissingFileErrorReporterRegistered()
    {
        lock (_lock)
        {
            if (_missingFileErrorReporterRegistered)
            {
                return;
            }
            _missingFileErrorReporterRegistered = true;

            Container.Get<FlatRedBall.Glue.Errors.GlueErrorManager>()
                .Add(new OfficialPlugins.ErrorPlugin.Logic.ErrorCreateRemoveLogic());
        }
    }

    /// <summary>
    /// The instance <see cref="EnsureCollisionPluginRegisteredWithPluginManager"/> registered, for asserting
    /// on its view models after a selection.
    /// </summary>
    public static MainCollisionPlugin RegisteredCollisionPlugin { get; private set; }

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
                GumPlugin.CodeGeneration.StateCodeGenerator.Self.Initialize(textCodeGenerator, containerCodeGenerator, nineSliceCodeGenerator);
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
