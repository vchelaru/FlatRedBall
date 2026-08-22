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
/// Pins LadderDemo's real Player.cs ladder-top behavior (issue #2148 follow-up, reported after manual
/// play-testing the fix in <see cref="LadderTopOfLadderYTests"/>): reaching the top of the ladder must
/// leave the player suspended in its climbing movement values, not swap to Ground movement immediately.
///
/// Player.cs's OnLadderTopReached used to swap GroundMovement to PlatformerValuesStatic["Ground"] the
/// instant the top clamp fired. UpdateCurrentMovement (generated) sets YAcceleration = -Gravity as soon
/// as CurrentMovement.CanClimb is false, regardless of whether any solid collision is actually under
/// the player - and Level1Map's ladder shafts have no floor tile in the ladder's own column (the floor
/// is only beside it), so that swap made the player fall straight through with nothing to catch them.
///
/// FRB2's reference ladder behavior - confirmed by manual testing - keeps the player suspended at the
/// top regardless of what is underneath, until they deliberately step off the ladder's horizontal
/// footprint. Player.cs's CustomActivity already has that exit (isOverLadder == false &&
/// CurrentMovement.CanClimb -> CurrentMovementType = Air), so the fix is for OnLadderTopReached to stop
/// swapping GroundMovement away from the climbing values, not to add new collision handling.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class PlayerLadderTopBehaviorTests
{
    [StaFact]
    public async Task ApplyClimbingInput_ClampedAtTop_DoesNotSwapAwayFromClimbingMovement()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var assembly = loaded.GameAssembly;

        var playerType = assembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();

        var platformerValuesType = assembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        platformerValuesType.ShouldNotBeNull();

        object CreateValues(bool canClimb)
        {
            var values = Activator.CreateInstance(platformerValuesType!);
            // The CSV-generated custom class emits public fields, not properties.
            platformerValuesType!.GetField("CanClimb")!.SetValue(values, canClimb);
            platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
            // A non-zero Gravity on the "Ground" values is what would have started pulling the player
            // down the instant OnLadderTopReached's old swap took effect.
            platformerValuesType.GetField("Gravity")!.SetValue(values, 500f);
            return values!;
        }

        var climbingValues = CreateValues(canClimb: true);
        var groundValues = CreateValues(canClimb: false);

        // Player.OnLadderTopReached (hand-written project code) reads PlatformerValuesStatic["Ground"]
        // directly - populate just that static dictionary via reflection instead of going through
        // LoadStaticContent, which also loads animation content and needs a GraphicsDevice this
        // headless test process doesn't have.
        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Ground"] = groundValues;
        var staticValuesField = playerType!.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static);
        staticValuesField.ShouldNotBeNull();
        staticValuesField!.SetValue(null, staticValues);

        // Uninitialized (no constructor, no field initializers) - Player's real constructor wires up
        // animation content and needs a GraphicsDevice this headless test process doesn't have (see
        // PlatformerLadderGoldProject's SmokeTestEntityName doc). IsPlatformingEnabled must be set back
        // to true by hand below since its field initializer never runs.
        var player = FormatterServices.GetUninitializedObject(playerType!);
        playerType.GetField("IsPlatformingEnabled")!.SetValue(player, true);

        // Order matters: PlatformerInit wires AfterGroundMovementSet -> UpdateCurrentMovement, but that
        // wiring never runs on an uninitialized object either - CurrentMovementType's own setter calls
        // UpdateCurrentMovement() directly, so GroundMovement must already be assigned before
        // CurrentMovementType is set here, or CurrentMovement resolves against null.
        playerType.GetProperty("GroundMovement")!.SetValue(player, climbingValues);
        var movementTypeType = assembly.GetType("LadderDemo.Entities.MovementType");
        movementTypeType.ShouldNotBeNull();
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Ground"));

        playerType.GetProperty("TopOfLadderY")!.SetValue(player, (float?)100f);
        // Above the clamp - simulates having just climbed to (or past) the top of the ladder.
        playerType.GetProperty("Y")!.SetValue(player, 105f);
        playerType.GetProperty("YVelocity")!.SetValue(player, 50f);

        var applyClimbingInput = playerType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        applyClimbingInput.ShouldNotBeNull();

        // VerticalInput is null here (never wired up) - ApplyClimbingInput reads it as
        // "VerticalInput?.Value ?? 0", i.e. not holding Up, which is the exit condition.
        Should.NotThrow(() => applyClimbingInput!.Invoke(player, null));

        // The clamp itself (pre-existing behavior): feet pinned to the top of the ladder.
        ((float)playerType.GetProperty("Y")!.GetValue(player)!).ShouldBe(100f);

        // The fix: reaching the top must NOT swap away from the climbing movement values - the player
        // stays suspended there (no gravity) instead of falling through whatever happens to be (or not
        // be) directly underneath the ladder's own column.
        playerType.GetProperty("GroundMovement")!.GetValue(player).ShouldBe(climbingValues);
    }

    /// <summary>
    /// The first test above only proved OnLadderTopReached itself doesn't swap GroundMovement - it never
    /// exercised the rest of the per-frame pipeline that runs afterward every frame the player just sits
    /// at the top: Player.Activity() calls PlatformerActivity() (ApplyInput() then
    /// DetermineMovementValues()) then CustomActivity(). DetermineMovementValues has its own
    /// !CurrentMovement.CanClimb check that could independently flip CurrentMovementType to Air, and
    /// CustomActivity's isOverLadder check is the only other thing allowed to do that - this test runs
    /// several simulated frames of that real sequence (skipping only ApplyHorizontalInput/ApplyJumpInput,
    /// which need JumpInput/HorizontalInput wired up and are neutral with no keys pressed either way) with
    /// mIsOnGround forced false throughout, matching Level1Map's real ladder shafts having no solid tile
    /// under them, and proves the player stays put instead of falling on any of those frames.
    /// </summary>
    [StaFact]
    public async Task RepeatedFramesAtTopWithNoSolidGroundBelow_PlayerStaysSuspendedAcrossFrames()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var assembly = loaded.GameAssembly;

        // ShapeManager (used by AddCollisionAtWorld below) enforces same-thread-as-init - see
        // LadderTopOfLadderYTests for why this reflection re-point is needed on every [StaFact].
        loaded.EngineAssembly.GetType("FlatRedBall.FlatRedBallServices")!
            .GetField("mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, (int?)Environment.CurrentManagedThreadId);

        var playerType = assembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();

        var platformerValuesType = assembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        platformerValuesType.ShouldNotBeNull();

        object CreateValues(bool canClimb)
        {
            var values = Activator.CreateInstance(platformerValuesType!);
            platformerValuesType!.GetField("CanClimb")!.SetValue(values, canClimb);
            platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
            platformerValuesType.GetField("Gravity")!.SetValue(values, 500f);
            return values!;
        }

        var climbingValues = CreateValues(canClimb: true);
        var groundValues = CreateValues(canClimb: false);

        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Ground"] = groundValues;
        playerType!.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, staticValues);

        var player = FormatterServices.GetUninitializedObject(playerType!);
        playerType.GetField("IsPlatformingEnabled")!.SetValue(player, true);
        playerType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, false);

        // CustomActivity() unconditionally calls animationController.Activity() and reads
        // InputDevice.DefaultUpPressable - both null on an uninitialized object. A real (but
        // layer-less) AnimationController.Activity() is a no-op, and InputManager.Keyboard is a real,
        // already-initialized IInputDevice with nothing pressed - both let CustomActivity run for real
        // without needing full construction or a GraphicsDevice.
        var animationControllerType = loaded.EngineAssembly.GetType("FlatRedBall.Graphics.Animation.AnimationController");
        animationControllerType.ShouldNotBeNull();
        playerType.GetField("animationController", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, Activator.CreateInstance(animationControllerType!));

        var inputManagerType = loaded.EngineAssembly.GetType("FlatRedBall.Input.InputManager");
        inputManagerType.ShouldNotBeNull();
        var keyboard = inputManagerType!.GetProperty("Keyboard", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        keyboard.ShouldNotBeNull();
        playerType.GetProperty("InputDevice")!.GetSetMethod(nonPublic: true)!.Invoke(player, new[] { keyboard });

        playerType.GetProperty("GroundMovement")!.SetValue(player, climbingValues);
        var movementTypeType = assembly.GetType("LadderDemo.Entities.MovementType");
        movementTypeType.ShouldNotBeNull();
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Ground"));

        // A real ladder rectangle (not a hand-built stand-in) so isOverLadder's Left/Right check in
        // CustomActivity is exercised against real engine geometry, same as LadderTopOfLadderYTests.
        var tileShapeCollectionType = assembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        tileShapeCollectionType.ShouldNotBeNull();
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        var ladderCollision = Activator.CreateInstance(tileShapeCollectionType!);
        const float gridSize = 16f;
        tileShapeCollectionType!.GetProperty("GridSize")!.SetValue(ladderCollision, gridSize);
        tileShapeCollectionType.GetProperty("SortAxis")!.SetValue(ladderCollision, Enum.Parse(axisType!, "Y"));
        var addCollisionAtWorld = tileShapeCollectionType.GetMethod("AddCollisionAtWorld", new[] { typeof(float), typeof(float) });
        addCollisionAtWorld!.Invoke(ladderCollision, new object[] { 100f, 232f });
        var topRectangle = tileShapeCollectionType
            .GetMethod("GetRectangleAtPosition", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { 100f, 232f });
        topRectangle.ShouldNotBeNull();
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, topRectangle);

        playerType.GetProperty("TopOfLadderY")!.SetValue(player, (float?)240f);
        playerType.GetProperty("X")!.SetValue(player, 100f); // matches the ladder rectangle's own X
        playerType.GetProperty("Y")!.SetValue(player, 245f); // above the clamp, simulating having just climbed up
        playerType.GetProperty("YVelocity")!.SetValue(player, 50f);

        var applyClimbingInput = playerType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        var determineMovementValues = playerType.GetMethod("DetermineMovementValues", BindingFlags.NonPublic | BindingFlags.Instance);
        var customActivity = playerType.GetMethod("CustomActivity", BindingFlags.NonPublic | BindingFlags.Instance);
        applyClimbingInput.ShouldNotBeNull();
        determineMovementValues.ShouldNotBeNull();
        customActivity.ShouldNotBeNull();

        for (int frame = 0; frame < 3; frame++)
        {
            Should.NotThrow(() =>
            {
                applyClimbingInput!.Invoke(player, null);
                determineMovementValues!.Invoke(player, null);
                customActivity!.Invoke(player, null);
            }, $"frame {frame}");

            ((float)playerType.GetProperty("Y")!.GetValue(player)!).ShouldBe(240f, $"frame {frame}: player fell from the top of the ladder");
            ((float)playerType.GetProperty("YAcceleration")!.GetValue(player)!).ShouldBe(0f, $"frame {frame}: gravity engaged with no solid ground below");
            playerType.GetProperty("GroundMovement")!.GetValue(player).ShouldBe(climbingValues, $"frame {frame}");
            playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Ground"), $"frame {frame}");
        }
    }

    /// <summary>
    /// The actual remaining bug (found from a real play-test, via file-logged diagnostics added to
    /// Player.cs): GameScreen.cs's DoCollisionActivity nulls LastCollisionLadderRectange every frame
    /// BEFORE re-running PlayerVsLadderCollision.DoCollisions(), on the theory that a fresh collision
    /// re-sets it if the player is still touching the ladder. That's true while climbing through the
    /// shaft, but false the instant the player is clamped flush at TopOfLadderY: standing ON TOP of the
    /// topmost ladder tile no longer vertically OVERLAPS it, so the collision doesn't re-fire and
    /// LastCollisionLadderRectange stays null - even though the player hasn't moved sideways at all.
    /// CustomActivity's isOverLadder reads that null as "stepped off the ladder" and forces
    /// CurrentMovementType = Air, which is real (non-climbing) AirMovement - gravity engages and the
    /// player falls. This reproduces exactly that one-frame sequence.
    /// </summary>
    [StaFact]
    public async Task LastCollisionLadderRectangeGoesNullAfterTopClamp_DoesNotDropPlayerIntoAir()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var assembly = loaded.GameAssembly;

        // ShapeManager (used by AddCollisionAtWorld below) enforces same-thread-as-init - see
        // LadderTopOfLadderYTests for why this reflection re-point is needed on every [StaFact].
        loaded.EngineAssembly.GetType("FlatRedBall.FlatRedBallServices")!
            .GetField("mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, (int?)Environment.CurrentManagedThreadId);

        var playerType = assembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();

        var platformerValuesType = assembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        platformerValuesType.ShouldNotBeNull();

        object CreateValues(bool canClimb)
        {
            var values = Activator.CreateInstance(platformerValuesType!);
            platformerValuesType!.GetField("CanClimb")!.SetValue(values, canClimb);
            platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
            platformerValuesType.GetField("Gravity")!.SetValue(values, 500f);
            return values!;
        }

        var climbingValues = CreateValues(canClimb: true);
        var groundValues = CreateValues(canClimb: false);

        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Ground"] = groundValues;
        playerType!.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, staticValues);

        var player = FormatterServices.GetUninitializedObject(playerType!);
        playerType.GetField("IsPlatformingEnabled")!.SetValue(player, true);
        playerType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, false);

        var animationControllerType = loaded.EngineAssembly.GetType("FlatRedBall.Graphics.Animation.AnimationController");
        playerType.GetField("animationController", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, Activator.CreateInstance(animationControllerType!));

        var inputManagerType = loaded.EngineAssembly.GetType("FlatRedBall.Input.InputManager");
        var keyboard = inputManagerType!.GetProperty("Keyboard", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        playerType.GetProperty("InputDevice")!.GetSetMethod(nonPublic: true)!.Invoke(player, new[] { keyboard });

        playerType.GetProperty("GroundMovement")!.SetValue(player, climbingValues);
        var movementTypeType = assembly.GetType("LadderDemo.Entities.MovementType");
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Ground"));

        var tileShapeCollectionType = assembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        var ladderCollision = Activator.CreateInstance(tileShapeCollectionType!);
        tileShapeCollectionType!.GetProperty("GridSize")!.SetValue(ladderCollision, 16f);
        tileShapeCollectionType.GetProperty("SortAxis")!.SetValue(ladderCollision, Enum.Parse(axisType!, "Y"));
        tileShapeCollectionType.GetMethod("AddCollisionAtWorld", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { 100f, 232f });
        var topRectangle = tileShapeCollectionType
            .GetMethod("GetRectangleAtPosition", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { 100f, 232f });

        playerType.GetProperty("TopOfLadderY")!.SetValue(player, (float?)240f);
        playerType.GetProperty("X")!.SetValue(player, 100f); // matches the ladder rectangle's own X - never moves
        playerType.GetProperty("Y")!.SetValue(player, 240f); // already clamped at the top

        var customActivity = playerType.GetMethod("CustomActivity", BindingFlags.NonPublic | BindingFlags.Instance);
        customActivity.ShouldNotBeNull();

        // Frame A: still vertically overlapping the ladder (LastCollisionLadderRectange set for real,
        // as OnPlayerVsLadderCollisionCollided would have just done) - baseline sanity check.
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, topRectangle);
        Should.NotThrow(() => customActivity!.Invoke(player, null));
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Ground"));

        // Frame B: mirrors GameScreen.cs's DoCollisionActivity - LastCollisionLadderRectange is nulled
        // before re-detecting, and re-detection finds nothing because standing flush at the top no
        // longer vertically overlaps the ladder tile. X has not changed at all.
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, null);
        Should.NotThrow(() => customActivity!.Invoke(player, null));

        // The bug: this used to force CurrentMovementType to Air purely from losing vertical overlap,
        // not from the player stepping sideways off the ladder.
        playerType.GetProperty("CurrentMovementType")!.GetValue(player)
            .ShouldBe(Enum.Parse(movementTypeType!, "Ground"), "isOverLadder going stale-null from losing vertical (not horizontal) contact must not drop the player into Air");
        playerType.GetProperty("GroundMovement")!.GetValue(player).ShouldBe(climbingValues);
    }

    /// <summary>
    /// Reported after playing at the ladder's top: pressing a horizontal direction with solid ground
    /// immediately beside the ladder (Level1Map's actual layout) lets the player glide off sideways
    /// forever, still visually in the climbing pose, instead of landing normally.
    ///
    /// Root cause: CustomActivity's "reset GroundMovement away from Climbing" block only runs when
    /// CurrentMovement.CanClimb is ALREADY false at the top of the method - but isOverLadder's own exit
    /// (further down the same method) is what flips CurrentMovementType to Air in the first place, one
    /// frame later than the reset check runs. If DetermineMovementValues (which runs earlier in the
    /// frame, via PlatformerActivity) flips CurrentMovementType back to Ground - because mIsOnGround
    /// just became true from touching the solid floor beside the ladder - before this method's own
    /// CanClimb check gets a chance to see CanClimb=false, GroundMovement is left as Climbing
    /// permanently: CurrentMovementType is Ground (correctly grounded) but resolves to the Climbing
    /// PlatformerValues (CanClimb=true, MaxSpeedX=50), so gravity never re-engages (YAcceleration stays
    /// 0) and horizontal input keeps sliding the player sideways without end.
    /// </summary>
    [StaFact]
    public async Task SteppingOffLadderOntoSolidGround_ResetsGroundMovementAwayFromClimbing()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var assembly = loaded.GameAssembly;

        loaded.EngineAssembly.GetType("FlatRedBall.FlatRedBallServices")!
            .GetField("mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, (int?)Environment.CurrentManagedThreadId);

        var playerType = assembly.GetType("LadderDemo.Entities.Player");
        playerType.ShouldNotBeNull();

        var platformerValuesType = assembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        platformerValuesType.ShouldNotBeNull();

        object CreateValues(bool canClimb)
        {
            var values = Activator.CreateInstance(platformerValuesType!);
            platformerValuesType!.GetField("CanClimb")!.SetValue(values, canClimb);
            platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
            platformerValuesType.GetField("Gravity")!.SetValue(values, 500f);
            // Matches the real CSV's Climbing.MaxSpeedX=50 - nonzero on purpose (question 1: horizontal
            // drift while climbing is intentional data, not a bug), which is exactly what makes this
            // bug visible: the player keeps sliding once stuck with Climbing as GroundMovement.
            platformerValuesType.GetField("MaxSpeedX")!.SetValue(values, canClimb ? 50f : 100f);
            return values!;
        }

        var climbingValues = CreateValues(canClimb: true);
        var groundValues = CreateValues(canClimb: false);

        var dictType = typeof(System.Collections.Generic.Dictionary<,>)
            .MakeGenericType(typeof(string), platformerValuesType!);
        var staticValues = (IDictionary)Activator.CreateInstance(dictType)!;
        staticValues["Ground"] = groundValues;
        // The fix reads PlatformerValuesStatic["Air"] too (resetting AirMovement alongside
        // GroundMovement) - reuse the same non-climbing preset, its identity doesn't matter here.
        staticValues["Air"] = groundValues;
        playerType!.GetField("PlatformerValuesStatic", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, staticValues);

        var player = FormatterServices.GetUninitializedObject(playerType!);
        playerType.GetField("IsPlatformingEnabled")!.SetValue(player, true);

        var animationControllerType = loaded.EngineAssembly.GetType("FlatRedBall.Graphics.Animation.AnimationController");
        playerType.GetField("animationController", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, Activator.CreateInstance(animationControllerType!));

        var inputManagerType = loaded.EngineAssembly.GetType("FlatRedBall.Input.InputManager");
        var keyboard = inputManagerType!.GetProperty("Keyboard", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        playerType.GetProperty("InputDevice")!.GetSetMethod(nonPublic: true)!.Invoke(player, new[] { keyboard });

        playerType.GetProperty("GroundMovement")!.SetValue(player, climbingValues);
        // AirMovement must be a real (non-climbing) value too - CustomActivity's isOverLadder exit sets
        // CurrentMovementType = Air, which resolves CurrentMovement to AirMovement, not GroundMovement.
        playerType.GetProperty("AirMovement")!.SetValue(player, groundValues);
        var movementTypeType = assembly.GetType("LadderDemo.Entities.MovementType");
        playerType.GetProperty("CurrentMovementType")!.SetValue(player, Enum.Parse(movementTypeType!, "Ground"));

        var tileShapeCollectionType = assembly.GetType("FlatRedBall.TileCollisions.TileShapeCollection");
        var axisType = loaded.EngineAssembly.GetType("FlatRedBall.Math.Axis");
        var ladderCollision = Activator.CreateInstance(tileShapeCollectionType!);
        tileShapeCollectionType!.GetProperty("GridSize")!.SetValue(ladderCollision, 16f);
        tileShapeCollectionType.GetProperty("SortAxis")!.SetValue(ladderCollision, Enum.Parse(axisType!, "Y"));
        tileShapeCollectionType.GetMethod("AddCollisionAtWorld", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { 100f, 232f });
        var topRectangle = tileShapeCollectionType
            .GetMethod("GetRectangleAtPosition", new[] { typeof(float), typeof(float) })!
            .Invoke(ladderCollision, new object[] { 100f, 232f });

        var determineMovementValues = playerType.GetMethod("DetermineMovementValues", BindingFlags.NonPublic | BindingFlags.Instance);
        var customActivity = playerType.GetMethod("CustomActivity", BindingFlags.NonPublic | BindingFlags.Instance);
        var mIsOnGroundField = playerType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Frame A: standing at the top of the ladder, still within its footprint.
        mIsOnGroundField.SetValue(player, false);
        playerType.GetProperty("X")!.SetValue(player, 100f);
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, topRectangle);
        Should.NotThrow(() =>
        {
            determineMovementValues!.Invoke(player, null);
            customActivity!.Invoke(player, null);
        });
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Ground"));

        // Frame B: the player has walked right, past the ladder's cached footprint. mIsOnGround is
        // still false here - the actual solid-ground touch registers on the FOLLOWING frame (Frame C),
        // matching real collision timing (CollideAgainst runs after Player.Activity - see GameScreen's
        // Activity order). This frame's CustomActivity is what should reset GroundMovement away from
        // Climbing, since it's the one call where isOverLadder actually goes false.
        playerType.GetProperty("X")!.SetValue(player, 150f);
        playerType.GetProperty("LastCollisionLadderRectange")!.SetValue(player, null);
        Should.NotThrow(() =>
        {
            determineMovementValues!.Invoke(player, null);
            customActivity!.Invoke(player, null); // isOverLadder=false -> CurrentMovementType=Air
        });
        playerType.GetProperty("CurrentMovementType")!.GetValue(player).ShouldBe(Enum.Parse(movementTypeType!, "Air"));

        // Frame C: the player is now touching the solid ground beside the ladder. DetermineMovementValues
        // (which runs BEFORE CustomActivity each frame, via PlatformerActivity) sees mIsOnGround=true and
        // CurrentMovementType=Air, and flips CurrentMovementType back to Ground immediately - resolving
        // CurrentMovement to GroundMovement. THE BUG: if Frame B's CustomActivity never reset
        // GroundMovement away from the Climbing preset, this resolves right back to it, so the player is
        // grounded but still "climbing" - CanClimb stays true, gravity never re-engages, and horizontal
        // input keeps sliding them forever. Checked immediately after DetermineMovementValues, before
        // CustomActivity runs again, since that's the exact moment the stale value is visible.
        mIsOnGroundField.SetValue(player, true);
        Should.NotThrow(() => determineMovementValues!.Invoke(player, null));

        var currentMovement = playerType.GetProperty("CurrentMovement", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player);
        var canClimb = (bool)platformerValuesType!.GetField("CanClimb")!.GetValue(currentMovement)!;
        canClimb.ShouldBeFalse("player is stuck in the climbing pose (GroundMovement never reset) after stepping off the ladder onto solid ground");
    }
}
