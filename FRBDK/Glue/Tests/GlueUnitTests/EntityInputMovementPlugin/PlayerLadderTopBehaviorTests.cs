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
}
