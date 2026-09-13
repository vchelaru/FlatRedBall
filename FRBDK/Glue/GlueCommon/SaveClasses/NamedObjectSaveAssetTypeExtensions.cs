using System;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>NamedObjectSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// this method is pure logic over a <see cref="NamedObjectSave"/>, but needs to resolve an
    /// <see cref="AssetTypeInfo"/> from a runtime type, so it depends on
    /// <see cref="IAvailableAssetTypesCore"/> (a narrow seam over <c>AvailableAssetTypes.Self</c>, see
    /// that interface's doc comment) instead of reaching for <c>AvailableAssetTypes.Self</c> directly.
    /// Lives here (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276.
    /// Named differently from the original class (not a forwarding stub) to avoid a duplicate-type
    /// clash now that both assemblies are visible together via <c>Glue.csproj</c>'s
    /// <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution doesn't care which
    /// class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class NamedObjectSaveAssetTypeExtensions
    {
        public static AssetTypeInfo GetAssetTypeInfo(this NamedObjectSave instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (instance.SourceType == SourceType.Entity)
            {
                return null;
            }
            if (string.IsNullOrEmpty(instance.ClassType))
            {
                return null;
            }
            // This is a common type, so let's go faster by returning the type:
            if (instance.SourceType == SourceType.FlatRedBallType && instance.SourceClassType.StartsWith("FlatRedBall.Math.PositionedObjectList"))
            {
                return AvailableAssetTypesCore.Self.PositionedObjectList;
            }

            // If this NOS uses an EntireFile, then we should ask the file for its AssetTypeInfo,
            // as there may be multiple file types that produce the same class type.
            // For example
            AssetTypeInfo returnAti = null;


            if (instance.IsEntireFile)
            {
                var container = instance.GetContainer();

                var rfs = container?.GetReferencedFileSave(instance.SourceFile);

                if (rfs != null)
                {
                    var candidateAti = rfs.GetAssetTypeInfo();

                    // The user may use a file, but may change the runtime type through the
                    // SourceName property, so we need to make sure they match:
                    if (candidateAti != null && candidateAti.RuntimeTypeName == instance.ClassType)
                    {
                        returnAti = candidateAti;
                    }
                }
            }

            if (returnAti == null)
            {
                returnAti =
                    // September 14, 2022
                    // We used to check only
                    // ClassType. Let's check
                    // both class type and name
                    // in case the ATI is qualified:
                    AvailableAssetTypesCore.Self.GetAssetTypeFromRuntimeType(instance.ClassType, instance, isObject: true);

                if (returnAti == null && instance.SourceClassType != null)
                {
                    returnAti = AvailableAssetTypesCore.Self.GetAssetTypeFromRuntimeType(instance.SourceClassType, instance, isObject: true);
                }
            }

            if (returnAti == null && instance.IsList)
            {
                return AvailableAssetTypesCore.Self.PositionedObjectList;
            }
            else
            {
                // Vic says: I don't think this should throw an exception anymore
                //if (returnAti == null)
                //{
                //    throw new InvalidOperationException("You probably need to add the class type " + this.ClassType +
                //        " to the ContentTypes.csv");
                //}

                return returnAti;
            }
        }

        public static bool CanBeInShapeCollection(this NamedObjectSave instance)
        {
            var ati = instance.GetAssetTypeInfo();
            var isOfCorrectType = instance.SourceType == SourceType.FlatRedBallType &&
                (
                    ati == AvailableAssetTypesCore.Self.CapsulePolygon ||
                    ati == AvailableAssetTypesCore.Self.Circle ||
                    ati == AvailableAssetTypesCore.Self.AxisAlignedRectangle ||
                    ati == AvailableAssetTypesCore.Self.Polygon
                );

            return isOfCorrectType;
        }

        public static bool ShouldInstantiateInConstructor(this NamedObjectSave namedObjectSave)
        {
            return
                (namedObjectSave.IsList || namedObjectSave.GetAssetTypeInfo() == AvailableAssetTypesCore.Self.ShapeCollection) &&
                namedObjectSave.Instantiate &&
                !namedObjectSave.InstantiatedByBase;
        }
    }
}
