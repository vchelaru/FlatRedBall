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
