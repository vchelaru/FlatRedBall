using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

// The guard the Rectangle/Circle (#1907), Arc-gradient, Text-dropshadow (#1948) and Skia-DropshadowBlur
// bugs all needed and none of them had: it *compiles* the generated Gum runtimes against the real engine
// instead of checking their members against a list.
//
// Why compiling is the thing. GumRuntimeMemberContractTests reflects over the built engine and asserts
// every ContainedXxx.Member the generator emits exists on the mapped runtime type. That catches CS1061,
// but only for variables it knows to feed in - and it builds its fixture from FRB's *own* schema
// (Gum.Managers.StandardElementsManager plus GumPlugin.Managers.SkiaStandardElementsManager). FRB's copy
// of the Skia schema is hand-maintained and has diverged from Gum's, so that sweep compared a
// self-consistent copy against itself and stayed green while real projects broke.
//
// Codegen does not emit from FRB's schema. It emits from the variables in the project's .gutx - whatever
// the Gum Editor that last saved it wrote there. So this test feeds the generator *Gum's* canonical
// states (StandardElementsManager.RegisterExtendedDefaultStates, which is what the editor writes) and then
// asks the C# compiler, rather than a name list, whether the result is valid. That covers the whole class
// in one assertion: missing members (CS1061), missing types (CS0234/CS0246), and type drift (CS0029/CS0266).
//
// The scratch project ProjectReferences the engine rather than hand-listing assemblies deliberately:
// MonoGame.Framework.dll (which the generated "Color" property needs) is not in the engine's build output
// at all, it comes from NuGet. Letting MSBuild resolve the closure avoids pinning a MonoGame version here.
//
// Tagged BuildSmoke because it shells out to `dotnet build`, same as GumRuntimeMemberContractTests and
// NewProjectCreationSmokeTests. glue.yml and pr-tests.yml already run Category=BuildSmoke.
[Trait("Category", "BuildSmoke")]
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumGeneratedCodeCompilesTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumGeneratedCodeCompilesTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        // LatestVersion stands in for a real, current project - see GumSkiaRenderableCodegenSweepTests'
        // header for why the Color property's ToXNA conversion makes this load-bearing rather than cosmetic.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
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
    public void GeneratedStandardElementRuntimes_ShouldCompileAgainstTheRealEngine()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var repoRoot = FindRepoRoot();

        var elementNames = GetElementNamesFromGumsOwnSchema();

        // Coverage sanity - a sweep that degenerates to nothing is worse than no test at all.
        elementNames.ShouldContain("Text");
        elementNames.ShouldContain("Sprite");

        var generated = new Dictionary<string, string>(StringComparer.Ordinal);
        var skipped = new List<string>();

        using (new GumSchemaWins())
        {
            // Fixture sanity: prove we're generating from the editor's schema, not FRB's shadowing copy.
            // Without this the test can pass vacuously - see GumSchemaWins for why that's the whole
            // ballgame. FRB's Arc has no dropshadow family at all and its sibling Skia shapes use the
            // per-axis DropshadowBlurX; Gum's Arc has the dropshadow family with the single scalar
            // DropshadowBlur. If Gum renames this again, this assertion failing is the correct outcome -
            // it means the fixture stopped tracking the editor.
            var arcVariables = GetGumsDefaultStateFor("Arc").ShouldNotBeNull()
                .Variables.Select(variable => variable.GetRootName()).ToHashSet(StringComparer.Ordinal);
            arcVariables.ShouldContain("DropshadowBlur",
                "Gum's Arc schema is not what's driving generation - FRB's copy is still shadowing it.");
            arcVariables.ShouldNotContain("DropshadowBlurX",
                "FRB's own Arc schema is still shadowing Gum's, so this test would check FRB against itself.");

            foreach (var elementName in elementNames)
            {
                var defaultState = GetGumsDefaultStateFor(elementName);
                if (defaultState == null)
                {
                    skipped.Add(elementName + " (Gum has no default state for it)");
                    continue;
                }

                var standardElementSave = new StandardElementSave { Name = elementName };
                standardElementSave.Initialize(defaultState);

                // The production entry point CodeGeneratorManager uses, so the file this test compiles is
                // byte-for-byte what Glue writes into a user's GumRuntimes folder - including the
                // `using System.Linq;` and namespace wrapper that GenerateStandardElementSaveCodeFor alone
                // doesn't emit. Reconstructing that wrapper by hand here would be a second thing to keep in
                // sync with production, which is the mistake this whole test exists to stop making.
                var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(standardElementSave);
                if (string.IsNullOrWhiteSpace(source))
                {
                    skipped.Add(elementName + " (GenerateCodeFor produced nothing)");
                    continue;
                }

                generated[elementName] = source;
            }
        }

        generated.ShouldNotBeEmpty();

        var scratchDirectory = Path.Combine(_tempProjectDirectory, "CompileScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generated);

        var (exitCode, output) = RunDotnetBuild(Path.Combine(scratchDirectory, "GumCodegenCompileScratch.csproj"));

        var errors = output
            .Split('\n')
            .Where(line => line.Contains(": error ", StringComparison.Ordinal))
            .Select(line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Report which elements were and weren't covered, so a shrinking sweep is visible rather than silent.
        var coverage =
            "Compiled: " + string.Join(", ", generated.Keys.OrderBy(name => name, StringComparer.Ordinal)) +
            Environment.NewLine +
            "Not generated: " + (skipped.Count == 0 ? "(none)" : string.Join(", ", skipped));

        exitCode.ShouldBe(0,
            "The generated Gum standard element runtimes do not compile against the real engine. Every " +
            "error below is one a user would hit in their own game after opening their project in a " +
            "current Gum Editor:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors) + Environment.NewLine + Environment.NewLine + coverage);
    }

    // The standard elements Glue generates runtimes for, read straight off the generator's own map so an
    // element added there is covered the day it is added.
    private static List<string> GetElementNamesFromGumsOwnSchema()
    {
        return StandardsCodeGenerator.Self.StandardElementToQualifiedTypes
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .Select(pair => pair.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    // Gum's canonical default state - what a current Gum Editor writes into a .gutx. RegisterExtendedDefaultStates
    // surfaces the shape/Skia types (Arc/ColoredCircle/RoundedRectangle/Line/Canvas/Svg/LottieAnimation) to
    // consumers that don't load Gum's WPF Skia plugin, which is exactly this test's situation.
    private static Gum.DataTypes.Variables.StateSave GetGumsDefaultStateFor(string elementName)
    {
        return Gum.Managers.StandardElementsManager.Self
            .TryGetDefaultStateFor(elementName, throwExceptionOnMissing: false);
    }

    // THE load-bearing part of this fixture. TryGetDefaultStateFor checks DefaultStates before falling
    // through to CustomGetDefaultState, and MainGumPlugin.StartUp (and any sibling test that calls
    // SkiaStandardElementsManager.AddSkiaStandards) puts FRB's own hand-maintained copies of the Skia
    // schemas into DefaultStates. DefaultStates is process-wide, so whether they're present here depends on
    // what else ran first.
    //
    // If FRB's copies win the lookup, this test compiles FRB's schema against FRB's engine, finds them
    // trivially consistent, and goes green forever - precisely how the previous guard missed the
    // DropshadowBlurX/DropshadowBlur divergence.
    //
    // Overwrite rather than remove: codegen re-reads DefaultStates mid-generation
    // (StateCodeGenerator.GetIfShouldGenerateVariableInStateSetter) and throws KeyNotFoundException if the
    // element is absent, so the entry has to be present - just holding Gum's version, which is what a .gutx
    // written by a current editor actually contains.
    private sealed class GumSchemaWins : IDisposable
    {
        private readonly Dictionary<string, Gum.DataTypes.Variables.StateSave> _replaced = new(StringComparer.Ordinal);
        private readonly List<string> _added = new();

        public GumSchemaWins()
        {
            var manager = Gum.Managers.StandardElementsManager.Self;
            manager.RegisterExtendedDefaultStates();

            foreach (var elementName in ElementsGumCanAnswerFor)
            {
                // Bypasses TryGetDefaultStateFor deliberately - that would hand back FRB's shadowing copy,
                // which is the thing being replaced.
                var gumState = manager.CustomGetDefaultState?.Invoke(elementName);
                if (gumState == null)
                {
                    continue;
                }

                if (manager.DefaultStates.TryGetValue(elementName, out var frbCopy))
                {
                    _replaced[elementName] = frbCopy;
                }
                else
                {
                    _added.Add(elementName);
                }

                manager.DefaultStates[elementName] = gumState;
            }
        }

        public void Dispose()
        {
            var defaultStates = Gum.Managers.StandardElementsManager.Self.DefaultStates;
            foreach (var pair in _replaced)
            {
                defaultStates[pair.Key] = pair.Value;
            }
            foreach (var elementName in _added)
            {
                defaultStates.Remove(elementName);
            }
        }
    }

    // Mirrors Gum's GetExtendedDefaultState switch - the types Gum can supply, and therefore the ones whose
    // FRB duplicate must not shadow it.
    private static readonly string[] ElementsGumCanAnswerFor =
    {
        "Arc", "ColoredCircle", "RoundedRectangle", "Line", "Canvas", "Svg", "LottieAnimation",
    };

    private static void WriteScratchProject(
        string scratchDirectory, string repoRoot, Dictionary<string, string> generated)
    {
        Directory.CreateDirectory(scratchDirectory);

        var formsProject = Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms",
            "FlatRedBall.Forms.DesktopGlNet6", "FlatRedBall.Forms.DesktopGlNet6.csproj");
        var skiaProject = Path.Combine(repoRoot, "Engines", "SkiaGum", "SkiaInGum.csproj");

        File.Exists(formsProject).ShouldBeTrue($"Expected the engine Forms project at {formsProject}");
        File.Exists(skiaProject).ShouldBeTrue($"Expected the SkiaInGum project at {skiaProject}");

        // The engine projects mark their MonoGame PackageReference private, so it doesn't flow through
        // ProjectReference and Microsoft.Xna.Framework.Color (the generated "Color" property's type) won't
        // resolve without naming it here. Read the version out of the engine project rather than pinning a
        // copy, so a MonoGame bump there can't silently turn this test into a framework-mismatch failure.
        var monoGameVersion = ReadNet6MonoGameVersion(skiaProject);

        // net6.0 to match what the engine projects build - a mismatch here fails as a framework error rather
        // than as the codegen errors this test exists to surface.
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
                <Nullable>disable</Nullable>
                <LangVersion>latest</LangVersion>
                <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{formsProject}" />
                <ProjectReference Include="{skiaProject}" />
                <PackageReference Include="MonoGame.Framework.DesktopGL" Version="{monoGameVersion}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(scratchDirectory, "GumCodegenCompileScratch.csproj"), csproj);

        foreach (var pair in generated)
        {
            File.WriteAllText(Path.Combine(scratchDirectory, pair.Key + "Runtime.Generated.cs"), pair.Value);
        }
    }

    // Mirrors the net6 arm of the engine's conditional MonoGame reference (3.8.4.1 is net8-only, see the
    // comment in SkiaInGum.csproj).
    private static string ReadNet6MonoGameVersion(string skiaProjectPath)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(skiaProjectPath),
            """<PackageReference\s+Include="MonoGame\.Framework\.DesktopGL"\s+Version="([^"]+)"\s+Condition="'\$\(TargetFramework\)'\s*!=\s*'net8\.0'""");

        match.Success.ShouldBeTrue(
            $"Could not read the net6 MonoGame version out of {skiaProjectPath}. If the engine's MonoGame " +
            "reference was restructured, this test's scratch project needs to follow it.");

        return match.Groups[1].Value;
    }

    private static (int exitCode, string output) RunDotnetBuild(string projectPath)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"build \"{projectPath}\" -c Debug")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

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
}
