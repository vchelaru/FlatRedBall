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
/// EmbeddedWindowLogic.IsEmbedded=true here, same production wiring as a real embedded session. There's
/// no real Glue.exe (process name "GlueFormsCore") running in this test process, so
/// EmbeddedWindowLogic.IsParentGlueFocused deterministically returns false - exactly the "window is not
/// the foreground window" state issue #2154 reports, without needing to fake real OS focus/covering.
/// </summary>
[Trait("Category", "LiveGame")]
public class EditModeActivityGateTests
{
    [StaFact]
    public async Task ClickWhileEmbeddedAndNotForegroundGlue_IsIgnored()
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
        // itself. No real Glue.exe (process name "GlueFormsCore") is running here, so
        // EmbeddedWindowLogic.IsParentGlueFocused is false - this is the "Glue is not the foreground
        // window" state.
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
            "a click should be ignored while the game window is embedded but Glue is not the foreground window (issue #2154)");
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
    /// Reproduces issue #2183: the #2154 fix above made IsGameOrGlueActive depend solely on
    /// IsParentGlueFocused (GetForegroundWindow() == glueProcess.MainWindowHandle) while embedded. But
    /// the embedded game window is reparented via a raw SetParent with no WS_CHILD style fixup
    /// (GameHostView.xaml.cs::EmbedHwnd), so Windows still lets it take OS foreground on its own when
    /// clicked - GetForegroundWindow() then returns the game's own window, not Glue's. That made
    /// click-select, deselect, and middle-mouse camera pan all silently stop working while embedded,
    /// since they're all gated behind IsGameOrGlueActive. SetEmbeddedFocusTestOverrideDto simulates that
    /// exact OS state (foreground owned by the game itself, not matching Glue's main window) since the
    /// LiveGameProcess harness can't fake real OS focus/window ownership.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndForegroundOwnedByGameItself_IsProcessed()
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

        // Simulates the real embedded state: a real Glue process exists, but OS foreground is owned by
        // the game's own reparented window instead of matching Glue's MainWindowHandle.
        var overrideResponse = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = true,
        });
        overrideResponse.Succeeded.ShouldBeTrue(overrideResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeTrue(
            "a click on the embedded game panel should be processed even though OS foreground is owned " +
            "by the game's own reparented window rather than exactly matching Glue's MainWindowHandle (issue #2183)");
        clickResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });
    }

    /// <summary>
    /// Regression guard for the #2183 fix: it must not reopen #2154. When some unrelated window (neither
    /// Glue's nor the embedded game's own) is truly in the foreground, clicks must stay ignored.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndForegroundOwnedByUnrelatedWindow_IsIgnored()
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

        var overrideResponse = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = false,
        });
        overrideResponse.Succeeded.ShouldBeTrue(overrideResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeFalse(
            "a click should still be ignored when OS foreground belongs to neither Glue nor the embedded game (issue #2154 must stay fixed)");
        clickResponse.Data.SelectedObjectNames.ShouldBeEmpty();
    }

    /// <summary>
    /// Reproduces the #2203 investigation's real-machine finding: on the affected machine,
    /// GetForegroundWindow() returned IntPtr.Zero - not Glue's window, not the game's own window, no
    /// window at all - on every single click while embedded, even though the click was clearly landing on
    /// the embedded panel. The fix is a direct spatial hit-test (WindowFromPoint at the cursor) instead of
    /// trusting OS foreground/activation state, so a click physically over our own window is recognized
    /// even when GetForegroundWindow() can't identify anything. This test simulates the outcome of that
    /// hit-test via TestOverride rather than moving a real cursor.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndCursorOverOwnWindow_IsProcessed()
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

        var overrideResponse = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = false,
            CursorOverOwnWindow = true,
        });
        overrideResponse.Succeeded.ShouldBeTrue(overrideResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeTrue(
            "a click should be processed when the cursor is physically over our own window, even though " +
            "GetForegroundWindow() couldn't identify any window at all (issue #2203)");
        clickResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });
    }

    /// <summary>
    /// Regression guard for the #2203 fix: it must not reopen #2154. The earlier "treat unidentified
    /// foreground as focused" approach was rejected specifically because this scenario - glueProcessExists
    /// true, foreground belongs to neither Glue nor the game, AND the cursor is not over our own window
    /// either (e.g. it's a lock screen, a UAC prompt, or the user genuinely alt-tabbed away) - must still
    /// block the click. This is the same override values as
    /// ClickWhileEmbeddedAndForegroundOwnedByUnrelatedWindow_IsIgnored above (CursorOverOwnWindow's default
    /// is false), kept as its own explicit test so a future change to the default doesn't silently drop
    /// this guarantee.
    /// </summary>
    [StaFact]
    public async Task ClickWhileEmbeddedAndCursorNotOverOwnWindowEither_IsIgnored()
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

        var overrideResponse = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = false,
            CursorOverOwnWindow = false,
        });
        overrideResponse.Succeeded.ShouldBeTrue(overrideResponse.Message);

        var clickResponse = await game.Send<SimulateClickSelectRespectingActivityGateResponse>(
            new SimulateClickSelectRespectingActivityGateDto
            {
                ObjectName = "TestObjectA",
                AdditiveModifierDown = false,
            });

        clickResponse.Succeeded.ShouldBeTrue(clickResponse.Message);
        clickResponse.Data.WasProcessed.ShouldBeFalse(
            "a click must stay ignored when neither the foreground window nor the cursor position identify " +
            "our own window - the #2203 fix must not reopen #2154");
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
        var overrideGateClosed = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = false,
        });
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
        var overrideGateOpen = await game.Send(new SetEmbeddedFocusTestOverrideDto
        {
            GlueProcessExists = true,
            ForegroundMatchesGlueMainWindow = false,
            ForegroundOwnedByThisGame = true,
        });
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
            var overrideGateClosed = await game.Send(new SetEmbeddedFocusTestOverrideDto
            {
                GlueProcessExists = true,
                ForegroundMatchesGlueMainWindow = false,
                ForegroundOwnedByThisGame = false,
            });
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

            var overrideGateOpen = await game.Send(new SetEmbeddedFocusTestOverrideDto
            {
                GlueProcessExists = true,
                ForegroundMatchesGlueMainWindow = false,
                ForegroundOwnedByThisGame = true,
            });
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

    /// <summary>
    /// Pins EmbeddedWindowLogic.ComputeCursorOverOwnWindow's decision table directly, via synthetic Win32-
    /// call outcomes (TestComputeCursorOverOwnWindowDto) rather than a real ClientToScreen/WindowFromPoint
    /// round trip against a real overlapping window - moving the real OS cursor precisely enough for that
    /// proved unreliable on a multi-monitor dev machine (issue #2205 discussion), so this is the regression
    /// coverage that actually shipped for the occlusion fix instead. One process, four cases: covers both
    /// fail-closed paths (no ClientToScreen result, no window found at the cursor) and both PID-match
    /// outcomes (occluded by another window vs. genuinely on top).
    /// </summary>
    [StaFact]
    public async Task ComputeCursorOverOwnWindow_DecisionTable()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

        var clientToScreenFailed = await game.Send<TestComputeCursorOverOwnWindowResponse>(
            new TestComputeCursorOverOwnWindowDto
            {
                ClientToScreenSucceeded = false,
                TopmostWindowFound = true,
                TopmostWindowOwnerPid = 123,
                ThisProcessId = 123,
            });
        clientToScreenFailed.Succeeded.ShouldBeTrue(clientToScreenFailed.Message);
        clientToScreenFailed.Data.Result.ShouldBeFalse(
            "a failed ClientToScreen call must fail closed even if the (unusable) window/PID inputs would otherwise match");

        var noWindowFound = await game.Send<TestComputeCursorOverOwnWindowResponse>(
            new TestComputeCursorOverOwnWindowDto
            {
                ClientToScreenSucceeded = true,
                TopmostWindowFound = false,
                TopmostWindowOwnerPid = 123,
                ThisProcessId = 123,
            });
        noWindowFound.Succeeded.ShouldBeTrue(noWindowFound.Message);
        noWindowFound.Data.Result.ShouldBeFalse(
            "no window found at the cursor position (e.g. the desktop background) must fail closed");

        var occludedByAnotherProcess = await game.Send<TestComputeCursorOverOwnWindowResponse>(
            new TestComputeCursorOverOwnWindowDto
            {
                ClientToScreenSucceeded = true,
                TopmostWindowFound = true,
                TopmostWindowOwnerPid = 456,
                ThisProcessId = 123,
            });
        occludedByAnotherProcess.Succeeded.ShouldBeTrue(occludedByAnotherProcess.Message);
        occludedByAnotherProcess.Data.Result.ShouldBeFalse(
            "a real window belonging to a different process is topmost at the cursor - this is the exact " +
            "occlusion scenario from the #2205 follow-up report and must not be detected as our own window");

        var genuinelyOnTop = await game.Send<TestComputeCursorOverOwnWindowResponse>(
            new TestComputeCursorOverOwnWindowDto
            {
                ClientToScreenSucceeded = true,
                TopmostWindowFound = true,
                TopmostWindowOwnerPid = 123,
                ThisProcessId = 123,
            });
        genuinelyOnTop.Succeeded.ShouldBeTrue(genuinelyOnTop.Message);
        genuinelyOnTop.Data.Result.ShouldBeTrue(
            "the topmost window at the cursor belongs to our own process - nothing is occluding us");
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
