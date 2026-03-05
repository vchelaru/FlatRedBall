using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Elements;

/// <summary>
/// Abstraction over <see cref="ReferenceService"/> so consumers can be unit-tested without
/// a live <see cref="ObjectFinder"/>.
/// </summary>
public interface IReferenceService
{
    #region Composite "find all references" queries

    IReadOnlyList<object> GetReferencesTo(ReferencedFileSave rfs);
    IReadOnlyList<object> GetReferencesToElement(IElement element);
    IReadOnlyList<object> GetReferencesTo(NamedObjectSave namedObjectSave, IElement container);
    IReadOnlyList<object> GetReferencesTo(CustomVariable customVariable, IElement container);

    #endregion

    #region File / RFS reference finding

    List<ReferencedFileSave> GetReferencedFileSavesFromSource(string sourceFile);
    List<ReferencedFileSave> GetAllReferencedFiles();
    List<ReferencedFileSave> GetMatchingReferencedFiles(ReferencedFileSave rfs);
    List<GlueElement> GetAllElementsReferencingFile(string rfsName);

    #endregion

    #region Named-object / entity reference finding

    IEnumerable<NamedObjectSave> GetAllNamedObjects();
    List<NamedObjectSave> GetAllNamedObjectsThatUseElement(IElement element);
    List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(EntitySave entity);
    List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(string baseEntity);
    List<NamedObjectSave> GetAllNamedObjectsThatUseEntityAsVariableType(string entityType);

    #endregion

    #region Variable / type reference finding

    List<CustomVariable> GetVariablesReferencingElementType(string elementType);

    #endregion

    #region Inheritance traversal

    List<EntitySave> GetAllEntitiesThatInheritFrom(EntitySave baseEntity);
    List<EntitySave> GetAllEntitiesThatInheritFrom(string baseEntity);
    List<ScreenSave> GetAllScreensThatInheritFrom(ScreenSave baseScreen);
    List<ScreenSave> GetAllScreensThatInheritFrom(string baseScreen);
    List<GlueElement> GetAllElementsThatInheritFrom(string elementName);
    List<GlueElement> GetAllElementsThatInheritFrom(IElement baseElement);
    bool GetIfInherits(GlueElement derivedElement, GlueElement baseElement);
    GlueElement GetRootBaseElement(GlueElement element);
    GlueElement GetBaseElement(IElement derivedElement);
    GlueElement GetBaseElementRecursively(GlueElement derivedElement);
    List<GlueElement> GetAllBaseElementsRecursively(GlueElement derivedElement);
    List<GlueElement> GetAllDerivedElementsRecursive(GlueElement baseElement);
    int GetHierarchyDepth(GlueElement element);
    List<GlueElement> GetInheritanceChain(GlueElement derivedElement);

    #endregion
}
