namespace FlatRedBall.Glue.SaveClasses
{
    public static class EntitySaveExtensionMethods
    {
        // GetRootBaseEntitySave, GetImplementsIWindowRecursively, GetImplementsIVisibleRecursively,
        // GetImplementsIClickableRecursively, GetImplementsITiledTileMetadataRecursively,
        // GetInheritsFromIWindow, GetInheritsFromITiledTileMetadata, GetHasImplementsCollidableProperty,
        // GetInheritsFromIVisible, GetInheritsFromIClickable, GetInheritsFromIWindowOrIClickable,
        // BaseElements, and both GetAllBaseEntities overloads moved to
        // GlueCommon.SaveClasses.EntitySaveInheritanceExtensions (#2276) - all only needed the existing
        // IObjectFinderCore/IAvailableAssetTypesCore seams over ObjectFinder.Self/AvailableAssetTypes.Self.
        // GetMemberMembershipInfo, GetMemberMembershipInfoForNamedObjectList, and HasMemberWithName moved
        // there too - unblocked once ReferencedFileSave.GetInstanceName() (which GetMemberMembershipInfo
        // calls) moved to GlueCommon.SaveClasses.ReferencedFileSaveElementExtensions.

        // InheritsFrom(this EntitySave, string) moved to
        // GlueCommon.SaveClasses.NamedObjectSaveElementExtensions (#2276) - needed by CanBeInList,
        // which moved alongside it. Only reached ObjectFinder.Self.GetEntitySave, so it goes through
        // the existing IObjectFinderCore seam.

        // GetTypedMembers (and its private AddRangeUnique/AddUnique/DoTypedMemberBasesMatch/ContainsMatch
        // helpers) moved there too (#2276) - unblocked once TypeManager.GetTypeFromString (which
        // AssetTypeInfoExtensionMethods.GetTypedMemberBase calls) got its own ITypeResolutionCore seam.
    }
}
