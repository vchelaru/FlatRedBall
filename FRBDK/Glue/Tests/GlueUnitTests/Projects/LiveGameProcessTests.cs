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

    // Pins #2006: selecting an abstract Screen with no concrete derived Screen (EditorTest1's GameScreen -
    // see the first test) previously had nothing to show - ScreenManager.LoadScreen can't instantiate an
    // abstract type, and RefreshManager.PushGlueSelectionToGame refused to even send the selection when it
    // found no concrete derived Screen to fall back to. The fix synthesizes a throwaway concrete subclass
    // at runtime (AbstractScreenPlaceholderFactory) purely so the abstract Screen's own Initialize/
    // AddToManagers can run - its SetByDerived fields are just default(T), same as any newly-created real
    // derived Screen would show.
    [StaFact]
    public async Task EditorTest1_SelectingAbstractScreenWithNoDerivedScreen_LoadsPlaceholder()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        (await game.GetCurrentScreenName()).ShouldBe("", "the game should boot with no screen - see the first test");

        var selectResponse = await game.SelectScreen("Screens\\GameScreen");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        (await game.GetCurrentScreenName()).ShouldBe("EditorTest1.Screens.GameScreen_EditorPlaceholder");
    }

    // Pins #2077: GameScreen's Map (a LayeredTileMap NamedObjectSave with ShiftMapToMoveGameplayLayerToZ0)
    // is SetByDerived - only a concrete derived Screen assigns it, so on the AbstractScreenPlaceholderFactory
    // placeholder from the test above it stays default(T), i.e. null. TmxCodeGenerator's
    // TryGenerateShiftZ0Code/GenerateCreateEntitiesCode unconditionally emitted
    // "Map.MapLayers.FindByName(...)" with no null guard, so AddToManagers() NREs the moment the
    // placeholder loads - independent of the new-Entity repro in the issue, which only happened to be how
    // it was noticed (see the issue's own "not yet root-caused" note).
    //
    // SelectObjectDto's response carries no failure signal (Succeeded is true either way - see the test
    // above; ScreenManager.LoadScreen sets CurrentScreen before Initialize ever runs), so the only way to
    // observe the exception from outside the game process is CommandReceiver.Receive's top-level
    // catch-all, which Console.WriteLines it - captured via LiveGameProcess's redirected stdout.
    [StaFact]
    public async Task EditorTest1_SelectingGameScreenPlaceholder_DoesNotThrowInAddToManagers()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectScreen("Screens\\GameScreen");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        // Give the game a moment to process the DTO and, if it throws, flush the exception to stdout -
        // there's no ack for "AddToManagers finished" to await instead.
        await Task.Delay(1000);

        var output = string.Join("\n", game.GetCapturedStandardOutputLines());
        output.ShouldNotContain("NullReferenceException");
    }
}
