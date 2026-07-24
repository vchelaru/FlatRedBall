using System;
using System.Collections.Generic;
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

// Part of the issue #1907/#1908 bug-class sweep (see RectangleCircleCodegenTests.cs's header for the full
// "why Initialize()-from-canonical-schema, not fresh-project-creation" rationale, and the gum-codegen
// skill for the two independent skip pipelines this bug class hinges on).
//
// Unlike Rectangle/Circle (mapped to plain outline-only RenderingLibrary.Math.Geometry types that can't
// back Gum's v3 fill/stroke/gradient/dropshadow/blend family), the six standard elements covered here -
// Arc, ColoredCircle, LottieAnimation, RoundedRectangle, Svg, Canvas (see
// StandardsCodeGenerator.mStandardElementToQualifiedTypes) - all map to SkiaGum.Renderables.* types. Their
// common base, RenderableSkiaObject, was purpose-built with the full fill/stroke/gradient/dropshadow
// surface already on it, plus each per-type generator (see GumPlugin.Managers.SkiaStandardElementsManager)
// adds its own extras (RenderableArc.Thickness/StartAngle/SweepAngle/IsEndRounded,
// RenderableRoundedRectangle.CornerRadius). Comparing each of the six schemas against its backing type's
// real members (Engines/SkiaGum/Renderables/*.cs) found no unbacked member for any of the six - every
// schema variable has a same-named, same-shape member on the generated ContainedXxx field, so all six get
// green regression tests here (contrast with the sibling Rectangle/Circle and Polygon sweeps, which found
// real bugs).
//
// One real, non-obvious risk found along the way (not a bug in current codegen, but easy to reintroduce):
// the "Color" convenience property StandardsCodeGenerator adds to every standard element
// (variableNamesToAddForProperties) is typed Microsoft.Xna.Framework.Color, but RenderableSkiaObject's
// backing Color field is System.Drawing.Color - a type the older, pre-migration standard elements
// (LineRectangle/LineCircle/etc.) never used. StandardsCodeGenerator only emits the
// RenderingLibrary.Graphics.XNAExtensions.ToXNA/ToSystemDrawing conversion for this property when
// GlueState.Self.CurrentGlueProject.FileVersion >= GluxVersions.GumUsesSystemTypes (or
// IsFrbSourceLinked()) - see AdjustStandardElementVariableGetIfNecessary/SetIfNecessary. The fixture below
// sets FileVersion to GlueProjectSave.LatestVersion (a stand-in for "a real, current project") specifically
// for this reason: at the default FileVersion of 0 (what RectangleCircleCodegenTests' fixture uses, harmless
// there only because that test never asserts on the Color property's generated body), the generated code
// is a straight `ContainedXxx.Color = value;` assignment that does NOT compile (CS0029) against a
// System.Drawing.Color-backed field. Not a reachable combination for these six standard elements in
// practice - SkiaStandardElementsManager's own schema already uses GeneralUnitType/DimensionUnitType, part
// of the same "system types" migration this version gates - but worth a dedicated test since the failure
// mode is silent (the generated *source* is produced either way; only real compilation catches it).
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumSkiaRenderableCodegenSweepTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumSkiaRenderableCodegenSweepTests()
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

        // FileVersion matters here (unlike RectangleCircleCodegenTests): the Color property's
        // ToXNA/ToSystemDrawing conversion is gated on FileVersion >= GluxVersions.GumUsesSystemTypes -
        // see this file's header comment for why LatestVersion (not the default 0) is the realistic choice.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
        };
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

    // MainGumPlugin.StartUp calls Gum.Managers.StandardElementsManager.Self.Initialize() and then
    // SkiaStandardElementsManager.AddSkiaStandards() - GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized
    // only does the former (it's shared by every Gum codegen test, and most don't need the Skia standards),
    // so the six Skia standard elements' schemas (Arc/ColoredCircle/etc.) need registering here.
    // AddSkiaStandards() isn't idempotent (Dictionary.Add throws on a second call in the same process), so
    // guard it the same way GlueTestBootstrap guards its own one-time init.
    private static void EnsureSkiaStandardElementsRegistered()
    {
        if (!Gum.Managers.StandardElementsManager.Self.DefaultStates.ContainsKey("Svg"))
        {
            GumPlugin.Managers.SkiaStandardElementsManager.AddSkiaStandards();
        }
    }

    private static (bool Generated, string Source, HashSet<string> FixtureVariableNames) GenerateFor(string standardElementName)
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        EnsureSkiaStandardElementsRegistered();

        // Build the fixture the same way GumPluginCommands.AddStandardElement builds a real one being added
        // to a project - Initialize()'d from the canonical, always-current schema, not from any (possibly
        // stale) on-disk/embedded template. See RectangleCircleCodegenTests.cs's header for why this matters.
        var standardElementSave = new StandardElementSave { Name = standardElementName };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor(standardElementName));

        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        return (generated, codeBlock.ToString(), fixtureVariableNames);
    }

    [Theory]
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("LottieAnimation")]
    [InlineData("RoundedRectangle")]
    [InlineData("Svg")]
    [InlineData("Canvas")]
    public void GenerateStandardElementSaveCodeFor_ConvertsColorPropertyForSkiaBackedColorField(string standardElementName)
    {
        var (generated, source, _) = GenerateFor(standardElementName);
        generated.ShouldBeTrue();

        // StandardsCodeGenerator's own "Color" convenience property (variableNamesToAddForProperties),
        // generated for every standard element regardless of schema - see this file's header for why the
        // ToXNA/ToSystemDrawing conversion (rather than a straight assignment) is required here.
        var containedName = "Contained" + standardElementName;
        source.ShouldContain("public Microsoft.Xna.Framework.Color Color");
        source.ShouldContain($"return RenderingLibrary.Graphics.XNAExtensions.ToXNA({containedName}.Color);");
        source.ShouldContain($"{containedName}.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Arc_GeneratesArcSpecificAndGradientMembers()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("Arc");
        generated.ShouldBeTrue();

        // Sanity check on the fixture: Arc uses its own Thickness/StartAngle/SweepAngle/IsEndRounded
        // surface (backed by RenderableArc directly) instead of the shared IsFilled/StrokeWidth pair other
        // Skia standards use, and doesn't get a dropshadow family at all - see SkiaStandardElementsManager.
        fixtureVariableNames.ShouldContain("Thickness");
        fixtureVariableNames.ShouldContain("GradientY1");
        fixtureVariableNames.ShouldNotContain("IsFilled");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");

        foreach (var member in new[]
                 {
                     "Thickness", "StartAngle", "SweepAngle", "IsEndRounded",
                     "UseGradient", "GradientType", "GradientX1", "GradientY1", "GradientX2", "GradientY2",
                     "GradientInnerRadius", "GradientOuterRadius",
                     "Red1", "Green1", "Blue1", "Red2", "Green2", "Blue2",
                 })
        {
            source.ShouldContain($"ContainedArc.{member}");
        }
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_ColoredCircle_GeneratesFullFillStrokeGradientDropshadowFamily()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("ColoredCircle");
        generated.ShouldBeTrue();

        fixtureVariableNames.ShouldContain("StrokeWidth");
        fixtureVariableNames.ShouldContain("HasDropshadow");
        fixtureVariableNames.ShouldContain("GradientY1");

        // RenderableCircle (via its RenderableSkiaObject base) backs all of these - a different backing
        // family from Rectangle/Circle's LineCircle, see this file's header.
        foreach (var member in new[]
                 {
                     "IsFilled", "StrokeWidth",
                     "UseGradient", "GradientType", "GradientX1", "GradientY1", "GradientX2", "GradientY2",
                     "GradientInnerRadius", "GradientOuterRadius",
                     "Red1", "Green1", "Blue1", "Red2", "Green2", "Blue2",
                     "HasDropshadow", "DropshadowOffsetX", "DropshadowOffsetY",
                     // BlurX/BlurY, not the singular "DropshadowBlur" the (excluded) Rectangle/Circle schema
                     // uses - SkiaStandardElementsManager.AddDropshadowVariables and
                     // RenderableSkiaObject.DropshadowBlurX/Y agree on this name.
                     "DropshadowBlurX", "DropshadowBlurY",
                     "DropshadowAlpha", "DropshadowRed", "DropshadowGreen", "DropshadowBlue",
                 })
        {
            source.ShouldContain($"ContainedColoredCircle.{member}");
        }
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_RoundedRectangle_GeneratesCornerRadiusAndFullFillStrokeGradientDropshadowFamily()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("RoundedRectangle");
        generated.ShouldBeTrue();

        fixtureVariableNames.ShouldContain("CornerRadius");
        fixtureVariableNames.ShouldContain("StrokeWidth");
        fixtureVariableNames.ShouldContain("HasDropshadow");

        // RenderableRoundedRectangle backs CornerRadius directly; the rest come from its
        // RenderableSkiaObject base, same as ColoredCircle.
        foreach (var member in new[]
                 {
                     "CornerRadius", "IsFilled", "StrokeWidth",
                     "UseGradient", "GradientType", "GradientX1", "GradientY1",
                     "GradientInnerRadius", "GradientOuterRadius",
                     "Red1", "Green1", "Blue1", "Red2", "Green2", "Blue2",
                     "HasDropshadow", "DropshadowBlurX", "DropshadowBlurY",
                     "DropshadowAlpha", "DropshadowRed", "DropshadowGreen", "DropshadowBlue",
                 })
        {
            source.ShouldContain($"ContainedRoundedRectangle.{member}");
        }
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Svg_GeneratesPlainColorTintOnlyNoFillStrokeGradientFamily()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("Svg");
        generated.ShouldBeTrue();

        // Svg only supports a plain color tint (RenderableSvg's ShouldApplyColorOnSpriteRender) - unlike
        // ColoredCircle/RoundedRectangle/Arc, its schema never calls AddGradientVariables/
        // AddDropshadowVariables/AddStrokeAndFilledVariables, so there's nothing from that family to
        // pin here beyond the shared Color-conversion theory above.
        fixtureVariableNames.ShouldContain("SourceFile");
        fixtureVariableNames.ShouldContain("Red");
        fixtureVariableNames.ShouldNotContain("UseGradient");
        fixtureVariableNames.ShouldNotContain("StrokeWidth");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");

        foreach (var member in new[] { "Red", "Green", "Blue", "Alpha" })
        {
            source.ShouldContain($"ContainedSvg.{member}");
        }
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_LottieAnimation_HasNoColorOrFillStrokeGradientFamilyBeyondConvenienceColor()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("LottieAnimation");
        generated.ShouldBeTrue();

        // LottieAnimation's schema deliberately skips AddColorVariables ("Do we support colors?" in
        // SkiaStandardElementsManager) - its only per-type variable is SourceFile. The unconditional "Color"
        // convenience property (covered by the shared theory above) is the only color-shaped surface here.
        fixtureVariableNames.ShouldContain("SourceFile");
        fixtureVariableNames.ShouldNotContain("Red");
        fixtureVariableNames.ShouldNotContain("UseGradient");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Canvas_HasNoCustomVariablesBeyondBaseGraphicalSurface()
    {
        var (generated, source, fixtureVariableNames) = GenerateFor("Canvas");
        generated.ShouldBeTrue();

        // Canvas (RenderableCanvas) is an empty draw target - no color, fill, stroke, gradient, or
        // dropshadow surface at all beyond the base positioning/dimensions/visibility every standard
        // element gets, plus the unconditional "Color" convenience property.
        fixtureVariableNames.ShouldNotContain("Red");
        fixtureVariableNames.ShouldNotContain("UseGradient");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");
        fixtureVariableNames.ShouldNotContain("StrokeWidth");

        // Sanity: ordinary GraphicalUiElement-level variables still flow through the state pipeline
        // (proves this isn't an accidental blanket exclusion that would trivially pass the above).
        source.ShouldContain("Width = ");
        source.ShouldContain("Height = ");
    }
}
