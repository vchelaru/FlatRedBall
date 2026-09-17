using FlatRedBall.Glue.Elements;

namespace GlueCommonUnitTests.SaveClasses;

public class VariableDefinitionExtensionsTests
{
    [Theory]
    [InlineData("float", "3", 3f)]
    [InlineData("bool", "true", true)]
    [InlineData("int", "42", 42)]
    [InlineData("string", "abc", "abc")]
    public void GetCastedDefaultValue_ParsesDefaultValueAsType(string type, string defaultValue, object expected)
    {
        var definition = new VariableDefinition { Type = type, DefaultValue = defaultValue };

        Assert.Equal(expected, definition.GetCastedDefaultValue());
    }

    [Fact]
    public void GetCastedDefaultValue_NullDefaultOnNumericType_ReturnsTypeDefault()
    {
        var definition = new VariableDefinition { Type = "float", DefaultValue = null };

        Assert.Equal(0f, definition.GetCastedDefaultValue());
    }
}
