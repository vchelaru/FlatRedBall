using System;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Moved out of Glue.csproj into GlueCommon (#2276): both methods only needed
    /// <see cref="TypeResolutionCore"/>, the seam over <c>TypeManager.GetTypeFromString</c> - the
    /// resolution half #2279 originally found coupled to <c>PluginManager</c>/<c>AvailableAssetTypes</c>/
    /// <c>DialogService</c>. That coupling now lives entirely behind the seam (see
    /// <c>ITypeResolutionCore</c>'s doc comment), so these two callers no longer need to.
    /// </summary>
    public static class AssetTypeInfoExtensionMethods
    {
        public static TypedMemberBase GetTypedMemberBase(string typeString, string memberName)
        {
            // make the typeString proper
            Type type = TypeResolutionCore.Self.GetTypeFromString(typeString);

            // At one time this was using GetTypedMember, but I don't know why we need them
            // to be equatable
            //TypedMemberBase typedMemberBase = TypedMemberBase.GetTypedMember(memberName, type);
            TypedMemberBase typedMemberBase = TypedMemberBase.GetTypedMemberUnequatable(memberName, type);
            // Sept 18, 2021
            // Vic says - I would
            // like to stop using the
            // Type object since this requires
            // that Glue understands the compiled
            // Types. Therefore, I'm going to specify
            // the custom type here if the Type is null:
            if(type == null)
            {
                typedMemberBase.CustomTypeName = typeString;
            }
            return typedMemberBase;
        }

        public static string QualifyBaseType(string typeString)
        {
            Type type = TypeResolutionCore.Self.GetTypeFromString(typeString);

            if (type != null)
            {
                return type.FullName;
            }
            else
            {
                return typeString;
            }
        }
    }
}
