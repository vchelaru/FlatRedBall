using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// One test per gold project, each loading a real checked-in FlatRedBall project through
/// <c>ProjectLoader.LoadProject</c> exactly as Glue.exe does on File > Load Project, then building the
/// regenerated result.
///
/// This is the only kind of test that exercises code generation the way it actually runs. Glue's
/// generators are plugins, and several decide what to emit by asking another plugin a question at
/// generation time (<c>PluginManager.CallPluginMethod("Gum Plugin", "HasGum")</c> gating
/// <c>#define HasGum</c> is the canonical one). Codegen unit tests call a single generator directly and
/// never cross those seams. See GitHub issue #1973.
///
/// Deliberately one test per project rather than a [Theory]: what is worth asserting differs per project,
/// and a shared parameter list would flatten that to the weakest common assertion.
///
/// Plugins are registered process-wide and stay registered across loads, which is what Glue does when a
/// user switches projects, so gold projects are differentiated by their *content* rather than by which
/// plugins are loaded.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class GoldProjectCompileTests
{
    // StaFact, not Fact: plugin StartUp methods construct real WPF toolbars, which need an STA thread.
    [StaFact]
    public async Task Beefball_LoadInGlue_ThenBuild_ShouldSucceed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csproj = Path.Combine(project.Root, "Beefball", "Beefball.csproj");

        // Deletes nothing on a fresh clone - *.Generated.cs is gitignored repo-wide - but a developer's
        // working copy has them from previous runs, and Glue does not rewrite a generated file whose
        // content is unchanged. Left in place, "codegen produced this" and "codegen never ran" are
        // indistinguishable locally and the build passes either way. Deliberately not asserting a count:
        // zero is the correct answer in CI.
        GoldProject.DeleteGeneratedCode(project.Root);

        await GoldProject.LoadInGlueAsync(csproj);

        // Any message here is Glue reporting something it would have shown the user a dialog about,
        // including an exception from a plugin's async void handler (see GlueTestBootstrap).
        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();

        // Code generation reports per-element failures to the error output instead of throwing, so without
        // this a generator that died halfway looks identical to one that had nothing to do.
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();
        // Asserting only on a zero build exit code is not enough: a regression that stops a generator from
        // running makes the project *smaller*, and smaller still compiles.
        var generated = GoldProject.GeneratedFiles(project.Root);
        generated.ShouldContain("Beefball/Screens/GameScreen.Generated.cs");
        generated.ShouldContain("Beefball/Entities/PlayerBall.Generated.cs");
        generated.ShouldContain("Beefball/Factories/PlayerBallFactory.Generated.cs");
        generated.ShouldContain("Beefball/Setup/CameraSetup.Generated.cs");

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed for the regenerated gold project:\n{output}");
    }

    // The project this exercise exists for: it has a .gumx, so it crosses the cross-plugin seam in the
    // issue - GlueControlCodeGenerator asking the Gum plugin "HasGum" at generation time, which returned
    // null and silently compiled out every #if HasGum branch.
    //
    // The build step was blocked until issue #1979: this project's NineSlice.gutx predates BorderScale and
    // IsTilingMiddleSections, and Glue never back-fills a loaded project's standard elements, so the
    // generated NineSliceRuntime declared Gum.Wireframe.INineSliceRuntime without implementing it (CS0535).
    [StaFact]
    public async Task FormsSampleProject_LoadInGlue_ThenBuild_ShouldSucceed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        var csproj = Path.Combine(project.Root, "FormsSampleProject", "FormsSampleProject.csproj");

        GoldProject.DeleteGeneratedCode(project.Root);

        await GoldProject.LoadInGlueAsync(csproj);

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        // With the Gum plugin actually dispatched to, HasGum answers for real instead of returning null.
        FlatRedBall.Glue.Plugins.PluginManager.CallPluginMethod("Gum Plugin", "HasGum").ShouldBe(true);

        // The Gum plugin's own generators ran, not just Glue's core ones: the Gum runtime wrappers, a Forms
        // component, and a standard element. None of these appear if the plugin is loaded but never
        // dispatched to, which is the failure mode behind this issue.
        var generated = GoldProject.GeneratedFiles(project.Root);
        generated.ShouldContain("FormsSampleProject/GumRuntimes/GumIdb.Generated.cs");
        generated.ShouldContain("FormsSampleProject/GumRuntimes/TextRuntime.Generated.cs");
        generated.ShouldContain("FormsSampleProject/Forms/Screens/MainMenuGumForms.Generated.cs");
        generated.ShouldContain("FormsSampleProject/Screens/MainMenu.Generated.cs");

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed for the regenerated gold project:\n{output}");
    }

    // Live edit's runtime half - the ~40 files EmbeddedCodeManager copies into a game project's GlueControl
    // folder - is <Compile Remove>d from GameCommunicationPlugin.csproj because it only compiles against a
    // game project. No checked-in sample has live edit turned on, so building any of them as-is skips that
    // closure entirely and a typo in Embedded\**\*.cs reaches every live-edit user before anything fails.
    //
    // EmbedAll is called directly rather than by turning live edit on in the copied CompilerSettings.json,
    // because the plugin that would react to that setting (MainCompilerPlugin) builds real WPF tabs and
    // opens sockets on registration and isn't seamed for the test host. What's at risk here is whether the
    // embedded closure compiles, not whether the plugin decides to embed it.
    [StaFact]
    public async Task Beefball_WithLiveEditCode_LoadInGlue_ThenBuild_ShouldSucceed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csproj = Path.Combine(project.Root, "Beefball", "Beefball.csproj");

        GoldProject.DeleteGeneratedCode(project.Root);

        await GoldProject.LoadInGlueAsync(csproj);

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        // Both together, in this order, is what MainCompilerPlugin.HandleGluxLoaded does in production.
        var wasSynchronous = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;
        try
        {
            EmbeddedCodeManager.EmbedAll(fullyGenerate: true);
            GlueCallsCodeGenerator.GenerateAll();
        }
        finally
        {
            TaskManager.SynchronousMode = wasSynchronous;
        }

        // Beefball sets EnableDefaultCompileItems to false, so the embedded files only reach the compiler
        // through the Compile items EmbedAll added to the in-memory project.
        GlueCommands.Self.ProjectCommands.SaveProjects();

        // Without these the build below would pass by compiling a project that simply has no live edit code
        // in it, which is indistinguishable from the embedding never having happened.
        var generated = GoldProject.GeneratedFiles(project.Root);
        generated.ShouldContain("Beefball/GlueControl/Editing/EditingManager.Generated.cs");
        generated.ShouldContain("Beefball/GlueControl/Screens/EntityViewingScreen.Generated.cs");
        generated.ShouldContain("Beefball/GlueControl/CommandReceiver.Generated.cs");

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed for the regenerated gold project:\n{output}");
    }
}
