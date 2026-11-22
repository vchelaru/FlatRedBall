using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.IO;

namespace GlueCommonUnitTests.Parsing;

public class IndexedTypeListTests
{
    // Real assemblies, so names repeat across namespaces and nested types the way they do in FlatRedBall's.
    static readonly Type[] FrbStandIn = typeof(TypeResolution).Assembly.GetTypes();
    static readonly Type[] XnaStandIn = typeof(System.Collections.Generic.List<>).Assembly.GetTypes();
    static readonly Type[] XnaGameStandIn = typeof(System.Linq.Enumerable).Assembly.GetTypes();

    static Type Resolve(string typeString, IReadOnlyList<Type> frb, IReadOnlyList<Type> xna, IReadOnlyList<Type> xnaGame,
        IEnumerable<AssetTypeInfo> atis) =>
        TypeResolution.GetTypeFromString(typeString, new Dictionary<string, Type>(), Array.Empty<Type>(),
            frb, xna, xnaGame, Array.Empty<Type>(), atis);

    public static IEnumerable<object[]> TypeStrings()
    {
        var strings = new HashSet<string> { "NotAType", "Some.Namespace.NotAType", "List<>", "IndexedTypeList" };
        foreach (var type in FrbStandIn.Concat(XnaStandIn.Take(400)).Concat(XnaGameStandIn.Take(200)))
        {
            if (type.Name != null) strings.Add(type.Name);
            if (type.FullName != null) strings.Add(type.FullName);
        }
        return strings.Select(s => new object[] { s });
    }

    [Fact]
    public void GetTypeFromString_IndexedLists_ResolveEveryNameTheSameAsPlainArrays()
    {
        // An asset type pointing at a type in the FlatRedBall list exercises the asset-type lookup too.
        var ati = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = typeof(TypeResolution).FullName }
        };
        var atis = new[] { ati };
        var frb = new IndexedTypeList(FrbStandIn);
        var xna = new IndexedTypeList(XnaStandIn);
        var xnaGame = new IndexedTypeList(XnaGameStandIn);

        var mismatches = new List<string>();
        foreach (var row in TypeStrings())
        {
            var typeString = (string)row[0];
            var expected = Resolve(typeString, FrbStandIn, XnaStandIn, XnaGameStandIn, atis);
            var actual = Resolve(typeString, frb, xna, xnaGame, atis);
            if (expected != actual) mismatches.Add($"{typeString}: {expected} vs {actual}");
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void FirstWithName_TwoTypesShareAName_ReturnsTheFirstInListOrder()
    {
        var list = new IndexedTypeList(new[] { typeof(System.Threading.Timer), typeof(System.Timers.Timer) });

        Assert.Equal(typeof(System.Threading.Timer), list.FirstWithName("Timer"));
        Assert.Equal(typeof(System.Timers.Timer), list.FirstWithFullName("System.Timers.Timer"));
        Assert.Null(list.FirstWithName("NotThere"));
    }

    [Fact]
    public void FirstWithNameOrFullName_LaterTypesNameEqualsEarlierTypesFullName_ReturnsTheEarlierType()
    {
        // A scan checks Name or FullName per type in order, so the earliest type matching either wins.
        var list = new IndexedTypeList(new[] { typeof(System.Threading.Timer), typeof(System.Timers.Timer) });

        Assert.Equal(typeof(System.Threading.Timer), list.FirstWithNameOrFullName("Timer"));
        Assert.Equal(typeof(System.Timers.Timer), list.FirstWithNameOrFullName("System.Timers.Timer"));
    }
}
