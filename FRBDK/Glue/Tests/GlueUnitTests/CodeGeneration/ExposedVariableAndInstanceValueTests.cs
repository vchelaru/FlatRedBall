using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Regression coverage for #1728. An Entity has a Sprite instance whose Red is set to one value, and an
// exposed variable that tunnels to that same Red with a different default. The instance's value is assigned
// while the Sprite is instantiated (AddToManagers) and the exposed variable's default is assigned later from
// AssignCustomVariables, so the exposed default always wins once AddToManagers finishes and the instance value
// is only visible before that. When an exposed variable owns the member, the instance value must not be written.
// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ExposedVariableAndInstanceValueTests
{
    public ExposedVariableAndInstanceValueTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    private static (EntitySave entity, NamedObjectSave sprite, CustomVariable exposed) CreateEntityWithExposedRed(
        float instanceValue, object exposedDefault, string exposedTargetsMember = "Red")
    {
        var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = glueProject;

        var entity = new EntitySave { Name = "Entities\\Player" };
        glueProject.Entities.Add(entity);

        var sprite = new NamedObjectSave
        {
            InstanceName = "SpriteInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite",
        };
        entity.NamedObjects.Add(sprite);
        sprite.SetVariableValue("Red", instanceValue);

        var exposed = new CustomVariable
        {
            Name = exposedTargetsMember,
            Type = "float",
            SourceObject = "SpriteInstance",
            SourceObjectProperty = exposedTargetsMember,
            DefaultValue = exposedDefault,
        };
        entity.CustomVariables.Add(exposed);

        return (entity, sprite, exposed);
    }

    private static string GenerateInstanceAssignments(EntitySave entity, NamedObjectSave sprite)
    {
        ICodeBlock code = new CodeDocument();
        NamedObjectSaveCodeGenerator.GenerateVariableAssignment(sprite, code, entity);
        return code.ToString();
    }

    [Fact]
    public void InstanceValue_IsNotAssigned_WhenExposedVariableWithDefaultTargetsTheSameMember()
    {
        var (entity, sprite, _) = CreateEntityWithExposedRed(instanceValue: 0.2f, exposedDefault: 0.5f);

        GenerateInstanceAssignments(entity, sprite).ShouldNotContain("SpriteInstance.Red");
    }

    [Fact]
    public void ExposedDefault_IsStillAssigned_WhenInstanceHasItsOwnValue()
    {
        var (entity, _, exposed) = CreateEntityWithExposedRed(instanceValue: 0.2f, exposedDefault: 0.5f);

        ICodeBlock code = new CodeDocument();
        CustomVariableCodeGenerator.AppendAssignmentForCustomVariableInElement(code, exposed, entity);

        code.ToString().ShouldContain("Red = 0.5f;");
    }

    [Fact]
    public void InstanceValue_IsAssigned_WhenExposedVariableHasNoDefault()
    {
        // A null default means "leave whatever the instance has", so there is no second writer.
        var (entity, sprite, _) = CreateEntityWithExposedRed(instanceValue: 0.2f, exposedDefault: null);

        GenerateInstanceAssignments(entity, sprite).ShouldContain("SpriteInstance.Red = 0.2f;");
    }

    [Fact]
    public void InstanceValue_IsNotAssigned_WhenABaseEntityExposesTheMemberWithADefault()
    {
        var (baseEntity, _, _) = CreateEntityWithExposedRed(instanceValue: 0.2f, exposedDefault: 0.5f);

        // The derived entity re-sets the instance's value, but the base's AssignCustomVariables still runs after it.
        var derived = new EntitySave { Name = "Entities\\DerivedPlayer", BaseEntity = baseEntity.Name };
        ObjectFinder.Self.GlueProject.Entities.Add(derived);
        var derivedSprite = new NamedObjectSave
        {
            InstanceName = "SpriteInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite",
        };
        derived.NamedObjects.Add(derivedSprite);
        derivedSprite.SetVariableValue("Red", 0.9f);

        GenerateInstanceAssignments(derived, derivedSprite).ShouldNotContain("SpriteInstance.Red");
    }

    [Fact]
    public void InstanceValue_IsAssigned_WhenExposedVariableTargetsADifferentMember()
    {
        var (entity, sprite, _) = CreateEntityWithExposedRed(instanceValue: 0.2f, exposedDefault: 0.5f,
            exposedTargetsMember: "Green");

        GenerateInstanceAssignments(entity, sprite).ShouldContain("SpriteInstance.Red = 0.2f;");
    }
}
