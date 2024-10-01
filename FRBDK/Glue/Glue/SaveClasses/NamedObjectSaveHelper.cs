using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using FlatRedBall.IO;

using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Controls;


namespace FlatRedBall.Glue.SaveClasses
{


    public static class NamedObjectContainerHelper
    {

        public static bool DoesMemberNeedToBeSetByContainer(this GlueElement namedObjectContainer, string memberName)
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

            foreach (NamedObjectSave nos in namedObjectContainer.AllNamedObjects)
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





        /// <summary>
        /// Returns the first found named object first checking the NamedObjectContainer directly, then checking base objects
        /// until one is found.
        /// </summary>
        /// <param name="namedObjectContainer">The object containing named objects, such as a Screen or Entity.</param>
        /// <param name="namedObjectName">The name of the NamedObject to search for.</param>
        /// <returns>The found NamedObjectSave, or null if none are found.</returns>


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
