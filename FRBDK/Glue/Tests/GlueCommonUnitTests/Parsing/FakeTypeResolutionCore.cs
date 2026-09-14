using FlatRedBall.Glue.Parsing;

namespace GlueCommonUnitTests.Parsing;

/// <summary>
/// Hand-rolled test double for <see cref="ITypeResolutionCore"/> - the real implementation
/// (<c>TypeManager</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeTypeResolutionCore : ITypeResolutionCore
{
    readonly Dictionary<string, Type> _typesByString = new();

    public void AddType(string typeString, Type type) => _typesByString[typeString] = type;

    public Type GetTypeFromString(string typeString) =>
        typeString != null && _typesByString.TryGetValue(typeString, out var type) ? type : null;
}
