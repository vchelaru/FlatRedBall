using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.Parsing;

// ParseContents drives PreProcessorDefineParser's shared static define-stack (Clear() on every
// parse), so this must not run concurrently with PreProcessorDefineParserTests.
[Collection(nameof(PreProcessorDefineParserCollection))]
public class ParsedClassTests
{
    const string SampleClass =
        "class MyClass\r\n" +
        "{\r\n" +
        "    private int mCount;\r\n" +
        "\r\n" +
        "    public string Name\r\n" +
        "    {\r\n" +
        "        get { return mName; }\r\n" +
        "        set { mName = value; }\r\n" +
        "    }\r\n" +
        "\r\n" +
        "    public void DoSomething(int amount)\r\n" +
        "    {\r\n" +
        "        mCount += amount;\r\n" +
        "    }\r\n" +
        "}\r\n";

    [Fact]
    public void Constructor_ParsesClassName()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        Assert.Equal("MyClass", parsedClass.Name);
    }

    [Fact]
    public void Constructor_ParsesField()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        var field = parsedClass.GetField("mCount");

        Assert.NotNull(field);
        Assert.Equal(Scope.Private, field!.Scope);
        Assert.Equal("int", field.Type.Name);
    }

    [Fact]
    public void Constructor_ParsesPropertyWithGetterAndSetter()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        var property = parsedClass.GetProperty("Name");

        Assert.NotNull(property);
        Assert.Equal(Scope.Public, property!.Scope);
        Assert.Equal("string", property.Type.Name);
        Assert.NotNull(property.GetContents);
        Assert.NotNull(property.SetContents);
    }

    [Fact]
    public void Constructor_ParsesMethodNameAndArguments()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        var method = parsedClass.GetMethod("DoSomething");

        Assert.NotNull(method);
        Assert.Single(method!.ArgumentList);
        Assert.Equal("amount", method.ArgumentList[0].Name);
        Assert.Equal("int", method.ArgumentList[0].Type.Name);
    }

    [Fact]
    public void Constructor_MethodWithQualifiedReturnType_UnqualifiesTypeName()
    {
        const string classWithQualifiedMethod =
            "class MyClass\r\n{\r\n    public System.String GetText()\r\n    {\r\n        return null;\r\n    }\r\n}\r\n";

        var parsedClass = new ParsedClass(classWithQualifiedMethod, trimContents: true);

        Assert.Equal("String", parsedClass.GetMethod("GetText")!.Type.Name);
    }

    [Fact]
    public void GetField_UnknownName_ReturnsNull()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        Assert.Null(parsedClass.GetField("doesNotExist"));
    }

    [Fact]
    public void RemoveComments_StripsLineComments()
    {
        const string withComment = "public int X; // this is a comment\r\npublic int Y;";

        var result = ParsedClass.RemoveComments(withComment);

        Assert.DoesNotContain("this is a comment", result);
        Assert.Contains("public int X;", result);
        Assert.Contains("public int Y;", result);
    }

    [Fact]
    public void NumberOfValid_IgnoresCharactersInsideQuotes()
    {
        var count = ParsedClass.NumberOfValid('{', "string s = \"{not a brace}\"; { }");

        Assert.Equal(1, count);
    }

    [Fact]
    public void Clone_ProducesIndependentCopyOfFields()
    {
        var parsedClass = new ParsedClass(SampleClass, trimContents: true);

        var clone = parsedClass.Clone();
        clone.GetField("mCount")!.Name = "renamed";

        Assert.Equal("mCount", parsedClass.GetField("mCount")!.Name);
        Assert.Null(parsedClass.GetField("renamed"));
    }
}
