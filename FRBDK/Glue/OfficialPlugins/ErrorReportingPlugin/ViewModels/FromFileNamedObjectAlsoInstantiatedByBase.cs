using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    internal class FromFileNamedObjectAlsoInstantiatedByBase : ErrorViewModel
    {
        public static bool IsError(NamedObjectSave nos)
        {
            if(nos.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.File)
            {
                if(nos.InstantiatedByBase && nos.IsDisabled == false)
                {
                    return true;
                }
            }
            return false;
        }

        NamedObjectSave NamedObjectSave;

        string uniqueId;

        public override string UniqueId => uniqueId;

        public FromFileNamedObjectAlsoInstantiatedByBase(NamedObjectSave nos)
        {
            this.Details = $"The object {nos} is instantiated by its base element, but it is also created " +
                $"from file {nos.SourceFile} which means the instance is re-assigned. At a minimum this can result " +
                $"in unnecessary instances being created at runtime, and worse it can result in memory leaks if the " +
                $"instance created in the base element is not cleaned up.\n" +
                $"If the derived object should be set by its screen, the base should set its Instantiate property to false";

            uniqueId = Details;

            NamedObjectSave = nos;

            // I started working on this, but it's a pain to do because it sets properties through
            // Glue commands, which interrupt the process with their own popups. No good
            //MenuItemList.Add(new MenuItemViewModel
            //{
            //    Header = $"Set the base object for {nos} to SetByDerived=true so it does not instantiate itself - only do this if " +
            //        $"you intend for all derived instances to instantiate this object!",
            //    Command = new Command(() => SetBaseObjectToSetByDerived(nos))
            //});
        }

        private void SetBaseObjectToSetByDerived(NamedObjectSave derivedNos)
        {
            var derivedElement = derivedNos.GetContainer();

            var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(derivedElement);

            var baseElementsWithMatchingNosName = baseElements
                .Where(item => item.NamedObjects.Any(nos => nos.InstanceName == derivedNos.InstanceName))
                .ToList();

            if(baseElementsWithMatchingNosName.Count == 0)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(
                    $"Could not find any base elements with an object with matching name {derivedNos.InstanceName}");
            }
            else if(baseElementsWithMatchingNosName.Count > 1)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(
                    $"Multiple elements have an object with matching name {derivedNos.InstanceName}. You must manually set the one you intend to SetByDerived");
            }
            else
            {
                // exactly 1 exists, so set this one:
                var toSet = baseElementsWithMatchingNosName.First().AllNamedObjects.FirstOrDefault(item => item.InstanceName == derivedNos.InstanceName);

                if(toSet != null)
                {
                    GlueCommands.Self.GluxCommands.SetPropertyOnAsync(toSet, nameof(NamedObjectSave.ExposedInDerived), 
                        false,
                        performSaveAndGenerateCode:false, updateUi:false, recordUndo:false);

                    GlueCommands.Self.GluxCommands.SetPropertyOnAsync(toSet, nameof(NamedObjectSave.SetByDerived),
                        true,
                        performSaveAndGenerateCode: true, updateUi: true, recordUndo: false);

                }
            }
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = NamedObjectSave;
            GlueCommands.Self.DialogCommands.FocusTab("Properties");
        }

        public override bool GetIfIsFixed()
        {
            var container = NamedObjectSave.GetContainer();
            if (container == null)
            {
                return true;
            }
            return !IsError(NamedObjectSave);
        }
    }
}
