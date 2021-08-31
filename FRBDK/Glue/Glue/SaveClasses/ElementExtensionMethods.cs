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
        [Obsolete("Use GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType")]
        public static bool UpdateFromBaseType(this IElement elementSave)
        {
            bool haveChangesOccurred = false;
            if (ObjectFinder.Self.GlueProject != null)
            {
                haveChangesOccurred |= UpdateNamedObjectsFromBaseType(elementSave);

                UpdateCustomVariablesFromBaseType(elementSave);
            }
            return haveChangesOccurred;

        }

        public static bool UpdateNamedObjectsFromBaseType(INamedObjectContainer namedObjectContainer)
        {
            bool haveChangesOccurred = false;

            List<NamedObjectSave> referencedObjectsBeforeUpdate = new List<NamedObjectSave>();

            for (int i = 0; i < namedObjectContainer.NamedObjects.Count; i++)
            {
                if (namedObjectContainer.NamedObjects[i].DefinedByBase)
                {
                    referencedObjectsBeforeUpdate.Add(namedObjectContainer.NamedObjects[i]);
                }
            }

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
            for (int i = referencedObjectsBeforeUpdate.Count - 1; i > -1; i--)
            {
                bool contains = false;

                for (int j = 0; j < namedObjectsSetByDerived.Count; j++)
                {
                    if (referencedObjectsBeforeUpdate[i].InstanceName == namedObjectsSetByDerived[j].InstanceName &&
                        referencedObjectsBeforeUpdate[i].DefinedByBase)
                    {
                        contains = true;
                        break;
                    }
                }

                for (int j = 0; j < namedObjectsExposedInDerived.Count; j++)
                {
                    if (referencedObjectsBeforeUpdate[i].InstanceName == namedObjectsExposedInDerived[j].InstanceName &&
                        referencedObjectsBeforeUpdate[i].DefinedByBase)
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {

                    NamedObjectSave nos = referencedObjectsBeforeUpdate[i];

                    string message = "The following object is marked as \"defined by base\" but it is not contained in " +
                        "any base elements\n\n" + nos.ToString() + "\n\nWhat would you like to do?";

                    var mbmb = new MultiButtonMessageBox();

                    mbmb.MessageText = message;

                    mbmb.AddButton("Remove " + nos.ToString(), DialogResult.Yes);
                    mbmb.AddButton("Keep it, make it not \"defined by base\"", DialogResult.No);

                    DialogResult result = mbmb.ShowDialog();

                    if (result == DialogResult.Yes)
                    {
                        // We got a NamedObject we should remove
                        namedObjectContainer.NamedObjects.Remove(nos);
                        referencedObjectsBeforeUpdate.RemoveAt(i);
                    }
                    else
                    {
                        nos.DefinedByBase = false;
                        nos.InstantiatedByBase = false;
                    }
                    haveChangesOccurred = true;
                }
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
                NamedObjectSave namedObjectSetByDerived = namedObjectsExposedInDerived[i];

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
                    AddSetByDerivedNos(namedObjectContainer, namedObjectSetByDerived, true);
                }
                else
                {
                    MatchDerivedToBase(namedObjectSetByDerived, matchingDefinedByBase);
                }
            }

            #endregion

            return haveChangesOccurred;
        }

        private static void MatchDerivedToBase(NamedObjectSave inBase, NamedObjectSave inDerived)
        {
            inDerived.SourceClassGenericType = inBase.SourceClassGenericType;


        }

        private static void AddSetByDerivedNos(INamedObjectContainer namedObjectContainer,
            NamedObjectSave namedObjectSetByDerived, bool instantiatedByBase)
        {
            NamedObjectSave existingNamedObject = null;

            foreach (NamedObjectSave namedObjectInDerived in namedObjectContainer.NamedObjects)
            {
                if (namedObjectInDerived.InstanceName == namedObjectSetByDerived.InstanceName)
                {
                    existingNamedObject = namedObjectInDerived;
                    break;
                }
            }

            if (existingNamedObject != null)
            {
                existingNamedObject.DefinedByBase = true;
                existingNamedObject.InstantiatedByBase = instantiatedByBase;
            }
            else
            {
                NamedObjectSave namedObjectSave = namedObjectSetByDerived.Clone();

                // This code may be cloning a list with contained objects, and the
                // contained objects may not SetByDerived
                namedObjectSave.ContainedObjects.Clear();
                foreach (var containedCandidate in namedObjectSetByDerived.ContainedObjects)
                {
                    if (containedCandidate.SetByDerived)
                    {
                        namedObjectSave.ContainedObjects.Add(containedCandidate);
                    }
                }


                namedObjectSave.SetDefinedByBaseRecursively(true);
                namedObjectSave.SetInstantiatedByBaseRecursively(instantiatedByBase);

                // This can't be set by derived because an object it inherits from has that already set
                namedObjectSave.SetSetByDerivedRecursively(false);


                namedObjectContainer.NamedObjects.Add(namedObjectSave);
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

            // See if there are any objects to be removed.
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
                        customVariable.SetByDerived = false;

                        elementToUpdate.CustomVariables.Add(customVariable);
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
