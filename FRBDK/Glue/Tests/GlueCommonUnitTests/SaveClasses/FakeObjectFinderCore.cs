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

    public void AddElement(string name, GlueElement element) => _elementsByName[name] = element;

    public void SetContainer(NamedObjectSave namedObjectSave, GlueElement container) =>
        _containersByNamedObject[namedObjectSave] = container;

    public GlueElement GetElement(string elementName) =>
        elementName != null && _elementsByName.TryGetValue(elementName, out var element) ? element : null;

    public GlueElement GetElementContaining(NamedObjectSave namedObjectSave) =>
        _containersByNamedObject.TryGetValue(namedObjectSave, out var container) ? container : null;

    public EntitySave GetEntitySave(string entityName) => GetElement(entityName) as EntitySave;
}
