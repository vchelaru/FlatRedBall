using System;
using System.Collections.Generic;
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

// Issue #1979. Glue decides to declare Gum.Wireframe.INineSliceRuntime on the generated NineSliceRuntime
// from the Glue file version alone (NineSliceCodeGenerator.AddAdditionalInheritance), but it generates the
// *members* from the variables in the project's own NineSlice.gutx. Those are two different sources of
// truth, and they disagree for every project written before Gum added BorderScale/IsTilingMiddleSections:
// Glue never runs Gum's load-time standard-element back-fill (GumProjectSave.Initialize - it only calls
// GumProjectSave.Load), so a stale .gutx is exactly what codegen sees, forever. The result is a class that
// declares an interface it doesn't implement - CS0535 in the user's game, for a variable they never touched.
//
// The interface is hand-written in the sibling Gum repo under #if FRB (Gum/Wireframe/
// CustomSetPropertyOnRenderable.cs) and is a migration surface: TrySetPropertyOnNineSlice is moving from
// setting the NineSlice renderable directly to setting the runtime through this interface, so it grows as
// that migration proceeds. Every member added there is a CS0535 in every FRB project until Glue catches up,
// which is why the compile-level guard in GumGeneratedCodeCompilesTests matters more than these tests -
// see LegacyGutxStandardElementRuntimes_ShouldCompileAgainstTheRealEngine. These pin the mechanism.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NineSliceRuntimeInterfaceContractTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public NineSliceRuntimeInterfaceContractTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
        };
        TaskManager.SynchronousMode = true;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
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
            System.IO.Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public void NineSlice_WithLegacyGutx_ShouldStillImplementEveryInterfaceMember()
    {
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var generatedSource = GenerateNineSliceFrom(BuildLegacyGutxNineSliceState());

        // The declaration side - unchanged by this fix, but if it ever stops being emitted the assertions
        // below would pass vacuously against a class that has no contract to honor.
        generatedSource.ShouldContain("global::Gum.Wireframe.INineSliceRuntime");

        generatedSource.ShouldContain("public float BorderScale");
        generatedSource.ShouldContain("public bool IsTilingMiddleSections");
    }

    [Fact]
    public void NineSlice_WithCurrentGutx_ShouldNotDeclareInterfaceMembersTwice()
    {
        // The project's own .gutx has to win when it already defines the variable. Generating the member
        // from both the .gutx and the interface contract is CS0102 (duplicate member) - the same way the
        // "Color" entry in StandardsCodeGenerator.variableNamesToAddForProperties once produced one.
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var currentState = Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("NineSlice");

        // Fixture sanity: this test only means something if the canonical schema really does carry the
        // variables, i.e. if there is something to duplicate.
        var variableNames = currentState.Variables.Select(variable => variable.GetRootName()).ToHashSet(StringComparer.Ordinal);
        variableNames.ShouldContain("BorderScale");
        variableNames.ShouldContain("IsTilingMiddleSections");

        var generatedSource = GenerateNineSliceFrom(currentState);

        CountPropertyDeclarations(generatedSource, "BorderScale").ShouldBe(1);
        CountPropertyDeclarations(generatedSource, "IsTilingMiddleSections").ShouldBe(1);
    }

    [Fact]
    public void NineSlice_BelowFrbRuntimeInterfaceVersion_ShouldNeitherDeclareNorImplementTheInterface()
    {
        // Below GluxVersions.GumHasFrbRuntimeInterfaces the interface is not declared, so there is no
        // contract to honor - and the members must not appear either. An older project's Gum runtime may
        // predate them entirely, which is the whole reason that gate exists.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GumHasFrbRuntimeInterfaces - 1;
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var generatedSource = GenerateNineSliceFrom(BuildLegacyGutxNineSliceState());

        generatedSource.ShouldNotContain("INineSliceRuntime");
        generatedSource.ShouldNotContain("BorderScale");
        generatedSource.ShouldNotContain("IsTilingMiddleSections");
    }

    private static string GenerateNineSliceFrom(Gum.DataTypes.Variables.StateSave defaultState)
    {
        var standardElementSave = new StandardElementSave { Name = "NineSlice" };
        standardElementSave.Initialize(defaultState);

        var codeBlock = new CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock)
            .ShouldBeTrue();

        return codeBlock.ToString();
    }

    // What a real project's NineSlice.gutx actually contains: the canonical schema minus the variables
    // added after that file was last written by a Gum Editor. Checked against
    // Samples/FormsSampleProject/FormsSampleProject/Content/GumProject/Standards/NineSlice.gutx, which has
    // neither - and which Glue will never back-fill, because it only calls GumProjectSave.Load and never
    // GumProjectSave.Initialize.
    private static Gum.DataTypes.Variables.StateSave BuildLegacyGutxNineSliceState()
    {
        var canonical = Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("NineSlice");

        var legacy = canonical.Clone();
        legacy.Variables.RemoveAll(variable =>
            variable.GetRootName() is "BorderScale" or "IsTilingMiddleSections");

        return legacy;
    }

    // A generated property declaration is "<modifiers> <type> Name" alone on its line (ICodeBlock.Property),
    // so anchoring on the end of the line distinguishes the declaration from the state-switch assignments
    // and getter/setter bodies that also mention the name.
    private static int CountPropertyDeclarations(string generatedSource, string memberName)
    {
        return System.Text.RegularExpressions.Regex.Matches(
            generatedSource,
            @"(?m)^\s*public\b[^\r\n;=]*?\b" + System.Text.RegularExpressions.Regex.Escape(memberName) + @"\s*$").Count;
    }
}
