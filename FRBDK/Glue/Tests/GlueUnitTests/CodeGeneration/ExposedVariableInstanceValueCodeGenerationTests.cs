using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Pins that an instance's own variable values are always generated, even when an exposed variable tunnels to the
// same member. Skipping them (#2399) dropped the Sprite's AnimationChains write, so CurrentChainName ran against an
// empty chain list and every entity exposing AnimationChains crashed at startup with "There is no animation named ...".
// A disagreement between the instance value and the exposed default is a sync problem, not something to fix by
// changing which assignments get generated.
// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ExposedVariableInstanceValueCodeGenerationTests
{
    public ExposedVariableInstanceValueCodeGenerationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    private static (EntitySave entity, NamedObjectSave sprite) CreateEntityWithSprite(string exposedMember, object exposedDefault)
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

        if (exposedMember != null)
        {
            entity.CustomVariables.Add(new CustomVariable
            {
                Name = "SpriteInstance" + exposedMember,
                Type = "float",
                SourceObject = "SpriteInstance",
                SourceObjectProperty = exposedMember,
                DefaultValue = exposedDefault,
            });
        }

        return (entity, sprite);
    }

    private static string GenerateInstanceAssignments(EntitySave entity, NamedObjectSave sprite)
    {
        ICodeBlock code = new CodeDocument();
        NamedObjectSaveCodeGenerator.GenerateVariableAssignment(sprite, code, entity);
        return code.ToString();
    }

    [Fact]
    public void InstanceValue_IsAssigned_WhenExposedVariableWithDefaultTargetsTheSameMember()
    {
        var (entity, sprite) = CreateEntityWithSprite("Red", exposedDefault: 0.5f);
        sprite.SetVariableValue("Red", 0.2f);

        GenerateInstanceAssignments(entity, sprite).ShouldContain("SpriteInstance.Red = 0.2f;");
    }

    [Fact]
    public void InstanceValue_IsAssigned_WhenAnExposedVariableHasNoDefault()
    {
        var (entity, sprite) = CreateEntityWithSprite("Red", exposedDefault: null);
        sprite.SetVariableValue("Red", 0.2f);

        GenerateInstanceAssignments(entity, sprite).ShouldContain("SpriteInstance.Red = 0.2f;");
    }

    [Fact]
    public void InstanceValue_IsAssigned_WhenABaseEntityExposesTheMemberWithADefault()
    {
        var (baseEntity, _) = CreateEntityWithSprite("Red", exposedDefault: 0.5f);

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

        GenerateInstanceAssignments(derived, derivedSprite).ShouldContain("SpriteInstance.Red = 0.9f;");
    }

    [Fact]
    public void AnimationChains_IsAssignedBeforeCurrentChainName_WhenAnExposedVariableTargetsAnimationChains()
    {
        // Sprite.CurrentChainName looks the chain up in AnimationChains, so AnimationChains must already be set.
        var (entity, sprite) = CreateEntityWithSprite("AnimationChains", exposedDefault: "AnimationChainListFile");
        sprite.SetVariableValue("AnimationChains", "AnimationChainListFile");
        sprite.SetVariableValue("CurrentChainName", "Container");

        var generated = GenerateInstanceAssignments(entity, sprite);

        var chainsIndex = generated.IndexOf("SpriteInstance.AnimationChains =");
        var currentChainIndex = generated.IndexOf("SpriteInstance.CurrentChainName =");
        chainsIndex.ShouldBeGreaterThan(-1, generated);
        currentChainIndex.ShouldBeGreaterThan(-1, generated);
        chainsIndex.ShouldBeLessThan(currentChainIndex, generated);
    }
}
