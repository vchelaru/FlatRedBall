using System;
using System.Reflection;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.EntityInputMovementPlugin;

/// <summary>
/// Pins runtime behavior of the platformer entity codegen's ladder floor transitions (issue #2148):
/// reaching the top of a ladder and standing on the floor above it, and reaching the bottom of a
/// ladder and standing on the floor below it. Builds the real Samples/Platformer/LadderDemo gold
/// project once, plus a bare content-free smoke-test entity added to it (see
/// <see cref="PlatformerLadderGoldProject"/>), and reflects into the generated entity - see the
/// glue-project-codegen skill's "Runtime-testing generated code" section for why compiling alone does
/// not prove this.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class LadderFloorTransitionTests
{
    static async Task<(object Entity, Type EntityType)> CreateSmokeTestEntityAsync()
    {
        var loaded = await PlatformerLadderGoldProject.LoadOnceAsync();
        var entityType = loaded.GameAssembly.GetType(
            "LadderDemo.Entities." + PlatformerLadderGoldProject.SmokeTestEntityName);
        entityType.ShouldNotBeNull();

        // (contentManagerName, addToManagers: false): the parameterless ctor reads
        // ScreenManager.CurrentScreen, which is null with no screen running.
        var entity = Activator.CreateInstance(entityType!, "GlueUnitTests", false);
        entity.ShouldNotBeNull();

        return (entity!, entityType!);
    }

    static object CreateClimbingValues(Type entityType)
    {
        var platformerValuesType = entityType.Assembly.GetType("LadderDemo.DataTypes.PlatformerValues");
        platformerValuesType.ShouldNotBeNull();
        var values = Activator.CreateInstance(platformerValuesType!);
        // The CSV-generated custom class emits public fields, not properties.
        platformerValuesType!.GetField("CanClimb")!.SetValue(values, true);
        platformerValuesType.GetField("MaxClimbingSpeed")!.SetValue(values, 100f);
        return values!;
    }

    static void SetGroundMovementThenMovementType(object entity, Type entityType, object values, string movementTypeName)
    {
        // Order matters: PlatformerInit wires AfterGroundMovementSet -> UpdateCurrentMovement, and
        // UpdateCurrentMovement reads GroundMovement - so GroundMovement must already be assigned
        // before CurrentMovementType is set, or CurrentMovement resolves against null.
        entityType.GetProperty("GroundMovement")!.SetValue(entity, values);

        var movementTypeType = entityType.Assembly.GetType("LadderDemo.Entities.MovementType");
        movementTypeType.ShouldNotBeNull();
        entityType.GetProperty("CurrentMovementType")!.SetValue(entity, Enum.Parse(movementTypeType!, movementTypeName));
    }

    static MethodInfo GetApplyClimbingInput(Type entityType)
    {
        var method = entityType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();
        return method!;
    }

    static bool GetBoolField(object entity, Type entityType, string name) =>
        (bool)entityType.GetField(name)!.GetValue(entity)!;

    [StaFact]
    public async Task ApplyClimbingInput_ClampedAtTopAndNotHoldingUp_ExitsClimbingAtTopOfLadder()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetGroundMovementThenMovementType(entity, entityType, climbing, "Ground");

        entityType.GetProperty("TopOfLadderY")!.SetValue(entity, (float?)100f);
        // Above the clamp - simulates having just climbed to (or past) the top of the ladder.
        entityType.GetProperty("Y")!.SetValue(entity, 105f);
        entityType.GetProperty("YVelocity")!.SetValue(entity, 50f);

        var applyClimbingInput = GetApplyClimbingInput(entityType);

        // VerticalInput is null here (never wired up) - ApplyClimbingInput reads it as
        // "VerticalInput?.Value ?? 0", i.e. not holding Up, which is the exit condition.
        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        // The existing clamp behavior (pre-dates this fix): Y pinned to TopOfLadderY, upward velocity zeroed.
        ((float)entityType.GetProperty("Y")!.GetValue(entity)!).ShouldBe(100f);
        ((float)entityType.GetProperty("YVelocity")!.GetValue(entity)!).ShouldBe(0f);

        // The fix: OnLadderTopReached fired - closing #2148's "reaching the top of the ladder and
        // standing on the upper floor" gap. Before this fix nothing called this hook, so a real
        // project's override (e.g. swapping GroundMovement to a non-climbing PlatformerValues, as
        // LadderDemo's Player.cs now does) never ran, leaving the entity clamped at the top with
        // gravity suppressed (CurrentMovement.CanClimb still true) until stepping sideways off the ladder.
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeTrue();
        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeFalse();
    }

    [StaFact]
    public async Task ApplyClimbingInput_GroundedAndNotHoldingUp_ExitsClimbingAtBottomOfLadder()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetGroundMovementThenMovementType(entity, entityType, climbing, "Ground");

        // No TopOfLadderY set (null) - this is the bottom-of-ladder scenario, not a top clamp.
        entityType.GetProperty("Y")!.SetValue(entity, 0f);

        var isOnGroundField = entityType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance);
        isOnGroundField.ShouldNotBeNull();
        isOnGroundField!.SetValue(entity, true);

        var applyClimbingInput = GetApplyClimbingInput(entityType);

        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        // OnLadderBottomReached fired: same fix, bottom-of-ladder case. This tightens the previous
        // sample behavior (which required an active down-press) to "grounded and not holding up",
        // matching FRB2's PlatformerBehavior.landedWhileDescending.
        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeTrue();
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeFalse();
    }

    [StaFact]
    public async Task ApplyClimbingInput_NotClampedAndNotGrounded_StaysClimbing()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetGroundMovementThenMovementType(entity, entityType, climbing, "Ground");

        entityType.GetProperty("TopOfLadderY")!.SetValue(entity, (float?)100f);
        entityType.GetProperty("Y")!.SetValue(entity, 50f); // below the clamp

        var isOnGroundField = entityType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance);
        isOnGroundField!.SetValue(entity, false);

        var applyClimbingInput = GetApplyClimbingInput(entityType);
        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        // Not clamped (Y < TopOfLadderY) and not grounded - neither exit hook should have fired.
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeFalse();
        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeFalse();
        entityType.GetProperty("GroundMovement")!.GetValue(entity).ShouldBe(climbing);
    }
}
