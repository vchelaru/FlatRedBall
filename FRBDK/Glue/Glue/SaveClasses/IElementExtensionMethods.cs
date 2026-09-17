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

    /// <summary>
    /// Gets the value of the argument variable recurisvely, checking "this" first, then the base elements. If this variable
    /// is on a NamedObjectSave, this searches the NamedObjectSave source type recursively too.
    /// </summary>
    /// <param name="element">The element owning the variable.</param>
    /// <param name="variableName">The variable name.</param>
    /// <returns>The value found recursively.</returns>
    public static object GetVariableValueRecursively(this GlueElement element, string variableName)
    {
        //////////////////////Early Out///////////////////////////////////
        if (string.IsNullOrEmpty(variableName))
        {
            return null;
        }
        ////////////////////End Early Out//////////////////////////

        if (variableName.StartsWith("this."))
        {
            variableName = variableName.Substring("this.".Length);

        }
        var variable = element.GetCustomVariable(variableName);

        object toReturn = null;
        bool foundValue = false;

        if (!foundValue && variable?.DefaultValue != null)
        {
            toReturn = variable.DefaultValue;
            foundValue = true;
        }

        if(!foundValue && !string.IsNullOrEmpty(variable?.SourceObject))
        {
            var nos = element.GetNamedObjectRecursively(variable.SourceObject);

            if(nos != null)
            {
                var value = ObjectFinder.Self.GetValueRecursively(nos, element, variable.SourceObjectProperty);

                foundValue = value != null;
                toReturn = value;
            }
        }

        if (!foundValue)
        {
            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                var baseElement = ObjectFinder.Self.GetBaseElement(element);

                if (baseElement != null)
                {
                    toReturn = GetVariableValueRecursively(baseElement, variableName);
                    foundValue = toReturn != null;
                }
            }
        }

        if (!foundValue)
        {
            var ati = element.GetAssetTypeInfo();
            if (ati != null)
            {
                var variableDefinition = ati.VariableDefinitions.FirstOrDefault(x => x.Name == variableName);
                toReturn = variableDefinition?.GetCastedDefaultValue();
                foundValue = toReturn != null;
            }
        }

        // special case - if the element is IVisible, and if the variable is Visible, then FRB will generate its default
        // as true, so return that to match what is code genned:
        if(variableName == "Visible" && element is EntitySave entity && entity.ImplementsIVisible)
        {
            toReturn = true;
            foundValue = true;
        }

        if (!foundValue && variable != null)
        {
            // get the default value for the type:
            // Could use the TypeManager and get full coverage but that is HEAVY and requires some (potentially) expensive conversions.
            // Therefore, just use the quick-n-dirty VariableDefinition
            toReturn = TypeManager.Parse(variable.Type, null);

        }
        return toReturn;
    }


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
