using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using FlatRedBall.IO;

using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Controls;

using FlatRedBall.Glue.SaveClasses;

#if GLUE
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using Glue;
using FlatRedBall.Glue.Reflection;
#endif

namespace FlatRedBall.Glue.SaveClasses
{


    public static class NamedObjectContainerHelper
    {

        public static bool DoesMemberNeedToBeSetByContainer(this IElement namedObjectContainer, string memberName)
        {
            foreach (NamedObjectSave namedObject in namedObjectContainer.NamedObjects)
            {
                if (namedObject.InstanceName == memberName && namedObject.SetByContainer)
                {
                    return namedObject.SetByContainer;
                }
            }

            if ( namedObjectContainer.InheritsFromElement())
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);

                return baseEntity.DoesMemberNeedToBeSetByContainer(memberName);
            }


            return false;
        }

        public static bool ReactToRenamedReferencedFile(this INamedObjectContainer namedObjectContainer, string oldName, string newName)
        {
            bool toReturn = false;

            for (int i = 0; i < namedObjectContainer.NamedObjects.Count; i++)
            {
                NamedObjectSave namedObject = namedObjectContainer.NamedObjects[i];

                if (namedObject.SourceFile == oldName)
                {
                    toReturn = true;
                    namedObject.SourceFile = newName;
                }
            }
            
            return toReturn;
        }

        public static List<NamedObjectSave> GetNamedObjectsToBeExposedInDerived(this INamedObjectContainer namedObjectContainer)
        {
            List<NamedObjectSave> namedObjectsToBeExposedInDerived = new List<NamedObjectSave>();

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject) && namedObjectContainer.BaseObject != "<NONE>")
            {
                //If this is a Screen
                if ((namedObjectContainer as EntitySave) == null)
                {
                    namedObjectsToBeExposedInDerived.AddRange(
                        ObjectFinder.Self.GetScreenSave(namedObjectContainer.BaseObject).GetNamedObjectsToBeExposedInDerived());
                }
                //Otherwise it's an Entity
                else
                {
                    EntitySave baseEntitySave = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);

                    if (baseEntitySave == null)
                    {
                        bool inheritsFromFrbType = 
                            namedObjectContainer is EntitySave && (namedObjectContainer as EntitySave).InheritsFromFrbType();

                        if (!inheritsFromFrbType)
                        {
                            System.Windows.Forms.MessageBox.Show("The Element\n\n" + namedObjectContainer.ToString() + "\n\nhas a base type\n\n" + namedObjectContainer.BaseObject +
                                "\n\nbut this base type can't be found.  " +
                                "It was probably removed from the project.  You will need to set the base object to NONE.");
                        }
                    }
                    else
                    {
                        namedObjectsToBeExposedInDerived.AddRange(
                            baseEntitySave.GetNamedObjectsToBeExposedInDerived());
                    }
                }
            }

            foreach (NamedObjectSave nos in namedObjectContainer.NamedObjects)
            {
                if (nos.ExposedInDerived)
                {
                    bool isAlreadyThere = false;

                    for (int i = namedObjectsToBeExposedInDerived.Count - 1; i > -1; i--)
                    {
                        if (namedObjectsToBeExposedInDerived[i].InstanceName == nos.InstanceName)
                        {
                            isAlreadyThere = true;
                            break;
                        }
                    }

                    if (!isAlreadyThere)
                    {
                        namedObjectsToBeExposedInDerived.Add(nos);
                    }
                }
                else if (nos.DefinedByBase)
                {
                    // This guy is handling the named object save, so let's remove it from the list

                    for (int i = namedObjectsToBeExposedInDerived.Count - 1; i > -1; i--)
                    {
                        if (namedObjectsToBeExposedInDerived[i].InstanceName == nos.InstanceName)
                        {
                            namedObjectsToBeExposedInDerived.RemoveAt(i);
                        }
                    }
                }
            }

            return namedObjectsToBeExposedInDerived;
        }

        public static List<NamedObjectSave> GetNamedObjectsToBeSetByDerived(this INamedObjectContainer namedObjectContainer)
        {
            List<NamedObjectSave> namedObjectsToBeSetByDerived = new List<NamedObjectSave>();

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject) && namedObjectContainer.BaseObject != "<NONE>")
            {
                //If this is a Screen
                if ((namedObjectContainer as EntitySave) == null)
                {
                    namedObjectsToBeSetByDerived.AddRange(
                        ObjectFinder.Self.GetScreenSave(namedObjectContainer.BaseObject).GetNamedObjectsToBeSetByDerived());
                }
                //Otherwise it's an Entity
                else
                {
                    EntitySave baseEntitySave = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);

                    if (baseEntitySave == null)
                    {
                        bool inheritsFromFrbType = 
                            namedObjectContainer is EntitySave && (namedObjectContainer as EntitySave).InheritsFromFrbType();

                        if (!inheritsFromFrbType)
                        {


                            System.Windows.Forms.MessageBox.Show("The Element\n\n" + namedObjectContainer.ToString() + "\n\nhas a base type\n\n" + namedObjectContainer.BaseObject +
                                "\n\nbut this base type can't be found.  " +
                                "It was probably removed from the project.  You will need to set the base object to NONE.");
                        }
                    }
                    else
                    {
                        namedObjectsToBeSetByDerived.AddRange(
                            baseEntitySave.GetNamedObjectsToBeSetByDerived());
                    }
                }
            }

            foreach (NamedObjectSave nos in namedObjectContainer.NamedObjects)
            {
                if (nos.SetByDerived)
                {
                    bool isAlreadyThere = false;

                    for (int i = namedObjectsToBeSetByDerived.Count - 1; i > -1; i--)
                    {
                        if (namedObjectsToBeSetByDerived[i].InstanceName == nos.InstanceName)
                        {
                            isAlreadyThere = true;
                            break;
                        }
                    }

                    if (!isAlreadyThere)
                    {
                        namedObjectsToBeSetByDerived.Add(nos);
                    }
                }
                else if (nos.DefinedByBase)
                {
                    // This guy is handling the named object save, so let's remove it from the list

                    for (int i = namedObjectsToBeSetByDerived.Count - 1; i > -1; i--)
                    {
                        if (namedObjectsToBeSetByDerived[i].InstanceName == nos.InstanceName)
                        {
                            namedObjectsToBeSetByDerived.RemoveAt(i);
                        }
                    }
                }
            }

            return namedObjectsToBeSetByDerived;
        }

        public static NamedObjectSave GetNamedObject(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            return GetNamedObjectInList(namedObjectContainer.NamedObjects, namedObjectName);
        }

        public static NamedObjectSave GetNamedObjectInList(List<NamedObjectSave> namedObjectList, string namedObjectName)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                NamedObjectSave nos = namedObjectList[i];

                if (nos.InstanceName == namedObjectName)
                {
                    return nos;
                }

                if (nos.ContainedObjects != null && nos.ContainedObjects.Count != 0)
                {
                    NamedObjectSave foundNos = GetNamedObjectInList(nos.ContainedObjects, namedObjectName);

                    if (foundNos != null)
                    {
                        return foundNos;
                    }
                }
            }

            return null;
        }


        public static NamedObjectSave GetNamedObjectRecursively(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            List<NamedObjectSave> namedObjectList = namedObjectContainer.NamedObjects;

            NamedObjectSave foundNos = GetNamedObjectInList(namedObjectList, namedObjectName);

            if (foundNos != null)
            {
                return foundNos;
            }

            // These methods need to check if the baseScreen/baseEntity is not null.
            // They can be null if the user deletes a base Screen/Entity and the tool
            // managing the Glux doesn't handle the changes.

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
            {
                if (namedObjectContainer is EntitySave)
                {
                    EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);
                    if (baseEntity != null)
                    {
                        return GetNamedObjectRecursively(baseEntity, namedObjectName);
                    }
                }

                else if (namedObjectContainer is ScreenSave)
                {
                    ScreenSave baseScreen = ObjectFinder.Self.GetScreenSave(namedObjectContainer.BaseObject);

                    if (baseScreen != null)
                    {
                        return GetNamedObjectRecursively(baseScreen, namedObjectName);
                    }
                }
            }

            return null;
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

            // June 1, 2011:
            // The following code
            // was using AddRange instead
            // of manually looping, but this
            // is only possible in .NET 4.0, and
            // GlueView still uses 3.1.  
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
            // to day it did.  This caused a lot of problems including generated
            // code creating the element twice.
            if (namedObjectContainer is EntitySave)
            {
                if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
                {
                    baseElements.Add(ObjectFinder.Self.GetIElement(namedObjectContainer.BaseObject));
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
                    baseElements.Add(ObjectFinder.Self.GetIElement(namedObjectContainer.BaseObject));
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

                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();

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

                namedObjectSave.SetDefinedByBaseRecursively(true);
                namedObjectSave.SetInstantiatedByBaseRecursively(instantiatedByBase);

                // This can't be set by derived because an object it inherits from has that already set
                namedObjectSave.SetSetByDerivedRecursively(false);


                namedObjectContainer.NamedObjects.Add(namedObjectSave);
            }
        }

        public static NamedObjectSave GetNamedObjectThatIsContainerFor(INamedObjectContainer element, NamedObjectSave containedNamedObject)
        {
            foreach (NamedObjectSave namedObjectSave in element.NamedObjects)
            {
                NamedObjectSave returnValue = GetNamedObjectThatIsContainerFor(namedObjectSave, containedNamedObject);

                if (returnValue != null)
                {
                    return returnValue;
                }
            }

            return null;
        }

        private static NamedObjectSave GetNamedObjectThatIsContainerFor(NamedObjectSave possibleContainer, NamedObjectSave containedNamedObject)
        {
            foreach (NamedObjectSave subNamedObject in possibleContainer.ContainedObjects)
            {
                if (subNamedObject == containedNamedObject)
                {
                    return possibleContainer;
                }

                NamedObjectSave returnValue = GetNamedObjectThatIsContainerFor(subNamedObject, containedNamedObject);

                if (returnValue != null)
                {
                    return returnValue;
                }

            }

            return null;
        }

    }


}
