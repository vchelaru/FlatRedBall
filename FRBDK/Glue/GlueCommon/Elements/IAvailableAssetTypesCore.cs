using System.Collections.Generic;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Seam over Glue.csproj's <c>AvailableAssetTypes.Self</c>, covering only the members the
    /// GlueCommon extractions in #2276 need to resolve an <see cref="AssetTypeInfo"/> from a runtime
    /// type or file extension - something only <c>AvailableAssetTypes</c> (Glue.csproj,
    /// net8.0-windows) can do today, since it holds the loaded ContentTypes.csv data. GlueCommon
    /// can't reference <c>AvailableAssetTypes</c> directly (wrong direction - Glue.csproj references
    /// GlueCommon, not the reverse), so the real <c>AvailableAssetTypes</c> implements this interface
    /// and wires itself into <see cref="AvailableAssetTypesCore.Self"/> from its own static
    /// constructor. Same pattern as <c>IObjectFinderCore</c>/<c>ObjectFinderCore</c> - see that
    /// interface's doc comment and #2276's third comment.
    /// </summary>
    public interface IAvailableAssetTypesCore
    {
        AssetTypeInfo PositionedObjectList { get; }
        AssetTypeInfo CapsulePolygon { get; }
        AssetTypeInfo Circle { get; }
        AssetTypeInfo AxisAlignedRectangle { get; }
        AssetTypeInfo Polygon { get; }
        AssetTypeInfo Line { get; }
        AssetTypeInfo ShapeCollection { get; }
        AssetTypeInfo Screen { get; }
        IEnumerable<AssetTypeInfo> AllAssetTypes { get; }
        AssetTypeInfo GetAssetTypeFromRuntimeType(string runtimeType, object callingObject, bool? isObject = null);
        AssetTypeInfo GetAssetTypeFromExtension(string extension);
        AssetTypeInfo GetAssetTypeFromExtensionAndQualifiedRuntime(string extension, string qualifiedRuntimeType);
    }

    public static class AvailableAssetTypesCore
    {
        public static IAvailableAssetTypesCore Self { get; set; }
    }
}
