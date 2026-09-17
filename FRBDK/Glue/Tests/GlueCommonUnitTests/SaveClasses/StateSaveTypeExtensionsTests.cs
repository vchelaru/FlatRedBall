using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Parsing;

namespace GlueCommonUnitTests.SaveClasses;

// FixAllTypes/FixEnumerationTypes read the shared static TypeResolutionCore.Self, so this can't run
// concurrently with any other test class that swaps it out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class StateSaveTypeExtensionsTests
{
    enum TestEnum { First = 0, Second = 1, Third = 2 }

    readonly FakeTypeResolutionCore _typeResolution = new();

    public StateSaveTypeExtensionsTests()
    {
        TypeResolutionCore.Self = _typeResolution;
    }

    static InstructionSave Instruction(string member, string type, object value) =>
        new() { Member = member, Type = type, Value = value };

    #region FixEnumerationTypes

    [Fact]
    public void FixEnumerationTypes_IntValue_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", 2));

        state.FixEnumerationTypes();

        Assert.Equal(TestEnum.Third, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_LongValue_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", 1L));

        state.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_AlreadyEnum_Unchanged()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", TestEnum.Second));

        state.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_NullValue_ConvertsToFirstEnumValue()
    {
        // Documents the existing behavior: a null value indexes the enum's values at 0, unlike the
        // NamedObjectSave version, which skips null values.
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", null));

        state.FixEnumerationTypes();

        Assert.Equal(TestEnum.First, state.InstructionSaves[0].Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UnknownType")]
    public void FixEnumerationTypes_UnresolvableType_Unchanged(string type)
    {
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", type, 2));

        state.FixEnumerationTypes();

        Assert.Equal(2, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_NonEnumType_Unchanged()
    {
        _typeResolution.AddType("int", typeof(int));
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Count", "int", 2L));

        state.FixEnumerationTypes();

        Assert.Equal(2L, state.InstructionSaves[0].Value);
    }

    #endregion

    #region ConvertEnumerationValuesToInts

    [Fact]
    public void ConvertEnumerationValuesToInts_EnumValue_ConvertsToInt()
    {
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", TestEnum.Third));

        state.ConvertEnumerationValuesToInts();

        Assert.Equal(2, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_NonEnumValues_Unchanged()
    {
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Count", "int", 5));
        state.InstructionSaves.Add(Instruction("Name", "string", "abc"));
        state.InstructionSaves.Add(Instruction("Nothing", "string", null));

        state.ConvertEnumerationValuesToInts();

        Assert.Equal(5, state.InstructionSaves[0].Value);
        Assert.Equal("abc", state.InstructionSaves[1].Value);
        Assert.Null(state.InstructionSaves[2].Value);
    }

    #endregion

    #region FixAllTypes

    [Fact]
    public void FixAllTypes_LongIntInstruction_ConvertsToInt()
    {
        var owner = new EntitySave();
        owner.CustomVariables.Add(new CustomVariable { Name = "Count", Type = "int" });
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Count", "int", 5L));

        state.FixAllTypes(owner);

        Assert.Equal(5, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_DoubleFloatInstruction_ConvertsToFloat()
    {
        var owner = new EntitySave();
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("X", "float", 1.5));

        state.FixAllTypes(owner);

        Assert.Equal(1.5f, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_VariableTypeChanged_UpdatesInstructionTypeAndValue()
    {
        var owner = new EntitySave();
        owner.CustomVariables.Add(new CustomVariable { Name = "X", Type = "float" });
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("X", "int", 3));

        state.FixAllTypes(owner);

        Assert.Equal("float", state.InstructionSaves[0].Type);
        Assert.Equal(3f, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_NoMatchingVariable_KeepsInstructionType()
    {
        var owner = new EntitySave();
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("X", "int", 3L));

        state.FixAllTypes(owner);

        Assert.Equal("int", state.InstructionSaves[0].Type);
        Assert.Equal(3, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_EnumInstruction_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var owner = new EntitySave();
        owner.CustomVariables.Add(new CustomVariable { Name = "Mode", Type = "TestEnum" });
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("Mode", "TestEnum", 1L));

        state.FixAllTypes(owner);

        Assert.Equal(TestEnum.Second, state.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_NullValueOrType_Unchanged()
    {
        var owner = new EntitySave();
        var state = new StateSave();
        state.InstructionSaves.Add(Instruction("A", "int", null));
        state.InstructionSaves.Add(Instruction("B", null, "5"));
        state.InstructionSaves.Add(Instruction("C", "", "5"));

        state.FixAllTypes(owner);

        Assert.Null(state.InstructionSaves[0].Value);
        Assert.Equal("5", state.InstructionSaves[1].Value);
        Assert.Equal("5", state.InstructionSaves[2].Value);
    }

    #endregion
}
