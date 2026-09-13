using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Instructions.Reflection;

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

        public static MembershipInfo GetMemberMembershipInfo(this EntitySave instance, string memberName)
        {
            for (int i = 0; i < instance.ReferencedFiles.Count; i++)
            {
                if (instance.ReferencedFiles[i].Name == memberName || instance.ReferencedFiles[i].GetInstanceName() == memberName)
                {
                    return MembershipInfo.ContainedInThis;
                }
            }

            MembershipInfo namedObjectMembershipInfo = instance.GetMemberMembershipInfoForNamedObjectList(memberName, instance.NamedObjects);
            if (namedObjectMembershipInfo != MembershipInfo.NotContained)
            {
                return namedObjectMembershipInfo;
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);
                if (baseEntity != null)
                {
                    bool value = baseEntity.HasMemberWithName(memberName);

                    if (value)
                    {
                        return MembershipInfo.ContainedInBase;
                    }
                }
            }

            return MembershipInfo.NotContained;

        }

        public static MembershipInfo GetMemberMembershipInfoForNamedObjectList(this EntitySave instance, string memberName, List<NamedObjectSave> namedObjectList)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                if (namedObjectList[i].FieldName == memberName)
                {
                    return MembershipInfo.ContainedInThis;
                }

                MembershipInfo membershipInfo = instance.GetMemberMembershipInfoForNamedObjectList(memberName, namedObjectList[i].ContainedObjects);

                if (membershipInfo != MembershipInfo.NotContained)
                {
                    return membershipInfo;
                }
            }



            return MembershipInfo.NotContained;
        }

        public static bool HasMemberWithName(this EntitySave instance, string memberName)
        {
            return instance.GetMemberMembershipInfo(memberName) != MembershipInfo.NotContained;
        }


        // InheritsFrom(this EntitySave, string) moved to
        // GlueCommon.SaveClasses.NamedObjectSaveElementExtensions (#2276) - needed by CanBeInList,
        // which moved alongside it. Only reached ObjectFinder.Self.GetEntitySave, so it goes through
        // the existing IObjectFinderCore seam.


        static void AddRangeUnique(this List<TypedMemberBase> listToAddTo, List<TypedMemberBase> whatToAdd)
        {
            foreach (var item in whatToAdd)
            {
                if (!listToAddTo.ContainsMatch(item))
                {
                    listToAddTo.Add(item);
                }
            }

        }

        static void AddUnique(this List<TypedMemberBase> listToAddTo, TypedMemberBase itemToAdd)
        {
            if (!listToAddTo.ContainsMatch(itemToAdd))
            {
                listToAddTo.Add(itemToAdd);
            }
        }

        static bool DoTypedMemberBasesMatch(TypedMemberBase item1, TypedMemberBase item2)
        {
            return item1.MemberName == item2.MemberName &&
                item1.MemberType == item2.MemberType;
        }

        static bool ContainsMatch(this List<TypedMemberBase> listToAddTo, TypedMemberBase itemToCheck)
        {
            foreach (var item in listToAddTo)
            {
                if (DoTypedMemberBasesMatch(item, itemToCheck))
                {
                    return true;
                }
            }

            return false;
        }


        public static List<FlatRedBall.Instructions.Reflection.TypedMemberBase> GetTypedMembers(this EntitySave instance)
        {
            List<TypedMemberBase> typedMembers = new List<TypedMemberBase>();

            foreach(var customVariable in instance.CustomVariables)
            {
                if(customVariable.Scope == Scope.Public || customVariable.Scope == Scope.Internal)
                {
                    string type = customVariable.Type;

                    if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                    {
                        type = customVariable.OverridingPropertyType;
                    }

                    TypedMemberBase typedMemberBase =
                        AssetTypeInfoExtensionMethods.GetTypedMemberBase(
                        type,
                        customVariable.Name);

                    typedMembers.Add(typedMemberBase);
                }
            }

            // Add any variables that are set by container
            for (int i = 0; i < instance.NamedObjects.Count; i++)
            {
                NamedObjectSave nos = instance.NamedObjects[i];

                if (nos.SetByContainer && !string.IsNullOrEmpty(nos.InstanceType))
                {
                    if (nos.SourceType == SourceType.Entity)
                    {
                        TypedMemberBase typedMemberBase = TypedMemberBase.GetTypedMember(nos.InstanceName, typeof(string));
                        typedMembers.Add(typedMemberBase);
                    }
                    else
                    {
                        if (!nos.IsList)
                        {
                            TypedMemberBase typedMemberBase =
                            AssetTypeInfoExtensionMethods.GetTypedMemberBase(
                                nos.InstanceType,
                                nos.InstanceName);

                            typedMembers.Add(typedMemberBase);
                        }
                    }
                }
            }

            // all categorized states should be typed too, even if they are not added as variables. Need to check
            foreach(var category in instance.StateCategoryList)
            {
                var name = $"Current{category.Name}State";
                var type = category.Name;
                var alreadyContains = typedMembers.Any(item => item.MemberName == name && item.CustomTypeName == type);
                if(!alreadyContains)
                {
                    var typedMember = AssetTypeInfoExtensionMethods.GetTypedMemberBase(
                                type,
                                name);
                    typedMembers.Add(typedMember);
                }
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(
                    instance.BaseEntity);

                // This may be null if the project improperly references
                // an EntitySave that really doesn't exist.
                if (entitySave != null)
                {
                    // We used to call "AddRange" but we don't want duplicates 
                    // (I don't think) so we're going to use the custom extension
                    // method to prevent duplicates:
                    //typedMembers.AddRange(entitySave.GetTypedMembers());
                    typedMembers.AddRangeUnique(entitySave.GetTypedMembers());
                }
            }

            return typedMembers;
        }

    }
}
