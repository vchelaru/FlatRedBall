using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using GlueCommonUnitTests.Parsing;
using GlueCommonUnitTests.SaveClasses;

namespace GlueCommonUnitTests.Elements;

// Reads/writes the shared static TypeResolutionCore.Self - reuses ObjectFinderCoreCollection (rather
// than defining a new collection) so this is also serialized against every other test class that
// swaps out one of these Core.Self statics, not just this one.
[Collection(nameof(ObjectFinderCoreCollection))]
public class AssetTypeInfoExtensionMethodsTests
{
    readonly FakeTypeResolutionCore _typeResolution = new();

    public AssetTypeInfoExtensionMethodsTests()
    {
        TypeResolutionCore.Self = _typeResolution;
    }

    [Fact]
    public void GetTypedMemberBase_KnownType_ReturnsTypedMemberWithResolvedType()
    {
        _typeResolution.AddType("int", typeof(int));

        var result = AssetTypeInfoExtensionMethods.GetTypedMemberBase("int", "MyField");

        Assert.Equal("MyField", result.MemberName);
        Assert.Equal(typeof(int), result.MemberType);
    }

    [Fact]
    public void GetTypedMemberBase_UnknownType_SetsCustomTypeName()
    {
        var result = AssetTypeInfoExtensionMethods.GetTypedMemberBase("SomeCustomType", "MyField");

        Assert.Equal("MyField", result.MemberName);
        Assert.Equal("SomeCustomType", result.CustomTypeName);
    }

    [Fact]
    public void QualifyBaseType_KnownType_ReturnsFullName()
    {
        _typeResolution.AddType("int", typeof(int));

        Assert.Equal(typeof(int).FullName, AssetTypeInfoExtensionMethods.QualifyBaseType("int"));
    }

    [Fact]
    public void QualifyBaseType_UnknownType_ReturnsInputUnchanged()
    {
        Assert.Equal("SomeCustomType", AssetTypeInfoExtensionMethods.QualifyBaseType("SomeCustomType"));
    }
}
