using System;
using System.Linq;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// Issue #1967 - Gum's "Rectangle" standard element only exposes Fill/Stroke color once a
// project is on gumx v3 (ShapeVariableExpansion) or later (StandardElementsManager's
// AddFillAndStrokeVariables / ShapeVariableVersionGate in the sibling Gum repo). Glue-generated
// projects were stamped at gumx v2, so the standalone Gum Editor showed no color variables at
// all for a Glue Rectangle. Fixing that means bumping Glue's template to v3+, which in turn
// means Glue's own codegen needs to actually understand the v3 Fill/Stroke variable family
// instead of unconditionally excluding it (as it does today per issue #1907). This file pins
// both sides of that version boundary: v2 must keep behaving exactly as before, v3 gains real
// Fill/Stroke codegen backed by RenderingLibrary.Math.Geometry.FilledStrokedRectangle (Gum PR
// #4342).
[Collection(nameof(TaskManagerSequentialCollection))]
public class RectangleFillStrokeCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public RectangleFillStrokeCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestSupport.TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave();
        TaskManager.SynchronousMode = true;
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

    private static Gum.DataTypes.StandardElementSave BuildRectangleFixture()
    {
        var standardElementSave = new Gum.DataTypes.StandardElementSave { Name = "Rectangle" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Rectangle"));
        return standardElementSave;
    }

    private void SetGumProjectVersion(int version)
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = new GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
            Version = version
        };
    }

    [Fact]
    public void V2Project_RectangleCodegen_StillMapsToLineRectangle_NoFillStrokeProperties()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.AttributeVersion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);
        generated.ShouldBeTrue();

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("RenderingLibrary.Math.Geometry.LineRectangle mContainedRectangle");

        foreach (var excludedMember in new[]
                 {
                     "IsFilled", "FillAlpha", "FillRed", "FillGreen", "FillBlue",
                     "StrokeWidth", "StrokeAlpha", "StrokeRed", "StrokeGreen", "StrokeBlue",
                 })
        {
            generatedSource.ShouldNotContain(excludedMember);
        }
    }

    [Fact]
    public void V3Project_RectangleCodegen_MapsToFilledStrokedRectangle()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("RenderingLibrary.Math.Geometry.FilledStrokedRectangle mContainedRectangle");
        generatedSource.ShouldNotContain("RenderingLibrary.Math.Geometry.LineRectangle mContainedRectangle");
    }

    [Fact]
    public void V3Project_RectangleCodegen_ConstructorExplicitlyAssignsRenderableComponent_BeforeSetGraphicalUiElement()
    {
        // Real-manual-test regression: Gum's FallbackRenderableFactory.TryHandleAsBaseType hardcodes
        // case "Rectangle" to always construct a LineRectangle for FRB builds (no gumx-version awareness -
        // see the class's own "do not extend this switch" doc comment, sibling Gum repo). That factory only
        // runs when SetGraphicalUiElement finds RenderableComponent still null after construction - so
        // without an explicit assignment here, a v3 Rectangle's RenderableComponent is actually a
        // LineRectangle, ContainedRectangle's "as FilledStrokedRectangle" cast silently returns null, and
        // SetInitialState's state-switch NREs on ContainedRectangle.FillColor (set_FillAlpha etc.).
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        var assignIndex = generatedSource.IndexOf(
            "this.RenderableComponent = new RenderingLibrary.Math.Geometry.FilledStrokedRectangle();",
            StringComparison.Ordinal);
        var setGraphicalUiElementIndex = generatedSource.IndexOf("SetGraphicalUiElement(", StringComparison.Ordinal);

        assignIndex.ShouldBeGreaterThanOrEqualTo(0, "Constructor must explicitly construct the v3 " +
            "RenderableComponent - relying on Gum's fallback factory silently keeps it a LineRectangle. " +
            "Generated source:" + Environment.NewLine + generatedSource);
        setGraphicalUiElementIndex.ShouldBeGreaterThanOrEqualTo(0);
        assignIndex.ShouldBeLessThan(setGraphicalUiElementIndex,
            "RenderableComponent must be assigned before SetGraphicalUiElement runs, per that method's own " +
            "\"could have already been created by the type that is instantiated\" contract (Gum's " +
            "ElementSaveExtensions.GumRuntime.cs).");
    }

    [Fact]
    public void V2Project_RectangleCodegen_ConstructorDoesNotAssignRenderableComponent()
    {
        // v2 keeps relying on Gum's fallback factory (which correctly returns LineRectangle for "Rectangle"
        // today) - no explicit assignment needed or wanted here.
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.AttributeVersion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldNotContain("this.RenderableComponent = new RenderingLibrary.Math.Geometry.FilledStrokedRectangle();");
    }

    [Fact]
    public void V3Project_RectangleCodegen_GeneratesIsFilledProperty()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("public bool IsFilled");
        generatedSource.ShouldContain("ContainedRectangle.IsFilled");
    }

    [Fact]
    public void V3Project_RectangleCodegen_GeneratesStrokeWidthProperty()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("public float StrokeWidth");
        generatedSource.ShouldContain("ContainedRectangle.StrokeWidth");
    }

    [Fact]
    public void V3Project_RectangleCodegen_FillRedComposesFillColor()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("public int FillRed");
        // Composes through the FillColor channel, not a nonexistent ContainedRectangle.FillRed member.
        generatedSource.ShouldContain("ContainedRectangle.FillColor.R");
        generatedSource.ShouldContain("ColorExtensions.WithRed(ContainedRectangle.FillColor");
        generatedSource.ShouldNotContain("ContainedRectangle.FillRed");
    }

    [Fact]
    public void V3Project_RectangleCodegen_StrokeRedComposesStrokeColor_NotStrokeWidth()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("public int StrokeRed");
        generatedSource.ShouldContain("ContainedRectangle.StrokeColor.R");
        generatedSource.ShouldContain("ColorExtensions.WithRed(ContainedRectangle.StrokeColor");
        generatedSource.ShouldNotContain("ContainedRectangle.StrokeRed");
        // The "Stroke" prefix match must not misfire on StrokeWidth (a plain float, not a color channel).
        generatedSource.ShouldContain("public float StrokeWidth");
        generatedSource.ShouldNotContain("ContainedRectangle.StrokeColor.W");
    }

    [Fact]
    public void V3Project_RectangleCodegen_AllFillAndStrokeChannelsGenerated()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        foreach (var propertyName in StandardsCodeGenerator.RectangleFillStrokeVariableNames)
        {
            generatedSource.ShouldContain($" {propertyName}");
        }
    }

    [Fact]
    public void V3Project_RectangleCodegen_ColorConveniencePropertyRoutesToStrokeColor()
    {
        // Regression pin for GumRuntimeMemberContractTests.V3RectangleFillStrokeMembers_ShouldExistOnFilledStrokedRectangle
        // (a real CS1061 that test caught): the synthetic "Color" convenience property
        // (variableNamesToAddForProperties) is generated for every Rectangle regardless of version, and
        // its generic passthrough targets ContainedRectangle.Color - which FilledStrokedRectangle doesn't
        // have (only FillColor/StrokeColor). Must route to StrokeColor instead, matching Gum's own
        // legacy-Color-routes-to-Stroke convention (#2938).
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("ContainedRectangle.StrokeColor");
        generatedSource.ShouldNotContain("ContainedRectangle.Color");
    }

    [Fact]
    public void V3Project_RectangleCodegen_StateSwitchAssignsIsFilledAndFillRed()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        // The Default state's IsFilled/FillRed values (StandardElementsManager.AddFillAndStrokeVariables
        // defaults: IsFilled=false, FillRed=255) must actually reach the generated properties via the
        // state switch, not just exist as inert properties with their C# type default.
        generatedSource.ShouldContain("IsFilled = False");
        generatedSource.ShouldContain("FillRed = 255");
    }

    [Fact]
    public void V2Project_RectangleCodegen_StateSwitchDoesNotAssignFillRed()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.AttributeVersion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = BuildRectangleFixture();

        var codeBlock = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldNotContain("FillRed");
        generatedSource.ShouldNotContain("IsFilled");
    }
}
