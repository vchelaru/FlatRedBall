using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Shouldly;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Builds <c>Samples/Platformer/LadderDemo</c> (a real checked-in Glue project, not a purpose-built
/// fixture) exactly once per test process and shares the resulting assemblies across every test that
/// needs to reflect into the generated Player entity - see the "Runtime-testing generated code" section
/// of the glue-project-codegen skill for why a build-and-reflect is required at all (compiling proves
/// the generated code resolves against the engine, not that its logic is correct).
///
/// Callers must be reflection-only after <see cref="LoadOnceAsync"/> completes: no test may call a Glue
/// command that mutates or regenerates the project, or the memoized build stops representing what every
/// other test observes. GlueUnitTests runs its whole assembly non-parallel (see
/// glue-unit-test-bootstrap), so the lazy Task is race-free without extra locking beyond what Lazy
/// itself provides.
/// </summary>
internal static class PlatformerLadderGoldProject
{
    public sealed class Loaded
    {
        public required string ProjectRoot { get; init; }
        public required Assembly GameAssembly { get; init; }
        public required Assembly EngineAssembly { get; init; }
    }

    /// <summary>
    /// Name of a bare platformer entity added to the gold project purely for reflection tests. Player
    /// (the real, documented sample entity) wires up animation content in its hand-written
    /// CustomInitialize/PostInitialize, which needs real content loading to construct headlessly - the
    /// same reason GoldProjectCompileTests' own platformer test uses a fresh, content-free entity
    /// rather than an existing sample's fully-featured one.
    /// </summary>
    public const string SmokeTestEntityName = "LadderFloorTransitionSmokeTestEntity";

    // Deliberately never disposed: the copied-out TempDir needs to outlive every test in the process,
    // and GlueUnitTests is a short-lived test-runner process where the OS reclaims temp files anyway.
    static readonly Lazy<Task<Loaded>> _load = new(BuildAsync);

    public static Task<Loaded> LoadOnceAsync() => _load.Value;

    static async Task<Loaded> BuildAsync()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var project = GoldProject.CopyOutOfRepo("Samples/Platformer/LadderDemo");
        var csproj = Path.Combine(project.Root, "LadderDemo", "LadderDemo.csproj");

        GoldProject.DeleteGeneratedCode(project.Root);

        await GoldProject.LoadInGlueAsync(csproj);

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        // Mirrors GoldProjectCompileTests.FormsSampleProject_WithAddedPlatformerEntity_..., but adding
        // to LadderDemo instead of standing up a separate project - the whole point is one build.
        var smokeTestEntity = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self
            .GluxCommands.EntityCommands.AddEntity("Entities\\" + SmokeTestEntityName, is2D: true);

        // MainController's view model is a process-wide singleton (see its ResetViewModel doc) - a
        // leftover IsPlatformer = true from another gold-project test in this same BuildSmoke run
        // would make the assignment below a silent no-op, leaving this entity's Properties bag
        // never actually marked as a platformer and skipping all of EntityCodeGenerator's output.
        FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.ResetViewModel();
        var viewModel = FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.GetViewModel();
        viewModel.BackingData = smokeTestEntity;
        viewModel.IsPlatformer = true;
        await FlatRedBall.Glue.Managers.TaskManager.Self.WaitForAllTasksFinished();

        // Test-only observation hooks: appended to the scaffolded hand-written partial class so
        // ApplyClimbingInput's OnLadderTopReached/OnLadderBottomReached calls have something to bind
        // to (an unimplemented partial method call compiles to nothing) without depending on
        // PlatformerValuesStatic/CSV content loading the way the real Player.cs sample does.
        var smokeTestEntityCsPath = Path.Combine(project.Root, "LadderDemo", "Entities", SmokeTestEntityName + ".cs");
        File.Exists(smokeTestEntityCsPath).ShouldBeTrue($"Expected Glue to scaffold {smokeTestEntityCsPath}");
        var smokeTestEntitySource = File.ReadAllText(smokeTestEntityCsPath);
        // The scaffolded file is "namespace X\n{\n    public partial class Y\n    {...\n    }\n}" -
        // insert before the CLASS's closing brace (4-space indented), not the outer namespace brace
        // (column 0), or the members land outside any type and the compiler rejects them (CS1519).
        var classClosingBrace = smokeTestEntitySource.LastIndexOf("\n    }");
        classClosingBrace.ShouldBeGreaterThan(-1);
        smokeTestEntitySource = smokeTestEntitySource.Insert(classClosingBrace, @"

        public bool ReachedTopOfLadder;
        public bool ReachedBottomOfLadder;
        partial void OnLadderTopReached() { ReachedTopOfLadder = true; }
        partial void OnLadderBottomReached() { ReachedBottomOfLadder = true; }");
        File.WriteAllText(smokeTestEntityCsPath, smokeTestEntitySource);

        // Unlike GoldProjectCompileTests' FormsSampleProject test (the first platformer entity ever
        // added to that project, so the shared PlatformerValues custom class did not exist yet),
        // LadderDemo already has Player as a platformer entity - the shared custom class already
        // exists and regenerates via the plain LoadInGlueAsync load above, so no explicit
        // GenerateCustomClassesCode() call is needed here.
        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(smokeTestEntity);
        await FlatRedBall.Glue.Managers.TaskManager.Self.WaitForAllTasksFinished();

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        // Adding a second platformer entity to a project that already has one leaves a stray
        // <Compile Include="DataTypes\PlatformerValuesStatic.Generated.cs" /> in the csproj that no
        // generator ever writes (the shared PlatformerValues custom class - and its static CSV
        // dictionary - lives entirely in DataTypes\PlatformerValues.Generated.cs, which Player already
        // references). This looks like a pre-existing Glue csproj-sync bug independent of #2148; worth
        // its own issue, but out of scope here - strip the dangling reference so the build isn't
        // blocked by it.
        RemoveDanglingCompileItems(csproj);

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed for the LadderDemo gold project:\n{output}");

        var binDir = Path.Combine(project.Root, "LadderDemo", "bin", "Debug", "net6.0");
        var gameAssemblyPath = Path.Combine(binDir, "LadderDemo.dll");
        File.Exists(gameAssemblyPath).ShouldBeTrue($"Expected build output at {gameAssemblyPath}");
        var gameAssembly = Assembly.LoadFrom(gameAssemblyPath);

        // LadderDemo references the engine via PackageReference (a pinned NuGet version), not
        // ProjectReference to engine source like FormsSampleProject/Beefball - so this proves the
        // Platformer codegen fix works against a real, published engine, not that it still matches
        // current engine source. That's an accepted tradeoff for reusing the shipped ladder sample
        // rather than standing up a new source-linked fixture project.
        var engineAssemblyPath = Path.Combine(binDir, "FlatRedBallDesktopGLNet6.dll");
        File.Exists(engineAssemblyPath).ShouldBeTrue($"Expected engine assembly at {engineAssemblyPath}");
        // Glue itself already has some version of FlatRedBallDesktopGLNet6 loaded in this process (a
        // plugin references it directly) - loading a second, differently-versioned copy of the same
        // simple name throws FileLoadException. gameAssembly's own type graph is already bound to
        // whichever copy is resident, so reuse that one instead of forcing LadderDemo's pinned
        // PackageReference version to load side by side with it.
        var engineAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "FlatRedBallDesktopGLNet6")
            ?? Assembly.LoadFrom(engineAssemblyPath);

        // Needed before constructing any PositionedObject-derived entity - LoadStaticContent reaches
        // FlatRedBallServices.GetContentManagerByName, which locks a static dictionary that is null
        // until this runs. See GoldProjectCompileTests' platformer test for the same requirement.
        // GoldProject.EnsureEngineInitialized (not a bare InitializeCommandLine() call) because this
        // resident engineAssembly instance may already be initialized by another gold-project test that
        // ran earlier in this same process.
        GoldProject.EnsureEngineInitialized(engineAssembly);

        return new Loaded
        {
            ProjectRoot = project.Root,
            GameAssembly = gameAssembly,
            EngineAssembly = engineAssembly,
        };
    }

    /// <summary>
    /// Removes any &lt;Compile Include="..."&gt; item whose target file does not exist on disk. See
    /// the call site's comment for why this is currently needed.
    /// </summary>
    static void RemoveDanglingCompileItems(string csprojPath)
    {
        var projectDirectory = Path.GetDirectoryName(csprojPath)!;
        var lines = File.ReadAllLines(csprojPath);
        var kept = new System.Collections.Generic.List<string>(lines.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("<Compile Include=\"") && trimmed.Contains("/>"))
            {
                var start = trimmed.IndexOf('"') + 1;
                var end = trimmed.IndexOf('"', start);
                var relativePath = trimmed.Substring(start, end - start);
                var fullPath = Path.Combine(projectDirectory, relativePath.Replace('\\', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    continue;
                }
            }
            kept.Add(line);
        }
        File.WriteAllLines(csprojPath, kept);
    }
}
