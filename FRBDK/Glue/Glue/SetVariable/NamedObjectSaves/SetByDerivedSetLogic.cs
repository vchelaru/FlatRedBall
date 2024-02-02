using EditorObjects.IoC;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueFormsCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GlueFormsCore.SetVariable.NamedObjectSaves
{
    class SetByDerivedSetLogic
    {
        public static async Task ReactToChangedSetByDerived(NamedObjectSave namedObjectSave, GlueElement element)
        {
            if (namedObjectSave.SourceType == SourceType.Entity &&
                !string.IsNullOrEmpty(namedObjectSave.SourceClassType))
            {
                if (ProjectManager.CheckForCircularObjectReferences(ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType)) == ProjectManager.CheckResult.Failed)
                    namedObjectSave.SetByDerived = !namedObjectSave.SetByDerived;
            }

            var didSet = true;
            if (namedObjectSave.SetByDerived && namedObjectSave.ExposedInDerived)
            {
                // The user has just set SetByDerived to true, but ExposedInDerived means that
                // the derived expects that the base instantiates.  We need to tell the user that
                // both values can't be true at the same time, and that ExposedInDerived will be set
                // to false.
                GlueCommands.Self.DialogCommands.ShowMessageBox("You have set SetByDerived to true, but ExposedInDerived is also true.  Both cannot be true at the same time " +
                    "so Glue will set ExposedInDerived to false.");
                namedObjectSave.ExposedInDerived = false;
                didSet = false;
            }


            if (namedObjectSave.SourceType == SourceType.FlatRedBallType &&
                namedObjectSave.IsList &&
                namedObjectSave.SetByDerived == true &&
                namedObjectSave.ContainedObjects.Count != 0)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("This list is not empty, so it can't be set to \"Set By Derived\".  You must first empty the list");

                namedObjectSave.SetByDerived = false;
                didSet = false;
            }

            if(didSet)
            {
                // let's see if this would undo any existing objects:
                if(namedObjectSave.SetByDerived == false)
                {
                    var orphanedNoses = new List<NamedObjectSave>();
                    var derivedElements = ObjectFinder.Self.GetAllDerivedElementsRecursive(element);

                    foreach(var derivedElement in derivedElements)
                    {
                        var derivedNos = derivedElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == namedObjectSave.InstanceName);
                        if(derivedNos != null)
                        {
                            orphanedNoses.Add(derivedNos);
                        }
                    }

                    if(orphanedNoses.Count > 0)
                    {
                        var singleOrPluralPhrase = orphanedNoses.Count == 1 ? "object" : "objects";
                        var thisOrTheseObjects = orphanedNoses.Count == 1 ? "this object" : "these objects";
                        var message = 
                            $"Changing SetByDerived will result in the following {singleOrPluralPhrase} no longer being defined by base. What " +
                            $"would you like to do with {thisOrTheseObjects}:\n";

                        foreach (var nos in orphanedNoses)
                        {
                            message += nos.ToString() + "\n";
                        }


                        message += "\nWhat would you like to do?";

                        var mbmb = new MultiButtonMessageBoxWpf();

                        mbmb.MessageText = message;

                        const string remove = "remove";
                        const string keep = "keep";
                        const string cancel = "cancel";

                        mbmb.AddButton($"Remove {thisOrTheseObjects}", remove);
                        mbmb.AddButton($"Keep {thisOrTheseObjects}, set \"defined by base\" to false", keep);
                        mbmb.AddButton($"Cancel", cancel);

                        var dialogResult = mbmb.ShowDialog();

                        
                        if(dialogResult == null || mbmb.ClickedResult as string == cancel)
                        {
                            didSet = false;
                            namedObjectSave.SetByDerived = true;
                        }
                        else if(mbmb.ClickedResult as string == remove)
                        {
                            foreach (var nos in orphanedNoses)
                            {
                                GlueCommands.Self.GluxCommands.RemoveNamedObject(nos);
                            }
                        }
                        else if(mbmb.ClickedResult as string == keep)
                        {
                            foreach(var nos in orphanedNoses)
                            {
                                nos.DefinedByBase = false;

                                await GlueCommands.Self.GluxCommands.ReactToPropertyChanged(nos, nameof(NamedObjectSave.SetByDerived), false);
                            }
                        }
                    }
                }
            }

            if(didSet)
            {
                await TaskManager.Self.AddAsync(
                    () => InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element), 
                    "UpdateAllDerivedElementFromBaseValues", doOnUiThread:true);
            }

            if(element is EntitySave entity && entity.CreatedByOtherEntities && namedObjectSave.SetByDerived)
            {
                entity.CreatedByOtherEntities = false;

                //EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                //    nameof(entity.CreatedByOtherEntities), true, nameof(entity.CreatedByOtherEntities), null);
                Container.Get<EntitySaveSetPropertyLogic>().ReactToEntityChangedProperty(nameof(entity.CreatedByOtherEntities), true, entity);



                GlueCommands.Self.PrintOutput($"{element} is now an abstract class because {namedObjectSave} is SetByDerived. Removing its Factory from the project.");
            }
        }


    }
}
