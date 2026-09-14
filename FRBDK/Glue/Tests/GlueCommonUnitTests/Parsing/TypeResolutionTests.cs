using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;

namespace GlueCommonUnitTests.Parsing;

public class TypeResolutionTests
{
    static Type Resolve(
        string typeString,
        IReadOnlyDictionary<string, Type> commonTypes = null,
        IReadOnlyList<Type> additionalTypes = null,
        IReadOnlyList<Type> flatRedBallTypes = null,
        IReadOnlyList<Type> xnaFrameworkTypes = null,
        IReadOnlyList<Type> xnaFrameworkGameTypes = null,
        IReadOnlyList<Type> pluginTypes = null,
        IEnumerable<AssetTypeInfo> allAssetTypes = null) =>
        TypeResolution.GetTypeFromString(
            typeString,
            commonTypes ?? new Dictionary<string, Type>(),
            additionalTypes ?? Array.Empty<Type>(),
            flatRedBallTypes ?? Array.Empty<Type>(),
            xnaFrameworkTypes ?? Array.Empty<Type>(),
            xnaFrameworkGameTypes ?? Array.Empty<Type>(),
            pluginTypes ?? Array.Empty<Type>(),
            allAssetTypes ?? Array.Empty<AssetTypeInfo>());

    [Fact]
    public void GetTypeFromString_NullTypeString_ReturnsNull()
    {
        Assert.Null(Resolve(null));
    }

    [Theory]
    [InlineData("bool", typeof(bool))]
    [InlineData("Boolean", typeof(bool))]
    [InlineData("System.Boolean", typeof(bool))]
    [InlineData("bool?", typeof(bool?))]
    [InlineData("float", typeof(float))]
    [InlineData("Single", typeof(float))]
    [InlineData("float?", typeof(float?))]
    [InlineData("string", typeof(string))]
    [InlineData("String", typeof(string))]
    [InlineData("char", typeof(char))]
    [InlineData("long", typeof(long))]
    [InlineData("long?", typeof(long?))]
    [InlineData("int", typeof(int))]
    [InlineData("Int32", typeof(int))]
    [InlineData("int?", typeof(int?))]
    [InlineData("uint", typeof(uint))]
    [InlineData("double", typeof(double))]
    [InlineData("Double", typeof(double))]
    [InlineData("double?", typeof(double?))]
    [InlineData("decimal", typeof(decimal))]
    [InlineData("Decimal", typeof(decimal))]
    [InlineData("decimal?", typeof(decimal?))]
    [InlineData("byte", typeof(byte))]
    [InlineData("byte?", typeof(byte?))]
    public void GetTypeFromString_Primitive_ReturnsExpectedType(string typeString, Type expected)
    {
        Assert.Equal(expected, Resolve(typeString));
    }

    [Fact]
    public void GetTypeFromString_ArraySuffix_ReturnsArrayType()
    {
        Assert.Equal(typeof(int[]), Resolve("int[]"));
    }

    [Fact]
    public void GetTypeFromString_UnknownType_ReturnsNull()
    {
        Assert.Null(Resolve("Some.Totally.Unknown.Type"));
    }

    [Fact]
    public void GetTypeFromString_CommonType_ReturnsFromCommonTypesDictionary()
    {
        var commonTypes = new Dictionary<string, Type> { { "List`1", typeof(List<>) } };

        Assert.Equal(typeof(List<>), Resolve("List`1", commonTypes: commonTypes));
    }

    [Fact]
    public void GetTypeFromString_UnqualifiedName_MatchesAdditionalTypeByName()
    {
        var additionalTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(nameof(TypeResolutionTests), additionalTypes: additionalTypes));
    }

    [Fact]
    public void GetTypeFromString_QualifiedName_MatchesAdditionalTypeByFullNameSuffix()
    {
        var additionalTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(typeof(TypeResolutionTests).FullName, additionalTypes: additionalTypes));
    }

    [Fact]
    public void GetTypeFromString_QualifiedName_MatchesFlatRedBallTypeByFullName()
    {
        var flatRedBallTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(typeof(TypeResolutionTests).FullName, flatRedBallTypes: flatRedBallTypes));
    }

    [Fact]
    public void GetTypeFromString_UnqualifiedName_MatchesFlatRedBallTypeByName()
    {
        var flatRedBallTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(nameof(TypeResolutionTests), flatRedBallTypes: flatRedBallTypes));
    }

    [Fact]
    public void GetTypeFromString_QualifiedName_MatchesPluginTypeByFullName()
    {
        var pluginTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(typeof(TypeResolutionTests).FullName, pluginTypes: pluginTypes));
    }

    [Fact]
    public void GetTypeFromString_UnqualifiedName_DoesNotMatchPluginTypeByNameAlone()
    {
        // The plugin-type lookup only matches on FullName, and only when the input itself looks
        // qualified (contains a '.') - an unqualified name never reaches it.
        var pluginTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Null(Resolve(nameof(TypeResolutionTests), pluginTypes: pluginTypes));
    }

    [Fact]
    public void GetTypeFromString_ArraySuffixMatchedViaAssetTypeInfo_BypassesArrayWrapping()
    {
        // Pins an existing quirk: the AssetTypeInfo match loop `return`s the matched type directly,
        // unconditionally (it isn't gated on a prior match being absent, and isn't gated on
        // isFullyQualified either) - unlike every other match path, it never reaches the final
        // isArray/MakeArrayType() step. Preserved here exactly, not fixed - out of scope for #2276.
        var flatRedBallTypes = new[] { typeof(TypeResolutionTests) };
        var assetTypes = new[]
        {
            new AssetTypeInfo
            {
                QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = typeof(TypeResolutionTests).FullName }
            }
        };

        var result = Resolve(nameof(TypeResolutionTests) + "[]", flatRedBallTypes: flatRedBallTypes, allAssetTypes: assetTypes);

        Assert.Equal(typeof(TypeResolutionTests), result);
    }

    [Fact]
    public void GetTypeFromString_QualifiedName_MatchesXnaFrameworkTypeByFullName()
    {
        var xnaTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(typeof(TypeResolutionTests).FullName, xnaFrameworkTypes: xnaTypes));
    }

    [Fact]
    public void GetTypeFromString_QualifiedName_MatchesXnaFrameworkGameTypeByFullName()
    {
        var xnaGameTypes = new[] { typeof(TypeResolutionTests) };

        Assert.Equal(typeof(TypeResolutionTests), Resolve(typeof(TypeResolutionTests).FullName, xnaFrameworkGameTypes: xnaGameTypes));
    }

    [Fact]
    public void GetTypeFromString_OpenGenericMarker_ResolvesFromCommonTypesAndMakesGenericType()
    {
        var commonTypes = new Dictionary<string, Type> { { "List<>", typeof(List<>) } };

        var result = Resolve("List<int>", commonTypes: commonTypes);

        Assert.Equal(typeof(List<int>), result);
    }

    [Fact]
    public void GetTypeFromString_GenericArraySuffix_ReturnsArrayOfGenericType()
    {
        var commonTypes = new Dictionary<string, Type> { { "List<>", typeof(List<>) } };

        var result = Resolve("List<int>[]", commonTypes: commonTypes);

        Assert.Equal(typeof(List<int>[]), result);
    }
}
