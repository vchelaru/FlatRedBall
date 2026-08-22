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
/// Pins LadderDemo's real Player.cs integration with the generated ladder-climbing mechanism (issue
/// #2148 follow-up). Two real bugs were found here via manual play-testing before the architecture
/// below existed: (1) an early hand-written OnLadderTopReached override swapped GroundMovement to a
/// non-climbing preset the instant the top clamp fired, which enabled gravity with nothing solid
/// underneath (Level1Map's ladder shafts have no floor tile in the ladder's own column); (2) after
/// fixing that, a hand-rolled isOverLadder check in CustomActivity raced against
/// DetermineMovementValues's grounded-check, and the loser left GroundMovement holding stale climbing
/// values while genuinely grounded - the player floated sideways forever in the climbing pose.
///
/// Both bugs were possible only because GroundMovement itself got mutated to hold climbing values.
/// The current architecture (matching FRB2's engine-owned PlatformerBehavior) makes that class of bug
/// structurally impossible: MovementType.Climbing is CurrentMovementType's own case, selecting a
/// dedicated ClimbingMovement slot that ladder logic never touches GroundMovement/AirMovement to
/// fake. All ladder grab/clamp/exit logic (isOverLadder tracking, top/bottom transitions) lives in
/// generated ApplyClimbingInput now - see LadderFloorTransitionTests for the codegen-level coverage of
/// that. These tests cover Player.cs's own integration points: assigning ClimbingMovement, and
/// CustomActivity's ducking/running/ground reselection correctly staying out of the way while climbing.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class PlayerLadderTopBehaviorTests
{
    static async Task<(object Player, Type PlayerType, PlatformerLadderGoldProject.Loaded Loaded)> SetUpAsync()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();

        // ShapeManager (used by AddCollisionAtWorld) enforces same-thread-as-init - see
        // LadderTopOfLadderYTests for why this reflection re-point is needed on every [StaFact].
        loaded.EngineAssembly.GetType("FlatRedBall.FlatRedBallServices")!
            .GetField("mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, (int?)Environment.CurrentManagedThreadId);

        var playerType = loaded.GameAssembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();

        // Uninitialized (no constructor, no field initializers) - Player's real constructor wires up
        // animation content and needs a GraphicsDevice this headless test process doesn't have (see
        // PlatformerLadderGoldProject's SmokeTestEntityName doc). IsPlatformingEnabled must be set
        // back to true by hand since its field initializer never runs.
        var player = FormatterServices.GetUninitializedObject(playerType!);
        playerType!.GetField("IsPlatformingEnabled")!.SetValue(player, true);

        // CustomActivity() unconditionally calls animationController.Activity() and reads
        // InputDevice.DefaultUpPressable - both null on an uninitialized object. A real (but
        // layer-less) AnimationController.Activity() is a no-op, and InputManager.Keyboard is a real,
        // already-initialized IInputDevice with nothing pressed - both let CustomActivity/
        // ApplyClimbingInput run for real without needing full construction or a GraphicsDevice.
        var animationControllerType = loaded.EngineAssembly.GetType("FlatRedBall.Graphics.Animation.AnimationController");
        playerType.GetField("animationController", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, Activator.CreateInstance(animationControllerType!));

        var inputManagerType = loaded.EngineAssembly.GetType("FlatRedBall.Input.InputManager");
        var keyboard = inputManagerType!.GetProperty("Keyboard", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        playerType.GetProperty("InputDevice")!.GetSetMethod(nonPublic: true)!.Invoke(player, new[] { keyboard });

        // CustomActivity's non-climbing branch reads VerticalInput.Value and RunInput.IsDown - both
        // null on an uninitialized object (InitializePlatformerInput/CustomInitializePlatformerInput
        // never ran). IInputDevice's own Default* inputs are real, already-initialized, "nothing
        // pressed" implementations - reuse them instead of hand-rolling stubs.
        var inputDeviceType = loaded.EngineAssembly.GetType("FlatRedBall.Input.IInputDevice");
        playerType.GetProperty("VerticalInput")!
            .SetValue(player, inputDeviceType!.GetProperty("DefaultVerticalInput")!.GetValue(keyboard));
        playerType.GetProperty("RunInput")!
            .SetValue(player, inputDeviceType.GetProperty("DefaultPrimaryActionInput")!.GetValue(keyboard));

        return (player, playerType, loaded);
    }

    static object CreatePlatformerValues(Type platformerValuesType, bool canClimb, float maxSpeedX = 100f)
    {
        var values = Activator.CreateInstance(platformerValuesType);
        platformerValuesType.GetField("CanClimb")!.SetValue(values, canClimb);
        platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
        platformerValuesType.GetField("Gravity")!.SetValue(values, 500f);
        platformerValuesType.GetField("MaxSpeedX")!.SetValue(values, maxSpeedX);
        return values!;
    }

    static object CreateLadderRectangleAt(Type gameAssemblyTileShapeCollectionType, Type axisType, float x, float y)
    {
        var ladderCollision = Activator.CreateInstance(gameAssemblyTileShapeCollectionType);
        gameAssemblyTileShapeCollectionType.GetProperty("GridSize")!.SetValue(ladderCollision, 16f);
        gameAssemblyTileShapeCollectionType.GetProperty("SortAxis")!.SetValue(ladderCollision, Enum.Parse(axisType, "Y"));
        gameAssemblyTileShapeCollectionType.GetMethod("AddCollisionAtWorld", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { x, y });
        return gameAssemblyTileShapeCollectionType
            .GetMethod("GetRectangleAtPosition", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { x, y })!;
    }

    // Matches EntityCodeGenerator.cs's ClimbingTopOverlapInset.
    const float ClimbingTopOverlapInset = 0.5f;

    [StaFact]
    public async Task CustomInitialize_AssignsClimbingMovementFromStaticValues()
    {
        var (player, playerType, loaded) = await SetUpAsync();
        var platformerValuesType = loaded.GameAssembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        var climbingValues = CreatePlatformerValues(platformerValuesType!, canClimb: true);

        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Climbing"] = climbingValues;
        playerType.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, staticValues);

        var customInitialize = playerType.GetMethod("CustomInitialize", BindingFlags.NonPublic | BindingFlags.Instance);
        customInitialize.ShouldNotBeNull();
        Should.NotThrow(() => customInitialize!.Invoke(player, null));

        playerType.GetProperty("ClimbingMovement")!.GetValue(player).ShouldBe(climbingValues);
    }

    /// <summary>
    /// Simulates several frames at the top of a ladder with no solid ground underneath (Level1Map's
    /// real geometry), proving the entity stays suspended - no gravity, no drift - across repeated
    /// frames rather than just the single frame the clamp first fires.
    /// </summary>
    [StaFact]
    public async Task RepeatedFramesAtTopWithNoSolidGroundBelow_PlayerStaysSuspendedAcrossFrames()
    {
        var (player, playerType, loaded) = await SetUpAsync();
        var platformerValuesType = loaded.GameAssembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        var climbingValues = CreatePlatformerValues(platformerValuesType!, canClimb: true);

        playerType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, false);
        playerType.GetProperty("ClimbingMovement")!.SetValue(player, climbingValues);
        var movementTypeType = loaded.GameAssembly.GetType("LadderDemo.Entities.MovementType");
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Climbing"));

        var tileShapeCollectionType = loaded.GameAssembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        var topRectangle = CreateLadderRectangleAt(tileShapeCollectionType!, axisType!, 100f, 232f);
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, topRectangle);

        playerType.GetProperty("TopOfLadderY")!.SetValue(player, (float?)240f);
        playerType.GetProperty("X")!.SetValue(player, 100f); // matches the ladder rectangle's own X
        playerType.GetProperty("Y")!.SetValue(player, 245f); // above the clamp, simulating having just climbed up
        playerType.GetProperty("YVelocity")!.SetValue(player, 50f);

        var applyClimbingInput = playerType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        var determineMovementValues = playerType.GetMethod("DetermineMovementValues", BindingFlags.NonPublic | BindingFlags.Instance);
        var customActivity = playerType.GetMethod("CustomActivity", BindingFlags.NonPublic | BindingFlags.Instance);

        for (int frame = 0; frame < 3; frame++)
        {
            Should.NotThrow(() =>
            {
                applyClimbingInput!.Invoke(player, null);
                determineMovementValues!.Invoke(player, null);
                customActivity!.Invoke(player, null);
            }, $"frame {frame}");

            ((float)playerType.GetProperty("Y")!.GetValue(player)!)
                .ShouldBe(240f - ClimbingTopOverlapInset, $"frame {frame}: player fell from the top of the ladder");
            ((float)playerType.GetProperty("YAcceleration")!.GetValue(player)!)
                .ShouldBe(0f, $"frame {frame}: gravity engaged with no solid ground below");
            playerType.GetProperty("CurrentMovementType")!.GetValue(player)
                .ShouldBe(Enum.Parse(movementTypeType!, "Climbing"), $"frame {frame}");
        }
    }

    /// <summary>
    /// Regression test for the two real bugs described in the class doc comment, using the real
    /// Player entity end to end: grab the ladder, step off sideways onto solid ground, land. Proves
    /// CurrentMovement never resolves back to the climbing preset once genuinely grounded - which is
    /// now true by construction (ClimbingMovement is a separate slot ladder logic alone can never
    /// leak into GroundMovement/AirMovement), but is worth pinning directly against the real entity
    /// rather than only the generic codegen coverage in LadderFloorTransitionTests.
    /// </summary>
    [StaFact]
    public async Task SteppingOffLadderOntoSolidGround_NeverResolvesBackToClimbingMovement()
    {
        var (player, playerType, loaded) = await SetUpAsync();
        var platformerValuesType = loaded.GameAssembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        // Climbing.MaxSpeedX=50 in the real CSV is nonzero on purpose (horizontal drift while
        // climbing is intentional data), which is exactly what made the original bug visible: the
        // player kept sliding once stuck with Climbing as the active slot while grounded.
        var climbingValues = CreatePlatformerValues(platformerValuesType!, canClimb: true, maxSpeedX: 50f);
        var groundValues = CreatePlatformerValues(platformerValuesType!, canClimb: false);

        // CustomActivity's non-climbing branch (still real, hand-written project code) reselects
        // GroundMovement/AirMovement from PlatformerValuesStatic every frame it isn't climbing.
        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Ground"] = groundValues;
        staticValues["Air"] = groundValues;
        playerType.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, staticValues);

        playerType.GetProperty("ClimbingMovement")!.SetValue(player, climbingValues);
        playerType.GetProperty("GroundMovement")!.SetValue(player, groundValues);
        playerType.GetProperty("AirMovement")!.SetValue(player, groundValues);
        var movementTypeType = loaded.GameAssembly.GetType("LadderDemo.Entities.MovementType");
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Climbing"));

        var mIsOnGroundField = playerType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance)!;
        mIsOnGroundField.SetValue(player, false);

        var tileShapeCollectionType = loaded.GameAssembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        var topRectangle = CreateLadderRectangleAt(tileShapeCollectionType!, axisType!, 100f, 232f);

        var applyClimbingInput = playerType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        var determineMovementValues = playerType.GetMethod("DetermineMovementValues", BindingFlags.NonPublic | BindingFlags.Instance);
        var customActivity = playerType.GetMethod("CustomActivity", BindingFlags.NonPublic | BindingFlags.Instance);

        // Frame A: standing at the top of the ladder, still within its footprint.
        playerType.GetProperty("X")!.SetValue(player, 100f);
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, topRectangle);
        Should.NotThrow(() =>
        {
            applyClimbingInput!.Invoke(player, null);
            determineMovementValues!.Invoke(player, null);
            customActivity!.Invoke(player, null);
        });
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Climbing"));

        // Frame B: the player has walked right, past the ladder's cached footprint, onto solid
        // ground beside it - not yet registering as grounded (collision runs after Player.Activity,
        // so the touch is detected on the FOLLOWING frame, matching real collision timing).
        playerType.GetProperty("X")!.SetValue(player, 150f);
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, null);
        Should.NotThrow(() =>
        {
            applyClimbingInput!.Invoke(player, null); // isOverLadder=false -> CurrentMovementType=Air
            determineMovementValues!.Invoke(player, null);
            customActivity!.Invoke(player, null);
        });
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Air"));

        // Frame C: now touching the solid ground beside the ladder.
        mIsOnGroundField.SetValue(player, true);
        Should.NotThrow(() =>
        {
            applyClimbingInput!.Invoke(player, null);
            determineMovementValues!.Invoke(player, null); // mIsOnGround=true -> CurrentMovementType=Ground
            customActivity!.Invoke(player, null);
        });
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Ground"));

        var currentMovement = playerType.GetProperty("CurrentMovement", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player);
        currentMovement.ShouldBe(groundValues, "grounded player must resolve to GroundMovement, never the ClimbingMovement slot");
        var canClimb = (bool)platformerValuesType!.GetField("CanClimb")!.GetValue(currentMovement)!;
        canClimb.ShouldBeFalse();
    }
}
