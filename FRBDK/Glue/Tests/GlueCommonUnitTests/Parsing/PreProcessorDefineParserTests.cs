using FlatRedBall.Glue.Parsing;

namespace GlueCommonUnitTests.Parsing;

// ParsedClass.ParseContents also drives PreProcessorDefineParser's shared static define-stack
// (it calls Clear() at the start of every parse), so this must not run concurrently with
// ParsedClassTests - hence the shared collection.
[Collection(nameof(PreProcessorDefineParserCollection))]
public class PreProcessorDefineParserTests
{
    public PreProcessorDefineParserTests()
    {
        PreProcessorDefineParser.Clear();
    }

    [Fact]
    public void ShouldLineBeSkipped_DefineNotInProjectDefines_ReturnsTrue()
    {
        PreProcessorDefineParser.ParseLine("#if SOME_DEFINE");

        var shouldSkip = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string>());

        Assert.True(shouldSkip);
    }

    [Fact]
    public void ShouldLineBeSkipped_DefineInProjectDefines_ReturnsFalse()
    {
        PreProcessorDefineParser.ParseLine("#if SOME_DEFINE");

        var shouldSkip = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string> { "SOME_DEFINE" });

        Assert.False(shouldSkip);
    }

    [Fact]
    public void ShouldLineBeSkipped_NegatedDefinePresent_ReturnsTrue()
    {
        PreProcessorDefineParser.ParseLine("#if !SOME_DEFINE");

        var shouldSkip = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string> { "SOME_DEFINE" });

        Assert.True(shouldSkip);
    }

    [Fact]
    public void ShouldLineBeSkipped_AfterEndif_NoLongerAffectsLines()
    {
        PreProcessorDefineParser.ParseLine("#if SOME_DEFINE");
        PreProcessorDefineParser.ParseLine("#endif");

        var shouldSkip = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string>());

        Assert.False(shouldSkip);
    }

    [Fact]
    public void ShouldLineBeSkipped_ElseBranch_InvertsIfCondition()
    {
        PreProcessorDefineParser.ParseLine("#if SOME_DEFINE");
        PreProcessorDefineParser.ParseLine("#else");

        var shouldSkipWithDefine = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string> { "SOME_DEFINE" });
        var shouldSkipWithoutDefine = PreProcessorDefineParser.ShouldLineBeSkipped(new List<string>());

        Assert.True(shouldSkipWithDefine);
        Assert.False(shouldSkipWithoutDefine);
    }
}

[CollectionDefinition(nameof(PreProcessorDefineParserCollection), DisableParallelization = true)]
public class PreProcessorDefineParserCollection
{
}
