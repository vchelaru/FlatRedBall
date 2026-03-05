using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using System.IO;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Events;

using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Content.Instructions;
using System.Windows.Navigation;

namespace FlatRedBall.Glue.Elements;

public class ObjectFinder : IObjectFinder
{
    #region Fields/Properties

    static ObjectFinder mSelf = new ObjectFinder();
    public static ObjectFinder Self = mSelf;

    public GlueProjectSave GlueProject { get; set; }

    #endregion

    public ObjectFinder()
    {
        NamedObjectSave.ToStringDelegate = NamedObjectSaveExtensionMethods.NamedObjectSaveToString;
        CustomVariable.ToStringDelegate = CustomVariableExtensionMethods.CustomVariableToString;
        ReferencedFileSave.ToStringDelegate = ReferencedFileSaveExtensionMethods.ReferencedFileSaveToString;
        EventResponseSave.ToStringDelegate = EventResponseSaveExtensionMethods.EventResponseSaveToString;
        StateSave.ToStringDelegate = StateSaveExtensionMethods.StateSaveToString;
    }

    #region File and RFS related

    public string MakeAbsoluteContent(string fileName)
    {
        if (FileManager.IsRelative(fileName))
        {
            return GlueState.Self.ContentDirectory + fileName;
        }
        else
        {
            return fileName;
        }
    }

    public List<ReferencedFileSave> GetReferencedFileSavesFromSource(string sourceFile) =>
        ReferenceService.Self.GetReferencedFileSavesFromSource(sourceFile);

    public List<ReferencedFileSave> GetAllReferencedFiles() =>
        ReferenceService.Self.GetAllReferencedFiles();

    public List<ReferencedFileSave> GetMatchingReferencedFiles(ReferencedFileSave rfs) =>
        ReferenceService.Self.GetMatchingReferencedFiles(rfs);

    #endregion

    #region Get Element

    public GlueElement[] GetAllElements()
    {
        List<GlueElement> toReturn = new List<GlueElement>();

        if (GlueProject != null)
        {
            toReturn.AddRange(GlueProject.Entities);
            toReturn.AddRange(GlueProject.Screens);
        }

        return toReturn.ToArray();
    }

    public EntitySave GetEntitySave(NamedObjectSave nos)
    {
        if (nos?.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos?.SourceClassType))
        {
            return GetEntitySave(nos.SourceClassType);
        }

        return null;
    }

    public EntitySave GetEntitySave(string entityName)
    {
        // This method is called by NamedObjectSave whenever its
        // SourceClassType is set.  This is set when the project
        // is first deserialized, and when that happens the project
        // isn't valid yet.  Therefore, we should check to see if it
        // is valid.
        if (GlueProject != null)
        {
            if (!string.IsNullOrEmpty(entityName))
            {
                // We don't know what project is using the Glue classes, and it may prefer
                // forward slashes or back slashes.  Therefore we should tolerate either when
                // making comparisons
                entityName = entityName.Replace('/', '\\');

                for (int i = 0; i < GlueProject.Entities.Count; i++)
                {
                    EntitySave entitySave = GlueProject.Entities[i];

                    if (entitySave.Name.Replace('/', '\\') == entityName)
                    {
                        return entitySave;
                    }
                }
            }
        }

        return null;
    }

    public IElement GetElementUnqualified(string elementName)
    {

        if (elementName != null && GlueProject != null)
        {
            elementName = elementName.Replace('/', '\\');

            for (int i = 0; i < GlueProject.Entities.Count; i++)
            {
                EntitySave entitySave = GlueProject.Entities[i];

                if (FileManager.RemovePath(entitySave.Name) == elementName)
                {
                    return entitySave;
                }
            }

            for (int i = 0; i < GlueProject.Screens.Count; i++)
            {
                ScreenSave screenSave = GlueProject.Screens[i];

                if (FileManager.RemovePath(screenSave.Name) == elementName)
                {
                    return screenSave;
                }
            }
        }
        return null;

    }

    public List<IElement> GetElementsUnqualified(string elementName)
    {
        List<IElement> listToReturn = new List<IElement>();

        if (elementName != null)
        {
            elementName = elementName.Replace('/', '\\');

            for (int i = 0; i < GlueProject.Entities.Count; i++)
            {
                EntitySave entitySave = GlueProject.Entities[i];

                if (FileManager.RemovePath(entitySave.Name) == elementName)
                {
                    listToReturn.Add(entitySave);
                }
            }

            for (int i = 0; i < GlueProject.Screens.Count; i++)
            {
                ScreenSave screenSave = GlueProject.Screens[i];

                if (FileManager.RemovePath(screenSave.Name) == elementName)
                {
                    listToReturn.Add(screenSave);
                }
            }
        }
        return listToReturn;
    }

    public EntitySave GetEntitySaveUnqualified(string entityName, EntitySave toIgnore = null)
    {
        if (GlueProject != null)
        {
            for (int i = 0; i < GlueProject.Entities.Count; i++)
            {
                EntitySave entitySave = GlueProject.Entities[i];

                if (entitySave != toIgnore && FileManager.RemovePath(entitySave.Name) == entityName)
                {
                    return entitySave;
                }
            }
        }

        return null;
    }


    public ScreenSave GetScreenSave(string screenName)
    {
        // try to get qualified first...
        if (screenName != null)
        {
            screenName = screenName.Replace('/', '\\');

            if (GlueProject != null)
            {
                for (int i = 0; i < GlueProject.Screens.Count; i++)
                {
                    ScreenSave screenSave = GlueProject.Screens[i];

                    if (screenSave.Name == screenName)
                    {
                        return screenSave;
                    }
                }

                for (int i = 0; i < GlueProject.Screens.Count; i++)
                {
                    ScreenSave screenSave = GlueProject.Screens[i];

                    if (screenSave.ClassName == screenName)
                    {
                        return screenSave;
                    }
                }


            }
        }


        return null;
    }

    public ScreenSave GetScreenSaveUnqualified(string screenName, ScreenSave screenToIgnore = null)
    {
        if (GlueProject != null)
        {
            for (int i = 0; i < GlueProject.Screens.Count; i++)
            {
                ScreenSave screenSave = GlueProject.Screens[i];

                if (screenToIgnore != screenSave && FileManager.RemovePath(screenSave.Name) == screenName)
                {
                    return screenSave;
                }
            }
        }

        return null;
    }

    [Obsolete("Use GetElement instead")]
    public GlueElement GetIElement(string elementName) => GetElement(elementName);

    /// <summary>
    /// Returns the element referenced by the argument NamedObjectSave's SourceClassType. If the NamedObjectSave
    /// does not reference an element (such as if it is a Sprite), then this method returns null.
    /// </summary>
    /// <param name="nos">The NamedObjectSave to check the SourceClassType and return the matching Element.</param>
    /// <returns>The matching GlueElement or null if one isn't found.</returns>
    public GlueElement GetElement(NamedObjectSave nos)
    {
        if (nos?.SourceType == SourceType.Entity)
        {
            return GetEntitySave(nos.SourceClassType);
        }
        return null;
    }

    public GlueElement GetElement(string elementName)
    {
        GlueElement retval;

        retval = GetScreenSave(elementName);

        if (retval == null)
        {
            retval = GetEntitySave(elementName);
        }

        return retval;
    }



    public bool DoesEntityExist(string entityName)
    {
        return GetEntitySave(entityName) != null;
    }


    public GlueElement GetElementContaining(NamedObjectSave namedObjectSave)
    {
        for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
        {
            var screen = ObjectFinder.Self.GlueProject.Screens[i];
            if (IsContainedInListOrAsChild(screen.NamedObjects, namedObjectSave))
            {
                return screen;
            }
        }
        for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
        {
            if (IsContainedInListOrAsChild(ObjectFinder.Self.GlueProject.Entities[i].NamedObjects, namedObjectSave))
            {
                return ObjectFinder.Self.GlueProject.Entities[i];
            }
        }

        return null;

    }

    public GlueElement GetElementContaining(ReferencedFileSave referencedFileSave)
    {

        for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
        {
            if (ObjectFinder.Self.GlueProject.Screens[i].ReferencedFiles.Contains(referencedFileSave))
            {
                return ObjectFinder.Self.GlueProject.Screens[i];
            }
        }
        for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
        {
            if (ObjectFinder.Self.GlueProject.Entities[i].ReferencedFiles.Contains(referencedFileSave))
            {
                return ObjectFinder.Self.GlueProject.Entities[i];
            }
        }

        return null;
    }

    public GlueElement GetElementContaining(EventResponseSave ers)
    {
        for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
        {
            foreach (EventResponseSave possibleErs in ObjectFinder.Self.GlueProject.Screens[i].Events)
            {
                if (possibleErs == ers)
                {
                    return ObjectFinder.Self.GlueProject.Screens[i];
                }
            }
        }
        for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
        {
            foreach (EventResponseSave possibleErs in ObjectFinder.Self.GlueProject.Entities[i].Events)
            {
                if (possibleErs == ers)
                {
                    return ObjectFinder.Self.GlueProject.Entities[i];
                }
            }
        }

        return null;

    }

    bool IsContainedInListOrAsChild(List<NamedObjectSave> namedObjects, NamedObjectSave objectToFind)
    {
        // This was done before to prevent access of objects during tasks.
        // This makes things slower and the task system is way better than before
        // so let's remove ToArray and go faster.
        //foreach (NamedObjectSave nos in namedObjects.ToArray())
        foreach (NamedObjectSave nos in namedObjects)
        {
            bool isListOrShapeCollection()
            {
                if (nos.IsList)
                {
                    return true;
                }
                else if (nos.SourceType == SourceType.FlatRedBallType)
                {
                    var sourceClassType = nos.SourceClassType;

                    return sourceClassType == "ShapeCollection" || sourceClassType == "FlatRedBall.Math.Geometry.ShapeCollection";

                }
                return false;


            }


            if (nos == objectToFind)
            {
                return true;
            }
            else if (isListOrShapeCollection() && IsContainedInListOrAsChild(nos.ContainedObjects, objectToFind))
            {
                return true;
            }
        }
        return false;
    }

    public GlueElement GetElementContaining(StateSave stateSave)
    {
        return GlueProject.GetElementContaining(stateSave);
    }

    public GlueElement GetElementContaining(CustomVariable customVariable)
    {
        if (GlueProject != null)
        {

            foreach (EntitySave entitySave in GlueProject.Entities)
            {
                if (entitySave.CustomVariables.Contains(customVariable))
                {
                    return entitySave;
                }
            }


            foreach (ScreenSave screenSave in GlueProject.Screens)
            {
                if (screenSave.CustomVariables.Contains(customVariable))
                {
                    return screenSave;
                }
            }

            return null;
        }
        else
        {
            return null;
        }
    }

    public GlueElement GetElementContaining(StateSaveCategory category)
    {
        if (GlueProject != null)
        {
            IEnumerable<GlueElement> screens = GlueProject.Screens;
            IEnumerable<GlueElement> entities = GlueProject.Entities;
            return screens.Concat(entities)
                .FirstOrDefault(item =>
                    item.StateCategoryList.Contains(category));
        }
        else
        {
            return null;
        }
    }

    public GlueElement GetElementDefiningStateCategory(string qualifiedTypeName)
    {
        if (qualifiedTypeName.Contains('.'))
        {
            var splitTypeName = qualifiedTypeName.Split('.');
            if (splitTypeName.Length > 1)
            {
                var elementName = string.Join('\\', splitTypeName.Take(splitTypeName.Length - 1));
                var elementDefiningCategory = ObjectFinder.Self.GetElement(elementName);

                return elementDefiningCategory;
            }

        }
        return null;
    }

    public List<GlueElement> GetAllElementsReferencingFile(string rfsName) =>
        ReferenceService.Self.GetAllElementsReferencingFile(rfsName);

    #endregion

    #region CSV

    public ReferencedFileSave GetFirstCsvUsingClass(string className)
    {
        return GetFirstCsvUsingClass(className, null);
    }

    public ReferencedFileSave GetFirstCsvUsingClass(string className, IElement elementToLookIn)
    {
        if (GlueProject != null)
        {
            if (elementToLookIn != null)
            {
                // Maybe we need to make this recursive?
                foreach (ReferencedFileSave rfs in elementToLookIn.ReferencedFiles)
                {
                    if (rfs.IsCsvOrTreatedAsCsv &&
                        rfs.GetTypeForCsvFile() == className)
                    {
                        return rfs;
                    }
                }
            }
            else
            {
                foreach (ScreenSave screenSave in GlueProject.Screens)
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                    {
                        if (rfs.IsCsvOrTreatedAsCsv &&
                            rfs.GetTypeForCsvFile() == className)
                        {
                            return rfs;
                        }
                    }
                }

                foreach (EntitySave entitySave in GlueProject.Entities)
                {
                    foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                    {
                        if (rfs.IsCsvOrTreatedAsCsv && rfs.GetTypeForCsvFile() == className)
                        {
                            return rfs;
                        }
                    }
                }
            }

            className = FileManager.RemovePath(className);

            foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles)
            {
                if (rfs.IsCsvOrTreatedAsCsv && rfs.GetTypeForCsvFile() == className)
                {
                    return rfs;
                }
            }
        }

        return null;
    }

    public CustomClassSave GetCustomClassFor(ReferencedFileSave rfs)
    {
        foreach (var customClass in GlueProject.CustomClasses)
        {
            if (customClass.CsvFilesUsingThis.Contains(rfs.Name))
            {
                return customClass;
            }
        }
        return null;
    }

    #endregion

    #region Inheritance

    public List<EntitySave> GetAllEntitiesThatInheritFrom(EntitySave baseEntity) =>
        ReferenceService.Self.GetAllEntitiesThatInheritFrom(baseEntity);

    public List<EntitySave> GetAllEntitiesThatInheritFrom(string baseEntity) =>
        ReferenceService.Self.GetAllEntitiesThatInheritFrom(baseEntity);

    public List<ScreenSave> GetAllScreensThatInheritFrom(ScreenSave baseScreen) =>
        ReferenceService.Self.GetAllScreensThatInheritFrom(baseScreen);

    public List<ScreenSave> GetAllScreensThatInheritFrom(string baseScreen) =>
        ReferenceService.Self.GetAllScreensThatInheritFrom(baseScreen);

    public List<GlueElement> GetAllElementsThatInheritFrom(string elementName) =>
        ReferenceService.Self.GetAllElementsThatInheritFrom(elementName);

    public List<GlueElement> GetAllElementsThatInheritFrom(IElement baseElement) =>
        ReferenceService.Self.GetAllElementsThatInheritFrom(baseElement);

    public bool GetIfInherits(GlueElement derivedElement, GlueElement baseElement) =>
        ReferenceService.Self.GetIfInherits(derivedElement, baseElement);

    public GlueElement GetRootBaseElement(GlueElement element) =>
        ReferenceService.Self.GetRootBaseElement(element);

    public GlueElement GetBaseElement(IElement derivedElement) =>
        ReferenceService.Self.GetBaseElement(derivedElement);

    public GlueElement GetBaseElementRecursively(GlueElement derivedElement) =>
        ReferenceService.Self.GetBaseElementRecursively(derivedElement);

    /// <summary>
    /// Returns all base elements, with the most derived first, and the most base last.
    /// </summary>
    public List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement) =>
        ReferenceService.Self.GetAllBaseElementsRecursively(derivedElement);

    public List<GlueElement> GetAllDerivedElementsRecursive(GlueElement baseElement) =>
        ReferenceService.Self.GetAllDerivedElementsRecursive(baseElement);

    #endregion

    #region NamedObjects

    /// <summary>
    /// Returns a fully recursive enumerable of all NamedObjectSaves in the project. This includes
    /// NamedObjectSaves in lists and in derived elements.
    /// </summary>
    public IEnumerable<NamedObjectSave> GetAllNamedObjects() =>
        ReferenceService.Self.GetAllNamedObjects();

    public List<NamedObjectSave> GetAllNamedObjectsThatUseElement(IElement element) =>
        ReferenceService.Self.GetAllNamedObjectsThatUseElement(element);

    /// <summary>
    /// Returns all NamedObjectSaves which have any reference to the argument entity. This includes references such as
    /// variant variables.
    /// </summary>
    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(EntitySave entity) =>
        ReferenceService.Self.GetAllNamedObjectsThatUseEntity(entity);

    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(string baseEntity) =>
        ReferenceService.Self.GetAllNamedObjectsThatUseEntity(baseEntity);

    public List<NamedObjectSave> GetAllNamedObjectsThatUseEntityAsVariableType(string entityType) =>
        ReferenceService.Self.GetAllNamedObjectsThatUseEntityAsVariableType(entityType);

    public INamedObjectContainer GetNamedObjectContainer(string namedObjectContainerName)
    {
        EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectContainerName);

        if (entitySave != null)
        {
            return entitySave;
        }
        else
        {
            return ObjectFinder.Self.GetScreenSave(namedObjectContainerName);
        }
    }

    public NamedObjectSave GetDefaultListToContain(NamedObjectSave namedObject, GlueElement containerElement)
    {
        var namedObjectSourceClassType = namedObject.SourceClassType;
        return GetDefaultListToContain(namedObjectSourceClassType, containerElement);

    }

    public List<NamedObjectSave> GetPossibleListsToContain(NamedObjectSave namedObject, GlueElement containerElement)
    {
        var namedObjectSourceClassType = namedObject.SourceClassType;
        return GetPossibleListsToContain(namedObjectSourceClassType, containerElement);
    }

    public NamedObjectSave GetDefaultListToContain(string namedObjectSourceClassType, GlueElement containerElement)
    {
        return GetPossibleListsToContain(namedObjectSourceClassType, containerElement).FirstOrDefault();
    }

    public List<NamedObjectSave> GetPossibleListsToContain(string namedObjectSourceClassType, GlueElement containerElement)
    {
        List<NamedObjectSave> possibleContainers = new List<NamedObjectSave>();
        var isNosShape = namedObjectSourceClassType == "FlatRedBall.Math.Geometry.Circle" ||
                          namedObjectSourceClassType == "FlatRedBall.Math.Geometry.AxisAlignedRectangle" ||
                          namedObjectSourceClassType == "FlatRedBall.Math.Geometry.Polygon";

        var nosEntity = GetEntitySave(namedObjectSourceClassType);


        if (isNosShape)
        {
            possibleContainers.AddRange(containerElement.NamedObjects.Where(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection));
        }

        if (nosEntity != null)
        {
            possibleContainers.AddRange(GetPossibleListsToContain(nosEntity, containerElement));
        }
        else
        {
            possibleContainers.AddRange(containerElement.NamedObjects.Where(item => item.IsList && item.SourceClassGenericType == namedObjectSourceClassType));
        }

        return possibleContainers;
    }

    public NamedObjectSave GetDefaultListToContain(EntitySave nosEntity, GlueElement containerElement)
    {
        return GetPossibleListsToContain(nosEntity, containerElement).FirstOrDefault();
    }

    public List<NamedObjectSave> GetPossibleListsToContain(EntitySave nosEntity, GlueElement containerElement)
    {
        var toReturn = new List<NamedObjectSave>();

        var baseEntityTypes = GetAllBaseElementsRecursively(nosEntity);

        // Do the top-level NamedObjects instead of AllNamedObjects
        foreach (var listCandidate in containerElement.NamedObjects)
        {
            if (listCandidate.IsList)
            {
                var listEntityType = GetEntitySave(listCandidate.SourceClassGenericType);

                if (listEntityType != null)
                {
                    if (listEntityType == nosEntity || baseEntityTypes.Contains(listEntityType))
                    {
                        toReturn.Add(listCandidate);
                    }
                }
            }
        }
        return toReturn;
    }

    public NamedObjectSave GetRootDefiningObject(NamedObjectSave derivedNos)
    {
        if (derivedNos.DefinedByBase == false)
        {
            return derivedNos;
        }
        else
        {
            var container = GetElementContaining(derivedNos);

            var baseContainer = GetBaseElement(container);

            var nosInBase = baseContainer?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == derivedNos.InstanceName);

            if (nosInBase == null)
            {
                return null;
            }
            else
            {
                return GetRootDefiningObject(nosInBase);
            }
        }
    }

    public NamedObjectSave GetNamedObjectFor(CustomVariable customVariable)
    {
        if (!string.IsNullOrEmpty(customVariable?.SourceObject))
        {
            var container = GetElementContaining(customVariable);
            return container?.GetAllNamedObjectsRecurisvely()
                .FirstOrDefault(item => item.InstanceName == customVariable.SourceObject);
        }
        return null;
    }



    #endregion

    #region NamedObjectSave properties

    public T GetPropertyValueRecursively<T>(NamedObjectSave nos, string propertyName)
    {
        var propertyOnNos = nos.Properties.FirstOrDefault(item => item.Name == propertyName);

        if (propertyOnNos != null)
        {
            if (typeof(T).IsEnum && propertyOnNos.Value is long asLong)
            {
                return (T)((object)(int)asLong);
            }
            // There seem to be some type leaks...Maybe from old projects.
            else if (typeof(T) == typeof(int) && propertyOnNos.Value is long asLong2)
            {
                return (T)((object)(int)asLong2);
            }
            else
            {
                return (T)((object)propertyOnNos.Value);
            }
        }
        else if (nos.DefinedByBase)
        {

            // this NOS doesn't have it, but maybe it's defined in a base:
            var elementContaining = GetElementContaining(nos);

            if (!string.IsNullOrEmpty(elementContaining?.BaseElement))
            {
                var baseElement = GetBaseElement(elementContaining);
                var nosInBase = baseElement.GetNamedObject(nos.InstanceName);
                if (nosInBase != null)
                {
                    return GetPropertyValueRecursively<T>(nosInBase, propertyName);
                }
            }
        }
        return default(T);
    }

    #endregion

    #region Variables

    public IElement GetVariableContainer(CustomVariable customVariable)
    {
        return GetElementContaining(customVariable);
    }

    public StateSaveCategory GetStateSaveCategory(StateSave stateSave)
    {
        var container = ObjectFinder.Self.GetElementContaining(stateSave);
        return container?.StateCategoryList.FirstOrDefault(item => item.States.Contains(stateSave));
    }

    public (bool IsState, StateSaveCategory Category) GetStateSaveCategory(CustomVariable customVariable, GlueElement containingElement)
    {
        if (customVariable.Type == null)
        {
            throw new NullReferenceException(
                $"The custom variable with name {customVariable.Name} has a Type that is null. This is not allowed");
        }
        bool isState = false;
        StateSaveCategory category = null;

        if (containingElement == null)
        {
            containingElement = ObjectFinder.Self.GetElementContaining(customVariable);
        }

        //////////////////////Early Out/////////////////////////
        // Update July 24, 2023 - 
        // Variables can be passed
        // in to this method to generate
        // code even if they are not contained
        // in a GlueElement, such as variables which
        // are used to generate the {ElementName}Type.
        // Therefore, tolerate a null containingElement
        //if(containingElement == null)
        //{
        //    return (isState, category);
        //}
        ///////////////////End Early Out////////////////////////


        if (customVariable.DefinedByBase)
        {
            // If this is DefinedByBase, it may represent a variable that is tunneling, but it
            // doesn't know it - we have to get the variable from the base to know for sure.
            if (containingElement != null && !string.IsNullOrEmpty(containingElement.BaseElement))
            {
                var baseElement = GlueState.Self.CurrentGlueProject.GetElement(containingElement.BaseElement);
                if (baseElement != null)
                {
                    CustomVariable customVariableInBase = baseElement.GetCustomVariableRecursively(customVariable.Name);
                    if (customVariableInBase != null)
                    {
                        (isState, category) = GetStateSaveCategory(customVariableInBase, baseElement);
                    }
                }
            }
        }
        else
        {

            bool isTunneled = !string.IsNullOrEmpty(customVariable.SourceObject) &&
                !string.IsNullOrEmpty(customVariable.SourceObjectProperty);



            if (isTunneled &&
                // This could be the case on an unload of the project:
                containingElement != null)
            {
                string property = customVariable.SourceObjectProperty;

                var nos = containingElement.GetNamedObjectRecursively(customVariable.SourceObject);
                var nosElement = GetElement(nos);
                var variableInNosElement = nosElement?.GetCustomVariableRecursively(customVariable.SourceObjectProperty);

                if (nosElement != null && variableInNosElement != null)
                {
                    (isState, category) = GetStateSaveCategory(variableInNosElement, nosElement);
                }
                else if (nosElement != null && customVariable.Type == "VariableState")
                {
                    // This is a special case where an object's VariableState is exposed directly.
                    // While this is not recommended because all states should be categorized, we still
                    // want to support old projects that may be doing this (and the auto test project too).
                    isState = true;
                    category = null;
                }
            }
            else
            {
                if (containingElement != null)
                {
                    if (customVariable.Type == "VariableState")
                    {
                        isState = true;
                        category = null;
                    }
                    else
                    {
                        var typeToSearchFor = customVariable.Type;
                        // this could be nullable since that's the type used in Variant codegen

                        if (typeToSearchFor?.EndsWith("?") == true)
                        {
                            typeToSearchFor = typeToSearchFor.Substring(0, typeToSearchFor.Length - 1);
                        }

                        category = containingElement.GetStateCategoryRecursively(typeToSearchFor);
                        isState = category != null;
                    }
                }
            }
        }

        if (!isState && customVariable.Type.StartsWith("Entities."))
        {
            // It may still be a state, so let's see the entity:
            var entityName = customVariable.GetEntityNameDefiningThisTypeCategory();

            var entity = ObjectFinder.Self.GetEntitySave(entityName);

            if (entity != null)
            {
                var lastPeriod = customVariable.Type.LastIndexOf('.');
                var startIndex = lastPeriod + 1;
                var stateCategory = customVariable.Type.Substring(startIndex);
                category = entity.GetStateCategory(stateCategory);
                isState = category != null;
            }
        }

        return (isState, category);
    }

    public CustomVariable GetBaseCustomVariable(CustomVariable customVariable, GlueElement owner = null)
    {
        if (customVariable?.DefinedByBase == true)
        {
            owner = owner ?? GetElementContaining(customVariable);

            if (owner != null)
            {
                var baseOfOwner = GetBaseElement(owner);

                if (baseOfOwner != null)
                {
                    var variableInBase = baseOfOwner.GetCustomVariable(customVariable.Name);

                    return GetBaseCustomVariable(variableInBase, baseOfOwner);

                }
            }
        }

        return customVariable;
    }

    public CustomVariable GetRootCustomVariable(CustomVariableInNamedObject variable, NamedObjectSave nos)
    {
        var element = ObjectFinder.Self.GetElement(nos);
        var variableInElement = element?.GetCustomVariableRecursively(variable.Member);

        if(variableInElement != null)
        {
            return GetRootCustomVariable(variableInElement);
        }
        return null;
    }

    public VariableDefinition GetVariableDefinition(string variableName, NamedObjectSave instance)
    {
        var element = ObjectFinder.Self.GetElement(instance);

        var customVariable = element?.GetCustomVariableRecursively(variableName);

        if(!string.IsNullOrEmpty( customVariable?.SourceObject ))
        {
            var innerInstance = element?.GetNamedObjectRecursively(customVariable.SourceObject);
            var variableOnInstance = customVariable.SourceObjectProperty;

            if(innerInstance != null)
            {
                return GetVariableDefinition(customVariable.SourceObjectProperty, innerInstance);
            }
        }

        if(element != null)
        {
            var rootElement = ObjectFinder.Self.GetRootBaseElement(element) ?? element;

            var ati = rootElement?.GetAssetTypeInfo();

        }
        else
        {
            return instance.GetAssetTypeInfo()?.VariableDefinitions.Find(item => item.Name == variableName);
        }


        return null;
    }

    public CustomVariable GetRootCustomVariable(CustomVariable customVariable)
    {
        var element = GetElementContaining(customVariable);

        var baseVariable = GetBaseCustomVariable(customVariable, element);

        if (!string.IsNullOrEmpty(baseVariable?.SourceObject))
        {
            var sourceObject = element.GetNamedObjectRecursively(baseVariable.SourceObject);
            if (sourceObject != null)
            {
                var sourceElement = GetElement(sourceObject);
                if (sourceElement != null)
                {
                    var sourceVariable = sourceElement.GetCustomVariableRecursively(baseVariable.SourceObjectProperty);
                    if (sourceVariable != null)
                    {
                        return GetRootCustomVariable(sourceVariable);
                    }
                }
            }
        }

        return baseVariable;
    }

    /// <summary>
    /// Returns the variable (InstructionSave) value on the argument NamedObjectSave recursively.
    /// </summary>
    /// <param name="instance">The NamedObjectSave, although this method may return values from base instances if the value isn't set
    /// on the argument instance.</param>
    /// <param name="container">The container of the NamedObjectSave. If the value is not found on this instance, this method will
    /// look in base GlueElements.</param>
    /// <param name="memberName">The name of the variable.</param>
    /// <returns>The value found, or null if not found.</returns>
    public object GetValueRecursively(NamedObjectSave instance, GlueElement container, string memberName)
    {
        var variableDefinition = instance?.GetAssetTypeInfo()?.VariableDefinitions.FirstOrDefault(item => item.Name == memberName);

        var typeName = variableDefinition?.Type;
        Type type = null;
        if (!string.IsNullOrEmpty(typeName))
        {
            type = TypeManager.GetTypeFromString(typeName);
        }

        return GetValueRecursively(instance, container, memberName, type, variableDefinition);
    }

    public object GetValueRecursively(NamedObjectSave instance, GlueElement container, string memberName, Type memberType, VariableDefinition variableDefinition)
    {
        var instruction = instance.GetCustomVariable(memberName);

        if (instruction == null)
        {
            GlueElement baseElement = null;
            if (instance.DefinedByBase)
            {
                baseElement = ObjectFinder.Self.GetBaseElement(container);

            }
            if (baseElement != null)
            {
                var nosInBase = baseElement.GetNamedObject(instance.InstanceName);
                if (nosInBase != null)
                {
                    var toReturn = GetValueRecursively(nosInBase, baseElement, memberName, memberType, variableDefinition);

                    if (toReturn != null)
                    {
                        return toReturn; ////////////////////////Early Out/////////////////////////////////
                    }
                }
            }
        }

        if (instruction == null)
        {
            // Get the value for this variable from the base element. 

            var getVariableResponse = GetVariableOnInstance(instance, container, memberName);

            if (getVariableResponse.customVariable?.DefaultValue != null)
            {
                return getVariableResponse.customVariable.DefaultValue;
            }
            else if (getVariableResponse.instructionOnState != null)
            {
                return getVariableResponse.instructionOnState.Value;
            }

            return variableDefinition?.GetCastedDefaultValue();
        }
        else
        {
            if (instruction.Value is int && memberType.IsEnum)
            {
                return Enum.ToObject(memberType, instruction.Value);
            }
            else
            {
                return instruction.Value;
            }
        }
    }

    private (CustomVariable customVariable, InstructionSave instructionOnState) GetVariableOnInstance(NamedObjectSave instance, GlueElement container, string memberName)
    {
        CustomVariable foundVariable = null;
        FlatRedBall.Content.Instructions.InstructionSave valueOnState = null;

        var instanceElementType = ObjectFinder.Self.GetElement(instance);



        if (instanceElementType != null)
        {


            foreach (var instructionOnObject in instance.InstructionSaves)
            {
                var variableOnInstanceName = instructionOnObject.Member;
                var variableOnInstanceValue = instructionOnObject.Value;

                // is it a state?
                CustomVariable possibleStateCustomVariable = instanceElementType.GetCustomVariable(variableOnInstanceName);

                StateSaveCategory matchingStateCategory = null;
                if (possibleStateCustomVariable != null)
                {
                    matchingStateCategory = instanceElementType.GetStateCategory(possibleStateCustomVariable.Type);
                }
                if (matchingStateCategory != null)
                {
                    var matchingState = matchingStateCategory.GetState(variableOnInstanceValue as string);
                    if (matchingState != null)
                    {
                        // does the state set the member?
                        valueOnState = matchingState.InstructionSaves.Find(item => item.Member == memberName && item.Value != null);

                    }
                }
                if (valueOnState != null)
                {
                    break;
                }
            }

            if (valueOnState == null)
            {
                // See if this variable is set by any states on the instance first
                var variablesOnThisInstance = container.CustomVariables.Where(item => item.SourceObject == instance.InstanceName);

                foreach (var variableOnInstance in variablesOnThisInstance)
                {
                    var variableOnInstanceName = variableOnInstance.Name;
                    var variableOnInstanceValue = variableOnInstance.DefaultValue;
                    // is it a state?
                    CustomVariable possibleStateCustomVariable = instanceElementType.GetCustomVariable(variableOnInstanceName);

                    StateSaveCategory matchingStateCategory = null;

                    if (possibleStateCustomVariable?.Type != null)
                    {
                        matchingStateCategory = instanceElementType.GetStateCategory(possibleStateCustomVariable.Type);
                    }

                    if (matchingStateCategory != null)
                    {
                        var matchingState = matchingStateCategory.GetState(variableOnInstanceValue as string);
                        if (matchingState != null)
                        {
                            // does the state set the member?
                            valueOnState = matchingState.InstructionSaves.Find(item => item.Member == memberName && item.Value != null);

                        }
                    }
                    if (valueOnState != null)
                    {
                        break;
                    }
                }
            }

            if (valueOnState == null)
            {
                foundVariable = instanceElementType.GetCustomVariableRecursively(memberName);
            }
        }

        return (foundVariable, valueOnState);
    }

    public List<CustomVariable> GetVariablesReferencingElementType(string elementType) =>
        ReferenceService.Self.GetVariablesReferencingElementType(elementType);

    #endregion

    public int GetHierarchyDepth(GlueElement element) =>
        ReferenceService.Self.GetHierarchyDepth(element);

    /// <summary>
    /// Returns all elements involved in the inheritance chain, with the most basic type first and most
    /// derived types after. The argument will be added as the last element in the list.
    /// </summary>
    public List<GlueElement> GetInheritanceChain(GlueElement derivedElement) =>
        ReferenceService.Self.GetInheritanceChain(derivedElement);
}
