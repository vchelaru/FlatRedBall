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
