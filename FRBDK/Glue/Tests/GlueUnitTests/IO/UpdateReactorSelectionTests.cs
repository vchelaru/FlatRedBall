using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;

namespace GlueUnitTests.IO;

// Pins UpdateReactor.GetEquivalentSelection - how ReloadGlux carries the selection across an
// external edit, which replaces the selected element with a freshly-loaded copy.
public class UpdateReactorSelectionTests
{
    static EntitySave MakeEntity()
    {
        var entity = new EntitySave { Name = "Entities\\Bosses\\HasturPhase2" };
        var parent = new NamedObjectSave { InstanceName = "SpriteList" };
        parent.ContainedObjects.Add(new NamedObjectSave { InstanceName = "ChildSprite" });
        entity.NamedObjects.Add(parent);
        entity.CustomVariables.Add(new CustomVariable { Name = "Health" });
        entity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Entities/Bosses/HasturPhase2/Anim.achx" });
        entity.Events.Add(new EventResponseSave { EventName = "Died" });
        entity.States.Add(new StateSave { Name = "Uncategorized" });
        var category = new StateSaveCategory { Name = "Phase" };
        category.States.Add(new StateSave { Name = "Angry" });
        entity.StateCategoryList.Add(category);
        return entity;
    }

    [Fact]
    public void ShouldReturnNewElement_WhenElementWasSelected()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();

        UpdateReactor.GetEquivalentSelection(oldEntity, oldEntity, newEntity).ShouldBeSameAs(newEntity);
    }

    [Fact]
    public void ShouldReturnNewElement_WhenSelectedTagIsNull()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();

        // A folder node under the element (e.g. "Variables") has no tag.
        UpdateReactor.GetEquivalentSelection(null, oldEntity, newEntity).ShouldBeSameAs(newEntity);
    }

    [Fact]
    public void ShouldReturnMatchingNamedObject_IncludingContainedObjects()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();

        UpdateReactor.GetEquivalentSelection(oldEntity.NamedObjects[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.NamedObjects[0]);
        UpdateReactor.GetEquivalentSelection(oldEntity.NamedObjects[0].ContainedObjects[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.NamedObjects[0].ContainedObjects[0]);
    }

    [Fact]
    public void ShouldReturnMatchingVariableFileAndEvent()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();

        UpdateReactor.GetEquivalentSelection(oldEntity.CustomVariables[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.CustomVariables[0]);
        UpdateReactor.GetEquivalentSelection(oldEntity.ReferencedFiles[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.ReferencedFiles[0]);
        UpdateReactor.GetEquivalentSelection(oldEntity.Events[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.Events[0]);
    }

    [Fact]
    public void ShouldReturnMatchingStatesAndCategory()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();

        UpdateReactor.GetEquivalentSelection(oldEntity.States[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.States[0]);
        UpdateReactor.GetEquivalentSelection(oldEntity.StateCategoryList[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.StateCategoryList[0]);
        UpdateReactor.GetEquivalentSelection(oldEntity.StateCategoryList[0].States[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity.StateCategoryList[0].States[0]);
    }

    [Fact]
    public void ShouldFallBackToNewElement_WhenSelectedItemWasRemoved()
    {
        var oldEntity = MakeEntity();
        var newEntity = MakeEntity();
        newEntity.CustomVariables.Clear();

        UpdateReactor.GetEquivalentSelection(oldEntity.CustomVariables[0], oldEntity, newEntity)
            .ShouldBeSameAs(newEntity);
    }
}
