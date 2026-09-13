using System.Collections.Generic;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Seam over Glue.csproj's <c>ObjectFinder.Self</c>, covering only the members the GlueCommon
    /// extractions in #2276 need to resolve elements/entities by name - something only
    /// <c>ObjectFinder</c> (Glue.csproj, net8.0-windows) can do today, since it holds the loaded
    /// <see cref="GlueProjectSave"/>. GlueCommon can't reference <c>ObjectFinder</c> directly (wrong
    /// direction - Glue.csproj references GlueCommon, not the reverse), so the real
    /// <c>ObjectFinder</c> implements this interface and wires itself into <see cref="ObjectFinderCore.Self"/>
    /// from its own static constructor. Follows the same "extract an interface for just the members a
    /// class calls" pattern as <c>IGlueState</c>/<c>ExportedInterfaces</c> - see #2276's third comment.
    /// </summary>
    public interface IObjectFinderCore
    {
        GlueProjectSave GlueProject { get; }
        GlueElement GetElement(string elementName);
        GlueElement GetElementContaining(NamedObjectSave namedObjectSave);
        GlueElement GetElementContaining(ReferencedFileSave referencedFileSave);
        EntitySave GetEntitySave(string entityName);
        List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement);
    }

    public static class ObjectFinderCore
    {
        public static IObjectFinderCore Self { get; set; }
    }
}
