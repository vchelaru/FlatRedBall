using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class IElementExtensionMethods
    {

        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();

        public static ReferencedFileSave GetReferencedFileSaveRecursively(this IElement instance, string fileName)
        {
            ReferencedFileSave rfs = FileReferencerHelper.GetReferencedFileSave(instance, fileName);

            if (rfs == null && !string.IsNullOrEmpty(instance.BaseObject))
            {
                EntitySave baseEntitySave = GlueState.CurrentGlueProject.GetEntitySave(instance.BaseObject);
                if (baseEntitySave != null)
                {
                    rfs = baseEntitySave.GetReferencedFileSaveRecursively(fileName);
                }
            }

            return rfs;
        }

        public static IEnumerable<ReferencedFileSave> GetAllReferencedFileSavesRecursively(this IElement instance)
        {
            foreach (ReferencedFileSave rfs in instance.ReferencedFiles)
            {
                yield return rfs;
            }

            if (!string.IsNullOrEmpty(instance.BaseElement))
            {

                IElement baseElement = GlueState.CurrentGlueProject.GetElement(instance.BaseElement);
                if (baseElement != null)
                {
                    foreach (ReferencedFileSave rfs in baseElement.GetAllReferencedFileSavesRecursively())
                    {
                        yield return rfs;
                    }
                }
            }

        }

        public static ReferencedFileSave GetReferencedFileSaveByInstanceName(this IElement element, string instanceName, bool caseSensitive = true)
        {
            if (!string.IsNullOrEmpty(instanceName))
            {
                foreach (ReferencedFileSave rfs in element.ReferencedFiles)
                {
                    var rfsInstanceName = rfs.GetInstanceName();
                    var matches = (caseSensitive && rfsInstanceName == instanceName) ||
                        (!caseSensitive && rfsInstanceName.Equals(instanceName, StringComparison.InvariantCultureIgnoreCase));
                    if (matches)
                    {
                        return rfs;
                    }
                }
            }
            return null;
        }


        public static ReferencedFileSave GetReferencedFileSaveByInstanceNameRecursively(this IElement element, string instanceName, bool caseSensitive = true)
        {
            ReferencedFileSave rfs = element.GetReferencedFileSaveByInstanceName(instanceName, caseSensitive);

            if (rfs == null && !string.IsNullOrEmpty(element.BaseElement))
            {
                EntitySave baseEntitySave = GlueState.CurrentGlueProject.GetEntitySave(element.BaseElement);

                if (baseEntitySave != null)
                {
                    rfs = baseEntitySave.GetReferencedFileSaveByInstanceNameRecursively(instanceName, caseSensitive);
                }
            }

            return rfs;

        }


        public static CustomVariable GetCustomVariableRecursively(this IElement container, string variableName)
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
            CustomVariable foundVariable = container.GetCustomVariable(variableName);

            if (foundVariable != null)
            {
                return foundVariable;
            }
            else
            {
                if (!string.IsNullOrEmpty(container.BaseObject))
                {
                    IElement element = GlueState.CurrentGlueProject.GetElement(container.BaseObject);

                    if (element != null)
                    {
                        foundVariable = GetCustomVariableRecursively(element, variableName);
                    }
                }

                return foundVariable;
            }
        }


        public static List<CustomVariable> GetCustomVariablesToBeSetByDerived(this IElement element)
        {
            List<CustomVariable> customVariablesToBeSetByDerived = new List<CustomVariable>();

            if (!string.IsNullOrEmpty(element.BaseObject) && element.BaseObject != "<NONE>")
            {
                IElement elementBase = GlueState.CurrentGlueProject.GetElement(element.BaseObject);

                if (elementBase == null)
                {
                    if (!element.InheritsFromFrbType())
                    {
                        throw new Exception("The object\n\n" + element.ToString() + "\n\nhas a base type of\n\n" +
                            element.BaseObject + "\n\nbut this type can't be found.  This probably happened if the base type was " +
                            "removed from the project.  You will want to set the base type to NONE");
                    }
                }
                else
                {
                    customVariablesToBeSetByDerived.AddRange(elementBase.GetCustomVariablesToBeSetByDerived());
                }
            }

            foreach (CustomVariable cv in element.CustomVariables)
            {
                if (cv.SetByDerived)
                {
                    customVariablesToBeSetByDerived.Add(cv);
                }
            }

            return customVariablesToBeSetByDerived;
        }

        public static bool ContainsCustomVariable(this IElement container, string variableName)
        {
            for (int i = 0; i < container.CustomVariables.Count; i++)
            {
                if (container.CustomVariables[i].Name == variableName)
                {
                    return true;
                }
            }
            return false;
        }


        public static void PostLoadInitialize(this IElement element)
        {
            foreach (CustomVariable cv in element.CustomVariables)
            {
                cv.FixEnumerationTypes();
            }
            foreach (NamedObjectSave nos in element.AllNamedObjects)
            {
                nos.PostLoadLogic();
            }
            
        }


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

        public static List<EventResponseSave> GetEventsOnVariable(this IElement instance, string variableName)
        {
            List<EventResponseSave> toReturn = new List<EventResponseSave>();
            foreach (EventResponseSave eventSave in instance.Events)
            {
                if (eventSave.SourceVariable == variableName)
                {
                    toReturn.Add(eventSave);
                }
            }
            return toReturn;
        }

        public static void FixAllTypes(this IElement instance)
        {
            foreach (NamedObjectSave nos in instance.NamedObjects)
            {
                nos.FixAllTypes();
            }
            foreach (StateSave state in instance.AllStates)
            {
                state.FixAllTypes();
            }
            foreach (CustomVariable customVariable in instance.CustomVariables)
            {
                customVariable.FixAllTypes();
            }
        }

        public static void FixEnumerationValues(this IElement instance)
        {

            foreach (NamedObjectSave nos in instance.NamedObjects)
            {
                nos.FixEnumerationTypes();
            }
            foreach (StateSave state in instance.AllStates)
            {
                state.FixEnumerationTypes();
            }
            foreach (CustomVariable customVariable in instance.CustomVariables)
            {
                customVariable.FixEnumerationTypes();
            }
        }

        public static void ConvertEnumerationValuesToInts(this IElement instance)
        {
            foreach (NamedObjectSave nos in instance.NamedObjects.ToList())
            {
                nos.ConvertEnumerationValuesToInts();
            }
            foreach (StateSave state in instance.AllStates)
            {
                state.ConvertEnumerationValuesToInts();
            }
            foreach (CustomVariable customVariable in instance.CustomVariables)
            {
                customVariable.ConvertEnumerationValuesToInts();
            }
        }


        public static StateSave GetState(this IElement element, string stateName, string categoryName = null)
        {
            if (string.IsNullOrEmpty(categoryName) || categoryName == "Uncategorized")
            {
                foreach (StateSave state in element.States)
                {
                    if (state.Name == stateName)
                    {
                        return state;
                    }
                }
            }

            foreach (StateSaveCategory category in element.StateCategoryList)
            {
                if (  string.IsNullOrEmpty(categoryName) || categoryName == category.Name)
                {
                    foreach (StateSave state in category.States)
                    {
                        if (state.Name == stateName)
                        {
                            return state;
                        }
                    }
                }
            }

            return null;

        }

        public static StateSave GetStateRecursively(this IElement element, string stateName, string categoryName = null)
        {
            StateSave stateSave = element.GetState(stateName, categoryName);

            if (stateSave != null)
            {
                return stateSave;
            }
            else if (stateSave == null && !string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    return GetStateRecursively(baseElement, stateName, categoryName);
                }
            }

            return null;
        }
        public static StateSave GetUncategorizedState(this IElement element, string stateName)
        {
            foreach (StateSave state in element.States)
            {
                if (state.Name == stateName)
                {
                    return state;
                }
            }
            return null;
        }

        public static StateSave GetUncategorizedStateRecursively(this IElement element, string stateName)
        {
            StateSave foundStateSave = element.GetUncategorizedState(stateName);

            if (foundStateSave == null && !string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    return baseElement.GetUncategorizedStateRecursively(stateName);
                }
            }

            return foundStateSave;
        }

        public static List<StateSave> GetUncategorizedStatesRecursively(this IElement element)
        {
            // We'll start at the top and move down so that derived types can override baset types....not sure if this is going to eventually change
            IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

            if(baseElement == null || element.States.Count != 0)
            {
                return element.States;
            }
            else
            {
                return baseElement.GetUncategorizedStatesRecursively();
            }

        }

        public static StateSaveCategory GetStateCategory(this IElement element, string stateCategoryName)
        {
            return element.StateCategoryList.FirstOrDefault(stateCategory => stateCategory.Name == stateCategoryName);
        }

        public static StateSaveCategory GetStateCategoryRecursively(this IElement element, string stateCategoryName)
        {
            // start at the top-down
            StateSaveCategory category = element.GetStateCategory(stateCategoryName);

            if (category == null && !string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    return baseElement.GetStateCategoryRecursively(stateCategoryName);
                }
            }


            return category;

        }

        public static bool DefinesCategoryEnumRecursive(this IElement element, string enumType)
        {
            bool uses = false;
            if (enumType == "VariableState")
            {
                uses = element.States.Count != 0;

                if (!uses)
                {
                    uses = element.StateCategoryList.Count(item => { return item.States.Count != 0 && item.SharesVariablesWithOtherCategories; }) != 0;
                }
            }
            else
            {
                uses = element.StateCategoryList.Count(item => { return item.SharesVariablesWithOtherCategories == false && item.Name == enumType; }) != 0;
            }

            if (!uses && !string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    uses = baseElement.DefinesCategoryEnumRecursive(enumType);
                }
            }

            return uses;
        }

        /// <summary>
        /// Returns all named objects contained in this object (both single and objects in lists) as well
        /// as all named objects in base elements.
        /// </summary>
        /// <param name="element">Element named object container.</param>
        /// <returns>All named objects.</returns>
        public static IEnumerable<NamedObjectSave> GetAllNamedObjectsRecurisvely(this IElement element)
        {
            if (element != null)
            {
                foreach (NamedObjectSave nos in element.AllNamedObjects)
                {
                    yield return nos;
                }

                if (!string.IsNullOrEmpty(element.BaseElement))
                {
                    IElement baseElement = GlueState.CurrentGlueProject.GetElement(element.BaseElement);

                    if (baseElement != null)
                    {
                        foreach (NamedObjectSave nos in baseElement.GetAllNamedObjectsRecurisvely())
                        {
                            yield return nos;
                        }
                    }
                }
            }
        }

        public static string GetQualifiedName(this IElement element, string projectName)
        {
            return projectName + '.' + element.Name.Replace('\\', '.');
        }

        public static bool InheritsFromElement(this IElement element)
        {
            return element.BaseElement != null &&
                (element.BaseElement.ToLowerInvariant().Replace('\\', '/').StartsWith("entities/") ||
                 element.BaseElement.ToLowerInvariant().Replace('\\', '/').StartsWith("screens/"));
            
        }

        public static bool InheritsFromEntity(this IElement element)
        {
            if (element is ScreenSave)
            {
                return false;
            }
            else
            {
                return element.BaseElement != null &&
                    element.BaseElement.ToLowerInvariant().Replace('\\', '/').StartsWith("entities/");
            }
        }

        public static bool InheritsFromFrbType(this IElement element)
        {
            if (element is ScreenSave)
            {
                return false;
            }
            else
            {
                return !string.IsNullOrEmpty(element.BaseElement) &&
                    element.BaseElement.ToLower().Replace('\\', '/').StartsWith("entities/") == false;
            }
        }
    }
}
