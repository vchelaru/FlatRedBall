using System;
using System.IO;
using System.Linq;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.AnimationChainPlugin.Errors;
using Shouldly;

namespace GlueUnitTests.AnimationChainPlugin;

// The error scan at load checked every object's animation against its .achx by reading that file again
// for each object, so a file a hundred sprites share was parsed a hundred times (#2422).
public class AnimationChainErrorReporterTests : IDisposable
{
    readonly GlueProjectSave originalGlueProject;
    readonly Func<FilePath, AnimationChainListSave> originalRead;
    readonly string achxPath = Path.Combine(Path.GetTempPath(), nameof(AnimationChainErrorReporterTests) + "_" + Guid.NewGuid().ToString("N") + ".achx");
    int reads;

    public AnimationChainErrorReporterTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        originalRead = AnimationReferenceErrorViewModel.ReadAnimationFile;
        File.WriteAllText(achxPath, "");
        AnimationReferenceErrorViewModel.ReadAnimationFile = _ =>
        {
            reads++;
            var list = new AnimationChainListSave();
            list.AnimationChains.Add(new AnimationChainSave { Name = "Walk" });
            list.AnimationChains.Add(new AnimationChainSave { Name = "Run" });
            return list;
        };
    }

    public void Dispose()
    {
        AnimationReferenceErrorViewModel.ReadAnimationFile = originalRead;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        try { File.Delete(achxPath); } catch (Exception) { }
    }

    static void AddSpritePlaying(EntitySave entity, string name, string animation)
    {
        var sprite = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };
        sprite.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "AnimationChains", Value = "Anims", Type = "string" });
        sprite.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "CurrentChainName", Value = animation, Type = "string" });
        entity.NamedObjects.Add(sprite);
    }

    [Fact]
    public void GetAllErrors_ShouldReadAnAnimationFileOnce_WhenManyObjectsUseIt()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Entities/Player/Anims.achx", FilePath = new FilePath(achxPath) });
        AddSpritePlaying(entity, "Walker", "Walk");
        AddSpritePlaying(entity, "Runner", "Run");
        AddSpritePlaying(entity, "Jumper", "Jump");
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        var errors = new AnimationChainErrorReporter().GetAllErrors();

        reads.ShouldBe(1);
        errors.Cast<AnimationReferenceErrorViewModel>().ShouldHaveSingleItem().AnimationName.ShouldBe("Jump");
    }
}
