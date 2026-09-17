using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Parsing;
using L = Localization;

namespace FlatRedBall.Glue.SaveClasses;

public static class IElementExtensionMethods
{

    static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
    static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


    // GetReferencedFileSaveRecursively (both overloads), GetAllReferencedFileSavesRecursively,
    // GetReferencedFileSaveByInstanceName, and GetReferencedFileSaveByInstanceNameRecursively moved to
    // GlueCommon.SaveClasses.ElementExtensions (#2276) - GlueState.CurrentGlueProject is just
    // ObjectFinder.Self.GlueProject (see GlueState.CurrentGlueProject's own getter), so they route
    // through the existing IObjectFinderCore seam instead.

    // GetCustomVariableRecursively moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - only
    // needed the existing IObjectFinderCore seam over ObjectFinder.Self.GetElement.

    // GetVariableValueRecursively moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - needed
    // IObjectFinderCore widened with GetValueRecursively and GetBaseElement (both already public on
    // ObjectFinder); TypeManager.Parse is a pure TypeConversion.Parse forwarder.


    // GetCustomVariablesToBeSetByDerived, ContainsCustomVariable, and ContainsCustomVariableRecursively
    // moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - GlueState.CurrentGlueProject is just
    // ObjectFinder.Self.GlueProject (see GlueState.CurrentGlueProject's own getter), so they route
    // through the existing IObjectFinderCore seam instead.


    // PostLoadInitialize moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - zero coupling
    // once CustomVariable.FixEnumerationTypes and NamedObjectSave.PostLoadLogic had moved.


    public static ReferencedFileSave AddReferencedFile(this IElement instance, string fileName, AssetTypeInfo ati, EditorObjects.SaveClasses.BuildToolAssociation bta = null)
    {

        var referencedFileSave = new ReferencedFileSave();
        referencedFileSave.DestroyOnUnload = true;

        if (ati != null)
        {
            referencedFileSave.RuntimeType = ati.QualifiedRuntimeTypeName.QualifiedType;
            if(ati.MustBeAddedToContentPipeline)
            {
                referencedFileSave.UseContentPipeline = true;
            }
        }




        referencedFileSave.IsSharedStatic = true;

        referencedFileSave.SetNameNoCall(fileName);

        if (ati != null && !string.IsNullOrEmpty(ati.CustomBuildToolName) && bta != null)
        {
            referencedFileSave.BuildTool = ati.CustomBuildToolName;

            referencedFileSave.SourceFile = referencedFileSave.Name;

            string newName = FileManager.RemoveExtension(referencedFileSave.Name);
            newName += "." + bta.DestinationFileType;

            referencedFileSave.SetNameNoCall(newName);
        }

        instance.ReferencedFiles.Add(referencedFileSave);


        referencedFileSave.IsSharedStatic = true;


        return referencedFileSave;
    }

    // GetEventsOnVariable moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - zero coupling.

    // FixAllTypes, FixEnumerationValues, and ConvertEnumerationValuesToInts moved to
    // GlueCommon.SaveClasses.ElementExtensions (#2276) - zero coupling once CustomVariable.FixAllTypes
    // had moved (it routes PluginManager through the IPluginManagerCore seam).


    // GetState, GetStateRecursively, GetUncategorizedState, GetUncategorizedStateRecursively,
    // GetUncategorizedStatesRecursively, GetStateCategory, GetStateCategoryRecursively, and
    // DefinesCategoryEnumRecursive moved to GlueCommon.SaveClasses.ElementExtensions (#2276) -
    // GlueState.CurrentGlueProject is just ObjectFinder.Self.GlueProject (see
    // GlueState.CurrentGlueProject's own getter), so they route through the existing IObjectFinderCore
    // seam instead.

    // GetAllNamedObjectsRecurisvely moved to GlueCommon.SaveClasses.ElementExtensions (#2276) - only
    // needed the existing IObjectFinderCore seam over ObjectFinder.Self.GetAllBaseElementsRecursively.

    // GetQualifiedName, InheritsFromElement, InheritsFromEntity, InheritsFromFrbType, and
    // GetAssetTypeInfo(this IElement) moved to GlueCommon.SaveClasses.ElementExtensions (#2276) -
    // GetAssetTypeInfo needed IAvailableAssetTypesCore widened with a Screen property
    // (AvailableAssetTypes.CommonAtis.Screen), same pattern as the seam's other members; the rest were
    // already zero-coupling or only needed the existing IObjectFinderCore seam.
}
