using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.EntityInputMovementPlugin;

/// <summary>
/// Pins that LadderDemo's real TopOfLadderY calculation (GameScreen.Event.cs's
/// OnPlayerVsLadderCollisionCollided - hand-written project code, untouched by #2148's codegen fix)
/// actually lands the player's feet at the top of the ladder column, not one tile short of it.
/// LadderFloorTransitionTests pins that the generated OnLadderTopReached hook fires at whatever height
/// TopOfLadderY holds; this test pins that the height itself is correct - TopOfLadderY is per-project
/// data, so a wrong height is a LadderDemo bug, not a codegen bug.
///
/// FlatRedBall.TileCollisions.TileShapeCollection is per-project GENERATED SOURCE (see
/// LadderDemo/TileCollisions/TileShapeCollection.Generated.cs) for this engine package version, not a
/// type shipped inside FlatRedBallDesktopGLNet6.dll - so it's compiled directly into LadderDemo.dll
/// (loaded.GameAssembly) and reflects from there like any other LadderDemo type, no engine-assembly
/// lookup involved.
///
/// Builds a synthetic 3-tile ladder column via the real TileShapeCollection.AddCollisionAtWorld API
/// (the same method TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom uses per tile) rather
/// than loading the real Level1Map.tmx through LayeredTileMap - that path needs a GraphicsDevice to
/// load tileset textures, which a headless test process doesn't have. The synthetic column is real
/// engine geometry, just not sourced from the shipped map file.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class LadderTopOfLadderYTests
{
    [StaFact]
    public async Task OnPlayerVsLadderCollisionCollided_ClimbingFromBottomTile_TopOfLadderYReachesTopOfColumn()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var assembly = loaded.GameAssembly;

        // ShapeManager (used by AddCollisionAtWorld below) enforces same-thread-as-init via
        // FlatRedBallServices.IsThreadPrimary(), backed by the private static mPrimaryThreadId. The
        // shared Lazy build in PlatformerLadderGoldProject calls InitializeCommandLine() exactly once,
        // on whichever test's [StaFact] thread wins the race to trigger it - re-running
        // InitializeCommandLine() itself isn't safe (InstructionManager.CreateInterpolators populates a
        // static dictionary and throws on a duplicate key the second time), so just re-point
        // mPrimaryThreadId at THIS test's own STA thread directly (GlueUnitTests runs its assembly
        // non-parallel, so reassigning it here is safe).
        var initFlatRedBallServicesType = loaded.EngineAssembly.GetType("FlatRedBall.FlatRedBallServices");
        var primaryThreadIdField = initFlatRedBallServicesType!.GetField(
            "mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static);
        primaryThreadIdField.ShouldNotBeNull();
        primaryThreadIdField!.SetValue(null, (int?)Environment.CurrentManagedThreadId);

        var tileShapeCollectionType = assembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        tileShapeCollectionType.ShouldNotBeNull();
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        axisType.ShouldNotBeNull();

        // A real TileShapeCollection, built the same way production code adds tiles one at a time
        // (TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom calls AddCollisionAtWorld per
        // tile) - just without going through a LayeredTileMap loaded from the .tmx, which needs a
        // GraphicsDevice this headless test process doesn't have.
        var ladderCollision = Activator.CreateInstance(tileShapeCollectionType!);
        const float gridSize = 16f; // matches Level1Map.tmx's tilewidth/tileheight
        tileShapeCollectionType!.GetProperty("GridSize")!.SetValue(ladderCollision, gridSize);
        tileShapeCollectionType.GetProperty("SortAxis")!.SetValue(ladderCollision, Enum.Parse(axisType!, "Y"));

        var addCollisionAtWorld = tileShapeCollectionType.GetMethod("AddCollisionAtWorld", new[] { typeof(float), typeof(float) });
        addCollisionAtWorld.ShouldNotBeNull();
        // Three stacked ladder tiles at X=100, centers at Y=200, 216, 232 - the topmost tile spans
        // Bottom=224..Top=240.
        addCollisionAtWorld!.Invoke(ladderCollision, new object[] { 100f, 200f });
        addCollisionAtWorld.Invoke(ladderCollision, new object[] { 100f, 216f });
        addCollisionAtWorld.Invoke(ladderCollision, new object[] { 100f, 232f });

        var getRectangleAtPosition = tileShapeCollectionType.GetMethod("GetRectangleAtPosition", new[] { typeof(float), typeof(float) });
        var bottomTileRect = getRectangleAtPosition!.Invoke(ladderCollision, new object[] { 100f, 200f });
        bottomTileRect.ShouldNotBeNull();

        // Seed LastCollisionAxisAlignedRectangles the way a real collision pass would - the property's
        // getter (TileShapeCollection.LastCollisionAxisAlignedRectangles => mShapes.
        // LastCollisionAxisAlignedRectangles) returns the live backing list, so this is a real write to
        // real collision state, not a reflection workaround.
        var lastCollisionList = tileShapeCollectionType.GetProperty("LastCollisionAxisAlignedRectangles")!
            .GetValue(ladderCollision);
        ((IList)lastCollisionList!).Add(bottomTileRect);

        // GameScreen.OnPlayerVsLadderCollisionCollided is a pure function of its (player, ladder)
        // parameters - it never reads `this` - so an uninitialized GameScreen/Player (no constructor,
        // no content loading) is a faithful way to invoke it without the animation-content-loading
        // problem PlatformerLadderGoldProject's own comment describes for the real Player entity.
        var gameScreenType = assembly.GetType("LadderDemo.Screens.GameScreen");
        gameScreenType.ShouldNotBeNull();
        var onPlayerVsLadderCollisionCollided = gameScreenType!.GetMethod(
            "OnPlayerVsLadderCollisionCollided", BindingFlags.NonPublic | BindingFlags.Instance);
        onPlayerVsLadderCollisionCollided.ShouldNotBeNull();

        var level1Type = assembly.GetType("LadderDemo.Screens.Level1");
        level1Type.ShouldNotBeNull();
        var gameScreenInstance = FormatterServices.GetUninitializedObject(level1Type!);

        var playerType = assembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();
        var playerInstance = FormatterServices.GetUninitializedObject(playerType!);

        onPlayerVsLadderCollisionCollided!.Invoke(gameScreenInstance, new[] { playerInstance, ladderCollision });

        var topOfLadderY = (float?)playerType!.GetProperty("TopOfLadderY")!.GetValue(playerInstance);
        topOfLadderY.HasValue.ShouldBeTrue();

        // The real assertion: TopOfLadderY must place the player's feet at the TOP of the topmost
        // ladder tile (Y=240 - where the ladder physically ends) so stepping off lands on solid ground
        // at or above that height. The current GameScreen.Event.cs uses topRectangle.Bottom (224) - a
        // full GridSize below the ladder's own top edge - which is this test's Red state.
        topOfLadderY!.Value.ShouldBe(240f, "TopOfLadderY should reach the TOP of the topmost ladder tile, not its bottom edge");
    }
}
