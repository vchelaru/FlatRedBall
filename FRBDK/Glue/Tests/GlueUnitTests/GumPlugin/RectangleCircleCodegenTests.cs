using System;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// GitHub issue #1907 - Gum's "Rectangle" (and "Circle") standard elements now define a
// fill/stroke/gradient/dropshadow/blend variable family (Gum's v3 standard surface -
// StandardElementsManager.AddFillAndStrokeVariables/AddGradientVariables/AddDropshadowVariables/
// AddBlendVariable), but Glue maps both to plain outline-only RenderingLibrary.Math.Geometry types
// (LineRectangle/LineCircle - see StandardsCodeGenerator.mStandardElementToQualifiedTypes), which
// can't back any of those members. Before the fix, RectangleRuntime.Generated.cs failed to compile
// with CS1061 for GradientY1/HasDropshadow/StrokeWidth/UseGradient/etc. (and CircleRuntime.Generated.cs
// had the identical latent bug - LineCircle is the same kind of outline-only shape - just not yet hit by
// any repro because no default template places a Circle).
//
// An earlier version of this test drove NewGumProjectCreationLogic.CreateGumProjectInternal (a freshly
// created, minimal Gum project) and inspected the resulting RectangleRuntime.Generated.cs. That did NOT
// pin the fix: a freshly-created project's "Rectangle"/"Circle" StandardElementSave is deserialized from
// GumPlugin's own embedded, static template resources (Embedded/EmptyProject/Standards/Rectangle.gutx,
// Circle.gutx) - snapshots saved before Gum's v3 fill/stroke/gradient/dropshadow/blend family existed
// (~24-28 <Variable> entries total, none of them from that family). Nothing in the fresh-project-creation
// path reconciles a deserialized StandardElementSave against the canonical, always-current schema in
// Gum.Managers.StandardElementsManager.Self.DefaultStates - that reconciliation
// (StandardElementSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor(name)), the same call
// GumPluginCommands.AddStandardElement makes) only runs when a user explicitly adds a standard element
// file to their project, not during fresh-project creation or project reload. So the old test's fixture
// never carried the vulnerable variables at all, regardless of the skip lists - it stayed green even with
// the fix's skip-list entries deleted entirely.
//
// This version builds the StandardElementSave fixture the same way GumPluginCommands.AddStandardElement
// does - Initialize()'d from the canonical StandardElementsManager.Self.GetDefaultStateFor(name), which is
// guaranteed to carry the full current variable family regardless of what any on-disk template contains -
// and feeds it directly to StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor (which itself
// calls both the property pipeline and, via GenerateStates, the state pipeline - one generated-code string
// covers both of the two skip lists described in the gum-codegen skill). This is closer to the actual
// decision point than round-tripping through file creation/codegen/file-read, and isn't coupled to
// NewGumProjectCreationLogic's specific (and, as above, stale) template contents.
//
// Side finding while building this fixture: the canonical v3 schema no longer has plain Red/Green/Blue/Alpha
// on Rectangle/Circle at all (the old test's "legit color family" assertion only passed because it read the
// same stale pre-v3 template - it has Red/Green/Blue/Alpha, not the v3 family). The one non-excluded,
// still-generated color surface today is StandardsCodeGenerator's own "Color" convenience property
// (variableNamesToAddForProperties) - that's what the sanity check below asserts on instead.
[Collection(nameof(TaskManagerSequentialCollection))]
public class RectangleCircleCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public RectangleCircleCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        // GenerateGetter/AdjustStandardElementVariableGetIfNecessary reads
        // GlueState.Self.CurrentMainProject.IsFrbSourceLinked() for Color-typed variables - a real Glue
        // session always has a loaded VS project by the time codegen runs.
        var vsProject = TestSupport.TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave();
        TaskManager.SynchronousMode = true;
        // GenerateEverythingFor's animation-enumerable step reads AppState.Self.GumProjectSave.FullFileName
        // (unconditionally, before any null-FullFileName check) - a real project always has this set by the
        // time codegen runs, so give it a harmless, non-null stand-in rather than relaxing production code.
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

    [Theory]
    [InlineData("Rectangle")]
    [InlineData("Circle")]
    public void GenerateStandardElementSaveCodeFor_ShouldNotGenerateUnsupportedFillStrokeGradientProperties(string standardElementName)
    {
        // Needed so GenerateStandardElementSaveCodeFor doesn't NRE (per-type generators wired up) and so
        // the skip lists this test is pinning are populated for the current code (same calls
        // MainGumPlugin.StartUp makes on glux load):
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        // Build the fixture the same way GumPluginCommands.AddStandardElement builds a real one being
        // added to a project - Initialize()'d from the canonical, always-current schema, not from any
        // (possibly stale) on-disk/embedded template.
        var standardElementSave = new StandardElementSave { Name = standardElementName };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor(standardElementName));

        // Sanity check on the fixture itself: if this ever fails, the fixture stopped carrying the
        // vulnerable variables and this test would silently stop pinning anything (the same failure mode
        // that made the old, file-creation-based version of this test worthless).
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("GradientY1");
        fixtureVariableNames.ShouldContain("StrokeWidth");
        fixtureVariableNames.ShouldContain("HasDropshadow");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // These are exactly the members LineRectangle/LineCircle can't back (no fill, no stroke, no
        // gradient, no dropshadow, and a get-only BlendState). This drives the real, unfaked
        // StandardsCodeGenerator/StateCodeGenerator pipelines - if the Rectangle/Circle skip-list entries
        // ever regress, one of these properties (or a state-switch assignment referencing it) starts
        // showing up again and CS1061/CS0103 comes right back in any real generated game project.
        foreach (var excludedMember in new[]
                 {
                     "StrokeWidth", "StrokeAlpha", "StrokeRed", "StrokeGreen", "StrokeBlue",
                     "IsFilled", "FillAlpha", "FillRed", "FillGreen", "FillBlue",
                     "UseGradient", "GradientType",
                     "GradientX1", "GradientX1Units", "GradientY1", "GradientY1Units",
                     "GradientX2", "GradientX2Units", "GradientY2", "GradientY2Units",
                     "GradientInnerRadius", "GradientInnerRadiusUnits",
                     "GradientOuterRadius", "GradientOuterRadiusUnits",
                     "Alpha2", "Red2", "Green2", "Blue2",
                     "HasDropshadow", "DropshadowOffsetX", "DropshadowOffsetY", "DropshadowBlur",
                     "DropshadowAlpha", "DropshadowRed", "DropshadowGreen", "DropshadowBlue",
                     "Blend",
                 })
        {
            generatedSource.ShouldNotContain(excludedMember);
        }

        if (standardElementName == "Rectangle")
        {
            // Rectangle-only rounded-corner surface (v3's per-corner override of the legacy
            // RoundedRectangle standard) must also be excluded - LineRectangle always renders hard corners.
            generatedSource.ShouldNotContain("CornerRadius");
            generatedSource.ShouldNotContain("CustomRadiusTopLeft");
        }

        // Sanity: properties Rectangle/Circle DO still support must keep being generated - proves this is
        // a targeted exclusion, not an accidental blanket one that would trivially satisfy the ShouldNotContain
        // assertions above by generating nothing at all.
        // - "Color" is StandardsCodeGenerator's own convenience property (variableNamesToAddForProperties,
        //   handled via TryHandleCustomGetter/TryHandleCustomSetter), unaffected by the fix's skip lists.
        generatedSource.ShouldContain("public Microsoft.Xna.Framework.Color Color");
        // - Ordinary GraphicalUiElement-level variables (not part of the excluded family) still flow through
        //   the state pipeline.
        generatedSource.ShouldContain("Width = ");
        generatedSource.ShouldContain("Height = ");
    }
}
