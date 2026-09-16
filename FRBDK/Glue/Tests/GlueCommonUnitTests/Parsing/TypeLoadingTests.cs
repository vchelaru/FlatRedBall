using FlatRedBall.Glue.Parsing;
using GlueCommonUnitTests.Controls;

namespace GlueCommonUnitTests.Parsing;

public class TypeLoadingTests
{
    [Fact]
    public void GetAdditionalTypes_NoNamespaceFilter_ReturnsAllTypesInAssembly()
    {
        var errorReporting = new FakeErrorReportingCore();

        var result = TypeLoading.GetAdditionalTypes(typeof(TypeLoadingTests).Assembly, null, errorReporting);

        Assert.Contains(typeof(TypeLoadingTests), result);
        Assert.Contains(typeof(FakeErrorReportingCore), result);
        Assert.Empty(errorReporting.Messages);
    }

    [Fact]
    public void GetAdditionalTypes_NamespaceFilter_OnlyReturnsMatchingTypes()
    {
        var errorReporting = new FakeErrorReportingCore();

        var result = TypeLoading.GetAdditionalTypes(typeof(TypeLoadingTests).Assembly, "GlueCommonUnitTests.Controls", errorReporting);

        Assert.Contains(typeof(FakeErrorReportingCore), result);
        Assert.DoesNotContain(typeof(TypeLoadingTests), result);
        Assert.Empty(errorReporting.Messages);
    }
}
