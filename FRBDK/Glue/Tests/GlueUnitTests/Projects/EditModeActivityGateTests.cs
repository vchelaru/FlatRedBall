using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Reproduces issue #2154: Edit Mode keeps processing mouse clicks even when the game/Glue window is
/// out of focus or covered by another window. Root cause: EditingManager.IsGameOrGlueActive OR's
/// IsEmbeddedInActiveGlue with FlatRedBallServices.Game.IsActive - but in embedded Edit Mode the game
/// window is reparented into Glue (SetParent), so it never receives normal WM_ACTIVATE/deactivate
/// notifications and Game.IsActive gets stuck true, masking the correct IsEmbeddedInActiveGlue check.
///
/// Drives EditingManager.SimulateClickSelectRespectingActivityGateForTesting (Embedded) - unlike
/// SimulateClickSelectForTesting (used by CtrlClickMultiSelectTests etc.), this respects
/// IsGameOrGlueActive first, the same gate a real click goes through in EditingManager.Activity.
/// SetBorderlessDto{IsBorderless=true} is the exact DTO real Glue sends to mark the window embedded
/// (CommandReceiver.HandleDto(SetBorderlessDto), GameHostView.EmbedHwnd) - it's what sets
/// EmbeddedWindowLogic.IsEmbedded=true here, same production wiring as a real embedded session.
///
/// Whether input is allowed comes from Glue as SetEmbeddedInputAllowedDto - also the real production
/// DTO (EmbeddedInputAllowedService). The game side no longer inspects OS focus state itself, so these
/// tests drive the gate through the same path a real session does rather than through a test-only
/// override, and the Win32 half that decides the value is tested against a real window in
/// EmbeddedInputAllowedServiceTests.
/// </summary>
[Trait("Category", "LiveGame")]
public class EditModeActivityGateTests
{
    [StaFact]
    public async Task ClickWhileEmbeddedAndGlueHasNotReportedInputAllowed_IsIgnored()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        // Real Glue sends this to mark the window embedded when it reparents the game window into
        // itself. Deliberately no SetEmbeddedInputAllowedDto after it: IsInputAllowedFromGlue starts
        // false, so this is also the fail-closed default - a game that has been embedded but hasn't yet
        // been told anything about the cursor must not act on clicks (issue #2154).
        var borderlessResponse = await game.Send(new SetBorderlessDto { IsBorderless = true });
        borderlessResponse.Succeeded.ShouldBeTrue(borderlessResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeFalse(
            "a click should be ignored while the game is embedded but Glue hasn't reported that the game " +
            "window is the topmost window under the cursor (issue #2154)");
        clickResponse.Data.SelectedObjectNames.ShouldBeEmpty();
    }

    [StaFact]
    public async Task ClickWhileNotEmbedded_IsStillProcessed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        // Deliberately no SetBorderlessDto here - EmbeddedWindowLogic.IsEmbedded stays false, matching a
        // standalone (non-Glue-embedded) run where Game.IsActive is the real, meaningful focus signal.
        // Regression guard: the #2154 fix must not block clicks in this mode.
        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeTrue(
            "a non-embedded (standalone) game window should still process clicks normally");
        clickResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });
    }

    /// <summary>
    /// The other side of the gate: Glue reports that the embedded game window IS the topmost window
    /// under the cursor, so a click must land. Covers what issues #2183 (click-select, deselect and
    /// middle-mouse pan silently dead while embedded) and #2205 (every click dropped) both reported.
    ///
    /// SetEmbeddedInputAllowedDto is the real production DTO - EmbeddedInputAllowedService in Glue
    /// sends exactly this - so unlike the test-only focus override this replaces, the game side is
    /// exercised through the same path a real session uses. The Win32 half that decides the value is
    /// tested separately, against a real window, in EmbeddedInputAllowedServiceTests.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndGlueReportsInputAllowed_IsProcessed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        var borderlessResponse = await game.Send(new SetBorderlessDto { IsBorderless = true });
        borderlessResponse.Succeeded.ShouldBeTrue(borderlessResponse.Message);

        var inputAllowedResponse = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = true });
        inputAllowedResponse.Succeeded.ShouldBeTrue(inputAllowedResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeTrue(
            "a click must be processed while Glue reports the embedded game window as topmost under the " +
            "cursor - this is the ordinary case of a user working in the Game tab (issues #2183, #2205)");
        clickResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });
    }

    /// <summary>
    /// Regression guard for issues #2154/#2214: Glue reports that something else is topmost under the
    /// cursor - another application's window dragged over the Game tab, or Glue sitting behind another
    /// app entirely - so the click belongs to that window and the game underneath must not act on it.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndGlueReportsInputBlocked_IsIgnored()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        var borderlessResponse = await game.Send(new SetBorderlessDto { IsBorderless = true });
        borderlessResponse.Succeeded.ShouldBeTrue(borderlessResponse.Message);

        // Explicitly allow first, so this proves the block actually closes an OPEN gate rather than
        // passing on the fail-closed default that ClickWhileEmbeddedAndGlueHasNotReportedInputAllowed_IsIgnored covers.
        var inputAllowedResponse = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = true });
        inputAllowedResponse.Succeeded.ShouldBeTrue(inputAllowedResponse.Message);

        var inputBlockedResponse = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = false });
        inputBlockedResponse.Succeeded.ShouldBeTrue(inputBlockedResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeFalse(
            "a click must be ignored when the topmost window under the cursor isn't the embedded game - " +
            "it belongs to whatever is drawn on top (issues #2154, #2214)");
        clickResponse.Data.SelectedObjectNames.ShouldBeEmpty();
    }

    /// <summary>
    /// Reproduces issue #2187: dragging an object in the embedded game fails on the first attempt after
    /// Glue's own UI (e.g. the property grid) had focus, requiring a throwaway click before a real
    /// click+drag works. Root cause: EditingManager.DoGrabLogic's fallback for "mouse already held down
    /// on the frame the activity gate reopens" was computed from FlatRedBallServices.Game.IsActive,
    /// which is stuck true for the whole embedded session (the reparented game window never gets a real
    /// deactivate notification) - so the fallback was permanently dead. A press that starts while the
    /// gate is closed and is still held when the gate opens (button down, but not a fresh ButtonPushed)
    /// must still register as a grab.
    /// </summary>
    [StaFact]
    public async Task GrabHeldAcrossFocusGateOpening_IsProcessed()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        var borderlessResponse = await game.Send(new SetBorderlessDto { IsBorderless = true });
        borderlessResponse.Succeeded.ShouldBeTrue(borderlessResponse.Message);

        // Simulate: the mouse button goes down on the object while the gate is still closed (Glue's UI
        // still has real OS focus) - a real click on an unfocused embedded window is correctly ignored,
        // same as issue #2154.
        var overrideGateClosed = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = false });
        overrideGateClosed.Succeeded.ShouldBeTrue(overrideGateClosed.Message);

        var pushWhileGateClosed = await game.Send<SimulateGrabAcrossFocusGateResponse>(
            new SimulateGrabAcrossFocusGateDto
            {
                ObjectName = "TestObjectA",
                ButtonPushed = true,
                ButtonDown = true,
                WasGameOrGlueActiveLastFrame = false,
                AdditiveModifierDown = false,
            });
        pushWhileGateClosed.Succeeded.ShouldBeTrue(pushWhileGateClosed.Message);
        pushWhileGateClosed.Data.WasProcessed.ShouldBeFalse(
            "a press that starts while the window is unfocused should still be ignored (issue #2154 must stay fixed)");

        // OS foreground now transfers to the embedded game's own reparented window (the click that
        // brought it back) - the gate opens. The button is still down from the same continuous gesture,
        // so this is ButtonDown without a fresh ButtonPushed - exactly like a real held mouse-drag that
        // straddles the focus transition.
        var overrideGateOpen = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = true });
        overrideGateOpen.Succeeded.ShouldBeTrue(overrideGateOpen.Message);

        var heldDownAsGateOpens = await game.Send<SimulateGrabAcrossFocusGateResponse>(
            new SimulateGrabAcrossFocusGateDto
            {
                ObjectName = "TestObjectA",
                ButtonPushed = false,
                ButtonDown = true,
                WasGameOrGlueActiveLastFrame = false,
                AdditiveModifierDown = false,
            });
        heldDownAsGateOpens.Succeeded.ShouldBeTrue(heldDownAsGateOpens.Message);
        heldDownAsGateOpens.Data.WasProcessed.ShouldBeTrue(
            "a mouse button held down through the moment the activity gate opens should still be treated " +
            "as a grab, or the drag is lost until an entirely separate click (issue #2187)");
        heldDownAsGateOpens.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });
    }

    /// <summary>
    /// Issue #2196: diagnoses issue #2183-style bugs (a focus-gate misfire that varies by machine and
    /// doesn't reproduce locally) by logging what the gate actually saw. Opt-in via
    /// SetEmbeddedDiagnosticsEnabledDto - drives both a blocked click (gate closed) and a processed one
    /// (gate open) through SimulateGrabAcrossFocusGateDto, the same seam GrabHeldAcrossFocusGateOpening_IsProcessed
    /// above uses, and asserts the resulting log file records the gate snapshot and the selection change.
    /// </summary>
    [StaFact]
    public async Task EmbeddedDiagnostics_LogsBlockedClickGateSnapshotAndSelectionChange()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestObjectToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        var borderlessResponse = await game.Send(new SetBorderlessDto { IsBorderless = true });
        borderlessResponse.Succeeded.ShouldBeTrue(borderlessResponse.Message);

        var enableResponse = await game.Send<SetEmbeddedDiagnosticsEnabledResponse>(
            new SetEmbeddedDiagnosticsEnabledDto { IsEnabled = true });
        enableResponse.Succeeded.ShouldBeTrue(enableResponse.Message);
        var logFilePath = enableResponse.Data.LogFilePath;

        try
        {
            var overrideGateClosed = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = false });
            overrideGateClosed.Succeeded.ShouldBeTrue(overrideGateClosed.Message);

            var blockedClick = await game.Send<SimulateGrabAcrossFocusGateResponse>(
                new SimulateGrabAcrossFocusGateDto
                {
                    ObjectName = "TestObjectA",
                    ButtonPushed = true,
                    ButtonDown = true,
                    WasGameOrGlueActiveLastFrame = false,
                    AdditiveModifierDown = false,
                });
            blockedClick.Succeeded.ShouldBeTrue(blockedClick.Message);
            blockedClick.Data.WasProcessed.ShouldBeFalse();

            var overrideGateOpen = await game.Send(new SetEmbeddedInputAllowedDto { IsAllowed = true });
            overrideGateOpen.Succeeded.ShouldBeTrue(overrideGateOpen.Message);

            var processedClick = await game.Send<SimulateGrabAcrossFocusGateResponse>(
                new SimulateGrabAcrossFocusGateDto
                {
                    ObjectName = "TestObjectA",
                    ButtonPushed = true,
                    ButtonDown = true,
                    WasGameOrGlueActiveLastFrame = false,
                    AdditiveModifierDown = false,
                });
            processedClick.Succeeded.ShouldBeTrue(processedClick.Message);
            processedClick.Data.WasProcessed.ShouldBeTrue();

            var disableResponse = await game.Send(new SetEmbeddedDiagnosticsEnabledDto { IsEnabled = false });
            disableResponse.Succeeded.ShouldBeTrue(disableResponse.Message);

            var logContents = System.IO.File.ReadAllText(logFilePath);

            // The blocked click (gate closed) must be logged, not just the ones that succeed - that's
            // exactly the case a user's machine-specific gate misfire needs to be read back from (#2183).
            logContents.ShouldContain("processed=False");
            logContents.ShouldContain("foregroundOwnedByThisGame=False");
            logContents.ShouldContain("processed=True");
            logContents.ShouldContain("foregroundOwnedByThisGame=True");
            logContents.ShouldContain("Selection changed: [TestObjectA]");
        }
        finally
        {
            if (System.IO.File.Exists(logFilePath))
            {
                System.IO.File.Delete(logFilePath);
            }
        }
    }

    static async Task AddTestObjectToGameScreen()
    {
        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var nos = new NamedObjectSave();
        nos.SetDefaults();
        nos.InstanceName = "TestObjectA";
        nos.SourceType = SourceType.FlatRedBallType;
        nos.SourceClassType = "FlatRedBall.Math.Geometry.AxisAlignedRectangle";

        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            nos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: true, updateUi: false);
    }
}
