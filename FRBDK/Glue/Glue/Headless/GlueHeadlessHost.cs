using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using EditorObjects.IoC;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Audio;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Services;
using Glue;
using GlueFormsCore.ViewModels;

// Container is obsolete, but MainGlueWindow fills it and several commands still read from it.
#pragma warning disable CS0618

namespace FlatRedBall.Glue.Headless;

/// <summary>
/// Starts Glue with no window: the one-time, process-wide static/DI registrations Glue.exe's real startup
/// (Program.cs / MainGlueWindow.cs) performs before GlueCommands.Self/GlueState.Self are usable, run without
/// a live WinForms app. Used by the unit tests (through GlueTestBootstrap) and by any headless tool, so
/// there is one headless startup path. Nothing here is a fake - it is the same calls production makes:
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
///    this is CSV-driven and needs no plugins, unlike per-plugin ATIs. Guarded on
///    <c>AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets == null</c>, not on
///    <c>CommonAtis == null</c>: when running the full test suite, something upstream of this host
///    (outside test code - not yet root-caused) already leaves <c>CommonAtis</c> non-null the first time
///    this method runs, which made the old <c>CommonAtis == null</c> guard skip calling
///    <c>Initialize</c> on <c>Self</c> entirely, leaving <c>Self.AdditionalExtensionsToTreatAsAssets</c>
///    permanently null - surfaced as a NullReferenceException deep in
///    <c>FileCommands.IsContent</c>/<c>GluxCommands.AddSingleFileTo</c>, which only a real "add a csv
///    ReferencedFileSave" call chain reaches. Guarding on the exact field this host needs to have set,
///    rather than a same-method side effect that can apparently already be true from elsewhere, is correct
///    regardless of that upstream cause.
///  - <see cref="FlatRedBall.Glue.Reflection.ExposedVariableManager"/>.Initialize populates the reflected
///    list of PositionedObject members (X/Y/Z/Velocity/etc.) that code generation uses to decide whether a
///    CustomVariable is "exposing" an already-existing base member - a plain, UI-free reflection pass over
///    <c>typeof(PositionedObject)</c>, the same call MainGlueWindow.cs makes. Without it,
///    CustomVariableCodeGenerator NREs (ArgumentNullException on a null source list) the first time code
///    generation runs for *any* entity - only reachable once something drives real entity code
///    generation (e.g. WizardProjectLogic.HandleAddPlayerEntity's AddEntityAsync), which is why this went
///    unnoticed until the first end-to-end player-entity test.
///  - <see cref="MainGlueWindow"/>.Self is given a <see cref="HeadlessMainGlueWindow"/> (only Program.cs ever
///    constructs the real WinForms window, which a headless host never does) so any code path reading
///    MainGlueWindow.Self - PropertyGrid, HasErrorOccurred, Invoke/BeginInvoke, etc. - gets a harmless
///    object instead of NRE-ing.
///  - <see cref="GlueState.Find"/> is given a <see cref="HeadlessFindManager"/> (only ever set by
///    MainTreeViewPlugin.StartUp, which a headless host never runs) so code that asks for tree nodes gets
///    "none" instead of NRE-ing. Selection itself does not need it: GlueState's Current* setters work by
///    model object (GitHub issue #2268).
///  - <see cref="PluginManager.TabControlViewModel"/> is given a real <c>TabControlViewModel</c>
///    - only ever set by MainPanelControl.xaml.cs's real WPF startup, which a headless host never runs.
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
///    <see cref="PluginManager.RegisterPlugin"/> - can resolve it instead of throwing "container
///    does not contain an entry". A plain in-memory list, no live UI needed.
///  - <see cref="TaskManager.SynchronousMode"/> is forced on, process-wide, for the lifetime of the
///    process (set here, first, before anything else touches <c>TaskManager.Self</c>). Individual
///    tests still flip it in their own constructor/Dispose for documentation purposes, but if it were left
///    at its default of false at first access, TaskManager's constructor spins up its real dedicated
///    background thread, which then runs for the entire process - any *other*, unrelated test whose
///    production code path enqueues work while SynchronousMode happens to be transiently false (e.g.
///    between two tests that each toggle it locally) leaves that work to be picked up by this still-live
///    background thread later, racing against whatever a completely different, currently-running test is
///    doing on the main thread. This was observed as an intermittent NullReferenceException inside a
///    shared, non-thread-safe List&lt;string&gt; (AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets)
///    when running the full suite - not a bug in the plugin/production code under test, but in ever
///    letting that background thread exist at all in a headless host. Forcing it here means the thread is
///    never spun up in the first place.
/// </summary>
public static class GlueHeadlessHost
{
    static readonly object _lock = new();
    static bool _initialized;
    static bool _projectLoadReady;

    /// <summary>
    /// True once <see cref="EnsureInitialized"/> has run in this process.
    /// </summary>
    public static bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return _initialized;
            }
        }
    }

    /// <summary>
    /// The GlueCommon seams startup wires, in order. GlueCommon can't reference Glue.csproj, so each of
    /// these points a GlueCommon static at its Glue implementation before any project-load code reads it.
    /// MainGlueWindow calls this too, so the editor and a headless host cannot drift apart here. Public so a
    /// test can clear one seam and re-run exactly the startup wiring, proving startup (not some earlier
    /// caller in the process) is what populates it.
    /// </summary>
    public static void WireGlueCommonSeams()
    {
        FlatRedBall.Glue.Parsing.TypeManager.EnsureTypeResolutionSeamWired();
        FlatRedBall.Glue.Controls.DialogService.EnsureErrorReportingSeamWired();
        FlatRedBall.Glue.Plugins.PluginManager.EnsurePluginSeamWired();
        FlatRedBall.Glue.Managers.BuildToolAssociationManager.EnsureBuildToolAssociationSeamWired();
    }

    /// <summary>
    /// Runs the base headless startup once per process; later calls return immediately. See the class
    /// comment for what each step sets up.
    /// </summary>
    /// <param name="getGlueStartupPath">Returns the folder holding Glue's <c>Content/ContentTypes.csv</c>,
    /// with a trailing separator. Only called when the asset types still need initializing. Null means the
    /// folder Glue's assembly is in, which is what Glue.exe uses.</param>
    public static void EnsureInitialized(Func<string>? getGlueStartupPath = null)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            // Must run before any other line here: see the SynchronousMode bullet above.
            TaskManager.SynchronousMode = true;

            // Must also run before anything constructs a WinForms control. WinForms installs a
            // WindowsFormsSynchronizationContext on a thread the first time a control is created there -
            // HeadlessMainGlueWindow's PropertyGrid below, the MenuStrip and plugin toolbars in
            // EnsureProjectLoadReady. Once it exists, async continuations post back to it, and it
            // only runs them while a message loop pumps. No headless host runs one. Combined with
            // SynchronousMode - whose RunSynchronously blocks the calling thread on the awaited task - the
            // first plugin that awaits during a real project load deadlocks the entire run: one blocked
            // thread, no child process, nothing else executing, and no timeout anywhere. Sending
            // continuations to the thread pool instead is what every non-STA test already gets.
            WindowsFormsSynchronizationContext.AutoInstall = false;
            SynchronizationContext.SetSynchronizationContext(null);

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
                var startupPath = getGlueStartupPath != null
                    ? getGlueStartupPath()
                    : FlatRedBall.IO.FileManager.GetDirectory(typeof(GlueHeadlessHost).Assembly.Location);
                AvailableAssetTypes.Self.Initialize(startupPath);
            }

            WireGlueCommonSeams();

            // MainGlueWindow.cs makes this call at startup. It fills the absolute/relative table
            // (X -> RelativeX) that codegen reads to emit `if (X.Parent == null) ... else ...RelativeX`.
            // Skipped, in-memory codegen tests generate text real Glue never does, and change output
            // once any test initializes the engine in the same process (#2429).
            FlatRedBall.Instructions.InstructionManager.Initialize();

            if (FlatRedBall.Glue.Reflection.ExposedVariableManager.PositionedObjectMembers == null)
            {
                FlatRedBall.Glue.Reflection.ExposedVariableManager.Initialize();
            }

            // Populates the built-in CustomVariable type converters ("Minutes:Seconds", etc.), the same call
            // MainGlueWindow.cs makes at startup. Only reachable once a real project containing a variable
            // with a TypeConverter is generated: CustomVariableCodeGenerator NREs in TypeConverterHelper.Convert
            // on the null converter table. Worth knowing how that failure presents - CodeWriter.GenerateCode is
            // an async Task that GenerateAllCodeSync calls without awaiting, so the exception is captured into
            // a discarded Task and lost. The symptom is an empty .Generated.cs (the placeholder from
            // CreateGeneratedFileIfNecessary) for that one element, no error output anywhere, and every later
            // step for it (e.g. its factory) silently skipped.
            FlatRedBall.Glue.TypeConversions.TypeConverterHelper.InitializeClasses();

            MainGlueWindow.Self ??= new HeadlessMainGlueWindow();
            GlueState.Self.Find ??= new HeadlessFindManager();

            // Headless: no dialogs, no load window, and no plugin views. Selection through GlueState runs
            // every registered plugin's selection handler for real (GitHub issue #2268), and a plugin that
            // builds its WPF view on selection needs a UI thread this host doesn't have - so plugins skip
            // the view (not the view model) when this is off, the way MainCollisionPlugin does.
            GlueGui.ShowGui = false;

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
    /// On top of <see cref="EnsureInitialized"/> (which must have run first): everything a headless host
    /// needs before <see cref="ProjectLoader"/>.<c>LoadProject</c> can run a real game project end to end
    /// (deserialize the .gluj, dispatch ReactToLoadedGlux to registered plugins, then GenerateAllCode). Runs
    /// once per process; later calls return immediately. Nothing here is a fake - it is the same set-up
    /// Glue.exe's own startup performs:
    ///  - <see cref="GlueGui.ShowGui"/> is already off (<see cref="EnsureInitialized"/>), so
    ///    <c>ProjectLoader.PrepareInitializationWindow</c> skips constructing <c>InitializationWindowWpf</c>
    ///    (constructing a WPF Window, not just showing it, needs a live WPF app) and every
    ///    <c>GlueGui.ShowMessageBox</c>/<c>TryShowDialog</c> is a no-op rather than a modal dialog on the
    ///    user's desktop.
    ///  - <see cref="DialogService.ShowMessageImpl"/> pointed at <paramref name="reportMessage"/>. That
    ///    seam is *not* covered by ShowGui - several production paths call DialogService directly - so
    ///    without it a load that hits an error path pops a real modal and wedges the process.
    ///  - <see cref="EnsureMsBuildEnvironmentVariable"/>, Glue's real SDK discovery.
    ///  - A real WinForms <see cref="MenuStrip"/> handed to <see cref="GlueGui"/>, then the embedded
    ///    <c>MainMenuStripPlugin</c> registered (with every other embedded plugin) to populate it with the
    ///    top-level items (File/Edit/Project/Content/...) that other plugins hang their menu items off of.
    ///    <c>PluginBase.AddMenuItemTo</c> reads <c>GlueGui.MenuStrip.Items</c> directly, so a plugin whose
    ///    StartUp adds a menu item (e.g. MainGumPlugin's "New Gum Project") NREs without this. Constructing
    ///    a MenuStrip needs no message loop, same as HeadlessMainGlueWindow's real PropertyGrid.
    ///  - <c>ProjectLoader.Self.Initialize</c>, normally called from MainGlueWindow's IoC set-up.
    ///
    /// <b>Must be called from an STA thread.</b> Plugin StartUp methods build real WPF toolbars
    /// (MainTiledPluginClass.CreateToolbar and friends), which throw "The calling thread must be STA"
    /// otherwise.
    /// </summary>
    /// <param name="reportMessage">Receives every message production code would have shown in a dialog,
    /// and every unhandled UI-thread exception. A real project load should produce none.</param>
    /// <param name="pluginsToRegisterFirst">Registered after the menu strip exists and before the
    /// embedded plugins, so they see anything those print while starting up.</param>
    public static void EnsureProjectLoadReady(Action<string> reportMessage, params PluginBase[] pluginsToRegisterFirst)
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"Call {nameof(GlueHeadlessHost)}.{nameof(EnsureInitialized)} before {nameof(EnsureProjectLoadReady)}.");
            }

            if (_projectLoadReady)
            {
                return;
            }
            _projectLoadReady = true;

            PrepareForProjectLoad(reportMessage);

            foreach (var plugin in pluginsToRegisterFirst)
            {
                PluginManager.RegisterPlugin(plugin);
            }

            RegisterEmbeddedPlugins();

            ProjectLoader.Self.Initialize(GlueCommands.Self.ProjectCommands);
        }
    }

    /// <summary>
    /// The alternative to <see cref="EnsureProjectLoadReady"/> for a headless tool (GlueCli) that should get
    /// the editor's results: everything that method sets up, plus the editor's own startup reads, and the
    /// plugins loaded the way Glue.exe loads them - <see cref="PluginManager"/>'s MEF scan of the
    /// <c>Plugins</c> folder next to the running exe, plus Glue's embedded plugins. Run in a process that
    /// has not loaded or registered any plugin yet (not the test process); runs once per process.
    ///
    /// Differences from Glue.exe, each deliberate:
    ///  - The editor's settings (settings.xml, PreferenceSettings.xml) are read from the editor's own
    ///    AppData folder, since <c>FileManager.UserApplicationDataForThisApplication</c> is named after the
    ///    entry exe, and saving them is then switched off, so a headless run never rewrites the user's
    ///    recent-project list or last-opened project.
    ///  - Pending plugin installs and uninstalls are left for the editor to apply.
    ///  - <see cref="SkippedEmbeddedPlugins"/> are dropped before they start, as in the test host.
    ///  - <c>PluginManager.ShareMenuStripReference</c> is never called (see <see cref="EnsureProjectLoadReady"/>),
    ///    so plugins get no <c>InitializeMenuHandler</c> call; the buffered startup output is flushed directly.
    ///  - <c>FileErrorReporter</c> has no timer; call <c>FileErrorReporter.FlushPendingErrors</c> after a load.
    ///
    /// <b>Must be called from an STA thread</b>, after <see cref="EnsureInitialized"/>.
    /// </summary>
    /// <param name="reportMessage">See <see cref="EnsureProjectLoadReady"/>.</param>
    /// <param name="gameProjectDirectory">The folder of the .csproj about to be loaded, whose
    /// plugin settings (PluginsToIgnore) apply, the way the editor applies its last project's.</param>
    /// <returns>One line per plugin that failed to start, naming the plugin and the exception. Glue.exe
    /// disables such a plugin and carries on; a headless tool should refuse to report results without
    /// it.</returns>
    public static IReadOnlyList<string> EnsureProjectLoadReadyWithInstalledPlugins(Action<string> reportMessage, string gameProjectDirectory)
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"Call {nameof(GlueHeadlessHost)}.{nameof(EnsureInitialized)} before {nameof(EnsureProjectLoadReadyWithInstalledPlugins)}.");
            }

            if (_projectLoadReady)
            {
                throw new InvalidOperationException("The headless host already prepared a project load in this process.");
            }
            _projectLoadReady = true;

            PrepareForProjectLoad(reportMessage);

            LoadEditorSettingsReadOnly();

            ProjectManager.PluginSettings = PluginSettings.FileExists(gameProjectDirectory)
                ? PluginSettings.Load(gameProjectDirectory)
                : new PluginSettings();

            AddPropertyValuePairAssemblies();
            EventManager.Initialize();
            FileErrorReporter.InitializeHeadless();
            AddErrorReporters();

            // PluginManagerBase's MEF catalog takes the embedded plugins from the entry assembly, which is
            // Glue itself only in Glue.exe.
            var glueAssembly = typeof(GlueHeadlessHost).Assembly;
            if (Assembly.GetEntryAssembly() != glueAssembly && !PluginManagerBase.AddGlobalOnInitialize.Contains(glueAssembly))
            {
                PluginManagerBase.AddGlobalOnInitialize.Add(glueAssembly);
            }

            FileManager.PreserveCase = true;

            PluginManager.PluginTypesToSkip.UnionWith(SkippedEmbeddedPlugins);
            PluginManager.Initialize(isStartup: true, ProjectManager.PluginSettings.PluginsToIgnore, applyPendingPluginInstalls: false);

            // Registered after the scan because RegisterPlugin creates the global plugin manager, which makes
            // Initialize skip the scan. Output printed before any manager existed is buffered, and flushed here.
            PluginManager.RegisterPlugin(new HeadlessOutputRecorder());
            PluginManager.PrintPreInitializeOutput();

            ProjectLoader.Self.Initialize(GlueCommands.Self.ProjectCommands);

            return PluginManagerBase.AllPluginContainers
                .Where(container => container.FailureException != null)
                .Select(container => $"{container.Name}: {container.FailureDetails}{Environment.NewLine}{container.FailureException}")
                .ToList();
        }
    }

    /// <summary>
    /// The assemblies <c>PropertyValuePair</c> resolves type names from, beyond its defaults. Glue.exe's
    /// startup calls this too.
    /// </summary>
    public static void AddPropertyValuePairAssemblies()
    {
        // Glue.Common
        AddOnce(typeof(PlatformSpecificType).Assembly);
        // MonoGame
        AddOnce(typeof(SoundEffectInstance).Assembly);

        static void AddOnce(Assembly assembly)
        {
            if (!PropertyValuePair.AdditionalAssemblies.Contains(assembly))
            {
                PropertyValuePair.AdditionalAssemblies.Add(assembly);
            }
        }
    }

    /// <summary>
    /// The error reporters that live in Glue itself rather than in a plugin. Glue.exe's startup calls this too.
    /// </summary>
    public static void AddErrorReporters()
    {
        Container.Get<GlueErrorManager>()
            .Add(new CsvErrorReporter());
    }

    /// <summary>
    /// The folder Glue.exe keeps its user settings in. <c>FileManager.UserApplicationDataForThisApplication</c>
    /// is named after the entry exe, so it is this folder only inside Glue.exe.
    /// </summary>
    public static string EditorUserDataDirectory =>
        FileManager.UserApplicationData + typeof(GlueHeadlessHost).Assembly.GetName().Name + "\\";

    /// <summary>
    /// Reads the editor's settings.xml and PreferenceSettings.xml from <see cref="EditorUserDataDirectory"/>,
    /// then switches settings saving off for the rest of the process.
    /// </summary>
    static void LoadEditorSettingsReadOnly()
    {
        var settingsFile = new FilePath(EditorUserDataDirectory + "settings.xml");
        GlueSettingsSave? settings = null;
        if (settingsFile.Exists())
        {
            settings = GlueCommands.TryLoadSettingsFromFile(settingsFile, out var error);
            if (error != null)
            {
                GlueCommands.Self.PrintError($"Could not read the editor settings at {settingsFile}, using defaults:\n{error}");
            }
        }
        GlueState.Self.GlueSettingsSave = settings ?? new GlueSettingsSave();

        EditorData.LoadPreferenceSettings(EditorUserDataDirectory);

        GlueSettingsSave.StopSavesAndLoads = true;
        FlatRedBall.Glue.Settings.FileAssociationSettings.StopSavesAndLoads = true;
        FlatRedBall.Glue.Settings.GlueLayoutSettings.StopSavesAndLoads = true;
    }

    /// <summary>
    /// What both project-load set-ups share: no dialogs, MSBuild located, UI-thread exceptions reported, and
    /// the menu strip plugins hang items off of.
    /// </summary>
    static void PrepareForProjectLoad(Action<string> reportMessage)
    {
        // EnsureInitialized turns AutoInstall off, but it only runs once and this method may be the
        // first thing a different (STA) thread calls, so clear this thread's context too.
        SynchronizationContext.SetSynchronizationContext(null);

        DialogService.ShowMessageImpl = reportMessage;

        EnsureMsBuildEnvironmentVariable();

        // A plugin's ReactToLoadedGlux handler is often `async void` (MainGumPlugin.HandleGluxLoad is),
        // and an exception in one of those does not reach PluginManager's try/catch - it gets posted to
        // the synchronization context, where WinForms turns it into a modal "Unhandled exception" dialog
        // on the user's desktop and the plugin container still reports no failure. Reporting these
        // makes that class of failure visible instead of a popup nobody expects. Attaching a handler is
        // enough: WinForms only falls back to its own dialog when there is none.
        // (SetUnhandledExceptionMode is not an option - EnsureInitialized has already constructed
        // HeadlessMainGlueWindow's PropertyGrid on this thread, and the mode is fixed once a control exists.)
        Application.ThreadException += (_, e) =>
            reportMessage("Unhandled exception on the UI thread: " + e.Exception);

        // Only GlueGui gets the menu strip. PluginManager must NOT: its PluginCommand dispatch reads
        // its own mMenuStrip and, when that is non-null, marshals every plugin call through
        // mMenuStrip.Invoke - which needs a created window handle and a running message loop, so calls
        // silently never execute and CallPluginMethod returns null (e.g. "HasGum returns null").
        // Leaving PluginManager's null keeps its existing "no live menu strip means run inline" guard
        // doing the right thing.
        GlueGui.Initialize(new MenuStrip());
    }

    /// <summary>
    /// Registers every <see cref="EmbeddedPlugin"/> in the Glue assembly, in <c>DesiredOrder</c>, the same
    /// set and order <c>PluginManager.LoadPlugins</c> gives them in Glue.exe.
    ///
    /// These are not optional extras: big pieces of what looks like "core" code generation are embedded
    /// plugins. <c>ICollidablePlugins</c> is what makes an entity's generated class implement
    /// <c>ICollidable</c>, and <c>FactoryPlugin</c> is what writes the <c>*Factory.Generated.cs</c> files.
    /// Skip them and a project regenerates almost completely, then fails to compile in ways that read like
    /// engine or sample bugs (CS0311 on a collision relationship, a missing factory file) rather than like
    /// missing plugins.
    ///
    /// Reflecting over the assembly is not the directory scan the headless host deliberately avoids: these
    /// are compile-time-known types in Glue's own assembly, and MEF finds exactly the same set. What is
    /// avoided is discovering *external* plugin DLLs off disk, because <c>StartupPlugin</c> swallows a
    /// failing <c>StartUp</c> into a disabled plugin, so a plugin that cannot come up headless would leave
    /// the host looking loaded while silently generating nothing.
    /// </summary>
    static void RegisterEmbeddedPlugins()
    {
        var embeddedPlugins = typeof(EmbeddedPlugin).Assembly
            .GetTypes()
            .Where(type => typeof(EmbeddedPlugin).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null)
            .Select(type => (EmbeddedPlugin)Activator.CreateInstance(type)!)
            .OrderBy(plugin => plugin.DesiredOrder)
            .ToList();

        var failures = new List<string>();

        foreach (var plugin in embeddedPlugins)
        {
            if (SkippedEmbeddedPlugins.Contains(plugin.GetType().FullName!))
            {
                continue;
            }

            try
            {
                PluginManager.RegisterPlugin(plugin);
            }
            catch (Exception e)
            {
                failures.Add($"{plugin.GetType().FullName}: {e.InnerException?.Message ?? e.Message}");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Embedded plugins failed to start in the headless host. Either seam whatever they need (the " +
                "way this host already does for the menu strip and toolbar tray) or add them to " +
                nameof(SkippedEmbeddedPlugins) + " with a reason:" +
                Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
    }

    /// <summary>
    /// Embedded plugins deliberately not started headless, each with the reason. Keep this as short as
    /// possible - anything skipped is codegen or behavior a headless load silently does not get.
    /// </summary>
    static readonly HashSet<string> SkippedEmbeddedPlugins = new()
    {
        // Its StartUp builds a WPF control whose XAML cannot resolve its resources without a real WPF
        // application context ("Provide value on TypeConverterMarkupExtension threw an exception"). It only
        // manages synced (multi-platform) projects, which a single-project load does not have, so
        // nothing it would contribute to code generation is lost.
        "FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects.MainPlugin",
    };

    /// <summary>
    /// Points MSBUILD_EXE_PATH at a compatible SDK by running Glue.exe's own discovery
    /// (<c>MainGlueWindow.SetMsBuildEnvironmentVariable</c>), unless it is already set. Without it
    /// <c>Microsoft.Build.Evaluation.Project</c> cannot resolve the SDK imports of any SDK-style .csproj
    /// ("The SDK 'Microsoft.NET.SDK.WorkloadAutoImportPropsLocator' specified could not be found").
    ///
    /// Must run before the *first* MSBuild evaluation in the process: Microsoft.Build caches its toolset on
    /// first use, so setting the variable later has no effect. Safe to call again before every load:
    /// production clears this variable around its own nested dotnet invocations (CompilerPlugin's
    /// Compiler.cs, SyncedProjects' ProjectListEntry.xaml.cs), so it can be unset again later in a process.
    ///
    /// When the variable is unset, <see cref="EnsureInitialized"/> must have run first:
    /// SetMsBuildEnvironmentVariable reports through GlueCommands.Self, whose type initializer needs the DI
    /// container.
    /// </summary>
    public static void EnsureMsBuildEnvironmentVariable()
    {
        if (IsMsBuildEnvironmentVariableSet)
        {
            return;
        }

        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                $"Call {nameof(GlueHeadlessHost)}.{nameof(EnsureInitialized)} before {nameof(EnsureMsBuildEnvironmentVariable)}.");
        }

        MainGlueWindow.SetMsBuildEnvironmentVariable();
    }

    /// <summary>
    /// Whether MSBUILD_EXE_PATH is currently set in this process.
    /// </summary>
    public static bool IsMsBuildEnvironmentVariableSet =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH"));
}
