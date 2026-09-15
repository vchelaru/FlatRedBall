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
    readonly Dictionary<GlueElement, List<GlueElement>> _baseElementsByElement = new();

    public string ContentDirectory { get; set; } = "";

    public void AddElement(string name, GlueElement element) => _elementsByName[name] = element;

    public void SetContainer(NamedObjectSave namedObjectSave, GlueElement container) =>
        _containersByNamedObject[namedObjectSave] = container;

    public void SetContainer(ReferencedFileSave referencedFileSave, GlueElement container) =>
        _containersByReferencedFile[referencedFileSave] = container;

    public void SetBaseElements(GlueElement derivedElement, List<GlueElement> baseElements) =>
        _baseElementsByElement[derivedElement] = baseElements;

    public GlueElement GetElement(string elementName) =>
        elementName != null && _elementsByName.TryGetValue(elementName, out var element) ? element : null;

    public GlueElement GetElementContaining(NamedObjectSave namedObjectSave) =>
        _containersByNamedObject.TryGetValue(namedObjectSave, out var container) ? container : null;

    public GlueElement GetElementContaining(ReferencedFileSave referencedFileSave) =>
        _containersByReferencedFile.TryGetValue(referencedFileSave, out var container) ? container : null;

    public EntitySave GetEntitySave(string entityName) => GetElement(entityName) as EntitySave;

    public List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement) =>
        _baseElementsByElement.TryGetValue(derivedElement, out var baseElements) ? baseElements : new List<GlueElement>();

    public string MakeAbsoluteContent(string fileName) =>
        FlatRedBall.IO.FileManager.IsRelative(fileName) ? ContentDirectory + fileName : fileName;
}
