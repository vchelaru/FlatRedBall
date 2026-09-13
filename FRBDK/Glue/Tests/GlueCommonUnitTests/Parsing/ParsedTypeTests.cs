using FlatRedBall.Glue.Parsing;

namespace GlueCommonUnitTests.Parsing;

public class ParsedTypeTests
{
    [Fact]
    public void Constructor_NonGenericType_SetsNameAndNoGenericType()
    {
        var type = new ParsedType("int");

        Assert.Equal("int", type.Name);
        Assert.Null(type.GenericType);
        Assert.Equal(1, type.NumberOfElements);
    }

    [Fact]
    public void Constructor_GenericType_SplitsNameAndGenericType()
    {
        var type = new ParsedType("List<string>");

        Assert.Equal("List", type.Name);
        Assert.NotNull(type.GenericType);
        Assert.Equal("string", type.GenericType!.Name);
    }

    [Fact]
    public void Constructor_InterfaceLikeName_SetsIsInterface()
    {
        var type = new ParsedType("IElement");

        Assert.True(type.IsInterface);
    }

    [Fact]
    public void Constructor_LowercaseLeadingI_DoesNotSetIsInterface()
    {
        var type = new ParsedType("int");

        Assert.False(type.IsInterface);
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("float", true)]
    [InlineData("MyClass", false)]
    public void IsPrimitiveType_ReturnsExpected(string typeName, bool expected)
    {
        Assert.Equal(expected, ParsedType.IsPrimitiveType(typeName));
    }

    [Theory]
    [InlineData("List")]
    [InlineData("List`1")]
    [InlineData("ArrayList")]
    public void IsList_KnownListTypeNames_ReturnsTrue(string typeName)
    {
        var type = new ParsedType(typeName);

        Assert.True(type.IsList);
    }

    [Fact]
    public void IsList_UnrelatedTypeName_ReturnsFalse()
    {
        var type = new ParsedType("Sprite");

        Assert.False(type.IsList);
    }

    [Fact]
    public void IsPrimitiveArray_PrimitiveArrayName_ReturnsTrue()
    {
        var type = new ParsedType("int[]");

        Assert.True(type.IsPrimitiveArray);
    }

    [Fact]
    public void IsPrimitiveArray_NonArrayName_ReturnsFalse()
    {
        var type = new ParsedType("int");

        Assert.False(type.IsPrimitiveArray);
    }

    [Fact]
    public void ToString_GenericType_IncludesGenericArgument()
    {
        var type = new ParsedType("List<int>");

        Assert.Equal("List<int>", type.ToString());
    }

    [Fact]
    public void Clone_ReturnsIndependentCopy()
    {
        var original = new ParsedType("List<int>");

        var clone = original.Clone();
        clone.GenericType!.Name = "float";

        Assert.Equal("int", original.GenericType!.Name);
        Assert.Equal("float", clone.GenericType!.Name);
    }
}
