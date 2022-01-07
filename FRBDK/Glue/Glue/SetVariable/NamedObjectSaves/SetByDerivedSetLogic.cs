using EditorObjects.IoC;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueFormsCore.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GlueFormsCore.SetVariable.NamedObjectSaves
{
    class SetByDerivedSetLogic
    {
        public static void ReactToChangedSetByDerived(NamedObjectSave namedObjectSave, GlueElement element)
        {
            if (namedObjectSave.SourceType == SourceType.Entity &&
                !string.IsNullOrEmpty(namedObjectSave.SourceClassType))
            {
                if (ProjectManager.CheckForCircularObjectReferences(ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType)) == ProjectManager.CheckResult.Failed)
                    namedObjectSave.SetByDerived = !namedObjectSave.SetByDerived;
            }


            if (namedObjectSave.SetByDerived && namedObjectSave.ExposedInDerived)
            {
                // The user has just set SetByDerived to true, but ExposedInDerived means that
                // the derived expects that the base instantiates.  We need to tell the user that
                // both values can't be true at the same time, and that ExposedInDerived will be set
                // to false.
                GlueCommands.Self.DialogCommands.ShowMessageBox("You have set SetByDerived to true, but ExposedInDerived is also true.  Both cannot be true at the same time " +
                    "so Glue will set ExposedInDerived to false.");
                namedObjectSave.ExposedInDerived = false;
            }


            if (namedObjectSave.SourceType == SourceType.FlatRedBallType &&
                namedObjectSave.IsList &&
                namedObjectSave.SetByDerived == true &&
                namedObjectSave.ContainedObjects.Count != 0)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("This list is not empty, so it can't be set to \"Set By Derived\".  You must first empty the list");

                namedObjectSave.SetByDerived = false;

            }
            else
            {
                InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);
            }

            if(element is EntitySave entity && entity.CreatedByOtherEntities && namedObjectSave.SetByDerived)
            {
                entity.CreatedByOtherEntities = false;

                //EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                //    nameof(entity.CreatedByOtherEntities), true, nameof(entity.CreatedByOtherEntities), null);
                Container.Get<EntitySaveSetPropertyLogic>().ReactToEntityChangedProperty(nameof(entity.CreatedByOtherEntities), true);



                GlueCommands.Self.PrintOutput($"{element} is now an abstract class because {namedObjectSave} is SetByDerived. Removing its Factory from the project.");
            }
        }


    }
}
