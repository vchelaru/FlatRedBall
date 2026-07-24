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

// Sweep of the bug class from issue #1907/PR #1908 (a Gum standard element's default-state schema
// gains a variable its Glue-mapped backing runtime type can't support, producing a CS1061/CS0029/CS0102
// generated-code build failure) across the remaining standard elements: NineSlice, Text, Sprite,
// Container, ColoredRectangle, SolidRectangle (Rectangle/Circle were already covered by
// RectangleCircleCodegenTests, Polygon is covered separately). Same fixture technique as that test -
// StandardElementSave.Initialize()'d from StandardElementsManager.Self.GetDefaultStateFor(name), the
// canonical always-current schema - see that file's header comment for why this beats driving fresh
// project creation (stale embedded templates never reconcile against the canonical schema).
//
// Sweep result: only Text and ColoredRectangle turned out clean on the first pass. Three real, live bugs
// were found and are now fixed (and pinned below), plus one non-bug:
//  - NineSlice: "Animate" was skipped in the property pipeline below GluxVersions.GumNineSliceHasAnimate
//    (NineSliceCodeGenerator.HasNineSliceAnimate) but had no matching gate in StateCodeGenerator.
//    RefreshVariableNamesToSkipBasedOnGlueVersion, so the state-switch still emitted "Animate = value;"
//    against a property that was never generated - CS0103 for any project below that version with a
//    NineSlice standard element. Fixed by adding the missing paired Include/Skip("Animate") block to
//    RefreshVariableNamesToSkipBasedOnGlueVersion, gated on the same GumNineSliceHasAnimate value.
//  - Sprite: "RenderTargetTextureSource" was a plain default-state variable (Type "string", never added
//    to any skip list) so the generic property loop generated "public string RenderTargetTextureSource"
//    backed by ContainedSprite.RenderTargetTextureSource, which is actually "IRenderableIpso?" (CS0029).
//    At/above GluxVersions.GumHasIRenderTargetTextureReferencer, SpriteCodeGenerator additionally emits
//    its own, correctly typed "RenderTargetTextureSource" property, so the two collided (CS0102 duplicate
//    member) instead. Fixed with a permanent (version-independent) skip in both pipelines -
//    SpriteCodeGenerator.GenerateIRenderTargetTextureReferencerProperties is the sole source whenever this
//    property exists at all.
//  - Container: "SourceShaderFile" (Gum v3) was never added to any skip list, so it was generated against
//    "((RenderingLibrary.Graphics.IRenderableIpso)this.RenderableComponent)", and IRenderableIpso has no
//    such member - CS1061. Container has no backing runtime type at all
//    (StandardsCodeGenerator.mStandardElementToQualifiedTypes["Container"] is null), so this is a
//    permanent skip in both pipelines, not version-gated.
//  - SolidRectangle: not applicable, not a bug. "SolidRectangle" is not a registered standard element
//    in Gum.Managers.StandardElementsManager at all - only "ColoredRectangle" (kept for legacy loading;
//    Gum's v3 Rectangle absorbed its role). GetDefaultStateFor("SolidRectangle") throws
//    InvalidOperationException, so no fixture can be built via this technique; see
//    GenerateStandardElementSaveCodeFor_SolidRectangle_HasNoCanonicalDefaultState below, which pins that
//    absence rather than a codegen behavior.
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumStandardElementCodegenSweepTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumStandardElementCodegenSweepTests()
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

        // Unlike RectangleCircleCodegenTests (which leaves FileVersion at its default/lowest value),
        // this sweep pins Text/ColoredRectangle at GlueProjectSave.LatestVersion: Text's newest
        // properties (LineHeightMultiplier, TextOverflowHorizontalMode/VerticalMode, IsBold) are
        // version-gated, and the latest version is what every current Glue project actually generates
        // against.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion
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

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Text_ShouldGenerateOnlySupportedTextProperties()
    {
        // Needed so GenerateStandardElementSaveCodeFor doesn't NRE (per-type generators wired up) and so
        // the skip lists this test is pinning are populated for the current code (same calls
        // MainGumPlugin.StartUp makes on glux load).
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "Text" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Text"));

        // Sanity check on the fixture: these are RenderingLibrary.Graphics.Text's newest members, gated
        // by GluxVersions - if this ever stops carrying them, the test below stops pinning anything.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("LineHeightMultiplier");
        fixtureVariableNames.ShouldContain("TextOverflowHorizontalMode");
        fixtureVariableNames.ShouldContain("TextOverflowVerticalMode");
        fixtureVariableNames.ShouldContain("MaxLettersToShow");
        fixtureVariableNames.ShouldContain("MaxNumberOfLines");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // Every one of these maps directly onto a real RenderingLibrary.Graphics.Text member (or, for
        // IsBold/Font/FontSize/OutlineThickness/UseCustomFont/UseFontSmoothing/CustomFontFile, onto a
        // member GraphicalUiElement itself already exposes - which is exactly why those are excluded
        // from property generation but still safe to reference from the state-switch).
        generatedSource.ShouldContain("public float LineHeightMultiplier");
        generatedSource.ShouldContain("public global::RenderingLibrary.Graphics.TextOverflowHorizontalMode TextOverflowHorizontalMode");
        generatedSource.ShouldContain("public new global::RenderingLibrary.Graphics.TextOverflowVerticalMode TextOverflowVerticalMode");
        generatedSource.ShouldContain("public int? MaxLettersToShow");
        generatedSource.ShouldContain("public int? MaxNumberOfLines");
        generatedSource.ShouldContain("public string Text");
        generatedSource.ShouldContain("public RenderingLibrary.Graphics.HorizontalAlignment HorizontalAlignment");
        generatedSource.ShouldContain("public RenderingLibrary.Graphics.VerticalAlignment VerticalAlignment");
        generatedSource.ShouldContain("public Microsoft.Xna.Framework.Color Color");

        // These are excluded from property generation (already handled by base GraphicalUiElement) but
        // the state-switch still assigns them by name - confirms they resolve against the base class,
        // not a never-generated property.
        generatedSource.ShouldContain("IsBold = false;");
        generatedSource.ShouldNotContain("public bool IsBold");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_ColoredRectangle_ShouldGenerateOnlySupportedColorProperties()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "ColoredRectangle" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("ColoredRectangle"));

        // Unlike Rectangle/Circle, ColoredRectangle's default state was never given the v3
        // fill/stroke/gradient/dropshadow/blend family (StandardElementsManager only calls
        // AddColorVariables + CreateBlendVariable for it) - so there's nothing to exclude here, and this
        // sanity check pins that staying true.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("Red");
        fixtureVariableNames.ShouldContain("Blend");
        fixtureVariableNames.ShouldNotContain("StrokeWidth");
        fixtureVariableNames.ShouldNotContain("UseGradient");
        fixtureVariableNames.ShouldNotContain("HasDropshadow");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // RenderingLibrary.Graphics.SolidRectangle (ColoredRectangle's backing type) exposes Alpha/
        // Red/Green/Blue as plain int properties directly (unlike Circle/Rectangle, which need the
        // Color-component get/set rewrite in StandardsCodeGenerator.TryHandleCustomGetter/Setter) and a
        // full BlendState get/set property (aliased to "Blend").
        generatedSource.ShouldContain("public int Alpha");
        generatedSource.ShouldContain("public int Red");
        generatedSource.ShouldContain("public int Green");
        generatedSource.ShouldContain("public int Blue");
        generatedSource.ShouldContain("public Gum.RenderingLibrary.Blend Blend");
        generatedSource.ShouldContain("public Microsoft.Xna.Framework.Color Color");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_SolidRectangle_HasNoCanonicalDefaultState()
    {
        // "SolidRectangle" only survives as a residual entry in StandardsCodeGenerator.
        // mStandardElementToQualifiedTypes (mapped to the same RenderingLibrary.Graphics.SolidRectangle
        // backing type as ColoredRectangle) - Gum.Managers.StandardElementsManager itself has no
        // "SolidRectangle" default state (only "ColoredRectangle"), so the fixture technique this sweep
        // otherwise uses (Initialize() from GetDefaultStateFor) has nothing to build for this name. This
        // pins that absence rather than any codegen behavior - a genuine "doesn't fit the pattern" case,
        // not a bug.
        Should.Throw<Exception>(() =>
            Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("SolidRectangle"));
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_NineSlice_BelowAnimateGate_ShouldNotReferenceAnimateAtAll()
    {
        // Below GluxVersions.GumNineSliceHasAnimate, the property pipeline already skipped "Animate"
        // (NineSliceCodeGenerator.HasNineSliceAnimate) before this fix - the bug was that the state
        // pipeline didn't, so the state-switch alone still referenced it (CS0103). Pin the whole surface:
        // neither pipeline should mention "Animate" at all below the gate.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion = 0;
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "NineSlice" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("NineSlice"));

        // Sanity check on the fixture: if this ever fails, the fixture stopped carrying the vulnerable
        // variable and this test would silently stop pinning anything.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("Animate");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        codeBlock.ToString().ShouldNotContain("Animate");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_NineSlice_AtAnimateGate_ShouldGenerateAndAssignAnimate()
    {
        // At/above the gate, both pipelines must agree the property exists: a generated property
        // declaration AND a state-switch assignment referencing it.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion = (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumNineSliceHasAnimate;
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "NineSlice" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("NineSlice"));

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // Property declaration (generic property-generation loop, driven by the schema's own type for
        // "Animate") and the state-switch assignment both need to be present - if either pipeline alone
        // skipped/included it, this would be exactly one or the other, not both.
        var animateMentions = System.Text.RegularExpressions.Regex.Matches(generatedSource, "Animate").Count;
        animateMentions.ShouldBeGreaterThanOrEqualTo(2);
        generatedSource.ShouldContain("Animate = ");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Sprite_BelowRenderTargetTextureSourceGate_ShouldNotGenerateIt()
    {
        // Below GluxVersions.GumHasIRenderTargetTextureReferencer, SpriteCodeGenerator itself generates
        // nothing for "RenderTargetTextureSource" - before this fix, the generic property loop still
        // generated "public string RenderTargetTextureSource" backed by ContainedSprite.
        // RenderTargetTextureSource, which is actually IRenderableIpso? (CS0029).
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion = 0;
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "Sprite" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Sprite"));

        // Sanity check on the fixture: if this ever fails, the fixture stopped carrying the vulnerable
        // variable and this test would silently stop pinning anything.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("RenderTargetTextureSource");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        codeBlock.ToString().ShouldNotContain("RenderTargetTextureSource");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Sprite_AtRenderTargetTextureSourceGate_ShouldGenerateOnlyTypedVersionOnce()
    {
        // At/above the gate, SpriteCodeGenerator.GenerateIRenderTargetTextureReferencerProperties emits
        // the correctly-typed IRenderableIpso? property (plus the explicit interface implementation).
        // Before this fix, the generic property loop ALSO generated a plain-string version of the same
        // name, producing a duplicate member (CS0102).
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion = (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumHasIRenderTargetTextureReferencer;
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "Sprite" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Sprite"));

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        // Only SpriteCodeGenerator's own correctly-typed property/explicit-interface-implementation pair
        // should exist - never the generic loop's plain "string" version.
        generatedSource.ShouldNotContain("public string RenderTargetTextureSource");
        generatedSource.ShouldContain("public global::RenderingLibrary.Graphics.IRenderableIpso? RenderTargetTextureSource");
        generatedSource.ShouldContain("global::RenderingLibrary.Graphics.IRenderTargetTextureReferencer.RenderTargetTextureSource");

        // Never state-switch driven at any version (see the permanent skip in
        // StateCodeGenerator.RefreshVariablesToSkipForStates) - it's a plain get/set pass-through.
        generatedSource.ShouldNotContain("RenderTargetTextureSource = null");
        generatedSource.ShouldNotContain("RenderTargetTextureSource = \"\"");
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Container_ShouldNotGenerateSourceShaderFile()
    {
        // Container has no backing runtime type at all (mStandardElementToQualifiedTypes["Container"] is
        // null) - before this fix, "SourceShaderFile" (Gum v3) was generated against a bare
        // IRenderableIpso cast, which has no such member (CS1061). Permanent skip, not version-gated, so
        // this uses the class's default LatestVersion fixture setup.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "Container" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Container"));

        // Sanity check on the fixture: if this ever fails, the fixture stopped carrying the vulnerable
        // variable and this test would silently stop pinning anything.
        var fixtureVariableNames = standardElementSave.DefaultState.Variables.Select(v => v.GetRootName()).ToHashSet();
        fixtureVariableNames.ShouldContain("SourceShaderFile");

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        codeBlock.ToString().ShouldNotContain("SourceShaderFile");
    }
}
