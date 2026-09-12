using FlatRedBall.Glue.CodeGeneration.CodeBuilder;

namespace GlueCommonUnitTests.CodeGeneration;

public class CodeBuilderTests
{
    private static readonly string NL = Environment.NewLine;

    [Fact]
    public void SpaceStrings_JoinsNonEmptyValuesWithSingleSpace()
    {
        Assert.Equal("public static void", StringHelper.SpaceStrings("public", "static", "void"));
    }

    [Fact]
    public void SpaceStrings_SkipsNullAndEmptyValues()
    {
        Assert.Equal("public void", StringHelper.SpaceStrings("public", null, "", "void"));
    }

    [Fact]
    public void Modifiers_CombinesScopeAndStaticInOrder()
    {
        Assert.Equal("public static", StringHelper.Modifiers(Public: true, Static: true));
    }

    [Fact]
    public void Modifiers_NoFlagsSet_ReturnsEmptyString()
    {
        Assert.Equal("", StringHelper.Modifiers());
    }

    [Fact]
    public void CodeLine_ToString_WritesValueWithIndentation()
    {
        var line = new CodeLine("int x = 3;");

        Assert.Equal($"\tint x = 3;{NL}", line.ToString(1, "\t"));
    }

    [Fact]
    public void CodeLine_NullValue_BecomesEmptyString()
    {
        var line = new CodeLine(null);

        Assert.Equal("", line.Value);
    }

    [Fact]
    public void Function_GeneratesSignatureBracesAndIndentedBody()
    {
        var root = new CodeBlockBase();

        root.Function("public", "DoSomething", "int x").Line("DoWork();");

        var code = root.ToString(0, "    ");

        Assert.Equal($"public DoSomething (int x) {NL}{{{NL}    DoWork();{NL}}}{NL}", code);
    }

    [Fact]
    public void Function_NoParameters_OmitsParameterText()
    {
        var root = new CodeBlockBase();

        root.Function("public void", "DoSomething", null);

        Assert.Equal($"public void DoSomething () {NL}{{{NL}}}{NL}", root.ToString(0, "    "));
    }

    [Fact]
    public void Class_GeneratesClassDeclarationWithBraces()
    {
        var root = new CodeBlockBase();

        root.Class("public", "MyClass", null);

        Assert.Equal($"public class MyClass{NL}{{{NL}}}{NL}", root.ToString(0, "    "));
    }

    [Fact]
    public void If_Else_GeneratesBothBranches()
    {
        var root = new CodeBlockBase();

        var ifBlock = root.If("x > 0");
        ifBlock.Line("Positive();");
        var elseBlock = ifBlock.End().Else();
        elseBlock.Line("NonPositive();");

        var code = root.ToString(0, "    ");

        Assert.Equal(
            $"if (x > 0){NL}{{{NL}    Positive();{NL}}}{NL}else{NL}{{{NL}    NonPositive();{NL}}}{NL}",
            code);
    }

    [Fact]
    public void AutoProperty_GeneratesGetSetOnOneLine()
    {
        var root = new CodeBlockBase();

        root.AutoProperty("public int", "Health");

        Assert.Equal($"public int Health {{ get; set; }}{NL}", root.ToString(0, "    "));
    }

    [Fact]
    public void Property_WithExplicitGetAndSet_GeneratesNestedBlocks()
    {
        var root = new CodeBlockBase();

        var property = root.Property("public int", "Health");
        property.Get().Line("return mHealth;");
        property.Set().Line("mHealth = value;");

        var code = root.ToString(0, "    ");

        Assert.Equal(
            $"public int Health{NL}{{{NL}    get{NL}    {{{NL}        return mHealth;{NL}    }}{NL}    set{NL}    {{{NL}        mHealth = value;{NL}    }}{NL}}}{NL}",
            code);
    }

    [Fact]
    public void Namespace_ContainingClass_IndentsClassBody()
    {
        var root = new CodeBlockBase();

        var ns = root.Namespace("MyGame");
        ns.Class("public", "MyClass", null);

        var code = root.ToString(0, "    ");

        Assert.Equal(
            $"namespace MyGame{NL}{{{NL}    public class MyClass{NL}    {{{NL}    }}{NL}}}{NL}",
            code);
    }

    [Fact]
    public void Line_And_UnderscoreExtension_AddPlainCodeLines()
    {
        var root = new CodeBlockBase();

        root.Line("var x = 1;")._("var y = 2;")._();

        Assert.Equal($"var x = 1;{NL}var y = 2;{NL}{NL}", root.ToString(0, "    "));
    }

    [Fact]
    public void HasLine_FindsMatchingBodyLine()
    {
        var root = new CodeBlockBase();
        root.Line("var x = 1;");

        Assert.True(root.HasLine("var x = 1;"));
        Assert.False(root.HasLine("var x = 2;"));
    }

    [Fact]
    public void Constructor_WithBaseCall_InsertsBaseCallLineBeforeBody()
    {
        var root = new CodeBlockBase();

        var ctor = root.Constructor("public", "MyClass", "int x", "base(x)");
        ctor.Line("Initialize();");

        var code = root.ToString(0, "    ");

        Assert.Equal(
            $"public MyClass (int x) {NL}\t: base(x){NL}{{{NL}    Initialize();{NL}}}{NL}",
            code);
    }

    [Fact]
    public void End_ReturnsParentBlock()
    {
        var root = new CodeBlockBase();
        var function = root.Function("public", "DoSomething", null);

        Assert.Same(root, function.End());
    }
}
