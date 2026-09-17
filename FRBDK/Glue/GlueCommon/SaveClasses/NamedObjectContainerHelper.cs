using System.Collections.Generic;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Moved whole from <c>Glue/SaveClasses/NamedObjectSaveHelper.cs</c> (Glue.csproj, net8.0-windows),
    /// so it keeps its name: no Glue-side class of that name remains, and the one non-extension
    /// caller (<c>GluxCommands</c>) resolves unchanged. The two derived-object walks resolve base
    /// elements through <see cref="IObjectFinderCore"/> and report a missing base through
    /// <see cref="IErrorReportingCore"/> instead of <c>ObjectFinder.Self</c>/<c>DialogService</c>.
    /// Lives here (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276.
    /// </summary>
    public static class NamedObjectContainerHelper
    {
        // DoesMemberNeedToBeSetByContainer and ReactToRenamedReferencedFile moved to
        // GlueCommon.SaveClasses.ElementExtensions (#2276).

        public static List<NamedObjectSave> GetNamedObjectsToBeExposedInDerived(this INamedObjectContainer namedObjectContainer)
        {
            List<NamedObjectSave> namedObjectsToBeExposedInDerived = new List<NamedObjectSave>();

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject) && namedObjectContainer.BaseObject != "<NONE>")
            {
                //If this is a Screen
                if ((namedObjectContainer as EntitySave) == null)
                {
                    namedObjectsToBeExposedInDerived.AddRange(
                        ObjectFinderCore.Self.GetScreenSave(namedObjectContainer.BaseObject).GetNamedObjectsToBeExposedInDerived());
                }
                //Otherwise it's an Entity
                else
                {
                    EntitySave baseEntitySave = ObjectFinderCore.Self.GetEntitySave(namedObjectContainer.BaseObject);

                    if (baseEntitySave == null)
                    {
                        bool inheritsFromFrbType =
                            namedObjectContainer is EntitySave && (namedObjectContainer as EntitySave).InheritsFromFrbType();

                        if (!inheritsFromFrbType)
                        {
                            ErrorReportingCore.Self.ShowMessage("The Element\n\n" + namedObjectContainer.ToString() + "\n\nhas a base type\n\n" + namedObjectContainer.BaseObject +
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
                        ObjectFinderCore.Self.GetScreenSave(namedObjectContainer.BaseObject).GetNamedObjectsToBeSetByDerived());
                }
                //Otherwise it's an Entity
                else
                {
                    EntitySave baseEntitySave = ObjectFinderCore.Self.GetEntitySave(namedObjectContainer.BaseObject);

                    if (baseEntitySave == null)
                    {
                        bool inheritsFromFrbType =
                            namedObjectContainer is EntitySave && (namedObjectContainer as EntitySave).InheritsFromFrbType();

                        if (!inheritsFromFrbType)
                        {
                            ErrorReportingCore.Self.ShowMessage("The Element\n\n" + namedObjectContainer.ToString() + "\n\nhas a base type\n\n" + namedObjectContainer.BaseObject +
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
        /// Returns the NamedObjectSave that directly contains <paramref name="containedNamedObject"/>,
        /// searching every top-level named object in <paramref name="element"/> recursively.
        /// </summary>
        /// <param name="element">The object containing named objects, such as a Screen or Entity.</param>
        /// <param name="containedNamedObject">The NamedObjectSave whose container to find.</param>
        /// <returns>The containing NamedObjectSave, or null if <paramref name="containedNamedObject"/> is top-level or not found.</returns>
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
