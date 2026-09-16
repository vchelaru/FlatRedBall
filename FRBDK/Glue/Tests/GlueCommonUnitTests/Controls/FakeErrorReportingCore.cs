using FlatRedBall.Glue.Controls;

namespace GlueCommonUnitTests.Controls;

/// <summary>
/// Hand-rolled test double for <see cref="IErrorReportingCore"/> - the real implementation
/// (<c>DialogService</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeErrorReportingCore : IErrorReportingCore
{
    public List<string> Messages { get; } = new();

    public void ShowMessage(string text) => Messages.Add(text);
}
