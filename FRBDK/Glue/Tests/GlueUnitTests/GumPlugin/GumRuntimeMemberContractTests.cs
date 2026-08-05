using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// The general guard for the "Glue generates a member the Gum runtime doesn't have" bug class.
//
// This class has now shipped three separate times: Rectangle/Circle's fill/stroke/gradient family
// (#1907), the Arc/ColoredCircle/RoundedRectangle gradient CS0266, and Text's dropshadow channel +
// LocalizeText family. Each was caught only after a real game project failed to compile, and each got a
// test pinning that one element against that one hardcoded list of forbidden names - so each new test
// only ever caught a regression someone already knew to look for. Nobody had written one for Text.
//
// Why this needs a real build rather than reflection over Glue's own output: Glue has NO compile-time
// knowledge of the runtime types it generates against. RenderingLibrary.Graphics.Text is not a
// referenced assembly in any Glue project, and it is not among GumPlugin's ~129 embedded LibraryFiles
// resources either (LineRectangle.cs is embedded; Text.cs is not). StandardsCodeGenerator's
// mStandardElementToQualifiedTypes holds nothing but *strings*. That is precisely why the compiler can
// never catch this and why it keeps recurring - so the check has to be made against the compiled engine
// assemblies, which is what this test does.
//
// Tagged BuildSmoke because it shells out to `dotnet build` for the engine, same as
// NewProjectCreationSmokeTests. glue.yml and pr-tests.yml both already run Category=BuildSmoke, so this
// gates a release with no workflow change.
[Trait("Category", "BuildSmoke")]
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumRuntimeMemberContractTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumRuntimeMemberContractTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        // A real, current project. GumSkiaRenderableCodegenSweepTests' header documents why FileVersion
        // matters here: at the default 0 the version-gated Color conversion is not emitted.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.LatestVersion,
        };
        TaskManager.SynchronousMode = true;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave
        {
            FullFileName = Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
        };
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

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
    public void EveryGeneratedContainedMemberShouldExistOnTheEngineRuntimeType()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        // The six Skia standard elements (Arc/ColoredCircle/LottieAnimation/RoundedRectangle/Svg/Canvas)
        // are registered by MainGumPlugin.StartUp, not by StandardElementsManager.Initialize - same reason
        // GumSkiaRenderableCodegenSweepTests calls this. Without it they aren't in the schema at all.
        EnsureSkiaStandardElementsRegistered();

        var repoRoot = FindRepoRoot();
        var runtimeTypes = BuildAndLoadEngineRuntimeTypes(repoRoot);

        var elementToRuntimeType = GetStandardElementToQualifiedTypeMap();

        // Fixture sanity: if the map stops being readable this test would sweep nothing at all, which is
        // the exact failure mode that made an earlier version of RectangleCircleCodegenTests worthless.
        elementToRuntimeType.ShouldNotBeEmpty();
        elementToRuntimeType.Keys.ShouldContain("Text");

        var problems = new List<string>();
        // A type the map names but the built engine doesn't have is a real coverage hole - fail on it.
        var missingRuntimeTypes = new List<string>();
        // Map keys that aren't user-facing Gum standard elements (LineRectangle/SolidRectangle are
        // internal aliases with no schema of their own) - reported, but not a failure.
        var noSchema = new List<string>();
        var sweptElements = new List<string>();

        foreach (var kvp in elementToRuntimeType.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            var elementName = kvp.Key;
            var qualifiedTypeName = kvp.Value;

            // Container maps to null on purpose ("This could be any type, so don't force it to line
            // rectangle") - there is no single runtime type to check it against.
            if (string.IsNullOrEmpty(qualifiedTypeName))
            {
                continue;
            }

            if (!runtimeTypes.TryGetValue(qualifiedTypeName, out var runtimeType))
            {
                missingRuntimeTypes.Add($"{elementName} -> {qualifiedTypeName} (type not found in the built engine assemblies)");
                continue;
            }

            var defaultState = Gum.Managers.StandardElementsManager.Self
                .TryGetDefaultStateFor(elementName, throwExceptionOnMissing: false);
            if (defaultState == null)
            {
                noSchema.Add(elementName);
                continue;
            }

            // Built the same way GumPluginCommands.AddStandardElement builds a real one - Initialize()'d
            // from the canonical, always-current schema rather than a possibly-stale embedded template.
            // RectangleCircleCodegenTests' header documents why that distinction is load-bearing: a
            // fresh-project-creation fixture carries pre-v3 variables and would pin nothing.
            var standardElementSave = new StandardElementSave { Name = elementName };
            standardElementSave.Initialize(defaultState);

            var codeBlock = new CodeBlockBase();
            if (!StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock))
            {
                missingRuntimeTypes.Add($"{elementName} (GenerateStandardElementSaveCodeFor returned false)");
                continue;
            }

            sweptElements.Add(elementName);

            problems.AddRange(FindUnbackedMembers(elementName, codeBlock.ToString(), runtimeType, qualifiedTypeName));
        }

        var coverageReport =
            "Swept: " + string.Join(", ", sweptElements) + Environment.NewLine +
            "No schema (not user-facing standard elements, expected): " +
            (noSchema.Count == 0 ? "(none)" : string.Join(", ", noSchema));

        // The actual point of the test - asserted first so a real unbacked member is what you see.
        problems.ShouldBeEmpty(
            string.Join(Environment.NewLine, problems) + Environment.NewLine + Environment.NewLine + coverageReport);

        // Coverage sanity - a sweep that silently degenerates to nothing is worse than no test at all.
        sweptElements.ShouldContain("Text", coverageReport);
        sweptElements.ShouldContain("Sprite", coverageReport);
        missingRuntimeTypes.ShouldBeEmpty(
            "A standard element's runtime type is missing from the built engine assemblies, so it was " +
            "not checked at all:" + Environment.NewLine +
            string.Join(Environment.NewLine, missingRuntimeTypes) + Environment.NewLine + coverageReport);
    }

    // Proves the sweep above is general rather than a restatement of the six members that happened to
    // break Text. Nothing in this file names DropshadowBlur/LocalizeText/etc. - the fixture is built from
    // Gum's canonical schema, so any variable Gum adds later flows in on its own. This test pins that
    // property by injecting a variable Gum has never defined and asserting the sweep reports it. If a
    // future refactor accidentally narrows the sweep to a fixed list, or stops picking up newly-added
    // schema variables, this goes red while the sweep above would stay green.
    [Fact]
    public void Sweep_ShouldCatchAVariableAddedToTheSchemaLater()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        EnsureSkiaStandardElementsRegistered();

        var runtimeTypes = BuildAndLoadEngineRuntimeTypes(FindRepoRoot());
        runtimeTypes.TryGetValue("RenderingLibrary.Graphics.Text", out var textRuntimeType).ShouldBeTrue();

        const string injectedName = "SomeVariableGumHasNotInventedYet";
        textRuntimeType.GetProperty(injectedName).ShouldBeNull("the injected name must not really exist");

        var defaultState = Gum.Managers.StandardElementsManager.Self
            .TryGetDefaultStateFor("Text", throwExceptionOnMissing: false);
        defaultState.ShouldNotBeNull();

        var injected = new Gum.DataTypes.Variables.VariableSave
        {
            SetsValue = true,
            Type = "float",
            Value = 0f,
            Name = injectedName,
            Category = "Text",
        };

        // StandardElementsManager.Self.DefaultStates is process-wide state - always restore it, or every
        // test that runs after this one sweeps a schema with a bogus variable in it.
        defaultState.Variables.Add(injected);
        try
        {
            var standardElementSave = new StandardElementSave { Name = "Text" };
            standardElementSave.Initialize(defaultState);

            var codeBlock = new CodeBlockBase();
            StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock)
                .ShouldBeTrue();

            var unbacked = FindUnbackedMembers("Text", codeBlock.ToString(), textRuntimeType, "RenderingLibrary.Graphics.Text");

            unbacked.ShouldContain(
                problem => problem.Contains(injectedName, StringComparison.Ordinal),
                "The sweep did not flag a newly-added schema variable, which means it is no longer general. " +
                "Reported: " + string.Join(" | ", unbacked));
        }
        finally
        {
            defaultState.Variables.Remove(injected);
        }
    }

    // Issue #1967 - the sweep above is driven off StandardElementToQualifiedTypes, the STATIC map, which
    // still points "Rectangle" at LineRectangle. It therefore never exercises the v3
    // (IsRectangleFillStrokeSupported) path that backs Fill*/Stroke*/IsFilled/StrokeWidth with
    // RenderingLibrary.Math.Geometry.FilledStrokedRectangle instead - exactly the kind of gap the class
    // header warns about ("Glue has NO compile-time knowledge of the runtime types it generates
    // against"). This targets that path directly against the real built engine assembly.
    [Fact]
    public void V3RectangleFillStrokeMembers_ShouldExistOnFilledStrokedRectangle()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        Gum.Managers.ObjectFinder.Self.GumProjectSave!.Version =
            (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

        var runtimeTypes = BuildAndLoadEngineRuntimeTypes(FindRepoRoot());
        runtimeTypes.TryGetValue("RenderingLibrary.Math.Geometry.FilledStrokedRectangle", out var runtimeType)
            .ShouldBeTrue("Expected FilledStrokedRectangle to be part of the built GumCore assembly - " +
                "see the gum-shared-source skill if this is missing (a new Gum source file needs adding " +
                "to a FRB-consumed .csproj/.projitems, not just existing in the sibling Gum repo).");

        var defaultState = Gum.Managers.StandardElementsManager.Self
            .TryGetDefaultStateFor("Rectangle", throwExceptionOnMissing: false);
        defaultState.ShouldNotBeNull();

        var standardElementSave = new StandardElementSave { Name = "Rectangle" };
        standardElementSave.Initialize(defaultState);

        var codeBlock = new CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock)
            .ShouldBeTrue();

        var problems = FindUnbackedMembers(
            "Rectangle", codeBlock.ToString(), runtimeType, "RenderingLibrary.Math.Geometry.FilledStrokedRectangle");

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
    }

    // AddSkiaStandards() isn't idempotent (Dictionary.Add throws on a second call in the same process),
    // so guard it the same way GumSkiaRenderableCodegenSweepTests does.
    private static void EnsureSkiaStandardElementsRegistered()
    {
        if (!Gum.Managers.StandardElementsManager.Self.DefaultStates.ContainsKey("Svg"))
        {
            GumPlugin.Managers.SkiaStandardElementsManager.AddSkiaStandards();
        }
    }

    // Every member the generated runtime touches on its contained runtime object, checked against the
    // real type. Deliberately driven off whatever the generator emitted rather than any list of known
    // variable names - that's what makes this cover variables Gum adds in the future.
    private static List<string> FindUnbackedMembers(
        string elementName, string generatedSource, Type runtimeType, string qualifiedTypeName)
    {
        var containedPropertyName = "Contained" + elementName;

        return Regex
            .Matches(generatedSource, Regex.Escape(containedPropertyName) + @"\.(\w+)")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Where(memberName => !HasPublicMember(runtimeType, memberName))
            .Select(memberName =>
                $"{elementName}Runtime.Generated.cs references {containedPropertyName}.{memberName}, " +
                $"but {qualifiedTypeName} has no public member by that name. " +
                "Any project using this element will fail to compile with CS1061.")
            .ToList();
    }

    private static bool HasPublicMember(Type type, string memberName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        return type.GetProperty(memberName, flags) != null
            || type.GetField(memberName, flags) != null
            || type.GetMethods(flags).Any(method => method.Name == memberName);
    }

    // Builds the engine solution that produces the assemblies a generated Gum runtime actually compiles
    // against, then loads them for reflection. This is the whole point of the test - see the header for
    // why Glue's own output can't answer the question.
    private static Dictionary<string, Type> BuildAndLoadEngineRuntimeTypes(string repoRoot)
    {
        var formsSolution = Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms", "FlatRedBall.Forms.DesktopGLNet6.sln");
        File.Exists(formsSolution).ShouldBeTrue($"Expected the engine Forms solution at {formsSolution}");

        var (exitCode, output) = RunDotnetBuild(formsSolution);
        exitCode.ShouldBe(0, $"Could not build the engine Forms solution, so the runtime contract can't be checked:\n{output}");

        // SkiaInGum builds to its own project folder rather than into the Forms output, because Forms
        // only references it under the AutoBuild configurations.
        var assemblyPaths = new[]
        {
            Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms", "FlatRedBall.Forms.DesktopGlNet6", "bin", "Debug", "net6.0", "GumCore.DesktopGlNet6.dll"),
            Path.Combine(repoRoot, "Engines", "SkiaGum", "bin", "Debug", "net6.0", "SkiaInGum.dll"),
        };

        var types = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assemblyPath in assemblyPaths)
        {
            File.Exists(assemblyPath).ShouldBeTrue($"Expected a built engine assembly at {assemblyPath}");

            var assembly = Assembly.LoadFrom(assemblyPath);
            foreach (var type in assembly.GetTypes())
            {
                if (type.FullName != null)
                {
                    types[type.FullName] = type;
                }
            }
        }

        return types;
    }

    private static (int exitCode, string output) RunDotnetBuild(string solutionPath) =>
        NestedDotnetCli.Run($"build \"{solutionPath}\" -c Debug");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Engines", "SkiaGum")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the FlatRedBall repo root above " + AppContext.BaseDirectory);
    }

    private static Dictionary<string, string> GetStandardElementToQualifiedTypeMap()
    {
        // Read straight off the generator so this sweep can never drift out of sync with the set of
        // standard elements Glue actually generates - a standard element added to that map is covered
        // the day it is added, with no edit here.
        return new Dictionary<string, string>(StandardsCodeGenerator.Self.StandardElementToQualifiedTypes);
    }
}
