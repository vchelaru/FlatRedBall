using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.Managers;
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
// This drives the real, unfaked codegen pipeline (same NewGumProjectCreationLogic.
// CreateGumProjectInternal -> CodeGeneratorManager.GenerateDerivedGueRuntimesAsync path exercised by
// GumProjectCreationTests) and asserts on the two real generated files. A full offline recompile of
// these files was evaluated but dropped: RectangleRuntime.Generated.cs's declared base type
// (Gum.Wireframe.GraphicalUiElement) and StateInterpolationPlugin.TweenerManager only get their real,
// complete member surface from a full game project's reference graph (MonoGameGum +
// FlatRedBall.Forms.StateInterpolation's shared source + the full engine) - reproducing that in this
// tool-side test process would mean loading a second, conflicting copy of MonoGame/the engine
// alongside Glue's own tool-side assemblies. See this PR's description for the real `dotnet build`
// verification against an actual generated game project instead.
[Collection(nameof(TaskManagerSequentialCollection))]
public class RectangleCircleCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Gum.DataTypes.GumProjectSave? _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public RectangleCircleCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
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
    public async Task CreateGumProjectInternal_ShouldNotGenerateUnsupportedFillStrokeGradientPropertiesForRectangleAndCircle()
    {
        // Needed so standard-element runtime generation actually runs (and doesn't silently no-op):
        // populates Gum.Managers.StandardElementsManager.Self.DefaultStates with the full variable
        // list (including the fill/stroke/gradient/dropshadow family under test), and wires up
        // StandardsCodeGenerator/StateCodeGenerator's per-type generators + skip lists the same way
        // MainGumPlugin.StartUp/glux-load does - without it, GenerateStandardElementSaveCodeFor NREs
        // internally, which CodeGeneratorManager.GenerateAllElements swallows, so no
        // RectangleRuntime.Generated.cs/CircleRuntime.Generated.cs get written at all.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());

        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);

        var rectangleRuntimePath = Directory
            .GetFiles(_tempProjectDirectory, "RectangleRuntime.Generated.cs", SearchOption.AllDirectories)
            .ShouldHaveSingleItem();
        var circleRuntimePath = Directory
            .GetFiles(_tempProjectDirectory, "CircleRuntime.Generated.cs", SearchOption.AllDirectories)
            .ShouldHaveSingleItem();

        var rectangleSource = File.ReadAllText(rectangleRuntimePath);
        var circleSource = File.ReadAllText(circleRuntimePath);

        // These are exactly the members LineRectangle/LineCircle can't back (no fill, no stroke, no
        // gradient, no dropshadow, and a get-only BlendState). This is the real, unfaked pipeline
        // output - if StandardsCodeGenerator's/StateCodeGenerator's Rectangle/Circle skip lists ever
        // regress, one of these properties will start showing up again and CS1061/CS0200 comes right
        // back in any real generated game project.
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
            rectangleSource.ShouldNotContain(excludedMember);
            circleSource.ShouldNotContain(excludedMember);
        }

        // Rectangle-only rounded-corner surface (v3's per-corner override of the legacy
        // RoundedRectangle standard) must also be excluded - LineRectangle always renders hard corners.
        rectangleSource.ShouldNotContain("CornerRadius");
        rectangleSource.ShouldNotContain("CustomRadiusTopLeft");

        // Sanity: the single-color family Rectangle/Circle DO support (handled via
        // TryHandleCustomGetter/TryHandleCustomSetter, not the excluded *2/Stroke*/Fill* family) must
        // still be generated - proves this is a targeted exclusion, not an accidental blanket one.
        rectangleSource.ShouldContain("public int Red");
        rectangleSource.ShouldContain("public int Green");
        rectangleSource.ShouldContain("public int Blue");
        rectangleSource.ShouldContain("public int Alpha");
        circleSource.ShouldContain("public int Red");
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
