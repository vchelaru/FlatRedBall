using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Regression coverage for #1988. A Screen's DefaultLayer is not an ordinary custom variable: the Screen
// Plugin registers a VariableDefinition for it on the Screen ATI with UsesCustomCodeGeneration, which is
// what stops CustomVariableCodeGenerator from declaring a `public string DefaultLayer;` field that would
// shadow FlatRedBall.Screens.Screen.DefaultLayer (a Layer).
//
// So the suppression is only as real as the plugin being loaded, which makes this a test of the harness as
// much as of the generator - see GoldProjectCompileTests.DoorsDemoProject_* for the end-to-end proof that
// a project regenerated without the plugin does not compile.
//
// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ScreenDefaultLayerCodeGenerationTests
{
    public ScreenDefaultLayerCodeGenerationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    // StaFact: EnsureGameProjectPluginsRegistered runs real plugin StartUp methods, which build WPF toolbars.
    [StaFact]
    public void ScreenDefaultLayerVariable_ShouldNotGenerateAMember()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = glueProject;

        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        var variable = new CustomVariable
        {
            Name = nameof(FlatRedBall.Screens.Screen.DefaultLayer),
            Type = "string",
            Category = "Layer",
        };
        screen.CustomVariables.Add(variable);
        glueProject.Screens.Add(screen);

        ICodeBlock codeBlock = new CodeDocument();
        CustomVariableCodeGenerator.AppendCodeForMember(screen, codeBlock, variable);

        // Anything at all here is a member declaration shadowing the base class's Layer-typed property.
        codeBlock.ToString().Trim().ShouldBeEmpty();
    }
}
