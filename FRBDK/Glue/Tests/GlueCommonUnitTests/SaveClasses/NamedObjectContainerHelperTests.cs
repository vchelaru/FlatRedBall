using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Controls;

namespace GlueCommonUnitTests.SaveClasses;

// GetNamedObjectsToBeExposedInDerived/GetNamedObjectsToBeSetByDerived read the shared static
// ObjectFinderCore.Self and ErrorReportingCore.Self, so this can't run concurrently with any other
// test class that swaps either out - hence the shared collection (see ObjectFinderCoreCollection in
// NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectContainerHelperTests
{
    readonly FakeObjectFinderCore _finder = new();
    readonly FakeErrorReportingCore _errors = new();

    public NamedObjectContainerHelperTests()
    {
        ObjectFinderCore.Self = _finder;
        ErrorReportingCore.Self = _errors;
    }

    static NamedObjectSave Nos(string name, bool exposedInDerived = false, bool setByDerived = false, bool definedByBase = false) =>
        new() { InstanceName = name, ExposedInDerived = exposedInDerived, SetByDerived = setByDerived, DefinedByBase = definedByBase };

    #region GetNamedObjectsToBeExposedInDerived

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_NoBase_ReturnsOwnExposedObjectsOnly()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.NamedObjects.Add(Nos("Sprite", exposedInDerived: true));
        entity.NamedObjects.Add(Nos("Collision"));

        var result = entity.GetNamedObjectsToBeExposedInDerived();

        Assert.Equal(new[] { "Sprite" }, result.Select(item => item.InstanceName));
        Assert.Empty(_errors.Messages);
    }

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_EntityBase_IncludesBaseObjectsWithoutDuplicates()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(Nos("Sprite", exposedInDerived: true));
        baseEntity.NamedObjects.Add(Nos("Light", exposedInDerived: true));
        _finder.AddElement(baseEntity.Name, baseEntity);

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = baseEntity.Name };
        derived.NamedObjects.Add(Nos("Sprite", exposedInDerived: true));

        var result = derived.GetNamedObjectsToBeExposedInDerived();

        Assert.Equal(new[] { "Sprite", "Light" }, result.Select(item => item.InstanceName));
        Assert.Same(baseEntity.NamedObjects[0], result[0]);
    }

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_DefinedByBase_RemovesInheritedEntry()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(Nos("Sprite", exposedInDerived: true));
        _finder.AddElement(baseEntity.Name, baseEntity);

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = baseEntity.Name };
        derived.NamedObjects.Add(Nos("Sprite", definedByBase: true));

        Assert.Empty(derived.GetNamedObjectsToBeExposedInDerived());
    }

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_ScreenBase_WalksBaseScreen()
    {
        var baseScreen = new ScreenSave { Name = "Screens\\BaseScreen" };
        baseScreen.NamedObjects.Add(Nos("Hud", exposedInDerived: true));
        _finder.AddElement(baseScreen.Name, baseScreen);

        var derived = new ScreenSave { Name = "Screens\\Level1", BaseScreen = baseScreen.Name };

        Assert.Equal(new[] { "Hud" }, derived.GetNamedObjectsToBeExposedInDerived().Select(item => item.InstanceName));
    }

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_MissingBaseEntity_ShowsMessage()
    {
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Gone" };
        derived.NamedObjects.Add(Nos("Sprite", exposedInDerived: true));

        var result = derived.GetNamedObjectsToBeExposedInDerived();

        Assert.Equal(new[] { "Sprite" }, result.Select(item => item.InstanceName));
        var message = Assert.Single(_errors.Messages);
        Assert.Contains("Entities\\Gone", message);
        Assert.Contains("can't be found", message);
    }

    [Fact]
    public void GetNamedObjectsToBeExposedInDerived_FrbTypeBase_DoesNotShowMessage()
    {
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "FlatRedBall.Sprite" };

        Assert.Empty(derived.GetNamedObjectsToBeExposedInDerived());
        Assert.Empty(_errors.Messages);
    }

    #endregion

    #region GetNamedObjectsToBeSetByDerived

    [Fact]
    public void GetNamedObjectsToBeSetByDerived_NoBase_ReturnsOwnSetByDerivedObjectsOnly()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.NamedObjects.Add(Nos("Sprite", setByDerived: true));
        entity.NamedObjects.Add(Nos("Collision"));

        Assert.Equal(new[] { "Sprite" }, entity.GetNamedObjectsToBeSetByDerived().Select(item => item.InstanceName));
        Assert.Empty(_errors.Messages);
    }

    [Fact]
    public void GetNamedObjectsToBeSetByDerived_EntityBase_IncludesBaseObjectsWithoutDuplicates()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(Nos("Sprite", setByDerived: true));
        baseEntity.NamedObjects.Add(Nos("Light", setByDerived: true));
        _finder.AddElement(baseEntity.Name, baseEntity);

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = baseEntity.Name };
        derived.NamedObjects.Add(Nos("Sprite", setByDerived: true));

        Assert.Equal(new[] { "Sprite", "Light" }, derived.GetNamedObjectsToBeSetByDerived().Select(item => item.InstanceName));
    }

    [Fact]
    public void GetNamedObjectsToBeSetByDerived_DefinedByBase_RemovesInheritedEntry()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(Nos("Sprite", setByDerived: true));
        _finder.AddElement(baseEntity.Name, baseEntity);

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = baseEntity.Name };
        derived.NamedObjects.Add(Nos("Sprite", definedByBase: true));

        Assert.Empty(derived.GetNamedObjectsToBeSetByDerived());
    }

    [Fact]
    public void GetNamedObjectsToBeSetByDerived_MissingBaseEntity_ShowsMessage()
    {
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Gone" };

        Assert.Empty(derived.GetNamedObjectsToBeSetByDerived());
        var message = Assert.Single(_errors.Messages);
        Assert.Contains("Entities\\Gone", message);
    }

    [Fact]
    public void GetNamedObjectsToBeSetByDerived_FrbTypeBase_DoesNotShowMessage()
    {
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "FlatRedBall.Sprite" };

        Assert.Empty(derived.GetNamedObjectsToBeSetByDerived());
        Assert.Empty(_errors.Messages);
    }

    #endregion

    #region GetNamedObjectThatIsContainerFor

    [Fact]
    public void GetNamedObjectThatIsContainerFor_NestedObject_ReturnsDirectParent()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var list = Nos("List");
        var inner = Nos("Inner");
        var leaf = Nos("Leaf");
        inner.ContainedObjects.Add(leaf);
        list.ContainedObjects.Add(inner);
        entity.NamedObjects.Add(Nos("Unrelated"));
        entity.NamedObjects.Add(list);

        Assert.Same(inner, NamedObjectContainerHelper.GetNamedObjectThatIsContainerFor(entity, leaf));
        Assert.Same(list, NamedObjectContainerHelper.GetNamedObjectThatIsContainerFor(entity, inner));
    }

    [Fact]
    public void GetNamedObjectThatIsContainerFor_TopLevelObject_ReturnsNull()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var topLevel = Nos("TopLevel");
        entity.NamedObjects.Add(topLevel);

        Assert.Null(NamedObjectContainerHelper.GetNamedObjectThatIsContainerFor(entity, topLevel));
    }

    #endregion
}
