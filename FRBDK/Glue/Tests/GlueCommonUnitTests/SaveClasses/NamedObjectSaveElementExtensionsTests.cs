using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetContainer/GetReferencedElement/GetContainerType/NamedObjectSaveToString/GetDefiningNamedObjectSave
// all read the shared static ObjectFinderCore.Self, so this can't run concurrently with any other test
// class that swaps it out - hence the shared collection (see ObjectFinderCoreCollection below).
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public NamedObjectSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    [Fact]
    public void GetContainer_GlueProjectNull_ReturnsNull()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave();

        Assert.Null(nos.GetContainer());
    }

    [Fact]
    public void GetContainer_GlueProjectSet_ReturnsContainerFromFinder()
    {
        _finder.GlueProject = new GlueProjectSave();
        var container = new EntitySave { Name = "Entities\\Player" };
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, container);

        Assert.Same(container, nos.GetContainer());
    }

    [Fact]
    public void GetReferencedElement_NullInstance_Throws()
    {
        NamedObjectSave instance = null;

        Assert.Throws<ArgumentNullException>(() => instance.GetReferencedElement());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetReferencedElement_NoSourceClassType_ReturnsNull(string sourceClassType)
    {
        var nos = new NamedObjectSave { SourceClassType = sourceClassType };

        Assert.Null(nos.GetReferencedElement());
    }

    [Fact]
    public void GetReferencedElement_KnownSourceClassType_ReturnsEntitySave()
    {
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        _finder.AddElement("Entities\\Enemy", entity);
        var nos = new NamedObjectSave { SourceClassType = "Entities\\Enemy" };

        Assert.Same(entity, nos.GetReferencedElement());
    }

    [Fact]
    public void GetContainerType_NoContainer_ReturnsNone()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave();

        Assert.Equal(ContainerType.None, nos.GetContainerType());
    }

    [Fact]
    public void GetContainerType_EntityContainer_ReturnsEntity()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, new EntitySave { Name = "Entities\\Player" });

        Assert.Equal(ContainerType.Entity, nos.GetContainerType());
    }

    [Fact]
    public void GetContainerType_ScreenContainer_ReturnsScreen()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, new ScreenSave { Name = "Screens\\GameScreen" });

        Assert.Equal(ContainerType.Screen, nos.GetContainerType());
    }

    [Fact]
    public void NamedObjectSaveToString_Uncontained_IncludesUncontainedSuffix()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite", InstanceName = "SpriteInstance" };

        var result = NamedObjectSaveElementExtensions.NamedObjectSaveToString(nos);

        Assert.Equal("Sprite SpriteInstance (Uncontained)", result);
    }

    [Fact]
    public void NamedObjectSaveToString_Contained_IncludesContainerName()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite", InstanceName = "SpriteInstance" };
        var container = new EntitySave { Name = "Entities\\Player" };
        _finder.SetContainer(nos, container);

        var result = NamedObjectSaveElementExtensions.NamedObjectSaveToString(nos);

        Assert.Equal($"Sprite SpriteInstance in {container}", result);
    }

    [Fact]
    public void GetDefiningNamedObjectSave_NotDefinedByBase_ReturnsInstance()
    {
        var nos = new NamedObjectSave { DefinedByBase = false };
        var container = new EntitySave { Name = "Entities\\Player" };

        Assert.Same(nos, nos.GetDefiningNamedObjectSave(container));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBaseWithNoBaseElement_Throws()
    {
        var nos = new NamedObjectSave { DefinedByBase = true, InstanceName = "SpriteInstance" };
        var container = new EntitySave { Name = "Entities\\Derived", BaseEntity = "" };

        Assert.Throws<Exception>(() => nos.GetDefiningNamedObjectSave(container));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBase_FindsMatchInBaseElement()
    {
        var baseNos = new NamedObjectSave { InstanceName = "SpriteInstance", SetByDerived = true };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(baseNos);
        _finder.AddElement("Entities\\Base", baseEntity);

        var derivedNos = new NamedObjectSave { InstanceName = "SpriteInstance", DefinedByBase = true };
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.Same(baseNos, derivedNos.GetDefiningNamedObjectSave(derivedEntity));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBase_NoMatchAnywhere_ReturnsNull()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        _finder.AddElement("Entities\\Base", baseEntity);

        var derivedNos = new NamedObjectSave { InstanceName = "SpriteInstance", DefinedByBase = true };
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.Null(derivedNos.GetDefiningNamedObjectSave(derivedEntity));
    }
}

[CollectionDefinition(nameof(ObjectFinderCoreCollection), DisableParallelization = true)]
public class ObjectFinderCoreCollection
{
}
