using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.SpritePlugin.CodeGenerators;
using OfficialPlugins.SpritePlugin.Managers;
using Shouldly;
using System;

namespace GlueUnitTests.SpritePlugin;

// GitHub issue #2256: the existing SetCollisionFromAnimation checkbox is repurposed (not duplicated) so
// it also works on non-ICollidable entities. Below SpriteHasSyncShapesFromAnimation it still requires
// ICollidable and calls the older Sprite.SetCollisionFromAnimation (writes directly into Collision), so
// existing projects that haven't upgraded their FileVersion see no change. At or above that version, the
// same checkbox instead calls Sprite.SyncShapesFromAnimation, which works on any entity and only adds a
// newly-created shape to Collision when the entity is ICollidable.
[Collection(nameof(TaskManagerSequentialCollection))]
public class SpriteCodeGeneratorTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public SpriteCodeGeneratorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    static NamedObjectSave AddSprite(EntitySave container, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };
        container.NamedObjects.Add(nos);
        return nos;
    }

    static void SetVariable(NamedObjectSave nos, string member, object value)
    {
        nos.InstructionSaves.Add(new CustomVariableInNamedObject
        {
            Member = member,
            Value = value
        });
    }

    static string GenerateActivityText(EntitySave entity)
    {
        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);
        return codeBlock.ToString();
    }

    static string GenerateActivityEditModeText(EntitySave entity)
    {
        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivityEditMode(codeBlock, entity);
        return codeBlock.ToString();
    }

    [Fact]
    public void GenerateActivity_BelowSyncShapesVersion_OnICollidableEntity_CallsSetCollisionFromAnimation()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation - 1
        };

        var entity = new EntitySave { Name = "Entities\\Enemy", ImplementsICollidable = true };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        GenerateActivityText(entity).ShouldContain("SpriteInstance.SetCollisionFromAnimation(this, false);");
    }

    [Fact]
    public void GenerateActivity_BelowSyncShapesVersion_OnNonICollidableEntity_GeneratesNothing()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation - 1
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        GenerateActivityText(entity).Trim().ShouldBeEmpty();
    }

    [Fact]
    public void GenerateActivity_AtSyncShapesVersion_OnNonICollidableEntity_CallsSyncShapesFromAnimation()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        GenerateActivityText(entity).ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, false);");
    }

    [Fact]
    public void GenerateActivity_AtSyncShapesVersion_OnICollidableEntity_CallsSyncShapesFromAnimation()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Enemy", ImplementsICollidable = true };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        // Same checkbox, same variable - at this version it uses the new method even on an ICollidable
        // entity, since SyncShapesFromAnimation already reproduces the old behavior there (see the engine
        // ShapeCollectionSave.SetValuesOn(PositionedObject, bool) tests).
        GenerateActivityText(entity).ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, false);");
    }

    [Fact]
    public void GenerateActivity_AtSyncShapesVersion_PassesCreateMissingShapesFlag_WhenTrue()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);
        SetVariable(sprite, AssetTypeInfoManager.GetCreateMissingShapesDefinition().Name, true);

        GenerateActivityText(entity).ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, true);");
    }

    [Fact]
    public void GenerateActivityEditMode_AtSyncShapesVersion_AlsoCallsSyncShapesFromAnimation()
    {
        // Regression coverage: ScreenManager.IsInEditMode skips normal Activity() entirely, so this call
        // must also be generated into ActivityEditMode() or shapes never track the animation while paused
        // in Glue's live-edit mode - see GitHub issue #2256 follow-up.
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        GenerateActivityEditModeText(entity).ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, false);");
    }

    [Fact]
    public void GenerateActivityEditMode_BelowSyncShapesVersion_OnICollidableEntity_AlsoCallsSetCollisionFromAnimation()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation - 1
        };

        var entity = new EntitySave { Name = "Entities\\Enemy", ImplementsICollidable = true };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        GenerateActivityEditModeText(entity).ShouldContain("SpriteInstance.SetCollisionFromAnimation(this, false);");
    }
}
