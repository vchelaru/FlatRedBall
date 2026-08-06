using System;
using System.Collections.Generic;
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

    // Issue #1967 bug (round 2) - the sweep above never exercises the v3 (IsRectangleFillStrokeSupported)
    // Rectangle path: it never sets Gum.Managers.ObjectFinder.Self.GumProjectSave.Version, which defaults
    // to AttributeVersion (2, GumProjectSave's own documented GOTCHA), so it always generates the v2
    // LineRectangle branch. That's exactly how `this.RenderableComponent = new FilledStrokedRectangle();`
    // (CS0200 - RenderableComponent is get-only) shipped past both this test and the string-matching unit
    // tests in RectangleFillStrokeCodegenTests: nothing actually compiled the v3 branch's constructor
    // against the real GraphicalUiElement API. This targets that path directly, the same way
    // GumRuntimeMemberContractTests.V3RectangleFillStrokeMembers_ShouldExistOnFilledStrokedRectangle
    // targets it for member-existence rather than full compilation.
    [Fact]
    public void V3RectangleConstructor_ShouldCompileAgainstTheRealGraphicalUiElementApi()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        Gum.Managers.ObjectFinder.Self.GumProjectSave.Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

        var defaultState = GetGumsDefaultStateFor("Rectangle");
        defaultState.ShouldNotBeNull();

        var standardElementSave = new StandardElementSave { Name = "Rectangle" };
        standardElementSave.Initialize(defaultState);

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(standardElementSave);
        source.ShouldNotBeNullOrWhiteSpace();
        // Fixture sanity: if this fixture ever stops actually reaching the v3 branch, the compile below
        // would trivially pass on the (already-covered) v2 path and this test would stop pinning anything.
        source.ShouldContain("FilledStrokedRectangle");

        var scratchDirectory = Path.Combine(_tempProjectDirectory, "V3RectangleCompileScratch");
        WriteScratchProject(scratchDirectory, FindRepoRoot(), new Dictionary<string, string> { ["Rectangle"] = source });

        var (exitCode, output) = RunDotnetBuild(Path.Combine(scratchDirectory, "GumCodegenCompileScratch.csproj"));

        var errors = output
            .Split('\n')
            .Where(line => line.Contains(": error ", StringComparison.Ordinal))
            .Select(line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        exitCode.ShouldBe(0,
            "The v3 Rectangle runtime does not compile against the real GraphicalUiElement API:" +
            Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    // Issue #1967 bug (round 3) - the compile-only test above proves the v3 Rectangle runtime is valid
    // C#, but a compile can never catch a construction-time NRE. The real bug: a Rectangle placed as a
    // named INSTANCE inside a screen/component - the common case - is constructed via
    // GumRuntime.ElementSaveExtensions.CreateGueForElement, whose fullInstantiation parameter DEFAULTS TO
    // FALSE (see InstanceSaveExtensionMethods.ToGraphicalUiElement -> ElementSaveExtensions.
    // ToGraphicalUiElement in the sibling Gum repo, which never passes it). The v3 constructor fix only
    // ran inside `if (fullInstantiation)`, so for every screen-placed Rectangle it never ran at all - the
    // contained object stayed unset, SetGraphicalUiElement's own fallback then created a plain
    // LineRectangle (Gum's FallbackRenderableFactory, no v3 awareness), and SetInitialState's state-switch
    // NREs on ContainedRectangle.FillColor. This test reproduces that exact path for real: builds an
    // executable scratch project, constructs the generated RectangleRuntime with fullInstantiation:false
    // and then calls SetGraphicalUiElement externally - precisely what CreateGueForElement's default does
    // - and asserts the process actually runs to completion instead of throwing.
    [Fact]
    public void V3RectangleRuntime_ConstructedAsScreenInstance_ShouldNotThrowDuringSetInitialState()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        Gum.Managers.ObjectFinder.Self.GumProjectSave.Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

        var defaultState = GetGumsDefaultStateFor("Rectangle");
        defaultState.ShouldNotBeNull();

        var standardElementSave = new StandardElementSave { Name = "Rectangle" };
        standardElementSave.Initialize(defaultState);

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(standardElementSave);
        source.ShouldNotBeNullOrWhiteSpace();
        source.ShouldContain("FilledStrokedRectangle");

        var scratchDirectory = Path.Combine(_tempProjectDirectory, "V3RectangleRuntimeScratch");
        var repoRoot = FindRepoRoot();
        WriteScratchProject(scratchDirectory, repoRoot, new Dictionary<string, string> { ["Rectangle"] = source });

        // WriteScratchProject writes a class-library csproj with no entry point - flip it to an
        // executable and add one that reproduces the real screen-instance construction path.
        var csprojPath = Path.Combine(scratchDirectory, "GumCodegenCompileScratch.csproj");
        var csprojContent = File.ReadAllText(csprojPath)
            .Replace("<Nullable>disable</Nullable>", "<Nullable>disable</Nullable>\n    <OutputType>Exe</OutputType>");
        File.WriteAllText(csprojPath, csprojContent);

        File.WriteAllText(Path.Combine(scratchDirectory, "Program.cs"), """
            using System;
            using Gum.DataTypes;
            using Gum.Managers;
            using GumRuntime;
            using TestProject.GumRuntimes;

            StandardElementsManager.Self.Initialize();

            // Real games wire this in generated Game1 code (GumGame1CodeGenerator's GetRenderable ->
            // Gum.Wireframe.RuntimeObjectCreator.TryHandleAsBaseType) - without it, SetGraphicalUiElement's
            // fallback throws before ever reaching the actual bug, masking it behind a different exception.
            ElementSaveExtensions.CustomCreateGraphicalComponentFunc =
                (name, managers) => Gum.Wireframe.FallbackRenderableFactory.TryHandleAsBaseType(name, managers);

            var elementSave = new StandardElementSave { Name = "Rectangle" };
            elementSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Rectangle"));

            ObjectFinder.Self.GumProjectSave = new GumProjectSave
            {
                Version = 3,
                FullFileName = "C:\\FakeGumProject\\GumProject.gumx",
            };
            ObjectFinder.Self.GumProjectSave.StandardElements.Add(elementSave);

            try
            {
                // Mirrors ElementSaveExtensions.ToGraphicalUiElement's real call shape: construct with the
                // default fullInstantiation:false (as CreateGueForElement does for every screen/component
                // instance), then call SetGraphicalUiElement externally - the caller's responsibility in
                // that path, per that method's own "could have already been created" contract.
                var instance = new RectangleRuntime(fullInstantiation: false, tryCreateFormsObject: false);
                elementSave.SetGraphicalUiElement(instance, null);
                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex);
                Environment.Exit(1);
            }
            """);

        var (exitCode, output) = RunDotnetRun(scratchDirectory);

        exitCode.ShouldBe(0,
            "Constructing the v3 Rectangle runtime the way a screen/component instance really is " +
            "(fullInstantiation:false, then an external SetGraphicalUiElement call) threw:" +
            Environment.NewLine + output);
        output.ShouldContain("OK");
    }

    // Issue #1979. The sweep at the top of this file builds its fixtures from Gum's *canonical* schema,
    // which is a current Gum Editor's output - so it never sees the case that actually breaks users. Glue
    // does not back-fill a loaded project's standard elements (it calls GumProjectSave.Load and never
    // GumProjectSave.Initialize), so codegen emits from whatever an older Gum Editor last wrote into the
    // .gutx, forever. Meanwhile ProjectLoader.LoadProject bumps the .gluj's FileVersion to LatestVersion on
    // load, which switches on every version-gated decision - including declaring the
    // Gum.Wireframe.I*Runtime interfaces. A stale .gutx plus a current FileVersion is the combination that
    // produced CS0535: a runtime declaring an interface whose members its .gutx never mentioned.
    //
    // So this loads a real checked-in project's .gumx through the same GumProjectSave.Load call Glue makes
    // and compiles what the generator produces for its standard elements. That covers the whole interface
    // family (NineSlice/Sprite/Container/Polygon) with no list of member names anywhere - which matters
    // because those interfaces are a migration surface in the sibling Gum repo (TrySetPropertyOnNineSlice
    // moving from the renderable to the runtime), so they gain members over time. Every one of those
    // additions lands here as a compile error instead of in a user's game.
    [Fact]
    public void LegacyGutxStandardElementRuntimes_ShouldCompileAgainstTheRealEngine()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var repoRoot = FindRepoRoot();

        var gumxPath = Path.Combine(repoRoot, "Samples", "FormsSampleProject", "FormsSampleProject",
            "Content", "GumProject", "GumProject.gumx");
        File.Exists(gumxPath).ShouldBeTrue($"Expected the FormsSampleProject Gum project at {gumxPath}");

        var gumProject = Gum.DataTypes.GumProjectSave.Load(gumxPath, out var loadResult);
        gumProject.ShouldNotBeNull($"Could not load {gumxPath}: {loadResult?.ErrorMessage}");

        // The real call path: codegen reads the loaded project for version-gated decisions
        // (StandardsCodeGenerator.IsRectangleFillStrokeSupported), so it has to be the sample's own
        // project rather than the empty stand-in this class's constructor installs.
        Gum.Managers.ObjectFinder.Self.GumProjectSave = gumProject;

        // Exactly what GumPlugin.Managers.FileReferenceTracker.InitializeElements does after Load, and the
        // reason this whole bug exists. Note what it passes: the element's OWN default state, not
        // StandardElementsManager's canonical one - so it resolves variable types (a .gutx stores enum
        // values as plain ints, and without this the state setter emits "Blend.0") while reconciling
        // against nothing. Glue's own comment there calls it "only a subset of initialization ... for
        // performance reasons". Skipping this step here generated code no version of Glue would produce;
        // skipping the canonical reconciliation is what leaves a real project's NineSlice.gutx permanently
        // without BorderScale.
        foreach (var standardElement in gumProject.StandardElements)
        {
            standardElement.Initialize(standardElement.DefaultState);
        }

        var nineSlice = gumProject.StandardElements
            .FirstOrDefault(element => element.Name == "NineSlice");
        nineSlice.ShouldNotBeNull("FormsSampleProject has no NineSlice standard element to check.");

        // Fixture sanity, and the whole reason this test is distinct from the canonical-schema sweep
        // above. If the sample is ever re-saved by a current Gum Editor these variables appear, the .gutx
        // stops being "legacy", and this test silently degenerates into a duplicate of that sweep. Failing
        // here is the correct outcome - point it at a project that is still old, or retire the test.
        var nineSliceVariables = nineSlice.DefaultState.Variables
            .Select(variable => variable.GetRootName())
            .ToHashSet(StringComparer.Ordinal);
        nineSliceVariables.ShouldNotContain("BorderScale",
            "FormsSampleProject's NineSlice.gutx has been regenerated, so it no longer reproduces the " +
            "stale-.gutx case this test exists for.");
        nineSliceVariables.ShouldNotContain("IsTilingMiddleSections",
            "FormsSampleProject's NineSlice.gutx has been regenerated, so it no longer reproduces the " +
            "stale-.gutx case this test exists for.");

        var generated = new Dictionary<string, string>(StringComparer.Ordinal);
        var skipped = new List<string>();

        foreach (var standardElement in gumProject.StandardElements.OrderBy(element => element.Name, StringComparer.Ordinal))
        {
            var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(standardElement);
            if (string.IsNullOrWhiteSpace(source))
            {
                skipped.Add(standardElement.Name + " (GenerateCodeFor produced nothing)");
                continue;
            }

            generated[standardElement.Name] = source;
        }

        generated.ShouldNotBeEmpty();

        // Fixture sanity: without the interface declaration there is no contract for the compile to check,
        // and this test would pass against a runtime that promises nothing.
        generated["NineSlice"].ShouldContain("global::Gum.Wireframe.INineSliceRuntime");

        var scratchDirectory = Path.Combine(_tempProjectDirectory, "LegacyGutxCompileScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generated);

        var (exitCode, output) = RunDotnetBuild(Path.Combine(scratchDirectory, "GumCodegenCompileScratch.csproj"));

        var errors = output
            .Split('\n')
            .Where(line => line.Contains(": error ", StringComparison.Ordinal))
            .Select(line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var coverage =
            "Compiled: " + string.Join(", ", generated.Keys.OrderBy(name => name, StringComparer.Ordinal)) +
            Environment.NewLine +
            "Not generated: " + (skipped.Count == 0 ? "(none)" : string.Join(", ", skipped));

        exitCode.ShouldBe(0,
            "The Gum standard element runtimes generated from a real project's own (older) .gutx do not " +
            "compile against the real engine. A CS0535 here means Glue declared a Gum.Wireframe.I*Runtime " +
            "interface it did not generate the members for:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors) + Environment.NewLine + Environment.NewLine + coverage);
    }

    private static (int exitCode, string output) RunDotnetRun(string projectDirectory) =>
        NestedDotnetCli.Run($"run --project \"{projectDirectory}\" -c Debug");

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

    private static (int exitCode, string output) RunDotnetBuild(string projectPath) =>
        NestedDotnetCli.Run($"build \"{projectPath}\" -c Debug");

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
