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

// GitHub issue #2256: SyncShapesFromAnimation lets a Sprite sync named shapes from its current animation
// frame as plain children, without requiring the container to be ICollidable (unlike the older
// SetCollisionFromAnimation, which stays ICollidable-gated since it writes into Collision).
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

    [Fact]
    public void GenerateActivity_ShouldGenerateSyncShapesFromAnimation_OnNonICollidableEntity()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSyncShapesFromAnimationVariableDefinition().Name, true);

        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);

        codeBlock.ToString().ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, false);");
    }

    [Fact]
    public void GenerateActivity_ShouldNotGenerateSetCollisionFromAnimation_OnNonICollidableEntity()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);

        codeBlock.ToString().ShouldNotContain("SetCollisionFromAnimation");
    }

    [Fact]
    public void GenerateActivity_ShouldNotGenerateSyncShapesFromAnimation_WhenFileVersionIsBelowThreshold()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation - 1
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSyncShapesFromAnimationVariableDefinition().Name, true);

        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);

        codeBlock.ToString().ShouldNotContain("SyncShapesFromAnimation");
    }

    [Fact]
    public void GenerateActivity_ShouldPassCreateMissingSyncedShapesFlag_WhenTrue()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Marker", ImplementsICollidable = false };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSyncShapesFromAnimationVariableDefinition().Name, true);
        SetVariable(sprite, AssetTypeInfoManager.GetCreateMissingSyncedShapesDefinition().Name, true);

        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);

        codeBlock.ToString().ShouldContain("SpriteInstance.SyncShapesFromAnimation(this, true);");
    }

    [Fact]
    public void GenerateActivity_ShouldStillGenerateSetCollisionFromAnimation_OnICollidableEntity()
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation
        };

        var entity = new EntitySave { Name = "Entities\\Enemy", ImplementsICollidable = true };
        var sprite = AddSprite(entity, "SpriteInstance");
        SetVariable(sprite, AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name, true);

        ICodeBlock codeBlock = new CodeDocument();
        new SpriteCodeGenerator().GenerateActivity(codeBlock, entity);

        codeBlock.ToString().ShouldContain("SpriteInstance.SetCollisionFromAnimation(this, false);");
    }
}
