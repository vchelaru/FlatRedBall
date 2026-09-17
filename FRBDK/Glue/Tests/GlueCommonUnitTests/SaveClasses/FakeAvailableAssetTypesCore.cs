using FlatRedBall.Glue.Elements;

namespace GlueCommonUnitTests.SaveClasses;

/// <summary>
/// Hand-rolled test double for <see cref="IAvailableAssetTypesCore"/> - the real implementation
/// (<c>AvailableAssetTypes</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeAvailableAssetTypesCore : IAvailableAssetTypesCore
{
    public AssetTypeInfo PositionedObjectList { get; set; }
    public AssetTypeInfo CapsulePolygon { get; set; }
    public AssetTypeInfo Circle { get; set; }
    public AssetTypeInfo AxisAlignedRectangle { get; set; }
    public AssetTypeInfo Polygon { get; set; }
    public AssetTypeInfo Line { get; set; }
    public AssetTypeInfo ShapeCollection { get; set; }
    public AssetTypeInfo Screen { get; set; }

    readonly List<AssetTypeInfo> _assetTypes = new();

    public IEnumerable<AssetTypeInfo> AllAssetTypes => _assetTypes;

    public void AddAssetType(AssetTypeInfo assetTypeInfo) => _assetTypes.Add(assetTypeInfo);

    public AssetTypeInfo GetAssetTypeFromRuntimeType(string runtimeType, object callingObject, bool? isObject = null) =>
        _assetTypes.FirstOrDefault(item =>
            item.RuntimeTypeName == runtimeType || item.QualifiedRuntimeTypeName.QualifiedType == runtimeType);

    public AssetTypeInfo GetAssetTypeFromExtension(string extension) =>
        _assetTypes.FirstOrDefault(item => item.Extension == extension);

    public AssetTypeInfo GetAssetTypeFromExtensionAndQualifiedRuntime(string extension, string qualifiedRuntimeType) =>
        _assetTypes.FirstOrDefault(item =>
            item.Extension == extension && item.QualifiedRuntimeTypeName.QualifiedType == qualifiedRuntimeType);
}
