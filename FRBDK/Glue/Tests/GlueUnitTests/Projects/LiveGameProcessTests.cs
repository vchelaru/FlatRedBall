using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Proves <see cref="LiveGameProcess"/> itself works before anything is built on top of it: launch a real
/// game process, have it connect back over the real socket protocol, and read its actual runtime state.
///
/// Tagged "LiveGame" rather than "BuildSmoke" - it launches an actual MonoGame DesktopGL window, which
/// needs a real display/GPU context. GitHub-hosted Windows runners are not guaranteed to have one, so this
/// category is excluded from both CI filters (see pr-tests.yml/glue.yml) and is developer-machine-only for
/// now: `dotnet test ... --filter "Category=LiveGame"`.
/// </summary>
[Trait("Category", "LiveGame")]
public class LiveGameProcessTests
{
    // StaFact, not Fact: GoldProject.LoadInGlueAsync needs an STA thread (plugin StartUp methods construct
    // real WPF toolbars) - see GoldProjectCompileTests.
    [StaFact]
    public async Task EditorTest1_Launches_AndReportsItsCurrentScreen()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var screenName = await game.GetCurrentScreenName();

        // EditorTest1's checked-in state: StartUpScreen is the abstract GameScreen, no derived screen
        // exists (see #2002) - ScreenManager.LoadScreen silently skips instantiating it, so no screen ever
        // loads. This is the harness's own correctness proof, not the bug's: an empty string here means
        // the full round trip worked (build, launch, connect, real DTO exchange) and returned the actual
        // live state, not a stub.
        screenName.ShouldBe("");
    }

    // Pins #2002's third bug: CommandReceiver.HandleDto's entity branch calls
    // ScreenManager.CurrentScreen.MoveToScreen(...) - an instance method that requires an existing screen to
    // transition FROM (ScreenManager.MoveToScreen's own static overload documents this: "There is no
    // current screen to move from. Call Start to create the first screen."). EditorTest1 boots with no
    // screen at all (its abstract GameScreen with no derived screen - see the first test), so CurrentScreen
    // is null and there is nothing to call MoveToScreen on. Selecting an entity should work regardless -
    // EntityViewingScreen is self-contained and never touches GameScreen - so the fix is to call
    // ScreenManager.Start instead when there's no current screen.
    [StaFact]
    public async Task EditorTest1_SelectingEntity_LoadsEntityViewingScreen_EvenWithNoCurrentScreen()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        (await game.GetCurrentScreenName()).ShouldBe("", "the game should boot with no screen - see the test above");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        (await game.GetCurrentScreenName()).ShouldBe("GlueControl.Screens.EntityViewingScreen");
    }
}
