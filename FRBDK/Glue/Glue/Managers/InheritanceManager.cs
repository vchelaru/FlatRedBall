using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CheckResult = FlatRedBall.Glue.ProjectManager.CheckResult;

namespace GlueFormsCore.Managers;

public class InheritanceManager
{
    public static InheritanceManager Self { get; internal set; }

    //Used to prevent recursive references and inheritence
    public static int VerificationId { get; set; }

    readonly IReferenceService _referenceService;

    public InheritanceManager(IReferenceService referenceService)
    {
        _referenceService = referenceService;
    }

    // The name is vague and the method does a ton, so it might be good to figure out a way to break this up
    public void UpdateAllDerivedElementFromBaseValues(bool regenerateCode, GlueElement currentElement = null)
    {
        currentElement = currentElement ?? GlueState.Self.CurrentElement;

        // This method can be slow, so we should store off the base elements to regenerate and only do those, in tasks, one by one:
        //HashSet<GlueElement> toRegenerate = new HashSet<GlueElement>();

        if (currentElement is EntitySave currentEntity)
        {
            List<EntitySave> derivedEntities = _referenceService.GetAllEntitiesThatInheritFrom(currentEntity.Name);

            List<NamedObjectSave> nosList = _referenceService.GetAllNamedObjectsThatUseEntity(currentEntity.Name);

            for (int i = 0; i < derivedEntities.Count; i++)
            {
                EntitySave entitySave = derivedEntities[i];

                nosList.AddRange(_referenceService.GetAllNamedObjectsThatUseEntity(entitySave.Name));

                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(entitySave);

                // Update the tree nodes
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entitySave);

                if (regenerateCode)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entitySave);
                }
            }

            foreach (NamedObjectSave nos in nosList)
            {
                nos.UpdateCustomProperties();

                var element = nos.GetContainer();

                if (element != null && regenerateCode)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }

            }
        }
        else if (currentElement is ScreenSave currentScreenSave)
        {
            List<ScreenSave> derivedScreens = _referenceService.GetAllScreensThatInheritFrom(currentScreenSave.Name);

            for (int i = 0; i < derivedScreens.Count; i++)
            {
                ScreenSave screenSave = derivedScreens[i];
                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(screenSave);

                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screenSave);

                if (regenerateCode)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screenSave);
                }
            }
        }
    }

    public void ReactToChangedBaseScreen(object oldValue, ScreenSave screenSave)
    {
        if (VerifyInheritanceGraph(screenSave) == CheckResult.Failed)
        {
            screenSave.BaseScreen = (string)oldValue;
        }
        else
        {
            //screenSave.UpdateFromBaseType();
            GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(screenSave);
        }
    }

    public void ReactToChangedBaseEntity(string oldValue, EntitySave entitySave)
    {
        bool isValidBase = GetIfCurrentEntityBaseIsValid(entitySave);

        if (isValidBase == false)
        {
            entitySave.BaseEntity = (string)oldValue;
            MainGlueWindow.Self.PropertyGrid.Refresh();
        }
        else
        {
            var oldEntity = ObjectFinder.Self.GetEntitySave(oldValue);
            var newEntity = ObjectFinder.Self.GetEntitySave(entitySave.BaseEntity);

            HashSet<ScreenSave> screensToRegenerate = new HashSet<ScreenSave>();

            if (oldEntity != null)
            {
                var allObjects = _referenceService.GetAllNamedObjectsThatUseEntity(oldEntity);
                var screens = allObjects.Select(item => item.GetContainer())
                    .Where(item => item as ScreenSave != null)
                    .Select(item => item as ScreenSave);

                foreach (var screen in screens)
                {
                    screensToRegenerate.Add(screen);
                }
            }

            if (newEntity != null)
            {
                var allObjects = _referenceService.GetAllNamedObjectsThatUseEntity(newEntity);
                var screens = allObjects.Select(item => item.GetContainer())
                    .Where(item => item as ScreenSave != null)
                    .Select(item => item as ScreenSave);

                foreach (var screen in screens)
                {
                    screensToRegenerate.Add(screen);
                }
            }

            foreach (var screen in screensToRegenerate)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
            }

            List<CustomVariable> variablesBefore = new List<CustomVariable>();

            variablesBefore.AddRange(entitySave.CustomVariables);

            GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(entitySave);

            AskToPreserveVariablesAfterInheritanceChange(entitySave, variablesBefore);
        }
        if(entitySave == GlueState.Self.CurrentEntitySave)
        {
            PropertyGridHelper.UpdateEntitySaveDisplay();
        }
    }

    private bool GetIfCurrentEntityBaseIsValid(EntitySave entitySave)
    {
        bool isValidBase = true;

        // Gotta check the new inhertiance tree to make sure that there are no duplicate object names
        if (!string.IsNullOrEmpty(entitySave.BaseEntity))
        {
            List<EntitySave> thisAndDerivedFromThisList;
            List<EntitySave> baseEntities = new List<EntitySave>();

            thisAndDerivedFromThisList = _referenceService.GetAllEntitiesThatInheritFrom(entitySave.Name);
            thisAndDerivedFromThisList.Add(entitySave);

            EntitySave newBase = ObjectFinder.Self.GetEntitySave(entitySave.BaseEntity);

            if (newBase != null)
            {

                baseEntities = GetAllEntitiesThatThisInherits(entitySave.BaseEntity);
                baseEntities.Add(newBase);
            }

            List<string> derivedNamedObjects = new List<string>();
            List<string> baseNamedObjects = new List<string>();
            List<string> baseNamedObjectsIncludingSetByDerived = new List<string>();

            List<string> derivedReferencedFiles = new List<string>();
            List<string> baseReferencedFiles = new List<string>();

            foreach (EntitySave es in baseEntities)
            {
                foreach (NamedObjectSave nos in es.NamedObjects)
                {
                    if (!nos.SetByDerived)
                    {
                        baseNamedObjects.Add(nos.InstanceName);
                    }
                    baseNamedObjectsIncludingSetByDerived.Add(nos.InstanceName);
                }


                foreach (ReferencedFileSave rfs in es.ReferencedFiles)
                {
                    baseReferencedFiles.Add(rfs.GetInstanceName());
                }
            }

            foreach (EntitySave es in thisAndDerivedFromThisList)
            {
                foreach (NamedObjectSave nos in es.NamedObjects)
                {
                    if (!nos.DefinedByBase)
                    {
                        derivedNamedObjects.Add(nos.InstanceName);
                    }
                }

                foreach (ReferencedFileSave rfs in es.ReferencedFiles)
                {
                    derivedReferencedFiles.Add(rfs.GetInstanceName());
                }
            }



            for (int i = 0; i < derivedNamedObjects.Count; i++)
            {
                if (baseNamedObjects.Contains(derivedNamedObjects[i]))
                {
                    DialogService.ShowMessage("There is a duplicate named object:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                    isValidBase = false;
                    break;
                }

                if (baseReferencedFiles.Contains(derivedNamedObjects[i]))
                {
                    DialogService.ShowMessage("There is a file and object both named:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                    isValidBase = false;
                    break;
                }
            }

            for (int i = 0; i < derivedReferencedFiles.Count; i++)
            {
                if (baseNamedObjectsIncludingSetByDerived.Contains(derivedReferencedFiles[i]))
                {
                    DialogService.ShowMessage("There is a file and object both named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                    isValidBase = false;
                    break;
                }

                if (baseReferencedFiles.Contains(derivedReferencedFiles[i]))
                {
                    DialogService.ShowMessage("There are two files named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                    isValidBase = false;
                    break;
                }
            }
        }


        if (isValidBase && VerifyInheritanceGraph(entitySave) == CheckResult.Failed)
        {
            isValidBase = false;
        }

        return isValidBase;
    }

    private void AskToPreserveVariablesAfterInheritanceChange(EntitySave entitySave, List<CustomVariable> variablesBefore)
    {
        foreach (CustomVariable oldVariable in variablesBefore)
        {
            if (entitySave.GetCustomVariableRecursively(oldVariable.Name) == null)
            {
                string message = "The variable\n\n" + oldVariable.ToString() + "\n\nIs no longer part of the Entity.  What do you want to do?";

                // If closed without a click (e.g. Escape), ShowConfirm returns null, which - same as the
                // original code (which didn't check ShowDialog()'s return value either) - falls through to
                // "do nothing, the variable will go away".
                var selectedOption = DialogService.ShowConfirm(message, DialogButton.Yes, DialogButton.No);

                if (selectedOption == DialogButton.Yes)
                {
                    var newVariable = new CustomVariable();
                    newVariable.Type = oldVariable.Type;
                    newVariable.Name = oldVariable.Name;
                    newVariable.DefaultValue = oldVariable.DefaultValue;
                    newVariable.SourceObject = oldVariable.SourceObject;
                    newVariable.SourceObjectProperty = oldVariable.SourceObjectProperty;

                    newVariable.Properties = new List<PropertySave>();
                    newVariable.Properties.AddRange(oldVariable.Properties);

                    newVariable.HasAccompanyingVelocityProperty = oldVariable.HasAccompanyingVelocityProperty;
                    newVariable.CreatesEvent = oldVariable.CreatesEvent;
                    newVariable.IsShared = oldVariable.IsShared;

                    if (!string.IsNullOrEmpty(oldVariable.OverridingPropertyType))
                    {
                        newVariable.OverridingPropertyType = oldVariable.OverridingPropertyType;
                        newVariable.TypeConverter = oldVariable.TypeConverter;
                    }

                    newVariable.CreatesEvent = oldVariable.CreatesEvent;

                    entitySave.CustomVariables.Add(newVariable);

                }

            }
        }
    }

    private static List<EntitySave> GetAllEntitiesThatThisInherits(string derivedEntity)
    {
        List<EntitySave> listToReturn = new List<EntitySave>();

        EntitySave entity = ObjectFinder.Self.GetEntitySave(derivedEntity);

        entity.GetAllBaseEntities(listToReturn);

        return listToReturn;
    }

    internal CheckResult VerifyInheritanceGraph(INamedObjectContainer node)
    {
        var project = GlueState.Self.CurrentGlueProject;
        if (project != null)
        {
            VerificationId++;
            string resultString = "";

            if (InheritanceVerificationHelper(ref node, ref resultString) == CheckResult.Failed)
            {
                DialogService.ShowMessage("This assignment has created an inheritence cycle containing the following classes:\n\n" +
                                resultString +
                                "\nThe assignment will be undone.");
                node.BaseObject = null;
                return CheckResult.Failed;
            }

        }

        return CheckResult.Passed;
    }

    private static CheckResult InheritanceVerificationHelper(ref INamedObjectContainer node, ref string cycleString)
    {
        //Assign the current VerificationId to identify nodes that have been visited
        node.VerificationIndex = VerificationId;

        //Travel upward through the inheritence tree from this object, stopping when either the
        //tree stops, or you reach a node that's already been visited.
        if (!string.IsNullOrEmpty(node.BaseObject))
        {
            INamedObjectContainer baseNode = ObjectFinder.Self.GetNamedObjectContainer(node.BaseObject);

            if (baseNode == null)
            {
                // We do nothing - the base object for this
                // Entity doesn't exist, so we'll continue as if this thing doesn't really have
                // a base Entity.  The user will have to address this in the Glue UI
            }
            else if (baseNode.VerificationIndex != VerificationId)
            {

                //If baseNode verification failed, add this node's name to the list and return Failed
                if (InheritanceVerificationHelper(ref baseNode, ref cycleString) == CheckResult.Failed)
                {
                    cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" + cycleString;
                    return CheckResult.Failed;
                }

            }
            else
            {
                //If the basenode has already been visited, begin the cycleString and return Failed

                cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" +
                                (baseNode as FlatRedBall.Utilities.INameable).Name + "\n";

                return CheckResult.Failed;
            }
        }


        return CheckResult.Passed;
    }

    /// <summary>
    /// Updates the argument glueElement from its base types. This updates variables and named objects. This is called whenever an element's base type changes,
    /// which can result in the element having new variables and named objects (automatically inherited), or existing variables and named objects being modified
    /// (such as being marked as instantiated by base).
    /// </summary>
    /// <param name="glueElement">The base Glue element to update.</param>
    /// <returns>Whether the object updated</returns>
    public bool UpdateFromBaseType(GlueElement glueElement, bool showPopupAboutObjectErrors = true)
    {
        bool haveChangesOccurred = false;
        if (ObjectFinder.Self.GlueProject != null)
        {
            haveChangesOccurred |= UpdateNamedObjectsFromBaseType(glueElement, showPopupAboutObjectErrors);

            UpdateCustomVariablesFromBaseType(glueElement);
        }
        return haveChangesOccurred;
    }

    /// <summary>
    /// This method is called whenever the derivedGlueElement has its base type changed, resulting in
    /// inheritance-based properties on the NamedObjects needing to be updated.
    /// </summary>
    /// <param name="derivedGlueElement">The derived element which conains named objects which should be upated.</param>
    /// <param name="showPopupAboutObjectErrors">Whether to show popups on errors. This should be true if this is called in response to a UI action.</param>
    /// <returns>Whether any changes have happened on the NamedObjects, which means a save is needed.</returns>
    private bool UpdateNamedObjectsFromBaseType(GlueElement derivedGlueElement, bool showPopupAboutObjectErrors)
    {
        bool haveChangesOccurred = false;

        List<NamedObjectSave> referencedObjectsBeforeUpdate = derivedGlueElement.AllNamedObjects.Where(item => item.DefinedByBase).ToList();

        List<NamedObjectSave> namedObjectsInBaseSetByDerived = new List<NamedObjectSave>();
        List<NamedObjectSave> namedObjectsExposedInDerived = new List<NamedObjectSave>();

        List<INamedObjectContainer> baseElements = new List<INamedObjectContainer>();

        // July 24, 2011
        // Before today, this
        // code would loop through
        // all base Entities and search
        // for SetByDerived properties in
        // any NamedObjectSave. This caused
        // bugs.  Basically if you had 3 Elements
        // in an inheritance chain and the one at the
        // very base defined a NOS to be SetByDerived, then
        // anything that inherited directly from the base should
        // be forced to define it.  If it does, then the 3rd Element
        // in the inheritance chain shouldn't have to define it, but before
        // today it did.  This caused a lot of problems including generated
        // code creating the element twice.
        if (derivedGlueElement is EntitySave)
        {
            if (!string.IsNullOrEmpty(derivedGlueElement.BaseObject))
            {
                baseElements.Add(ObjectFinder.Self.GetElement(derivedGlueElement.BaseObject));
            }
            //List<EntitySave> allBase = ((EntitySave)namedObjectContainer).GetAllBaseEntities();
            //foreach (EntitySave baseEntitySave in allBase)
            //{
            //    baseElements.Add(baseEntitySave);
            //}
        }
        else
        {
            if (!string.IsNullOrEmpty(derivedGlueElement.BaseObject))
            {
                baseElements.Add(ObjectFinder.Self.GetElement(derivedGlueElement.BaseObject));
            }
            //List<ScreenSave> allBase = ((ScreenSave)namedObjectContainer).GetAllBaseScreens();
            //foreach (ScreenSave baseScreenSave in allBase)
            //{
            //    baseElements.Add(baseScreenSave);
            //}
        }


        foreach (INamedObjectContainer baseNamedObjectContainer in baseElements)
        {

            if (baseNamedObjectContainer != null)
            {
                namedObjectsInBaseSetByDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeSetByDerived());
                namedObjectsExposedInDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeExposedInDerived());
            }
        }


        var derivedNosesToAskAbout = new List<NamedObjectSave>();


        for (int i = referencedObjectsBeforeUpdate.Count - 1; i > -1; i--)
        {
            var atI = referencedObjectsBeforeUpdate[i];

            var contains = atI.DefinedByBase && namedObjectsInBaseSetByDerived.Any(item => item.InstanceName == atI.InstanceName);

            if (!contains)
            {
                contains = namedObjectsExposedInDerived.Any(item => item.InstanceName == atI.InstanceName);
            }

            if (!contains)
            {

                NamedObjectSave nos = referencedObjectsBeforeUpdate[i];

                derivedNosesToAskAbout.Add(nos);
            }
        }


        #region See if there are any objects to be removed from the derived.
        if (derivedNosesToAskAbout.Count > 0 && showPopupAboutObjectErrors)
        {
            // This can happen whenever, like if a project is reloaded and data is changed on disk. However, this
            // code should never hit as a result of the user making changes in the UI, that should be caught earlier.
            // See SetByDerivedSetLogic
            var singleOrPluralPhrase = derivedNosesToAskAbout.Count == 1 ? "object is" : "objects are";
            var thisOrTheseObjects = derivedNosesToAskAbout.Count == 1 ? "this object" : "these objects";


            string message = "The following object is marked as \"defined by base\" but not contained in " +
                "any base elements\n\n";

            foreach (var nos in derivedNosesToAskAbout)
            {
                message += nos.ToString() + "\n";
            }

            message += "\nWhat would you like to do?";

            // If closed without a click (e.g. Escape), ShowConfirm returns null, which matches neither the
            // "remove" nor "keep, set defined-by-base false" branches below - same as the original code's
            // (dialogResult == true && ...) guard on the first branch.
            var confirmResult = DialogService.ShowConfirm(message, DialogButton.Yes, DialogButton.No);

            if (confirmResult == DialogButton.Yes)
            {
                foreach (var nos in derivedNosesToAskAbout)
                {
                    if (derivedGlueElement.NamedObjects.Contains(nos))
                    {
                        derivedGlueElement.NamedObjects.Remove(nos);
                    }
                    else
                    {
                        derivedGlueElement.NamedObjects
                            .FirstOrDefault(item => item.ContainedObjects.Contains(nos))
                            ?.ContainedObjects.Remove(nos);
                    }
                    // We got a NamedObject we should remove
                    referencedObjectsBeforeUpdate.Remove(nos);
                }
            }
            else if (confirmResult == DialogButton.No)
            {
                foreach (var nos in derivedNosesToAskAbout)
                {
                    nos.DefinedByBase = false;
                    nos.InstantiatedByBase = false;
                }
            }
            haveChangesOccurred = true;
        }
        #endregion

        #region Next, see if there are any objects to be added
        for (int i = 0; i < namedObjectsInBaseSetByDerived.Count; i++)
        {
            NamedObjectSave namedObjectInBase = namedObjectsInBaseSetByDerived[i];

            NamedObjectSave matchingDefinedByBase = null;// contains = false;
            for (int j = 0; j < referencedObjectsBeforeUpdate.Count; j++)
            {
                if (referencedObjectsBeforeUpdate[j].InstanceName == namedObjectInBase.InstanceName &&
                    referencedObjectsBeforeUpdate[j].DefinedByBase)
                {
                    matchingDefinedByBase = referencedObjectsBeforeUpdate[j];
                    break;
                }
            }

            if (matchingDefinedByBase == null)
            {
                AddSetByDerivedNos(derivedGlueElement, namedObjectInBase, false);
            }
            else
            {
                MatchDerivedToBase(namedObjectInBase, matchingDefinedByBase);
            }
        }

        for (int i = 0; i < namedObjectsExposedInDerived.Count; i++)
        {
            NamedObjectSave nosInBase = namedObjectsExposedInDerived[i];

            NamedObjectSave nosInDerived = referencedObjectsBeforeUpdate
                .FirstOrDefault(item => item.InstanceName == nosInBase.InstanceName && item.DefinedByBase);


            if (nosInDerived == null)
            {
                nosInDerived = AddSetByDerivedNos(derivedGlueElement, nosInBase, true);
            }
            else
            {
                MatchDerivedToBase(nosInBase, nosInDerived);
            }

            foreach (var containedInBaseNos in nosInBase.ContainedObjects)
            {
                if (containedInBaseNos.ExposedInDerived)
                {
                    var containedInDerived = referencedObjectsBeforeUpdate
                        .FirstOrDefault(item => item.InstanceName == containedInBaseNos.InstanceName && item.DefinedByBase);
                    if (containedInDerived == null)
                    {
                        AddSetByDerivedNos(derivedGlueElement, containedInBaseNos, true, nosInDerived);
                    }
                    else
                    {
                        MatchDerivedToBase(containedInBaseNos, containedInDerived);
                    }
                }
            }
        }

        #endregion

        return haveChangesOccurred;
    }

    private static NamedObjectSave AddSetByDerivedNos(INamedObjectContainer derivedContainer,
    NamedObjectSave namedObjectInBase, bool instantiatedByBase, NamedObjectSave containerToAddTo = null)
    {
        NamedObjectSave existingNamedObject = derivedContainer.AllNamedObjects
            .FirstOrDefault(item => item.InstanceName == namedObjectInBase.InstanceName);

        if (existingNamedObject != null)
        {
            existingNamedObject.DefinedByBase = true;
            existingNamedObject.InstantiatedByBase = instantiatedByBase;
            return existingNamedObject;
        }
        else
        {
            NamedObjectSave newNamedObject = namedObjectInBase.Clone();

            // This code may be cloning a list with contained objects, and the
            // contained objects may not SetByDerived
            newNamedObject.ContainedObjects.Clear();
            foreach (var containedCandidate in namedObjectInBase.ContainedObjects)
            {
                if (containedCandidate.SetByDerived)
                {
                    newNamedObject.ContainedObjects.Add(containedCandidate);
                }
            }

            // For more information on this property, see GlueProjectSaveExtensions.DetermineIfShouldStripNos
            newNamedObject.SetDefinedByBaseRecursively(true);
            newNamedObject.SetInstantiatedByBaseRecursively(instantiatedByBase);

            // This can't be set by derived because an object it inherits from has that already set
            newNamedObject.SetSetByDerivedRecursively(false);

            if (containerToAddTo == null)
            {
                var indexToAddAt = derivedContainer.NamedObjects.Count;

                var baseContainer = ObjectFinder.Self.GetElementContaining(namedObjectInBase);
                if (baseContainer != null)
                {
                    var indexOfNosInBase = baseContainer.NamedObjects.IndexOf(namedObjectInBase);
                    if (indexOfNosInBase == -1)
                    {
                        var container = baseContainer.NamedObjects.FirstOrDefault(item =>
                            item.ContainedObjects.Contains(namedObjectInBase));
                        // todo - this is rare, but we need to find what index this is after in the base, and make this get added
                        // after in the derived:
                    }
                    else
                    {
                        for (int i = indexOfNosInBase - 1; i > -1; i--)
                        {
                            var itemInBase = baseContainer.NamedObjects[i];
                            var name = itemInBase.InstanceName;

                            var foundMatchInDerived = derivedContainer.NamedObjects.Find(item => item.InstanceName == name);
                            if (foundMatchInDerived != null)
                            {
                                indexToAddAt = derivedContainer.NamedObjects.IndexOf(foundMatchInDerived) + 1;
                                break;
                            }
                        }
                    }
                }

                derivedContainer.NamedObjects.Insert(indexToAddAt, newNamedObject);
            }
            else
            {
                containerToAddTo.ContainedObjects.Add(newNamedObject);
            }

            return newNamedObject;
        }
    }

    private void UpdateCustomVariablesFromBaseType(GlueElement derivedElement)
    {
        GlueElement baseElement = _referenceService.GetBaseElement(derivedElement);

        List<CustomVariable> customVariablesBeforeUpdate = derivedElement.CustomVariables
            .Where(item => item.DefinedByBase)
            .ToList();

        List<CustomVariable> newCustomVariables =
            baseElement?.GetCustomVariablesToBeSetByDerived()?? new List<CustomVariable>();

        // See if there are any variables to be removed.
        for (int i = customVariablesBeforeUpdate.Count - 1; i > -1; i--)
        {
            bool contains = newCustomVariables.Any(item => item.Name == customVariablesBeforeUpdate[i].Name);

            if (!contains)
            {
                // We got a NamedObject we should remove
                derivedElement.CustomVariables.Remove(customVariablesBeforeUpdate[i]);
                customVariablesBeforeUpdate.RemoveAt(i);
            }
        }

        // Next, see if there are any variables to be added
        for (int i = 0; i < newCustomVariables.Count; i++)
        {
            bool alreadyContainedAsDefinedByBase =
                customVariablesBeforeUpdate.Any(item => item.Name == newCustomVariables[i].Name);

            if (!alreadyContainedAsDefinedByBase)
            {
                // There isn't a variable by this
                // name that is already DefinedByBase,
                // but there may still be a variable that
                // is just a regular variable - and in that
                // case we want to connect the existing variable
                // with the variable in the base
                CustomVariable existingInDerived = derivedElement.GetCustomVariable(newCustomVariables[i].Name);
                if (existingInDerived != null)
                {
                    existingInDerived.DefinedByBase = true;
                }
                else
                {
                    CustomVariable customVariable = newCustomVariables[i].Clone();

                    // March 4, 2012
                    // We used to not
                    // change the SourceObject
                    // or the SourceObjectProperty
                    // values; however, this new variable
                    // should behave like an exposed variable,
                    // so it shouldn't have any SourceObject or
                    // SourceObjectProperty.  If it did, then it
                    // will access the base NamedObjectSave to set
                    // its property, and this could cause compilation
                    // errors if the NOS in the base is marked as private.
                    // It may also avoid raising events defined in the base.
                    // Update April 13, 2025
                    // This is causing problems if the base source object is ExposedInDerived
                    // because this variable is not connected. I originally considered setting
                    // SourceObject and SourceObjectProperty, but this may have side effects that
                    // I'd have to deal with later. Instead, I'm going to look at where we set variables
                    // and do a recursive serach for the definition, see if it's got a source object, and
                    // set the value there.
                    customVariable.SourceObject = null;
                    customVariable.SourceObjectProperty = null;

                    // December 13, 2025
                    // When we create a variable
                    // in a derived element, the derived
                    // variable should not explicitly set
                    // its value. It should instead have a
                    // null value to indicate that it is inheriting
                    // its value from the base element:
                    customVariable.DefaultValue = null;

                    customVariable.DefinedByBase = true;
                    // We'll assume that this thing is going to be the actual definition
                    // Update April 11, 2023
                    // Setting this to false doesn't
                    // prevent this from being the final
                    // definition. We want the SetByDerived
                    // to be true so that the derived variable
                    // can get removed when saving the .json file.
                    //customVariable.SetByDerived = false;
                    customVariable.SetByDerived = true;

                    var indexToInsertAt = derivedElement.CustomVariables.Count;
                    if (i == 0)
                    {
                        indexToInsertAt = 0;
                    }
                    else
                    {
                        var itemBefore = newCustomVariables[i - 1];
                        var withMatchingName = derivedElement.CustomVariables.Find(item => item.Name == itemBefore.Name);
                        if (withMatchingName != null)
                        {
                            indexToInsertAt = 1 + derivedElement.CustomVariables.IndexOf(withMatchingName);
                        }
                    }

                    derivedElement.CustomVariables.Insert(indexToInsertAt, customVariable);
                }
            }
        }

        // update the category
        foreach (var customVariable in derivedElement.CustomVariables)
        {
            if (customVariable.DefinedByBase)
            {
                var baseVariable = baseElement.CustomVariables.FirstOrDefault(item => item.Name == customVariable.Name);
                if (!string.IsNullOrEmpty(baseVariable?.Category))
                {
                    customVariable.Category = baseVariable.Category;
                }
            }
        }
    }



    private static void MatchDerivedToBase(NamedObjectSave inBase, NamedObjectSave inDerived)
    {
        inDerived.SourceClassGenericType = inBase.SourceClassGenericType;
    }
}
