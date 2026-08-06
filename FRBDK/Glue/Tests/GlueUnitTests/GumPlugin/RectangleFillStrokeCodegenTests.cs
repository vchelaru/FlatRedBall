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

    /// <summary>
    /// A component holding a single Rectangle instance whose Default state sets the v3 fill/stroke
    /// variables on it - i.e. the shape a real .gucx has, as opposed to the standard element's own
    /// .gutx that every other test in this file generates. Registers both elements on the loaded
    /// GumProjectSave so ObjectFinder can resolve the instance's base type during generation.
    /// </summary>
    private static Gum.DataTypes.ComponentSave BuildComponentWithRectangleInstanceFixture(
        string instanceName = "HighlightRectangle")
    {
        var rectangle = BuildRectangleFixture();

        var component = new Gum.DataTypes.ComponentSave { Name = "Styles", BaseType = "Container" };
        component.Instances.Add(new Gum.DataTypes.InstanceSave
        {
            Name = instanceName,
            BaseType = "Rectangle"
        });

        var defaultState = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        component.States.Add(defaultState);

        // Types are Gum's own type names as they appear in a real .gucx ("int", not "Int32") - the
        // generator prefixes unrecognized type names onto the value, so a CLR name here silently
        // produces "= Int32.128" instead of "= 128".
        foreach (var (variableName, type, value) in new (string, string, object)[]
                 {
                     ("StrokeRed", "int", 0), ("StrokeGreen", "int", 128), ("StrokeBlue", "int", 0),
                     ("IsFilled", "bool", true),
                     ("FillRed", "int", 0), ("FillGreen", "int", 0), ("FillBlue", "int", 0),
                     ("StrokeWidth", "float", 0f),
                     // A variable that is valid on any gumx version, so a test asserting the fill/stroke
                     // family is gone can still tell "gated correctly" apart from "generated nothing".
                     ("Visible", "bool", false),
                 })
        {
            defaultState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            {
                SetsValue = true,
                Name = $"{instanceName}.{variableName}",
                Type = type,
                Value = value
            });
        }

        component.Initialize(defaultState);

        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        gumProject.StandardElements.Add(rectangle);
        gumProject.Components.Add(component);

        return component;
    }

    [Fact]
    public void V2Project_ComponentWithRectangleInstance_DoesNotAssignFillStrokeVariables()
    {
        // Issue #1987 - the property pipeline correctly omits Fill*/Stroke* from a v2 RectangleRuntime,
        // so a component's state switch must not assign them on a Rectangle instance either (CS1061:
        // 'RectangleRuntime' does not contain a definition for 'StrokeBlue'). The gate used to key on
        // container.Name == "Rectangle", which is only ever true for RectangleRuntime's own file.
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.AttributeVersion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var component = BuildComponentWithRectangleInstanceFixture();

        var generatedSource = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);

        // Fixture sanity: generation actually ran and reached this instance's variables.
        generatedSource.ShouldContain("HighlightRectangle.Visible");

        foreach (var excludedMember in StandardsCodeGenerator.RectangleFillStrokeVariableNames)
        {
            generatedSource.Contains($"HighlightRectangle.{excludedMember}").ShouldBeFalse(
                $"A v2 RectangleRuntime has no {excludedMember} property, so assigning it here is a CS1061 " +
                "in the user's game build.");
        }
    }

    [Fact]
    public void V3Project_ComponentWithRectangleInstance_AssignsFillStrokeVariables()
    {
        // The other side of the same gate: on v3 the properties do exist (backed by
        // FilledStrokedRectangle), so the component must keep setting the values the user authored.
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var component = BuildComponentWithRectangleInstanceFixture();

        var generatedSource = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);

        generatedSource.ShouldContain("HighlightRectangle.StrokeGreen = 128");
        generatedSource.ShouldContain("HighlightRectangle.IsFilled = True");
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
            "this.SetContainedObject(new RenderingLibrary.Math.Geometry.FilledStrokedRectangle());",
            StringComparison.Ordinal);
        var setGraphicalUiElementIndex = generatedSource.IndexOf("SetGraphicalUiElement(", StringComparison.Ordinal);

        assignIndex.ShouldBeGreaterThanOrEqualTo(0, "Constructor must explicitly construct the v3 " +
            "RenderableComponent via SetContainedObject (RenderableComponent itself is get-only) - " +
            "relying on Gum's fallback factory silently keeps it a LineRectangle. " +
            "Generated source:" + Environment.NewLine + generatedSource);
        setGraphicalUiElementIndex.ShouldBeGreaterThanOrEqualTo(0);
        assignIndex.ShouldBeLessThan(setGraphicalUiElementIndex,
            "The contained object must be set before SetGraphicalUiElement runs, per that method's own " +
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

        generatedSource.ShouldNotContain("this.SetContainedObject(new RenderingLibrary.Math.Geometry.FilledStrokedRectangle());");
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
