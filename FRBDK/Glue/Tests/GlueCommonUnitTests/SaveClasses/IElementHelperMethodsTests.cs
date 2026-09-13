using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// ContainsRecursively reads the shared static ObjectFinderCore.Self, so this can't run concurrently
// with any other test class that swaps it out - hence the shared collection.
[Collection(nameof(ObjectFinderCoreCollection))]
public class IElementHelperMethodsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public IElementHelperMethodsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    [Fact]
    public void ContainsRecursively_DirectlyReferenced_ReturnsTrue()
    {
        var rfs = new ReferencedFileSave();
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.ReferencedFiles.Add(rfs);

        Assert.True(entity.ContainsRecursively(rfs));
    }

    [Fact]
    public void ContainsRecursively_NotReferencedAndNoBaseElement_ReturnsFalse()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };

        Assert.False(entity.ContainsRecursively(new ReferencedFileSave()));
    }

    [Fact]
    public void ContainsRecursively_ReferencedByBaseElement_ReturnsTrue()
    {
        var rfs = new ReferencedFileSave();
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.ReferencedFiles.Add(rfs);
        _finder.AddElement("Entities\\Base", baseEntity);

        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.True(derivedEntity.ContainsRecursively(rfs));
    }

    [Fact]
    public void ContainsRecursively_BaseElementNameNotFound_ReturnsFalse()
    {
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\DoesNotExist" };

        Assert.False(derivedEntity.ContainsRecursively(new ReferencedFileSave()));
    }
}
