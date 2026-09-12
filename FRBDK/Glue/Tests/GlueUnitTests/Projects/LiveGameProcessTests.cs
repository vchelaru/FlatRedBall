using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
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

    // #2261's actual root cause, fixed and pinned here: renaming a NamedObjectSave used to only update
    // Glue's own model and regenerate code (GluxCommands.RenameNamedObjectSave) - nothing ever told a
    // running game the rename happened, so a live-added object renamed in Glue's tree/property grid kept
    // its ORIGINAL runtime Name forever, even though Glue's own model (and every DTO addressing it from
    // then on) used the new one.
    //
    // The fix has two parts: GluxCommands.RenameNamedObjectSave now calls
    // PluginManager.ReactToNamedObjectChangedValue(nameof(NamedObjectSave.InstanceName), oldName, nos) -
    // the same event GameCommunicationPlugin's MainCompilerPlugin already listens to for ordinary NOS
    // property changes, so it reuses the existing, already-working notify-the-game pipeline rather than
    // adding a new one. This test cannot exercise THAT half directly - MainCompilerPlugin's own StartUp
    // isn't registered in this headless test host (it builds real WPF toolbars, like several plugins this
    // harness's GlueTestBootstrap already skips for the same reason - see LiveGameProcess's own doc
    // comment on MainCompilerPlugin not being safe to run here), so PluginManager has no subscriber to
    // dispatch to. That one-line call follows the exact same PluginManager.ReactToNamedObjectChangedValue
    // pattern already used (and exercised in real Glue every time a property-grid edit reaches the game)
    // by many other call sites - ElementCommands.cs, GluxCommands.cs's other renames, SetPropertyManager.cs.
    //
    // What this test DOES pin, directly, is the actually-new logic: VariableSendingManager's InstanceName
    // special case (GetNamedObjectValueChangedDtos) builds a "this.<oldName>.Name" variable-set DTO
    // (addressing the OLD name - the only one the game has ever heard of), which VariableAssignmentLogic's
    // existing generic reflection path turns into a real `.Name =` assignment on the live runtime object -
    // by calling that manager the same way MainCompilerPlugin's event handler would, one level below the
    // plugin-dispatch this host can't run.
    [StaFact]
    public async Task EditorTest1_RenamingLiveAddedObject_NotifiesTheGame_SoItsReachableByItsNewName()
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

        // Also added to Glue's own project model, exactly as a real "add object" flow would - so
        // GetElementContaining(sprite) below can find its containing element the same way it would for a
        // real live-added object.
        entity.NamedObjects.Add(sprite);

        var oldName = sprite.InstanceName;
        sprite.InstanceName = "Head";

        // Builds the DTO exactly the way MainCompilerPlugin's ReactToNamedObjectChangedValue handler
        // would (RefreshManager.HandleNamedObjectVariableOrPropertyChanged ->
        // VariableSendingManager.HandleNamedObjectVariableChanged), then sends it directly - that handler
        // itself can't run in this test host (see doc comment above), and its own send is deliberately
        // fire-and-forget in production, which would swallow this test's own assertions on the response.
        var vsm = new VariableSendingManager(new RefreshManager((_, __) => Task.FromResult(""), (_, __) => { }));
        var builtDtos = vsm.GetNamedObjectValueChangedDtos(
            nameof(NamedObjectSave.InstanceName), oldName, sprite, AssignOrRecordOnly.Assign, gameScreenName: "");
        builtDtos.Count.ShouldBe(1, $"expected exactly one rename DTO to be built (oldName={oldName})");
        builtDtos[0].VariableName.ShouldBe($"this.{oldName}.Name");
        builtDtos[0].VariableValue.ShouldBe("Head");
        // A real Glue session's SetEditMode DTO (sent once, first, over the wire) is what normally
        // populates the game's own ObjectFinder.Self.GlueProject; this harness never sends one, so
        // without this the game's own CommandReceiver.HandleDto(GlueVariableSetData) NREs trying to load
        // a project from a null path. Production code never needs this field on an ordinary NOS
        // property-change DTO (see GetGlueVariableSetDataDto) for the same reason.
        builtDtos[0].AbsoluteGlueProjectFilePath = Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj");

        var renameResponse = await game.Send<GlueVariableSetDataResponse>(builtDtos[0]);
        renameResponse.Succeeded.ShouldBeTrue(renameResponse.Message);
        renameResponse.Data.Exception.ShouldBeNull(renameResponse.Data.Exception);

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

        // The rename reached the running game - addressed by the NEW name, the object is found.
        var byNewName = await game.Send<GlueVariableSetDataResponse>(SetXDto("Head"));
        byNewName.Succeeded.ShouldBeTrue(byNewName.Message);
        byNewName.Data.Exception.ShouldBeNull(byNewName.Data.Exception);
        byNewName.Data.WasVariableAssigned.ShouldBeTrue();

        // ...and the runtime object was actually renamed, not just found under a second alias - its old
        // name no longer resolves.
        var byOriginalName = await game.Send<GlueVariableSetDataResponse>(SetXDto("LeftWingTest"));
        byOriginalName.Succeeded.ShouldBeTrue(byOriginalName.Message);
        byOriginalName.Data.Exception.ShouldNotBeNull("the object was renamed, so its old name should no longer resolve");
    }

    // Adjacent bug found (and fixed) while fixing #2261, in the game-side bookkeeping
    // ReplaceNamedObjectSave pushes via NamedObjectsToUpdate keep in sync
    // (EditingManager.CurrentGlueElement.NamedObjects - GetCurrentElementNamedObjectsDto, added to
    // observe it, since it isn't visible over the wire any other way): ReplaceNamedObjectSave used to
    // find the entry to replace by matching `item.InstanceName == nos.InstanceName` - fine when a NOS's
    // own name never changes, but nos.InstanceName IS the new name for a rename, so it never found the
    // old entry (still under the old name) and just added a second one instead of replacing it. Proven
    // red before the fix: sending a rename's NamedObjectsToUpdate entry without OldInstanceName (the way
    // this test builds it below) produced ["CircleInstance", "LeftWingTest", "Head"] - both names
    // present. Fixed by NamedObjectWithElementName.OldInstanceName, which PushVariableChangesToGame now
    // populates for exactly this case and ReplaceNamedObjectSave matches against instead.
    [StaFact]
    public async Task EditorTest1_RenamedLiveObjectPushedViaNamedObjectsToUpdate_ReplacesRatherThanDuplicatesBookkeeping()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

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

        var beforeRename = await game.Send<GetCurrentElementNamedObjectsResponse>(new GetCurrentElementNamedObjectsDto());
        beforeRename.Succeeded.ShouldBeTrue(beforeRename.Message);
        beforeRename.Data.InstanceNames.Count(n => n == "LeftWingTest").ShouldBe(1,
            "sanity check: the live add itself should register exactly one bookkeeping entry");

        entity.NamedObjects.Add(sprite);
        var oldName = sprite.InstanceName;
        sprite.InstanceName = "Head";

        var vsm = new VariableSendingManager(new RefreshManager((_, __) => Task.FromResult(""), (_, __) => { }));
        var renameDtos = vsm.GetNamedObjectValueChangedDtos(
            nameof(NamedObjectSave.InstanceName), oldName, sprite, AssignOrRecordOnly.Assign, gameScreenName: "");
        renameDtos[0].AbsoluteGlueProjectFilePath = Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj");

        // Built by hand rather than via PushVariableChangesToGame (which now populates OldInstanceName
        // itself for exactly this case) so the test can assert on the NamedObjectsToUpdate shape
        // directly - OldInstanceName is what tells ReplaceNamedObjectSave which stale entry to replace.
        var listDto = new GlueVariableSetDataList();
        listDto.Data.AddRange(renameDtos);
        listDto.NamedObjectsToUpdate.Add(new NamedObjectWithElementName
        {
            NamedObjectSave = sprite,
            GlueElementName = "Entities\\Entity1",
            OldInstanceName = oldName,
        });

        var pushResponse = await game.Send<GlueVariableSetDataResponseList>(listDto);
        pushResponse.Succeeded.ShouldBeTrue(pushResponse.Message);

        var afterRename = await game.Send<GetCurrentElementNamedObjectsResponse>(new GetCurrentElementNamedObjectsDto());
        afterRename.Succeeded.ShouldBeTrue(afterRename.Message);
        afterRename.Data.InstanceNames.ShouldContain("Head");
        afterRename.Data.InstanceNames.ShouldNotContain("LeftWingTest",
            "ReplaceNamedObjectSave should have replaced the stale old-named entry, not left it behind");
        afterRename.Data.InstanceNames.Count(n => n == "Head").ShouldBe(1,
            "ReplaceNamedObjectSave should not have added a second entry for the renamed object");
    }

    // #2261's diagnostics gap, found while reproducing the test above: GlueViewSettingsViewModel.
    // RestartOnFailedCommands (default true) kills and relaunches the game the moment a variable-set
    // response reports an Exception - wiping EmbeddedDiagnosticsLogger's in-memory buffer before anyone
    // can click "View Diagnostics Log" to fetch it, so a second repro of the same bug produced a log with
    // no trace of the failure at all. FlushToDiskOnFailure writes the buffer to disk synchronously, as
    // part of producing the failing response, so it lands before any restart could possibly race it. This
    // pins that it actually does.
    [StaFact]
    public async Task EditorTest1_FailedVariableSet_FlushesDiagnosticsLogToDiskBeforeAnyRestart()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        var entity = ObjectFinder.Self.GetEntitySave("Entities\\Entity1");

        // Addresses an instance that was never added - guaranteed to fail the lookup and report an
        // Exception, regardless of which internal sub-path it takes (see the rename test above for why
        // that sub-path can vary).
        var setVariableDto = new GlueVariableSetData
        {
            AssignOrRecordOnly = AssignOrRecordOnly.Assign,
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
            VariableName = "this.NoSuchObject2261.X",
            VariableValue = "5",
            Type = "float",
            AbsoluteGlueProjectFilePath = Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        };

        var setResponse = await game.Send<GlueVariableSetDataResponse>(setVariableDto);
        setResponse.Succeeded.ShouldBeTrue(setResponse.Message);
        setResponse.Data.Exception.ShouldNotBeNull("this test needs a failing variable set to exercise the flush");

        Directory.Exists(game.EmbeddedDiagnosticsOnFailureDirectory).ShouldBeTrue(
            "a failed variable-set response should have triggered EmbeddedDiagnosticsLogger's on-failure disk flush");
        var flushedFiles = Directory.GetFiles(game.EmbeddedDiagnosticsOnFailureDirectory, "communication-onfailure-*.log");
        flushedFiles.ShouldNotBeEmpty();
        var flushedContent = File.ReadAllText(flushedFiles[0]);
        flushedContent.ShouldContain("Received GlueVariableSetData");
        flushedContent.ShouldContain("NoSuchObject2261");
    }

    // #2261's other diagnostics gap, found only because the user had to manually notice and report this
    // message from Glue's Output panel: EditingManager.GetObjectByName's "Tried to get object by name X
    // but couldn't find anything" (fired here by re-selecting a NamedObjectSave whose name Glue's model
    // knows but the running game does not - exactly what the rename gap above leaves behind) is sent
    // game->Glue as a PrintOutput call, a completely different channel than the DTO traffic
    // LogDtoReceived/LogDtoResponse already captured - so it never reached the diagnostics log at all.
    // GlueControlManager.SendToGlue is the single choke point for every such outbound call, so logging
    // there (LogDtoSent) covers this message, and any other PrintOutput/Undo/etc. call, for free.
    [StaFact]
    public async Task EditorTest1_SelectingNamedObjectMissingAtRuntime_LogsOutboundGetObjectByNameFailure()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var selectResponse = await game.SelectEntity("Entities\\Entity1");
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        var entity = ObjectFinder.Self.GetEntitySave("Entities\\Entity1");

        // Simulates Glue's own model believing a NamedObjectSave named "Head" exists on this entity -
        // exactly what a rename (which never notifies the running game - see the test above) leaves
        // behind. No such object was ever added to the live game itself.
        entity.NamedObjects.Add(new NamedObjectSave
        {
            InstanceName = "Head",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Sprite",
        });

        // EntitySave must be included so the game's own model (a separate process/copy - see
        // CommandReceiver.HandleDto(SelectObjectDto)'s GlueElement != null branch) picks up the "Head"
        // NamedObjectSave just added above; without it GetSelectedNamedObjects finds nothing and
        // EditingManager.Select is never even called.
        var selectHeadResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Entities\\Entity1",
            EntitySave = entity,
            NamedObjectNames = new List<string> { "Head" },
        });
        selectHeadResponse.Succeeded.ShouldBeTrue(selectHeadResponse.Message);

        var logResponse = await game.Send<GetEmbeddedDiagnosticsLogResponse>(new GetEmbeddedDiagnosticsLogDto());
        logResponse.Succeeded.ShouldBeTrue(logResponse.Message);
        logResponse.Data.LogText.ShouldContain("Tried to get object by name Head but couldn't find anything");
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
