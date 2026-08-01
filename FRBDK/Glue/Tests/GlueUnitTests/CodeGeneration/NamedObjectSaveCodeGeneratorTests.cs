using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Regression coverage for #1770: a derived (variant) entity's LayeredTileMap object, exposed from its
// base with InstantiatedByBase = true, could have its SourceFile changed in the property grid, but the
// override was silently ignored - the derived class never re-ran its instantiation code, so only the
// base's own file was ever loaded. See ShouldReinstantiateDespiteInstantiatedByBase's doc comment in
// NamedObjectSaveCodeGenerator.cs for the fix.
// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NamedObjectSaveCodeGeneratorTests
{
    public NamedObjectSaveCodeGeneratorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    private static (EntitySave baseEntity, EntitySave derivedEntity) CreateBaseAndDerivedEntities()
    {
        var glueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject = glueProject;

        var baseEntity = new EntitySave { Name = "Entities\\BaseEntity" };
        var derivedEntity = new EntitySave { Name = "Entities\\DerivedEntity", BaseEntity = baseEntity.Name };

        glueProject.Entities.Add(baseEntity);
        glueProject.Entities.Add(derivedEntity);

        return (baseEntity, derivedEntity);
    }

    private static NamedObjectSave CreateFileSourcedNamedObject(string sourceFile, bool instantiatedByBase)
    {
        return new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.File,
            SourceFile = sourceFile,
            InstantiatedByBase = instantiatedByBase
        };
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnTrue_WhenDerivedSourceFileDiffersFromBase()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        baseEntity.NamedObjects.Add(CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: false));
        var derivedNos = CreateFileSourcedNamedObject("Content/Derived.tmx", instantiatedByBase: true);
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenDerivedSourceFileMatchesBase()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        baseEntity.NamedObjects.Add(CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: false));
        var derivedNos = CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: true);
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenSourceTypeIsNotFile()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        var baseNos = new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.FlatRedBallType,
            InstantiatedByBase = false
        };
        baseEntity.NamedObjects.Add(baseNos);

        var derivedNos = new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.FlatRedBallType,
            InstantiatedByBase = true
        };
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenThereIsNoBaseElement()
    {
        var glueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject = glueProject;

        var entity = new EntitySave { Name = "Entities\\StandaloneEntity" };
        glueProject.Entities.Add(entity);

        var nos = CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: true);
        entity.NamedObjects.Add(nos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(nos, entity)
            .ShouldBeFalse();
    }
}
