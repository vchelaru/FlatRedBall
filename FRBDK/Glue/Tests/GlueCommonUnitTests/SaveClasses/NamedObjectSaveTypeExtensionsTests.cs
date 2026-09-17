using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Parsing;
using GlueSaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// FixAllTypes/FixEnumerationTypes/Clone read the shared static TypeResolutionCore.Self and (through
// GetAssetTypeInfo) AvailableAssetTypesCore.Self, so this can't run concurrently with any other test
// class that swaps one out - hence the shared collection (see ObjectFinderCoreCollection in
// NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectSaveTypeExtensionsTests
{
    enum TestEnum { First = 0, Second = 1, Third = 2 }

    readonly FakeTypeResolutionCore _typeResolution = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();
    readonly FakeObjectFinderCore _finder = new();

    public NamedObjectSaveTypeExtensionsTests()
    {
        TypeResolutionCore.Self = _typeResolution;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
        ObjectFinderCore.Self = _finder;
    }

    static CustomVariableInNamedObject Instruction(string member, string type, object value) =>
        new() { Member = member, Type = type, Value = value };

    #region FixEnumerationTypes

    [Fact]
    public void FixEnumerationTypes_IntValue_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", "TestEnum", 2));

        nos.FixEnumerationTypes();

        Assert.Equal(TestEnum.Third, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_LongValue_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", "TestEnum", 1L));

        nos.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_AlreadyEnum_Unchanged()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", "TestEnum", TestEnum.Second));

        nos.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_UndefinedIntValue_Unchanged()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", "TestEnum", 99));

        nos.FixEnumerationTypes();

        Assert.Equal(99, nos.InstructionSaves[0].Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UnknownType")]
    public void FixEnumerationTypes_UnresolvableType_Unchanged(string type)
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", type, 2));

        nos.FixEnumerationTypes();

        Assert.Equal(2, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixEnumerationTypes_ContainedObjects_FixedRecursively()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        var contained = new NamedObjectSave();
        contained.InstructionSaves.Add(Instruction("Mode", "TestEnum", 1));
        nos.ContainedObjects.Add(contained);

        nos.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, contained.InstructionSaves[0].Value);
    }

    #endregion

    #region FixAllTypes

    [Fact]
    public void FixAllTypes_LongIntInstruction_ConvertsToInt()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Count", "int", 5L));

        nos.FixAllTypes();

        Assert.Equal(5, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_EnumInstruction_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Mode", "TestEnum", 2L));

        nos.FixAllTypes();

        Assert.Equal(TestEnum.Third, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_NullInstructionType_FilledFromAssetTypeInfoVariableDefinition()
    {
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" } };
        ati.VariableDefinitions.Add(new VariableDefinition { Name = "Alpha", Type = "float" });
        _availableAssetTypes.AddAssetType(ati);
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };
        nos.InstructionSaves.Add(Instruction("Alpha", null, 1));

        nos.FixAllTypes();

        Assert.Equal("float", nos.InstructionSaves[0].Type);
        Assert.Equal(1f, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_NullInstructionTypeNotOnAssetTypeInfo_StaysNull()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };
        nos.InstructionSaves.Add(Instruction("Unknown", null, 1L));

        nos.FixAllTypes();

        Assert.Null(nos.InstructionSaves[0].Type);
        Assert.Equal(1L, nos.InstructionSaves[0].Value);
    }

    [Fact]
    public void FixAllTypes_TypedProperty_ValueConverted()
    {
        var nos = new NamedObjectSave();
        nos.Properties.Add(new PropertySave { Name = "Count", Type = "int", Value = 5L });

        nos.FixAllTypes();

        Assert.Equal(5, nos.Properties[0].Value);
    }

    [Fact]
    public void FixAllTypes_UntypedProperty_Unchanged()
    {
        var nos = new NamedObjectSave();
        nos.Properties.Add(new PropertySave { Name = "Count", Type = null, Value = 5L });

        nos.FixAllTypes();

        Assert.Equal(5L, nos.Properties[0].Value);
    }

    [Fact]
    public void FixAllTypes_DestinationRectangleString_ConvertsToFloatRectangle()
    {
        var nos = new NamedObjectSave();
        nos.Properties.Add(new PropertySave { Name = "DestinationRectangle", Value = "(1,2,3,4)" });

        nos.FixAllTypes();

        var rectangle = Assert.IsType<FloatRectangle>(nos.Properties[0].Value);
        Assert.Equal(1f, rectangle.X);
        Assert.Equal(2f, rectangle.Y);
        Assert.Equal(3f, rectangle.Width);
        Assert.Equal(4f, rectangle.Height);
    }

    [Fact]
    public void FixAllTypes_ContainedObjects_FixedRecursively()
    {
        var nos = new NamedObjectSave();
        var contained = new NamedObjectSave();
        contained.InstructionSaves.Add(Instruction("Count", "int", 5L));
        nos.ContainedObjects.Add(contained);

        nos.FixAllTypes();

        Assert.Equal(5, contained.InstructionSaves[0].Value);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_ReturnsDistinctInstanceWithSameFields()
    {
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };

        var clone = nos.Clone();

        Assert.NotSame(nos, clone);
        Assert.Equal("SpriteInstance", clone.InstanceName);
        Assert.Equal(SourceType.FlatRedBallType, clone.SourceType);
        Assert.Equal("FlatRedBall.Sprite", clone.SourceClassType);
    }

    [Fact]
    public void Clone_CopiesInstructionsAsNewObjects()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("X", "float", 3f));

        var clone = nos.Clone();

        var instruction = Assert.Single(clone.InstructionSaves);
        Assert.NotSame(nos.InstructionSaves[0], instruction);
        Assert.Equal("X", instruction.Member);
        Assert.Equal(3f, instruction.Value);
    }

    [Fact]
    public void Clone_ClearsEventOnSet()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "X", Type = "float", Value = 3f, EventOnSet = "XSet" });

        var clone = nos.Clone();

        Assert.Null(clone.InstructionSaves[0].EventOnSet);
    }

    [Fact]
    public void Clone_KeepsInstructionsNotOnCurrentType()
    {
        // Instructions for a previous type survive a clone so the user can switch back.
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };
        nos.InstructionSaves.Add(Instruction("OldTypeOnly", "int", 1));

        var clone = nos.Clone();

        Assert.Equal("OldTypeOnly", Assert.Single(clone.InstructionSaves).Member);
    }

    [Fact]
    public void Clone_ClonesContainedObjectsRecursively()
    {
        var nos = new NamedObjectSave();
        var contained = new NamedObjectSave { InstanceName = "Nested" };
        nos.ContainedObjects.Add(contained);

        var clone = nos.Clone();

        var containedClone = Assert.Single(clone.ContainedObjects);
        Assert.NotSame(contained, containedClone);
        Assert.Equal("Nested", containedClone.InstanceName);
    }

    [Fact]
    public void Clone_FixesInstructionTypes()
    {
        // Json round-tripping turns ints into longs; Clone fixes them back.
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Count", "int", 5));

        var clone = nos.Clone();

        Assert.Equal(5, clone.InstructionSaves[0].Value);
    }

    #endregion

    #region ResetVariablesReferencing

    [Fact]
    public void ResetVariablesReferencing_FileVariableNamingRfs_Removed()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Sheet.png" };
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Texture", "Texture2D", "Sheet"));

        nos.ResetVariablesReferencing(rfs);

        Assert.Empty(nos.InstructionSaves);
    }

    [Fact]
    public void ResetVariablesReferencing_FileVariableNamingOtherRfs_Kept()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Sheet.png" };
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Texture", "Texture2D", "OtherSheet"));

        nos.ResetVariablesReferencing(rfs);

        Assert.Single(nos.InstructionSaves);
    }

    [Fact]
    public void ResetVariablesReferencing_NonFileVariableMatchingName_Kept()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Sheet.png" };
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Label", "string", "Sheet"));

        nos.ResetVariablesReferencing(rfs);

        Assert.Single(nos.InstructionSaves);
    }

    [Fact]
    public void ResetVariablesReferencing_MultipleMatches_AllRemoved()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Sheet.png" };
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(Instruction("Texture", "Texture2D", "Sheet"));
        nos.InstructionSaves.Add(Instruction("Label", "string", "Sheet"));
        nos.InstructionSaves.Add(Instruction("OtherTexture", "Texture2D", "Sheet"));

        nos.ResetVariablesReferencing(rfs);

        Assert.Equal("Label", Assert.Single(nos.InstructionSaves).Member);
    }

    #endregion
}
