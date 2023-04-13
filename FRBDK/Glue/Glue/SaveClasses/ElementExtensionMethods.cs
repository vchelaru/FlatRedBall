using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ElementExtensionMethods
    {
        // todo - move this to ElementCommands
        [Obsolete("Use ElementCommands.UpdateFromBaseType")]
        public static bool UpdateNamedObjectsFromBaseType(INamedObjectContainer namedObjectContainer)
        {
            bool haveChangesOccurred = false;

            List<NamedObjectSave> referencedObjectsBeforeUpdate = namedObjectContainer.AllNamedObjects.Where(item => item.DefinedByBase).ToList();

            List<NamedObjectSave> namedObjectsSetByDerived = new List<NamedObjectSave>();
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
            if (namedObjectContainer is EntitySave)
            {
                if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
                {
                    baseElements.Add(ObjectFinder.Self.GetElement(namedObjectContainer.BaseObject));
                }
                //List<EntitySave> allBase = ((EntitySave)namedObjectContainer).GetAllBaseEntities();
                //foreach (EntitySave baseEntitySave in allBase)
                //{
                //    baseElements.Add(baseEntitySave);
                //}
            }
            else
            {
                if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
                {
                    baseElements.Add(ObjectFinder.Self.GetElement(namedObjectContainer.BaseObject));
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
                    namedObjectsSetByDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeSetByDerived());
                    namedObjectsExposedInDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeExposedInDerived());
                }
            }

            #region See if there are any objects to be removed from the derived.

            var derivedNosesToAskAbout = new List<NamedObjectSave>();


            for (int i = referencedObjectsBeforeUpdate.Count - 1; i > -1; i--)
            {
                var atI = referencedObjectsBeforeUpdate[i];

                var contains = atI.DefinedByBase && namedObjectsSetByDerived.Any(item => item.InstanceName == atI.InstanceName);

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


            if(derivedNosesToAskAbout.Count > 0)
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

                var mbmb = new MultiButtonMessageBoxWpf();

                mbmb.MessageText = message;

                mbmb.AddButton($"Remove {thisOrTheseObjects}", DialogResult.Yes);
                mbmb.AddButton($"Keep {thisOrTheseObjects}, set \"defined by base\" to false", DialogResult.No);

                var dialogResult = mbmb.ShowDialog();

                if(dialogResult == true && (DialogResult)mbmb.ClickedResult == DialogResult.Yes)
                {
                    foreach(var nos in derivedNosesToAskAbout)
                    {
                        if(namedObjectContainer.NamedObjects.Contains(nos))
                        {
                            namedObjectContainer.NamedObjects.Remove(nos);
                        }
                        else
                        {
                            namedObjectContainer.NamedObjects
                                .FirstOrDefault(item => item.ContainedObjects.Contains(nos))
                                ?.ContainedObjects.Remove(nos);
                        }
                        // We got a NamedObject we should remove
                        referencedObjectsBeforeUpdate.Remove(nos);
                    }
                }
                else if((DialogResult)mbmb.ClickedResult == DialogResult.No)
                {
                    foreach(var nos in derivedNosesToAskAbout)
                    {
                        nos.DefinedByBase = false;
                        nos.InstantiatedByBase = false;
                    }
                }
                haveChangesOccurred = true;
            }
            #endregion

            #region Next, see if there are any objects to be added
            for (int i = 0; i < namedObjectsSetByDerived.Count; i++)
            {
                NamedObjectSave namedObjectSetByDerived = namedObjectsSetByDerived[i];

                NamedObjectSave matchingDefinedByBase = null;// contains = false;
                for (int j = 0; j < referencedObjectsBeforeUpdate.Count; j++)
                {
                    if (referencedObjectsBeforeUpdate[j].InstanceName == namedObjectSetByDerived.InstanceName &&
                        referencedObjectsBeforeUpdate[j].DefinedByBase)
                    {
                        matchingDefinedByBase = referencedObjectsBeforeUpdate[j];
                        break;
                    }
                }

                if (matchingDefinedByBase == null)
                {
                    AddSetByDerivedNos(namedObjectContainer, namedObjectSetByDerived, false);
                }
                else
                {
                    MatchDerivedToBase(namedObjectSetByDerived, matchingDefinedByBase);
                }
            }

            for (int i = 0; i < namedObjectsExposedInDerived.Count; i++)
            {
                NamedObjectSave nosInBase = namedObjectsExposedInDerived[i];

                NamedObjectSave nosInDerived = referencedObjectsBeforeUpdate
                    .FirstOrDefault(item => item.InstanceName == nosInBase.InstanceName && item.DefinedByBase);
                

                if (nosInDerived == null)
                {
                    nosInDerived = AddSetByDerivedNos(namedObjectContainer, nosInBase, true);
                }
                else
                {
                    MatchDerivedToBase(nosInBase, nosInDerived);
                }

                foreach(var containedInBaseNos in nosInBase.ContainedObjects)
                {
                    if(containedInBaseNos.ExposedInDerived)
                    {
                        var containedInDerived = referencedObjectsBeforeUpdate
                            .FirstOrDefault(item => item.InstanceName == containedInBaseNos.InstanceName && item.DefinedByBase);
                        if (containedInDerived == null)
                        {
                            AddSetByDerivedNos(namedObjectContainer, containedInBaseNos, true, nosInDerived);
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

        private static void MatchDerivedToBase(NamedObjectSave inBase, NamedObjectSave inDerived)
        {
            inDerived.SourceClassGenericType = inBase.SourceClassGenericType;
        }

        private static NamedObjectSave AddSetByDerivedNos(INamedObjectContainer namedObjectContainer,
            NamedObjectSave namedObjectSetByDerived, bool instantiatedByBase, NamedObjectSave containerToAddTo = null)
        {
            NamedObjectSave existingNamedObject = namedObjectContainer.AllNamedObjects
                .FirstOrDefault(item => item.InstanceName == namedObjectSetByDerived.InstanceName);

            if (existingNamedObject != null)
            {
                existingNamedObject.DefinedByBase = true;
                existingNamedObject.InstantiatedByBase = instantiatedByBase;
                return existingNamedObject;
            }
            else
            {
                NamedObjectSave newNamedObject = namedObjectSetByDerived.Clone();

                // This code may be cloning a list with contained objects, and the
                // contained objects may not SetByDerived
                newNamedObject.ContainedObjects.Clear();
                foreach (var containedCandidate in namedObjectSetByDerived.ContainedObjects)
                {
                    if (containedCandidate.SetByDerived)
                    {
                        newNamedObject.ContainedObjects.Add(containedCandidate);
                    }
                }

                newNamedObject.SetDefinedByBaseRecursively(true);
                newNamedObject.SetInstantiatedByBaseRecursively(instantiatedByBase);

                // This can't be set by derived because an object it inherits from has that already set
                newNamedObject.SetSetByDerivedRecursively(false);

                if(containerToAddTo == null)
                {
                    namedObjectContainer.NamedObjects.Add(newNamedObject);
                }
                else
                {
                    containerToAddTo.ContainedObjects.Add(newNamedObject);
                }

                return newNamedObject;
            }
        }

        public static void UpdateCustomVariablesFromBaseType(IElement elementToUpdate)
        {

            IElement baseElement = null;
            baseElement = ObjectFinder.Self.GetBaseElement(elementToUpdate);

            List<CustomVariable> customVariablesBeforeUpdate = new List<CustomVariable>();

            for (int i = 0; i < elementToUpdate.CustomVariables.Count; i++)
            {
                if (elementToUpdate.CustomVariables[i].DefinedByBase)
                {
                    customVariablesBeforeUpdate.Add(elementToUpdate.CustomVariables[i]);
                }
            }

            List<CustomVariable> newCustomVariables = null;

            //EntitySave entity = ProjectManager.GetEntitySave(mBaseEntity);

            if (baseElement != null)
            {
                newCustomVariables = baseElement.GetCustomVariablesToBeSetByDerived();
            }
            else
            {
                newCustomVariables = new List<CustomVariable>();
            }

            // See if there are any variables to be removed.
            for (int i = customVariablesBeforeUpdate.Count - 1; i > -1; i--)
            {
                bool contains = false;

                for (int j = 0; j < newCustomVariables.Count; j++)
                {
                    if (customVariablesBeforeUpdate[i].Name == newCustomVariables[j].Name &&
                        customVariablesBeforeUpdate[i].DefinedByBase)
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    // We got a NamedObject we should remove
                    elementToUpdate.CustomVariables.Remove(customVariablesBeforeUpdate[i]);
                    customVariablesBeforeUpdate.RemoveAt(i);
                }
            }

            // Next, see if there are any objects to be added
            for (int i = 0; i < newCustomVariables.Count; i++)
            {
                bool alreadyContainedAsDefinedByBase = false;
                for (int j = 0; j < customVariablesBeforeUpdate.Count; j++)
                {
                    if (customVariablesBeforeUpdate[j].Name == newCustomVariables[i].Name &&
                        customVariablesBeforeUpdate[j].DefinedByBase)
                    {
                        alreadyContainedAsDefinedByBase = true;
                    }
                }

                if (!alreadyContainedAsDefinedByBase)
                {
                    // There isn't a variable by this
                    // name that is already DefinedByBase, 
                    // but there may still be a variable that
                    // is just a regular variable - and in that
                    // case we want to connect the existing variable
                    // with the variable in the base
                    CustomVariable existingInDerived = elementToUpdate.GetCustomVariable(newCustomVariables[i].Name);
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
                        customVariable.SourceObject = null;
                        customVariable.SourceObjectProperty = null;

                        customVariable.DefinedByBase = true;
                        // We'll assume that this thing is going to be the acutal definition
                        // Update April 11, 2023
                        // Setting this to false doesn't
                        // prevent this from being the final
                        // definition. We want the SetByDerived
                        // to be true so that the derived variable
                        // can get removed when saving the .json file.
                        //customVariable.SetByDerived = false;
                        customVariable.SetByDerived = true;

                        var indexToInsertAt = elementToUpdate.CustomVariables.Count;
                        if(i == 0)
                        {
                            indexToInsertAt = 0;
                        }
                        else
                        {
                            var itemBefore = newCustomVariables[i-1];
                            var withMatchingName = elementToUpdate.CustomVariables.Find(item => item.Name == itemBefore.Name);
                            if(withMatchingName != null)
                            {
                                indexToInsertAt = 1 + elementToUpdate.CustomVariables.IndexOf(withMatchingName);
                            }
                        }

                        elementToUpdate.CustomVariables.Insert(indexToInsertAt, customVariable);
                    }
                }
            }

            // update the category
            foreach(var customVariable in elementToUpdate.CustomVariables)
            {
                if (customVariable.DefinedByBase)
                {
                    var baseVariable = baseElement.CustomVariables.FirstOrDefault(item => item.Name == customVariable.Name);
                    if(!string.IsNullOrEmpty(baseVariable?.Category))
                    {
                        customVariable.Category = baseVariable.Category;
                    }
                }
            }
        }


    }
}
