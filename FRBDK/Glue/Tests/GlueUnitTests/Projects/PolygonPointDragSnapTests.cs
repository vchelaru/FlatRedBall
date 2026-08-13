using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GlueUnitTests.TestSupport;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Reproduces issue #2074 end to end against a real running game: whole-object dragging in live edit
/// snaps to SnapSize, but dragging a Polygon point did not, even with PolygonPointSnapSize/EnableSnapping
/// configured exactly like production Glue sends them. Drives PolygonPointHandles.SimulateDragForTesting
/// (Embedded) - the exact same production drag+snap code a real mouse drag would run, just fed an explicit
/// delta instead of reading Cursor/hardware, so this exercises the real DTO -> EditingManager ->
/// SelectionMarker -> PolygonPointHandles wiring, not just the snap arithmetic in isolation.
/// </summary>
[Trait("Category", "LiveGame")]
public class PolygonPointDragSnapTests
{
    static readonly List<Vector2> InitialPoints = new List<Vector2>
    {
        new Vector2(-8, 8),
        new Vector2(8, 8),
        new Vector2(8, -8),
        new Vector2(-8, -8),
        new Vector2(-8, 8),
    };

    [StaFact]
    public async Task DraggingAPolygonPoint_WithSnappingEnabled_SnapsToGrid()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestPolygonToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        // Load the screen first, with nothing selected yet.
        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        // Must happen before selecting the polygon: while ScreenManager.IsInEditMode is false,
        // EditingManager's per-frame update takes its "not in edit mode" branch, which clears
        // itemsSelected/SelectedMarkers (but not CurrentNamedObjects) every frame. Selecting first and
        // enabling edit mode after leaves CurrentNamedObjects stale-"selected" while itemsSelected is
        // empty, so the next select is treated as a no-op echo of an already-current selection and never
        // actually re-applied.
        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            // The game's own ObjectFinder.Self.GlueProject is null until this loads it - without it,
            // entering edit mode NREs inside GlueCommands.LoadProject/GlueProjectSaveExtensions.Load.
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);

        // SetEditMode only sets ScreenManager.IsNextScreenInEditMode and restarts the current screen
        // (RestartScreenRerunCommands) - IsInEditMode itself only flips once that restart's ScreenLoaded
        // callback actually runs, which happens on a later frame, not synchronously within the DTO
        // handler that already returned above.
        await Task.Delay(1000);

        var selectResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
            NamedObjectNames = { "TestPolygon" },
        });
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        var setPointsResponse = await game.Send(new SetPolygonPointsForTestingDto { Points = InitialPoints });
        setPointsResponse.Succeeded.ShouldBeTrue(setPointsResponse.Message);

        // The exact DTO/values production Glue sends on entering edit mode - see
        // MainCompilerPlugin.SendGlueViewSettingsToGame and CompilerSettingsModel.SetDefaults.
        var settingsResponse = await game.Send(new GlueViewSettingsDto
        {
            EnableSnapping = true,
            SnapSize = 8,
            PolygonPointSnapSize = 8,
        });
        settingsResponse.Succeeded.ShouldBeTrue(settingsResponse.Message);

        // Drag point 0 by 3 pixels - smaller than one 8-unit grid step. An unsnapped drag lands on a
        // fraction of a unit, which is the exact reported symptom ("point editing was smooth and moved
        // fractions of a unit"). A working snap rounds this back down to the starting grid line.
        var dragResponse = await game.Send<SimulatePolygonPointDragResponse>(new SimulatePolygonPointDragDto
        {
            PointIndex = 0,
            ScreenXChange = 3,
            ScreenYChange = 0,
        });

        dragResponse.Succeeded.ShouldBeTrue(dragResponse.Message);
        dragResponse.Data.X.ShouldBe(-8,
            "a 3-pixel drag with an 8-unit snap should round back to the starting grid line, not move by a fraction of a unit");

        // Same drag again, but with snapping disabled - proves this test can tell snapped from unsnapped
        // rather than the point always landing on -8 regardless of settings.
        var disableSnappingResponse = await game.Send(new GlueViewSettingsDto
        {
            EnableSnapping = false,
            SnapSize = 8,
            PolygonPointSnapSize = 8,
        });
        disableSnappingResponse.Succeeded.ShouldBeTrue(disableSnappingResponse.Message);

        var unsnappedDragResponse = await game.Send<SimulatePolygonPointDragResponse>(new SimulatePolygonPointDragDto
        {
            PointIndex = 0,
            ScreenXChange = 3,
            ScreenYChange = 0,
        });

        unsnappedDragResponse.Succeeded.ShouldBeTrue(unsnappedDragResponse.Message);
        unsnappedDragResponse.Data.X.ShouldBe(-5,
            "with snapping disabled, a 3-pixel drag should move by exactly 3 units instead of rounding to a grid line");
    }

    static async Task AddTestPolygonToGameScreen()
    {
        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var nos = new NamedObjectSave();
        nos.SetDefaults();
        nos.InstanceName = "TestPolygon";
        nos.SourceType = SourceType.FlatRedBallType;
        nos.SourceClassType = "FlatRedBall.Math.Geometry.Polygon";

        // AddNamedObjectToAsync (not AddNewNamedObjectToAsync - see CollisionAssetTypeRegistrationTests for
        // why) is the same production add-and-generate path Glue.exe uses; performSaveAndGenerateCode:true
        // makes "TestPolygon" a real field in the built GameScreen.Generated.cs.
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            nos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: true, updateUi: false);
    }
}
