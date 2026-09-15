using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

/// <summary>
/// Hand-rolled test double for <see cref="IGlueStateCore"/> - the real implementation
/// (<c>GlueState</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeGlueStateCore : IGlueStateCore
{
    public string ProjectNamespace { get; set; } = "";
}
