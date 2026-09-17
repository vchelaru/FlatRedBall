using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>IElementExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): these
    /// methods are pure logic over an <see cref="IElement"/>/<see cref="GlueElement"/>, needing at most
    /// <see cref="IObjectFinderCore"/>/<see cref="IAvailableAssetTypesCore"/> (narrow seams over
    /// <c>ObjectFinder.Self</c>/<c>AvailableAssetTypes.Self</c>, see those interfaces' doc comments) in
    /// place of the singletons the original reached directly (including
    /// <c>GlueState.Self.CurrentGlueProject</c>, which is just <c>ObjectFinder.Self.GlueProject</c> - see
    /// <c>GlueState.CurrentGlueProject</c>'s own getter). Lives here (net8.0, no WPF) so it and its tests
    /// can build and run on Linux/macOS. See issue #2276. Named differently from the original class (not
    /// a forwarding stub) to avoid a duplicate-type clash now that both assemblies are visible together
    /// via <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ElementExtensions
    {
        /// <summary>
        /// Returns the CustomVarible by the argument name. If not found, this will search the base element recursively.
        /// </summary>
        /// <param name="element">The element to search</param>
        /// <param name="variableName">The variable name to search for</param>
        /// <returns>The found variable</returns>
        public static CustomVariable GetCustomVariableRecursively(this GlueElement element, string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return null;
            }

            if (variableName.StartsWith("this."))
            {
                variableName = variableName.Substring("this.".Length);
            }
            CustomVariable foundVariable = element.GetCustomVariable(variableName);

            if (foundVariable != null)
            {
                return foundVariable;
            }
            else
            {
                if (!string.IsNullOrEmpty(element.BaseObject))
                {
                    var baseElement = ObjectFinderCore.Self.GetElement(element.BaseObject);

                    if (baseElement != null)
                    {
                        foundVariable = GetCustomVariableRecursively(baseElement, variableName);
                    }
                }

                return foundVariable;
            }
        }

        public static List<CustomVariable> GetCustomVariablesToBeSetByDerived(this IElement element)
        {
            var customVariablesToBeSetByDerived = new List<CustomVariable>();

            if (!string.IsNullOrEmpty(element.BaseObject) && element.BaseObject != "<NONE>")
            {
                IElement elementBase = ObjectFinderCore.Self.GetElement(element.BaseObject);

                if (elementBase == null)
                {
                    if (!element.InheritsFromFrbType())
                    {
                        throw new Exception("The object\n\n" + element + "\n\nhas a base type of\n\n" +
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

        public static bool ContainsCustomVariableRecursively(this IElement container, string variableName)
        {
            for (int i = 0; i < container.CustomVariables.Count; i++)
            {
                if (container.CustomVariables[i].Name == variableName)
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(container.BaseElement))
            {
                var baseElement = ObjectFinderCore.Self.GetElement(container.BaseElement);
                if (baseElement != null && baseElement.ContainsCustomVariableRecursively(variableName))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<EventResponseSave> GetEventsOnVariable(this IElement instance, string variableName)
        {
            return instance.Events.Where(eventSave => eventSave.SourceVariable == variableName).ToList();
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
                if (string.IsNullOrEmpty(categoryName) || categoryName == category.Name)
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
                IElement baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);

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
                IElement baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);

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
            IElement baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);

            if (baseElement == null || element.States.Count != 0)
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
                IElement baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);

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
            }
            else
            {
                uses = element.StateCategoryList.Count(item => { return item.Name == enumType; }) != 0;
            }

            if (!uses && !string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);

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
        public static IEnumerable<NamedObjectSave> GetAllNamedObjectsRecurisvely(this GlueElement element)
        {
            if (element != null)
            {
                foreach (NamedObjectSave nos in element.AllNamedObjects)
                {
                    yield return nos;
                }

                var allDerived = ObjectFinderCore.Self.GetAllBaseElementsRecursively(element);

                foreach (var derived in allDerived)
                {
                    foreach (NamedObjectSave nos in derived.AllNamedObjects)
                    {
                        yield return nos;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all referenced files in this and base elements.
        /// </summary>
        /// <param name="instance">The element to search</param>
        /// <returns>All referenced file saves from this and base elements.</returns>
        public static IEnumerable<ReferencedFileSave> GetAllReferencedFileSavesRecursively(this IElement instance)
        {
            if (instance == null)
            {
                yield break;
            }

            foreach (ReferencedFileSave rfs in instance.ReferencedFiles)
            {
                yield return rfs;
            }

            if (!string.IsNullOrEmpty(instance.BaseElement))
            {
                var baseElement = ObjectFinderCore.Self.GetElement(instance.BaseElement);
                if (baseElement != null)
                {
                    foreach (ReferencedFileSave rfs in baseElement.GetAllReferencedFileSavesRecursively())
                    {
                        yield return rfs;
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
                   (element.BaseElement.Replace('\\', '/').StartsWith($"Entities/", StringComparison.OrdinalIgnoreCase) ||
                    element.BaseElement.Replace('\\', '/').StartsWith($"Screens/", StringComparison.OrdinalIgnoreCase));
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
                       element.BaseElement.Replace('\\', '/').StartsWith($"Entities/", StringComparison.OrdinalIgnoreCase);
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
                       !element.BaseElement.Replace('\\', '/').StartsWith($"Entities/", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static AssetTypeInfo GetAssetTypeInfo(this IElement element)
        {
            if (element is ScreenSave)
            {
                return AvailableAssetTypesCore.Self.Screen;
            }
            else if (!string.IsNullOrEmpty(element.BaseElement))
            {
                var baseEntity = ObjectFinderCore.Self.GetEntitySave(element.BaseElement);

                if (baseEntity != null)
                {
                    return baseEntity.GetAssetTypeInfo();
                }
                else
                {
                    return AvailableAssetTypesCore.Self.AllAssetTypes.FirstOrDefault(item => item.RuntimeTypeName == element.BaseElement ||
                        item.QualifiedRuntimeTypeName.QualifiedType == element.BaseElement);
                }
            }
            else
            {
                var specificType = AvailableAssetTypesCore.Self.AllAssetTypes.FirstOrDefault(item => item.RuntimeTypeName == element.Name);
                return specificType ??
                       AvailableAssetTypesCore.Self.AllAssetTypes.FirstOrDefault(item => item.RuntimeTypeName == "PositionedObject");
            }
        }

        // DoesMemberNeedToBeSetByContainer and ReactToRenamedReferencedFile came from Glue.csproj's
        // NamedObjectContainerHelper (#2276); the ScreenSave overload from ScreenSaveExtensionMethods.
        // The ScreenSave overload calls the IElement one by class name on purpose - an extension-syntax
        // call on a ScreenSave would pick the more specific ScreenSave overload and recurse forever.

        public static bool DoesMemberNeedToBeSetByContainer(this IElement namedObjectContainer, string memberName)
        {
            foreach (NamedObjectSave namedObject in namedObjectContainer.NamedObjects)
            {
                if (namedObject.InstanceName == memberName && namedObject.SetByContainer)
                {
                    return namedObject.SetByContainer;
                }
            }

            if ( namedObjectContainer.InheritsFromElement())
            {
                EntitySave baseEntity = ObjectFinderCore.Self.GetEntitySave(namedObjectContainer.BaseObject);

                return baseEntity.DoesMemberNeedToBeSetByContainer(memberName);
            }


            return false;
        }

        public static bool DoesMemberNeedToBeSetByContainer(this ScreenSave instance, string memberName)
        {
            return ElementExtensions.DoesMemberNeedToBeSetByContainer((IElement)instance, memberName);
        }

        public static bool ReactToRenamedReferencedFile(this INamedObjectContainer namedObjectContainer, string oldName, string newName)
        {
            bool toReturn = false;

            for (int i = 0; i < namedObjectContainer.NamedObjects.Count; i++)
            {
                NamedObjectSave namedObject = namedObjectContainer.NamedObjects[i];

                if (namedObject.SourceFile == oldName)
                {
                    toReturn = true;
                    namedObject.SourceFile = newName;
                }
            }

            return toReturn;
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
    }
}
