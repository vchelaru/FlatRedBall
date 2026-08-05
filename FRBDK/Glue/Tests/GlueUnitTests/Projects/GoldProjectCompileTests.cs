using System.IO;
using System.Threading.Tasks;
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

        // Mandatory, and the single easiest thing to get wrong here: Beefball commits its generated code,
        // and Glue does not rewrite a generated file whose content is unchanged. Left in place, "codegen
        // produced this" and "codegen never ran" are indistinguishable and the build passes either way.
        GoldProject.DeleteGeneratedCode(project.Root).ShouldBeGreaterThan(0);

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
}
