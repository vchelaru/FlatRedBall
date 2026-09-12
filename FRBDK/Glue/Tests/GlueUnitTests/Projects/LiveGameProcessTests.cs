using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
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

    // #2261 reports a Sprite added live to an Entity, renamed, reordered, then failing a variable set
    // with "Could not find an object named Head in EntityViewingScreen". The obvious suspect -
    // VariableAssignmentLogic's setOnEntity branch only resolving targets via reflection over the
    // entity's COMPILED members, with no fallback for a live-added child - turned out to be wrong:
    // Screen.GetInstance (Engines\FlatRedBallXNA\FlatRedBall\Screens\Screen.cs:796-799) already falls
    // back to searching PositionedObject.Children by name when reflection finds nothing. This test pins
    // that the base case (add, then set a variable, no rename/reorder) already works, so nobody "fixes"
    // this lookup again for the wrong reason. The real trigger is still open - see the issue for the
    // pivot to EmbeddedDiagnosticsLogger DTO logging below, which is what should nail it down next time
    // it reproduces.
    [StaFact]
    public async Task EditorTest1_SettingVariableOnSpriteAddedLiveToEntity_FindsItViaRuntimeChildren()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);
        (await game.GetCurrentScreenName()).ShouldBe("GlueControl.Screens.EntityViewingScreen");

        var entity = ObjectFinder.Self.GetEntitySave("Entities\\Entity1");

        // Same DTO shape as RefreshManager.CreateAddObjectDtoFor - a Sprite added live via the "add
        // object" flow, exactly like a user dragging a new Sprite onto the entity in Glue while it's
        // running.
        var headSprite = new NamedObjectSave
        {
            InstanceName = "Head",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Sprite",
            AddToManagers = true,
        };
        var addObjectDto = new AddObjectDto
        {
            NamedObjectSave = headSprite,
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
        };
        addObjectDto.NamedObjectsToUpdate.Add(new NamedObjectWithElementName
        {
            NamedObjectSave = headSprite,
            GlueElementName = "Entities\\Entity1",
        });

        var addResponse = await game.Send<AddObjectDtoResponse>(addObjectDto);
        addResponse.Succeeded.ShouldBeTrue(addResponse.Message);
        addResponse.Data.CreationResponse.Succeeded.ShouldBeTrue("the game should have created the live Sprite");

        // Same DTO shape as VariableSendingManager - setting a variable on the newly-added Sprite,
        // exactly like the user editing it in the property grid. X rather than CurrentChainName so this
        // only exercises the lookup, not Sprite's animation-chain validation.
        var setVariableDto = new GlueVariableSetData
        {
            AssignOrRecordOnly = AssignOrRecordOnly.Assign,
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
            VariableName = "this.Head.X",
            VariableValue = "5",
            Type = "float",
            AbsoluteGlueProjectFilePath = Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        };

        var setResponse = await game.Send<GlueVariableSetDataResponse>(setVariableDto);
        setResponse.Succeeded.ShouldBeTrue(setResponse.Message);
        setResponse.Data.Exception.ShouldBeNull();
        setResponse.Data.WasVariableAssigned.ShouldBeTrue();
    }

    // #2261's actual root cause, pinned directly via the diagnostics log from a real repro: renaming a
    // NamedObjectSave - GluxCommands.RenameNamedObjectSave (Glue/Plugins/ExportedImplementations/
    // CommandInterfaces/GluxCommands.cs:1039) - only ever sets InstanceName on Glue's own model and
    // regenerates code (NamedObjectSetVariableLogic.ReactToNamedObjectChangedInstanceName). Neither calls
    // CommandSender.Self.Send - there is no DTO for "a named object was renamed" at all. So a live-added
    // object renamed in Glue's tree/property grid keeps its ORIGINAL runtime Name forever, even though
    // Glue's own model (and therefore every DTO addressing it from then on) uses the new one. There is
    // nothing to replay here - the rename is a no-op on the wire - so this goes straight to the
    // observable consequence: the running game still answers to the pre-rename name, not the name Glue
    // now shows.
    //
    // Measured (not just read) via a temporary Console.WriteLine probe: addressing "this.Head.X" resolves
    // "Head" to nothing (no compiled member, no child by that name), falls back to
    // Screen.GetInstanceRecursive("this.Head"), which - finding nothing there either - returns the
    // SCREEN itself rather than null. VariableAssignmentLogic then tries to apply "X" to the screen,
    // which has no such member, throwing a raw MemberAccessException that propagates out uncaught by
    // SetValueOnObjectInElement. That is a DIFFERENT failure shape than the original issue's clean
    // "Could not find an object named Head..." message with a contradictory WasVariableAssigned=true -
    // this repro's exact variable ("X", a plain float) resolves through a different branch than the
    // original's ("CurrentChainName" on a Sprite with AnimationChains set). Reproducing that exact
    // contradictory shape would need the same variable/setup as the original log, not attempted here.
    // Both shapes share the same root cause: the runtime object was never renamed.
    [StaFact]
    public async Task EditorTest1_RenamedLiveAddedObject_IsStillFoundByItsOriginalRuntimeName()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);
        (await game.GetCurrentScreenName()).ShouldBe("GlueControl.Screens.EntityViewingScreen");

        var entity = ObjectFinder.Self.GetEntitySave("Entities\\Entity1");

        var sprite = new NamedObjectSave
        {
            InstanceName = "LeftWingTest",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Sprite",
            AddToManagers = true,
        };
        var addObjectDto = new AddObjectDto
        {
            NamedObjectSave = sprite,
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
        };
        addObjectDto.NamedObjectsToUpdate.Add(new NamedObjectWithElementName
        {
            NamedObjectSave = sprite,
            GlueElementName = "Entities\\Entity1",
        });

        var addResponse = await game.Send<AddObjectDtoResponse>(addObjectDto);
        addResponse.Succeeded.ShouldBeTrue(addResponse.Message);
        addResponse.Data.CreationResponse.Succeeded.ShouldBeTrue("the game should have created the live Sprite");

        GlueVariableSetData SetXDto(string instanceName) => new GlueVariableSetData
        {
            AssignOrRecordOnly = AssignOrRecordOnly.Assign,
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
            VariableName = $"this.{instanceName}.X",
            VariableValue = "5",
            Type = "float",
            AbsoluteGlueProjectFilePath = Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        };

        // Addressed by the name Glue's own model would use after a rename to "Head" - fails, because the
        // runtime object is still named "LeftWingTest". See this test's doc comment for why the failure
        // is a raw MemberAccessException here rather than the original issue's clean "Could not find an
        // object named" message - different sub-path, same root cause.
        var byNewName = await game.Send<GlueVariableSetDataResponse>(SetXDto("Head"));
        byNewName.Succeeded.ShouldBeTrue(byNewName.Message);
        byNewName.Data.Exception.ShouldNotBeNull("the runtime object was never renamed, so lookup by the new name should fail");
        byNewName.Data.WasVariableAssigned.ShouldBeFalse();

        // Addressed by the name the game still actually uses - succeeds, proving the object was never
        // lost, just never renamed.
        var byOriginalName = await game.Send<GlueVariableSetDataResponse>(SetXDto("LeftWingTest"));
        byOriginalName.Succeeded.ShouldBeTrue(byOriginalName.Message);
        byOriginalName.Data.Exception.ShouldBeNull(byOriginalName.Data.Exception);
        byOriginalName.Data.WasVariableAssigned.ShouldBeTrue();
    }

    // Pins the EmbeddedDiagnosticsLogger extension added for #2261: every DTO the game receives (and its
    // response, when it sends one back) is recorded in memory, always on - no enable step needed - and
    // fetchable at any time via GetEmbeddedDiagnosticsLogDto. See CommandReceiver.Receive and
    // EditingManager.cs's EmbeddedDiagnosticsLogger.
    [StaFact]
    public async Task EditorTest1_EmbeddedDiagnostics_RecordsDtoTrafficAlwaysOn()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        // No enable/opt-in step - the log is already recording from process start.
        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        var logResponse = await game.Send<GetEmbeddedDiagnosticsLogResponse>(new GetEmbeddedDiagnosticsLogDto());
        logResponse.Succeeded.ShouldBeTrue(logResponse.Message);
        logResponse.Data.LogText.ShouldContain("Received SelectObjectDto");
    }
}
