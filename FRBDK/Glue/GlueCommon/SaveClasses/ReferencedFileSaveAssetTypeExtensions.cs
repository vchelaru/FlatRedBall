using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.Linq;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ReferencedFileSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// these methods are pure logic over a <see cref="ReferencedFileSave"/>, but need to resolve an
    /// <see cref="AssetTypeInfo"/> from a runtime type or file extension, so they depend on
    /// <see cref="IAvailableAssetTypesCore"/> (a narrow seam over <c>AvailableAssetTypes.Self</c>, see
    /// that interface's doc comment) instead of reaching for <c>AvailableAssetTypes.Self</c> directly.
    /// <c>GetCanUseContentPipeline</c>/<c>GetGeneratesMember</c> only call the seam indirectly, through
    /// <see cref="GetAssetTypeInfo"/>. Lives here (net8.0, no WPF) so it and its tests can build and run
    /// on Linux/macOS. See #2276. Named differently from the original class (not a forwarding stub) to
    /// avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ReferencedFileSaveAssetTypeExtensions
    {
        /// <summary>
        /// Returns the associated AssetTypeInfo for the ReferencedFileSave. If the ReferencedFileSave
        /// specifies a runtime type, then this will return the AssetTypeInfo for that runtime type. Otherwise
        /// the AssetTypeInfo for the extension will be returned.
        /// </summary>
        /// <param name="referencedFileSave">The argument ReferencedFileSave</param>
        /// <returns>The AssetTypeInfo for the argument ReferencedFileSave.</returns>
        public static AssetTypeInfo GetAssetTypeInfo(this ReferencedFileSave referencedFileSave)
        {
            string extension = FileManager.GetExtension(referencedFileSave.Name);

            if (!string.IsNullOrEmpty(referencedFileSave.RuntimeType))
            {
                // try finding one based on extension and type. If that doesn't exist, then just look at type

                var found = AvailableAssetTypesCore.Self.GetAssetTypeFromExtensionAndQualifiedRuntime(
                    extension, referencedFileSave.RuntimeType);

                if (found == null)
                {
                    found = AvailableAssetTypesCore.Self.AllAssetTypes.FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == referencedFileSave.RuntimeType);
                }

                return found;
            }
            else
            {
                return AvailableAssetTypesCore.Self.GetAssetTypeFromExtension(extension);
            }
        }

        public static bool GetCanUseContentPipeline(this ReferencedFileSave instance)
        {
            var assetTypeInfo = instance.GetAssetTypeInfo();
            return
                // CSVs can use content pipeline
                // Update 1/29/2020 - no it can't:
                //instance.IsCsvOrTreatedAsCsv ||

                (!string.IsNullOrEmpty(assetTypeInfo?.ContentProcessor));
        }

        public static bool GetGeneratesMember(this ReferencedFileSave instance)
        {
            bool toReturn = instance.LoadedAtRuntime && !instance.IsDatabaseForLocalizing;

            if (!instance.IsCsvOrTreatedAsCsv)
            {
                var ati = instance.GetAssetTypeInfo();

                if (ati != null &&
                    string.IsNullOrEmpty(ati.QualifiedRuntimeTypeName.QualifiedType))
                {
                    return false;
                }
            }

            return toReturn;
        }
    }
}
