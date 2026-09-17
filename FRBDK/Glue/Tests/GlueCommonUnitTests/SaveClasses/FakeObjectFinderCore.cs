using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

/// <summary>
/// Hand-rolled test double for <see cref="IObjectFinderCore"/> - the real implementation
/// (<c>ObjectFinder</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeObjectFinderCore : IObjectFinderCore
{
    public GlueProjectSave GlueProject { get; set; }

    readonly Dictionary<string, GlueElement> _elementsByName = new();
    readonly Dictionary<NamedObjectSave, GlueElement> _containersByNamedObject = new();
    readonly Dictionary<ReferencedFileSave, GlueElement> _containersByReferencedFile = new();
    readonly Dictionary<CustomVariable, GlueElement> _containersByCustomVariable = new();
    readonly Dictionary<StateSave, GlueElement> _containersByStateSave = new();
    readonly Dictionary<GlueElement, List<GlueElement>> _baseElementsByElement = new();

    public string ContentDirectory { get; set; } = "";

    /// <summary>
    /// Stands in for ObjectFinder.GetStateSaveCategory; defaults to "not a state".
    /// </summary>
    public Func<CustomVariable, GlueElement, (bool IsState, StateSaveCategory Category)> StateSaveCategoryResolver { get; set; } =
        (_, _) => (false, null);

    /// <summary>
    /// Stands in for ObjectFinder.GetValueRecursively(NamedObjectSave, GlueElement, string); defaults to "no value".
    /// </summary>
    public Func<NamedObjectSave, GlueElement, string, object> ValueRecursivelyResolver { get; set; } =
        (_, _, _) => null;

    public void AddElement(string name, GlueElement element) => _elementsByName[name] = element;

    public void SetContainer(NamedObjectSave namedObjectSave, GlueElement container) =>
        _containersByNamedObject[namedObjectSave] = container;

    public void SetContainer(ReferencedFileSave referencedFileSave, GlueElement container) =>
        _containersByReferencedFile[referencedFileSave] = container;

    public void SetContainer(CustomVariable customVariable, GlueElement container) =>
        _containersByCustomVariable[customVariable] = container;

    public void SetContainer(StateSave stateSave, GlueElement container) =>
        _containersByStateSave[stateSave] = container;

    public void SetBaseElements(GlueElement derivedElement, List<GlueElement> baseElements) =>
        _baseElementsByElement[derivedElement] = baseElements;

    public GlueElement GetElement(string elementName) =>
        elementName != null && _elementsByName.TryGetValue(elementName, out var element) ? element : null;

    public GlueElement GetElementContaining(NamedObjectSave namedObjectSave) =>
        _containersByNamedObject.TryGetValue(namedObjectSave, out var container) ? container : null;

    public GlueElement GetElementContaining(ReferencedFileSave referencedFileSave) =>
        _containersByReferencedFile.TryGetValue(referencedFileSave, out var container) ? container : null;

    public GlueElement GetElementContaining(CustomVariable customVariable) =>
        _containersByCustomVariable.TryGetValue(customVariable, out var container) ? container : null;

    public GlueElement GetElementContaining(StateSave stateSave) =>
        _containersByStateSave.TryGetValue(stateSave, out var container) ? container : null;

    public (bool IsState, StateSaveCategory Category) GetStateSaveCategory(CustomVariable customVariable, GlueElement containingElement) =>
        StateSaveCategoryResolver(customVariable, containingElement);

    public EntitySave GetEntitySave(string entityName) => GetElement(entityName) as EntitySave;

    public EntitySave GetEntitySave(NamedObjectSave nos) =>
        nos?.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos.SourceClassType) ? GetEntitySave(nos.SourceClassType) : null;

    public ScreenSave GetScreenSave(string screenName) => GetElement(screenName) as ScreenSave;

    public List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement) =>
        _baseElementsByElement.TryGetValue(derivedElement, out var baseElements) ? baseElements : new List<GlueElement>();

    public GlueElement GetBaseElement(IElement derivedElement) => GetElement(derivedElement?.BaseElement);

    public object GetValueRecursively(NamedObjectSave instance, GlueElement container, string memberName) =>
        ValueRecursivelyResolver(instance, container, memberName);

    public string MakeAbsoluteContent(string fileName) =>
        FlatRedBall.IO.FileManager.IsRelative(fileName) ? ContentDirectory + fileName : fileName;
}
