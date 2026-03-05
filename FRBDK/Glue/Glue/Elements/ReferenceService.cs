using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Events;

namespace FlatRedBall.Glue.Elements;

/// <summary>
/// Centralised "who uses X?" query service. Contains all reference-finding and
/// inheritance-traversal methods that were previously on <see cref="ObjectFinder"/>.
/// <see cref="ObjectFinder"/> retains thin forwarding stubs for backward compatibility;
/// prefer calling this class directly in new code.
/// </summary>
public class ReferenceService : IReferenceService
{
    public static IReferenceService Self { get; internal set; } = new ReferenceService();

    // ReferenceService reads the live project through ObjectFinder so there is only one
    // source of truth for GlueProject. Methods that need element lookup delegate back to
    // ObjectFinder.Self (which retains all GetElement / GetEntitySave / GetScreenSave methods).
    private GlueProjectSave GlueProject => ObjectFinder.Self.GlueProject;

    #region Composite "find all references" queries
    // These methods bundle all reference types for a given subject into a single flat list of
    // objects. They are the intended entry point for "Find All References" UI features.

    /// <summary>
    /// Returns all objects that reference <paramref name="rfs"/>: the matching
    /// <see cref="ReferencedFileSave"/> in each owning element, any <see cref="NamedObjectSave"/>
    /// that sources from the file, and — for CSVs — any <see cref="CustomVariable"/> whose type
    /// matches the CSV class name.
    /// </summary>
    public IReadOnlyList<object> GetReferencesTo(ReferencedFileSave rfs)
    {
        var results = new List<object>();

        foreach (var element in GetAllElementsReferencingFile(rfs.Name))
        {
            var rfsInElement = element.GetReferencedFileSave(rfs.Name);
            results.Add(rfsInElement);

            foreach (var nos in element.AllNamedObjects)
            {
                if (nos.SourceType == SourceType.File && nos.SourceFile == rfsInElement.Name)
                    results.Add(nos);
            }
        }

        if (rfs.IsCsvOrTreatedAsCsv)
        {
            var className = rfs.Name;
            var customClass = GlueProject.GetCustomClassReferencingFile(rfs.Name);
            if (customClass != null)
                className = customClass.Name;

            foreach (var cv in GlueProject.Screens
                         .Cast<IElement>()
                         .Concat(GlueProject.Entities.Cast<IElement>())
                         .SelectMany(e => e.CustomVariables.Where(cv =>
                             string.Equals(cv.Type, className, StringComparison.OrdinalIgnoreCase))))
            {
                results.Add(cv);
            }
        }

        return results;
    }

    /// <summary>
    /// Returns all objects that reference <paramref name="element"/>: named objects using it as their
    /// type, screens/entities that inherit from it, and screens that link to it via
    /// <see cref="ScreenSave.NextScreen"/>.
    /// </summary>
    public IReadOnlyList<object> GetReferencesToElement(IElement element)
    {
        var results = new List<object>();

        if (element is EntitySave entitySave)
            results.AddRange(GetAllNamedObjectsThatUseEntity(entitySave));

        if (element is ScreenSave screenSave)
        {
            var screens = GetAllScreensThatInheritFrom(screenSave);

            foreach (var screen in GlueProject.Screens
                         .Where(s => s.NextScreen == element.Name && !screens.Contains(s)))
            {
                screens.Add(screen);
            }

            results.AddRange(screens);
        }
        else if (element is EntitySave asEntity)
        {
            results.AddRange(GetAllEntitiesThatInheritFrom(asEntity));
        }

        return results;
    }

    /// <summary>
    /// Returns all objects that reference <paramref name="namedObjectSave"/> within
    /// <paramref name="container"/>: <see cref="CustomVariable"/>s that tunnel from it, and
    /// matching <see cref="NamedObjectSave"/>s defined-by-base in derived elements.
    /// </summary>
    public IReadOnlyList<object> GetReferencesTo(NamedObjectSave namedObjectSave, IElement container)
    {
        var results = new List<object>();

        foreach (var variable in container.CustomVariables
                     .Where(v => v.SourceObject == namedObjectSave.InstanceName))
        {
            results.Add(variable);
        }

        foreach (var nos in GetAllElementsThatInheritFrom(container)
                     .SelectMany(e => e.NamedObjects
                         .Where(n => n.DefinedByBase &&
                                     string.Equals(n.InstanceName, namedObjectSave.InstanceName,
                                         StringComparison.OrdinalIgnoreCase))))
        {
            results.Add(nos);
        }

        return results;
    }

    /// <summary>
    /// Returns all objects that reference <paramref name="customVariable"/> within
    /// <paramref name="container"/>: states that set it, matching variables defined-by-base in
    /// derived elements, and event responses wired to it.
    /// </summary>
    public IReadOnlyList<object> GetReferencesTo(CustomVariable customVariable, IElement container)
    {
        var results = new List<object>();

        foreach (var state in container.AllStates)
        {
            if (state.InstructionSaves.Any(instr =>
                    string.Equals(instr.Member, customVariable.Name, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(state);
            }
        }

        foreach (var variable in GetAllElementsThatInheritFrom(container)
                     .SelectMany(e => e.CustomVariables
                         .Where(v => v.DefinedByBase &&
                                     string.Equals(v.Name, customVariable.Name,
                                         StringComparison.OrdinalIgnoreCase))))
        {
            results.Add(variable);
        }

        foreach (var ers in container.GetEventsOnVariable(customVariable.Name))
        {
            results.Add(ers);
        }

        return results;
    }

    #endregion

    #region File / RFS reference finding

    public List<ReferencedFileSave> GetReferencedFileSavesFromSource(string sourceFile)
    {
        var toReturn = new List<ReferencedFileSave>();

        sourceFile = sourceFile.ToLowerInvariant();
        if (GlueProject != null)
        {
            foreach (ScreenSave screenSave in GlueProject.Screens)
            {
                foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                {
                    if (rfs.IsFileSourceForThis(sourceFile))
                    {
                        toReturn.Add(rfs);
                    }
                }
            }

            foreach (EntitySave entitySave in GlueProject.Entities)
            {
                foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                {
                    if (rfs.IsFileSourceForThis(sourceFile))
                    {
                        toReturn.Add(rfs);
                    }
                }
            }

            foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles)
            {
                if (rfs.IsFileSourceForThis(sourceFile))
                {
                    toReturn.Add(rfs);
                }
            }
        }

        return toReturn;
    }

    public List<ReferencedFileSave> GetAllReferencedFiles()
    {
        if (GlueProject != null)
        {
            return GlueProject.GetAllReferencedFiles();
        }
        else
        {
            return new List<ReferencedFileSave>();
        }
    }

    public List<ReferencedFileSave> GetMatchingReferencedFiles(ReferencedFileSave rfs)
    {
        // The same file can be referenced in multiple parts of a Glue project. For example,
        // one .scnx file may be used in two unrelated Entities, or a RFS may be both in
        // GlobalContent and in an Entity. In that case the two RFS's should be linked:
        // changing a property on one (e.g. content-pipeline flag) should change the other too.
        List<ReferencedFileSave> toReturn = new List<ReferencedFileSave>();

        List<ReferencedFileSave> allRfses = GetAllReferencedFiles();

        foreach (ReferencedFileSave possibleRfs in allRfses)
        {
            if (possibleRfs != rfs && possibleRfs.Name == rfs.Name)
            {
                toReturn.Add(possibleRfs);
            }
        }

        return toReturn;
    }

    public List<GlueElement> GetAllElementsReferencingFile(string rfsName)
    {
        var returnList = new List<GlueElement>();

        foreach (ScreenSave screenSave in GlueProject.Screens)
        {
            foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
            {
                if (rfs.Name == rfsName)
                {
                    returnList.Add(screenSave);
                    break;
                }
            }
        }

        foreach (EntitySave entitySave in GlueProject.Entities)
        {
            foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
            {
                if (rfs.Name == rfsName)
                {
                    returnList.Add(entitySave);
                    break;
                }
            }
        }

        return returnList;
    }

    #endregion

    #region Named-object / entity reference finding

    /// <summary>
    /// Returns a fully recursive enumerable of all NamedObjectSaves in the project, including
    /// NamedObjectSaves in lists and in derived elements.
    /// </summary>
    public IEnumerable<NamedObjectSave> GetAllNamedObjects()
    {
        foreach (ScreenSave screenSave in GlueProject.Screens)
        {
            foreach (NamedObjectSave nos in screenSave.AllNamedObjects)
            {
                yield return nos;
            }
        }

        foreach (EntitySave entitySave in GlueProject.Entities)
        {
            foreach (NamedObjectSave nos in entitySave.AllNamedObjects)
            {
                yield return nos;
            }
        }
    }

    public List<NamedObjectSave> GetAllNamedObjectsThatUseElement(IElement element)
    {
        if (element is EntitySave entitySave)
        {
            return GetAllNamedObjectsThatUseEntity(entitySave);
        }
        else
        {
            return new List<NamedObjectSave>();
        }
    }

    /// <summary>
    /// Returns all NamedObjectSaves which have any reference to the argument entity, including
    /// references via variant variables.
    /// </summary>
    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(EntitySave entity)
    {
        return GetAllNamedObjectsThatUseEntity(entity.Name);
    }

    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(string baseEntity)
    {
        List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();

        foreach (EntitySave entitySave in GlueProject.Entities)
        {
            FillNamedObjectsThatUseElement(entitySave.NamedObjects, namedObjects, baseEntity, SourceType.Entity, entitySave);
        }

        foreach (ScreenSave screenSave in GlueProject.Screens)
        {
            FillNamedObjectsThatUseElement(screenSave.NamedObjects, namedObjects, baseEntity, SourceType.Entity, screenSave);
        }

        return namedObjects;
    }

    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntityAsVariableType(string entityType)
    {
        List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();

        foreach (EntitySave entitySave in GlueProject.Entities)
        {
            FillFrom(entitySave);
        }

        foreach (ScreenSave screenSave in GlueProject.Screens)
        {
            FillFrom(screenSave);
        }

        void FillFrom(GlueElement element)
        {
            foreach (var nos in element.AllNamedObjects)
            {
                foreach (var variable in nos.InstructionSaves)
                {
                    var startsWithScreensOrEntities =
                        variable.Type.StartsWith("Screens.") || variable.Type.StartsWith("Entities.");
                    var endsWithType =
                        variable.Type.EndsWith("Type") ||
                        variable.Type.EndsWith("Variant");
                    if (startsWithScreensOrEntities && endsWithType && (variable.Value as string) == entityType)
                    {
                        namedObjects.Add(nos);
                        break;
                    }
                }
            }
        }

        return namedObjects;
    }

    private void FillNamedObjectsThatUseElement(List<NamedObjectSave> sourceList, List<NamedObjectSave> listToAddTo, string name, SourceType sourceType, GlueElement parentGlueElement)
    {
        bool DoesNosMatchType(NamedObjectSave nos)
        {
            if (nos == null)
            {
                return false;
            }
            else if (nos.SourceType == sourceType && nos.SourceClassType == name)
            {
                return true;
            }
            else if (nos.SourceType == SourceType.FlatRedBallType && nos.IsList && nos.SourceClassGenericType == name)
            {
                return true;
            }
            return false;
        }

        foreach (NamedObjectSave nos in sourceList)
        {
            if (DoesNosMatchType(nos))
            {
                listToAddTo.Add(nos);
            }
            else if (nos.IsCollisionRelationship())
            {
                var firstItem = nos.Properties.GetValue<string>("FirstCollisionName");
                var secondItem = nos.Properties.GetValue<string>("SecondCollisionName");

                var firstNos = parentGlueElement.GetNamedObjectRecursively(firstItem);
                var secondNos = parentGlueElement.GetNamedObjectRecursively(secondItem);

                if (DoesNosMatchType(firstNos) || DoesNosMatchType(secondNos))
                {
                    listToAddTo.Add(nos);
                }
            }
            else
            {
                foreach (var variable in nos.InstructionSaves)
                {
                    if (variable.Type?.StartsWith("Entities.") == true && variable.Value is string asString && asString == name)
                    {
                        listToAddTo.Add(nos);
                        break;
                    }
                }
            }
            FillNamedObjectsThatUseElement(nos.ContainedObjects, listToAddTo, name, sourceType, parentGlueElement);
        }
    }

    #endregion

    #region Variable / type reference finding

    public List<CustomVariable> GetVariablesReferencingElementType(string elementType)
    {
        List<CustomVariable> customVariables = new List<CustomVariable>();

        foreach (EntitySave entitySave in GlueProject.Entities)
        {
            FillFrom(entitySave);
        }

        foreach (ScreenSave screenSave in GlueProject.Screens)
        {
            FillFrom(screenSave);
        }

        void FillFrom(GlueElement elementSave)
        {
            foreach (var customVariable in elementSave.CustomVariables)
            {
                var startsWithScreensOrEntities =
                    customVariable.Type.StartsWith("Screens.") || customVariable.Type.StartsWith("Entities.");
                var endsWithType = customVariable.Type.EndsWith("Type") ||
                    customVariable.Type.EndsWith("Variant");

                var elementTypeWithVariant = elementType.Replace("\\", ".") + "Variant";

                if (startsWithScreensOrEntities && endsWithType)
                {
                    if ((customVariable.DefaultValue as string) == elementType)
                    {
                        customVariables.Add(customVariable);
                    }
                    else if (customVariable.Type == elementTypeWithVariant)
                    {
                        customVariables.Add(customVariable);
                    }
                }
            }
        }

        return customVariables;
    }

    #endregion

    #region Inheritance traversal

    public List<EntitySave> GetAllEntitiesThatInheritFrom(EntitySave baseEntity)
    {
        return GetAllEntitiesThatInheritFrom(baseEntity.Name);
    }

    public List<EntitySave> GetAllEntitiesThatInheritFrom(string baseEntity)
    {
        List<EntitySave> derivedEntities = new List<EntitySave>();

        for (int i = 0; i < GlueProject.Entities.Count; i++)
        {
            EntitySave entitySave = GlueProject.Entities[i];

            if (entitySave.InheritsFrom(baseEntity))
            {
                derivedEntities.Add(entitySave);
            }
        }

        return derivedEntities;
    }

    public List<ScreenSave> GetAllScreensThatInheritFrom(ScreenSave baseScreen)
    {
        return GetAllScreensThatInheritFrom(baseScreen.Name);
    }

    public List<ScreenSave> GetAllScreensThatInheritFrom(string baseScreen)
    {
        List<ScreenSave> derivedScreens = new List<ScreenSave>();

        for (int i = 0; i < GlueProject.Screens.Count; i++)
        {
            ScreenSave screenSave = GlueProject.Screens[i];

            if (screenSave.InheritsFrom(baseScreen))
            {
                derivedScreens.Add(screenSave);
            }
        }

        return derivedScreens;
    }

    public List<GlueElement> GetAllElementsThatInheritFrom(string elementName)
    {
        var element = ObjectFinder.Self.GetElement(elementName);

        return GetAllElementsThatInheritFrom(element);
    }

    public List<GlueElement> GetAllElementsThatInheritFrom(IElement baseElement)
    {
        var derivedElements = new List<GlueElement>();
        var count = 0;
        var isScreen = baseElement is ScreenSave;
        var isEntity = baseElement is EntitySave;

        if (baseElement is ScreenSave)
            count = GlueProject.Screens.Count;
        else if (baseElement is EntitySave)
            count = GlueProject.Entities.Count;

        for (var i = 0; i < count; i++)
        {
            bool add;
            GlueElement derivedElement;

            if (isScreen)
            {
                derivedElement = GlueProject.Screens[i];
                add = GlueProject.Screens[i].InheritsFrom(baseElement.Name);
            }
            else if (isEntity)
            {
                derivedElement = GlueProject.Entities[i];
                add = GlueProject.Entities[i].InheritsFrom(baseElement.Name);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (add)
            {
                derivedElements.Add(derivedElement);
            }
        }

        return derivedElements;
    }

    public bool GetIfInherits(GlueElement derivedElement, GlueElement baseElement)
    {
        var allDerived = GetAllElementsThatInheritFrom(baseElement);

        return allDerived.Contains(derivedElement);
    }

    public GlueElement GetRootBaseElement(GlueElement element)
    {
        GlueElement derived = null;

        if (!string.IsNullOrEmpty(element?.BaseElement))
        {
            derived = ObjectFinder.Self.GetElement(element.BaseElement);
        }

        if (derived == null)
        {
            return element;
        }
        else
        {
            return GetRootBaseElement(derived);
        }
    }

    public GlueElement GetBaseElement(IElement derivedElement)
    {
        if (!string.IsNullOrEmpty(derivedElement?.BaseElement))
        {
            return ObjectFinder.Self.GetElement(derivedElement.BaseElement);
        }
        else
        {
            return null;
        }
    }

    public GlueElement GetBaseElementRecursively(GlueElement derivedElement)
    {
        GlueElement baseElement = null;
        if (!string.IsNullOrEmpty(derivedElement?.BaseElement))
        {
            baseElement = ObjectFinder.Self.GetElement(derivedElement.BaseElement);
        }

        if (baseElement == null)
        {
            return derivedElement;
        }
        else
        {
            return GetBaseElementRecursively(baseElement);
        }
    }

    /// <summary>
    /// Returns all base elements, with the most derived first and the most base last.
    /// </summary>
    public List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement)
    {
        var baseElements = new List<GlueElement>();

        while (!string.IsNullOrEmpty(derivedElement.BaseElement))
        {
            var baseElement = ObjectFinder.Self.GetElement(derivedElement.BaseElement);

            if (baseElement == null)
            {
                break;
            }
            else
            {
                baseElements.Add(baseElement);
                derivedElement = baseElement;
            }
        }

        return baseElements;
    }

    public List<GlueElement> GetAllDerivedElementsRecursive(GlueElement baseElement)
    {
        HashSet<GlueElement> toReturnHashSet = new HashSet<GlueElement>();

        AddAllDerivedElementsRecursive(baseElement, toReturnHashSet);

        return toReturnHashSet.ToList();
    }

    private void AddAllDerivedElementsRecursive(GlueElement baseElement, HashSet<GlueElement> derivedElements)
    {
        var directDerived = GetAllElementsThatInheritFrom(baseElement);

        foreach (var derived in directDerived)
        {
            derivedElements.Add(derived);

            AddAllDerivedElementsRecursive(derived, derivedElements);
        }
    }

    public int GetHierarchyDepth(GlueElement element)
    {
        var baseElement = GetBaseElement(element);

        if (baseElement == null)
        {
            return 0;
        }
        else
        {
            return 1 + GetHierarchyDepth(baseElement);
        }
    }

    /// <summary>
    /// Returns all elements in the inheritance chain with the most base type first and the most
    /// derived type last. The argument element is included as the last entry.
    /// </summary>
    public List<GlueElement> GetInheritanceChain(GlueElement derivedElement)
    {
        List<GlueElement> toReturn = new List<GlueElement>();

        toReturn.Add(derivedElement);

        var currentElement = derivedElement;
        while (currentElement != null)
        {
            var baseElement = GetBaseElement(currentElement);

            if (baseElement != null)
            {
                toReturn.Insert(0, baseElement);
            }
            currentElement = baseElement;
        }

        return toReturn;
    }

    #endregion
}
