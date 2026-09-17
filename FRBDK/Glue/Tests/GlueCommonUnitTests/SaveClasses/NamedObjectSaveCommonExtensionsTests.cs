using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

public class NamedObjectSaveCommonExtensionsTests
{
    [Fact]
    public void UpdateCustomProperties_SortsInstructionSavesByMember()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Z" });
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "A" });
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "M" });

        nos.UpdateCustomProperties();

        Assert.Equal(new[] { "A", "M", "Z" }, nos.InstructionSaves.Select(i => i.Member));
    }

    [Fact]
    public void UpdateCustomProperties_NullMember_SortsToStart()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "A" });
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = null });

        nos.UpdateCustomProperties();

        Assert.Null(nos.InstructionSaves[0].Member);
        Assert.Equal("A", nos.InstructionSaves[1].Member);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_InstructionEnumValue_ConvertsToInt()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Alignment", Value = StringComparison.Ordinal });

        nos.ConvertEnumerationValuesToInts();

        Assert.IsType<int>(nos.InstructionSaves[0].Value);
        Assert.Equal((int)StringComparison.Ordinal, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_PropertyEnumValue_ConvertsToInt()
    {
        var nos = new NamedObjectSave();
        nos.Properties.SetValue("Alignment", StringComparison.OrdinalIgnoreCase);

        nos.ConvertEnumerationValuesToInts();

        Assert.Equal((int)StringComparison.OrdinalIgnoreCase, nos.Properties.GetValue("Alignment"));
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_RecursesIntoContainedObjects()
    {
        var contained = new NamedObjectSave();
        contained.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Alignment", Value = StringComparison.Ordinal });

        var nos = new NamedObjectSave();
        nos.ContainedObjects.Add(contained);

        nos.ConvertEnumerationValuesToInts();

        Assert.IsType<int>(contained.InstructionSaves[0].Value);
    }

    [Fact]
    public void PostLoadLogic_RemovesInstructionsWithNullValue()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "HasValue", Value = 1 });
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "NoValue", Value = null });

        nos.PostLoadLogic();

        Assert.Single(nos.InstructionSaves);
        Assert.Equal("HasValue", nos.InstructionSaves[0].Member);
    }

    [Fact]
    public void SetProperty_StoresValueInPropertiesList()
    {
        var nos = new NamedObjectSave();

        nos.SetProperty("SomeProperty", 42);

        Assert.Equal(42, nos.Properties.GetValue("SomeProperty"));
    }

    [Fact]
    public void AddInstruction_UsesCommonTypeNameForType()
    {
        var nos = new NamedObjectSave();

        var instruction = nos.AddInstruction("X", "Single");

        Assert.Equal("X", instruction.Member);
        Assert.Equal("float", instruction.Type);
        Assert.Null(instruction.Value);
        Assert.Contains(instruction, nos.InstructionSaves);
    }

    [Fact]
    public void AddNewGenericInstructionFor_ListOfString_UsesListStringLiteral()
    {
        var nos = new NamedObjectSave();

        var instruction = nos.AddNewGenericInstructionFor("Names", typeof(List<string>));

        Assert.Equal("List<string>", instruction.Type);
    }

    [Fact]
    public void AddNewGenericInstructionFor_NonGenericType_UsesFullName()
    {
        var nos = new NamedObjectSave();

        var instruction = nos.AddNewGenericInstructionFor("Count", typeof(int));

        Assert.Equal("int", instruction.Type);
    }

    [Fact]
    public void GetNamedObject_MatchingTopLevelInstance_ReturnsIt()
    {
        var container = new EntitySave();
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        container.NamedObjects.Add(nos);

        var found = container.GetNamedObject("SpriteInstance");

        Assert.Same(nos, found);
    }

    [Fact]
    public void GetNamedObject_MatchingNestedInstance_ReturnsIt()
    {
        var container = new EntitySave();
        var contained = new NamedObjectSave { InstanceName = "NestedSprite" };
        var top = new NamedObjectSave { InstanceName = "Top" };
        top.ContainedObjects.Add(contained);
        container.NamedObjects.Add(top);

        var found = container.GetNamedObject("NestedSprite");

        Assert.Same(contained, found);
    }

    [Fact]
    public void GetNamedObject_NoMatch_ReturnsNull()
    {
        var container = new EntitySave();
        container.NamedObjects.Add(new NamedObjectSave { InstanceName = "SpriteInstance" });

        var found = container.GetNamedObject("DoesNotExist");

        Assert.Null(found);
    }

    #region UpdateCustomProperties(INamedObjectContainer)

    [Fact]
    public void UpdateCustomProperties_Container_NullContainer_Throws()
    {
        INamedObjectContainer container = null;

        Assert.Throws<ArgumentException>(() => container.UpdateCustomProperties());
    }

    [Fact]
    public void UpdateCustomProperties_Container_SortsEveryTopLevelNamedObject()
    {
        var container = new EntitySave();
        var first = new NamedObjectSave();
        first.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Z" });
        first.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "A" });
        var second = new NamedObjectSave();
        second.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Y" });
        second.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "B" });
        container.NamedObjects.Add(first);
        container.NamedObjects.Add(second);

        container.UpdateCustomProperties();

        Assert.Equal(new[] { "A", "Z" }, first.InstructionSaves.Select(i => i.Member));
        Assert.Equal(new[] { "B", "Y" }, second.InstructionSaves.Select(i => i.Member));
    }

    [Fact]
    public void UpdateCustomProperties_Container_RecursesIntoContainedObjects()
    {
        var container = new EntitySave();
        var list = new NamedObjectSave();
        var nested = new NamedObjectSave();
        nested.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Z" });
        nested.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "A" });
        list.ContainedObjects.Add(nested);
        container.NamedObjects.Add(list);

        container.UpdateCustomProperties();

        Assert.Equal(new[] { "A", "Z" }, nested.InstructionSaves.Select(i => i.Member));
    }

    #endregion
}
