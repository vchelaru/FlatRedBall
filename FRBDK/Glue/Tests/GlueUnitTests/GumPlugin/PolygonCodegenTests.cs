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

// Follow-up sweep to issue #1907 / PR #1908 (see RectangleCircleCodegenTests.cs): Gum's "Rectangle" and
// "Circle" standard elements gained a fill/stroke/gradient/dropshadow/blend variable family that their
// backing RenderingLibrary.Math.Geometry types (LineRectangle/LineCircle) can't support, causing a real
// CS1061 build failure. "Polygon" maps to the same family of outline-only type
// (RenderingLibrary.Math.Geometry.LinePolygon - see StandardsCodeGenerator.mStandardElementToQualifiedTypes)
// so it was the top suspect for the identical bug.
//
// Investigation result: Polygon does NOT hit this bug. Unlike Circle/Rectangle,
// StandardElementsManager's "Polygon" block never calls AddFillAndStrokeVariables/AddGradientVariables/
// AddDropshadowVariables/AddBlendVariable - it only adds positioning, Visible, AddColorVariables (legacy
// Red/Green/Blue/Alpha, not the v3 family), Rotation, and a VariableListSave-typed "Points" list. The
// Red/Green/Blue/Alpha family already has dedicated custom getter/setter handling for "Polygon" in
// StandardsCodeGenerator.TryHandleCustomGetter/TryHandleCustomSetter that routes through
// ContainedPolygon.Color (LinePolygon.Color, a real, settable System.Drawing.Color property) - no CS1061
// risk there. "Points" is a VariableListSave, not a VariableSave; both StandardsCodeGenerator's property
// pipeline (GenerateStandardElementSaveCodeFor) and StateCodeGenerator's state pipeline only iterate
// standardElementSave.DefaultState.Variables / state.Variables (VariableSave), never
// DefaultState.VariableLists, so "Points" never becomes a generated property or state-switch assignment at
// all (LinePolygon exposes point data via SetPoints(...)/PointAt(...) methods, not a "Points" property, so
// this is a real gap in coverage today, not a bug - see PolygonCodeGenerator.GenerateAdditionalMethods for
// the only Points-adjacent codegen, which just emits an IsPointInside override).
//
// This test pins that current-and-correct state as a regression guard: if Gum's "Polygon" schema is ever
// extended with the v3 fill/stroke/gradient/dropshadow/blend family (the same way Circle/Rectangle were),
// this test goes red and flags that Glue's skip lists need a "Polygon" entry mirroring Circle/Rectangle's,
// before it ships as a broken build in a real generated game project.
[Collection(nameof(TaskManagerSequentialCollection))]
public class PolygonCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public PolygonCodegenTests()
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

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Polygon_ShouldNotGenerateUnsupportedFillStrokeGradientProperties()
    {
        // Needed so GenerateStandardElementSaveCodeFor doesn't NRE (per-type generators wired up) and so
        // the skip lists this test is pinning are populated for the current code (same calls
        // MainGumPlugin.StartUp makes on glux load):
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        // Build the fixture the same way GumPluginCommands.AddStandardElement builds a real one being
        // added to a project - Initialize()'d from the canonical, always-current schema, not from any
        // (possibly stale) on-disk/embedded template.
        var standardElementSave = new StandardElementSave { Name = "Polygon" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Polygon"));

        // Sanity check on the fixture itself: confirms Polygon's canonical schema, as of today, carries
        // the variables this test reasons about. Points is a VariableListSave (not a VariableSave), so it
        // lives in .VariableLists, not .Variables.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("Red");
        fixtureVariableNames.ShouldContain("Green");
        fixtureVariableNames.ShouldContain("Blue");
        fixtureVariableNames.ShouldContain("Alpha");
        fixtureVariableNames.ShouldContain("Rotation");
        standardElementSave.DefaultState.VariableLists.Select(v => v.Name).ShouldContain("Points");
        // And confirms the vulnerable v3 family is NOT (yet) part of Polygon's schema - if this ever
        // starts failing, Polygon has been upgraded the same way Circle/Rectangle were and needs the same
        // StandardsCodeGenerator/StateCodeGenerator skip-list treatment before this test's other
        // assertions below mean anything.
        fixtureVariableNames.ShouldNotContain("GradientY1");
        fixtureVariableNames.ShouldNotContain("StrokeWidth");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // Defense in depth, mirroring RectangleCircleCodegenTests: these are exactly the members
        // LinePolygon can't back (no fill, no stroke, no gradient, no dropshadow, and a get-only
        // BlendState). Guaranteed absent today because the fixture never carries them (see sanity check
        // above), but pinning it here means a future change that adds these names to Polygon's skip-list
        // exemptions (or to codegen's own variableNamesToAddForProperties) without backing support on
        // LinePolygon still gets caught.
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

        // Polygon has no Width/Height in its default state at all (StandardElementsManager's "Polygon"
        // block never calls AddDimensionsVariables, unlike every other shape standard) - LinePolygon's
        // Width/Height are explicit-interface, get-only-in-practice members that couldn't back a settable
        // property anyway, so this is worth pinning too.
        generatedSource.ShouldNotContain("public float Width");
        generatedSource.ShouldNotContain("public float Height");

        // Sanity: properties Polygon DOES support must keep being generated - proves this is a targeted
        // (non-)exclusion, not an accidental blanket skip that would trivially satisfy the assertions
        // above by generating nothing at all. Red/Green/Blue/Alpha route through
        // StandardsCodeGenerator's Circle/Rectangle/Polygon-specific custom getter/setter onto
        // ContainedPolygon.Color (LinePolygon.Color is a real, settable System.Drawing.Color property).
        generatedSource.ShouldContain("ContainedPolygon.Color");
        generatedSource.ShouldContain("public int Red");
        generatedSource.ShouldContain("public int Green");
        generatedSource.ShouldContain("public int Blue");
        generatedSource.ShouldContain("public int Alpha");
    }
}
