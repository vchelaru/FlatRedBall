using System;
using System.Reflection;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.EntityInputMovementPlugin;

/// <summary>
/// Pins runtime behavior of the platformer entity codegen's ladder floor transitions (issue #2148):
/// reaching the top of a ladder and standing on the upper floor, and reaching the bottom of a
/// ladder and transitioning back to normal ground movement. Builds the real Samples/Platformer/LadderDemo
/// gold project once, plus a bare content-free smoke-test entity added to it (see
/// <see cref="PlatformerLadderGoldProject"/>), and reflects into the generated entity - see the
/// glue-project-codegen skill's "Runtime-testing generated code" section for why compiling alone does
/// not prove this.
///
/// Ladder grab/clamp/exit is entirely generated (ApplyClimbingInput) since the #2148 follow-up that
/// moved it out of hand-rolled per-project CustomActivity code, matching FRB2's engine-owned
/// PlatformerBehavior architecture: CurrentMovementType.Climbing selects a dedicated ClimbingMovement
/// slot that GroundMovement/AirMovement are never mutated to hold, so there is no stale-climbing-values
/// state to leak once the entity genuinely leaves the ladder.
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

    /// <summary>
    /// Builds a real AxisAlignedRectangle centered on centerX (Width 16, matching the real ladder's
    /// GridSize) and assigns it to LastCollisionLadderRectange - the generated ApplyClimbingInput
    /// caches its Left/Right into ladderColumnLeft/Right from this, exactly as project collision glue
    /// (GameScreen.Event.cs) would after a real ladder collision.
    /// </summary>
    static void SetLadderRectangleAt(object entity, Type entityType, float centerX)
    {
        var rectProperty = entityType.GetProperty("LastCollisionLadderRectange")!;
        var rectType = rectProperty.PropertyType;
        var rect = Activator.CreateInstance(rectType);
        rectType.GetProperty("X")!.SetValue(rect, centerX);
        rectType.GetProperty("Width")!.SetValue(rect, 16f);
        rectProperty.SetValue(entity, rect);
    }

    /// <summary>
    /// Enters the climbing state the way generated ApplyClimbingInput's grab branch does: assign
    /// ClimbingMovement (never GroundMovement - that slot is never mutated by climbing logic) and set
    /// CurrentMovementType directly to Climbing.
    /// </summary>
    static void SetClimbingMovementThenMovementType(object entity, Type entityType, object values)
    {
        entityType.GetProperty("ClimbingMovement")!.SetValue(entity, values);

        var movementTypeType = entityType.Assembly.GetType("LadderDemo.Entities.MovementType");
        movementTypeType.ShouldNotBeNull();
        entityType.GetProperty("CurrentMovementType")!.SetValue(entity, Enum.Parse(movementTypeType!, "Climbing"));
    }

    static MethodInfo GetApplyClimbingInput(Type entityType)
    {
        var method = entityType.GetMethod("ApplyClimbingInput", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();
        return method!;
    }

    static bool GetBoolField(object entity, Type entityType, string name) =>
        (bool)entityType.GetField(name)!.GetValue(entity)!;

    static object CurrentMovementTypeValue(object entity, Type entityType) =>
        entityType.GetProperty("CurrentMovementType")!.GetValue(entity)!;

    static object MovementType(Type entityType, string name) =>
        Enum.Parse(entityType.Assembly.GetType("LadderDemo.Entities.MovementType")!, name);

    // Matches EntityCodeGenerator.cs's ClimbingTopOverlapInset: the clamp lands the entity slightly
    // inside the topmost ladder tile rather than flush with its edge.
    const float ClimbingTopOverlapInset = 0.5f;

    [StaFact]
    public async Task ApplyClimbingInput_ClampedAtTopAndNotHoldingUp_ExitsClimbingAtTopOfLadder()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetClimbingMovementThenMovementType(entity, entityType, climbing);

        entityType.GetProperty("X")!.SetValue(entity, 0f);
        SetLadderRectangleAt(entity, entityType, 0f); // establishes ladderColumnLeft/Right so isOverLadder stays true

        entityType.GetProperty("TopOfLadderY")!.SetValue(entity, (float?)100f);
        // Above the clamp - simulates having just climbed to (or past) the top of the ladder.
        entityType.GetProperty("Y")!.SetValue(entity, 105f);
        entityType.GetProperty("YVelocity")!.SetValue(entity, 50f);

        var applyClimbingInput = GetApplyClimbingInput(entityType);

        // VerticalInput is null here (never wired up) - ApplyClimbingInput reads it as
        // "VerticalInput?.Value ?? 0", i.e. not holding Up, which is the exit condition.
        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        // The clamp: feet pinned just inside the top of the ladder (inset, not flush - see
        // ClimbingTopOverlapInset), upward velocity zeroed.
        ((float)entityType.GetProperty("Y")!.GetValue(entity)!).ShouldBe(100f - ClimbingTopOverlapInset);
        ((float)entityType.GetProperty("YVelocity")!.GetValue(entity)!).ShouldBe(0f);

        // The notification hook fired...
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeTrue();
        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeFalse();

        // ...but the entity stays in the climbing state (still within the ladder's footprint,
        // X unchanged) - matches FRB2: reaching the top alone does not exit climbing.
        CurrentMovementTypeValue(entity, entityType).ShouldBe(MovementType(entityType, "Climbing"));
    }

    [StaFact]
    public async Task ApplyClimbingInput_GroundedAndNotHoldingUp_ExitsClimbingAtBottomOfLadder()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetClimbingMovementThenMovementType(entity, entityType, climbing);

        entityType.GetProperty("X")!.SetValue(entity, 0f);
        // Still within the ladder's footprint - isolates this test to the mIsOnGround exit path
        // rather than the (separately tested) isOverLadder exit path.
        SetLadderRectangleAt(entity, entityType, 0f);

        entityType.GetProperty("Y")!.SetValue(entity, 0f);

        var isOnGroundField = entityType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance);
        isOnGroundField.ShouldNotBeNull();
        isOnGroundField!.SetValue(entity, true);

        var applyClimbingInput = GetApplyClimbingInput(entityType);

        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeTrue();
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeFalse();

        // Reaching solid ground while climbing and not holding Up must exit the climbing state -
        // this used to only happen because the (now-removed) hand-rolled hook overwrote GroundMovement;
        // generated ApplyClimbingInput must do this itself now.
        CurrentMovementTypeValue(entity, entityType).ShouldNotBe(MovementType(entityType, "Climbing"));
    }

    [StaFact]
    public async Task ApplyClimbingInput_NotClampedAndNotGrounded_StaysClimbing()
    {
        var (entity, entityType) = await CreateSmokeTestEntityAsync();
        var climbing = CreateClimbingValues(entityType);
        SetClimbingMovementThenMovementType(entity, entityType, climbing);

        entityType.GetProperty("X")!.SetValue(entity, 0f);
        SetLadderRectangleAt(entity, entityType, 0f);

        entityType.GetProperty("TopOfLadderY")!.SetValue(entity, (float?)100f);
        entityType.GetProperty("Y")!.SetValue(entity, 50f); // below the clamp

        var isOnGroundField = entityType.GetField("mIsOnGround", BindingFlags.NonPublic | BindingFlags.Instance);
        isOnGroundField!.SetValue(entity, false);

        var applyClimbingInput = GetApplyClimbingInput(entityType);
        Should.NotThrow(() => applyClimbingInput.Invoke(entity, null));

        // Not clamped (Y < TopOfLadderY) and not grounded - neither exit hook should have fired.
        GetBoolField(entity, entityType, "ReachedTopOfLadder").ShouldBeFalse();
        GetBoolField(entity, entityType, "ReachedBottomOfLadder").ShouldBeFalse();
        CurrentMovementTypeValue(entity, entityType).ShouldBe(MovementType(entityType, "Climbing"));
        entityType.GetProperty("ClimbingMovement")!.GetValue(entity).ShouldBe(climbing);
    }
}
