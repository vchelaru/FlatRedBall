using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    class InvalidSetByDerivedErrorViewModel : ErrorViewModel
    {
        NamedObjectSave baseNos;
        NamedObjectSave derivedNos;

        public override string UniqueId => Details;
        public InvalidSetByDerivedErrorViewModel(NamedObjectSave baseNos, NamedObjectSave derivedNos)
        {
            this.baseNos = baseNos;
            this.derivedNos = derivedNos;

            this.Details = $"The object {derivedNos} is also defined in the base element {baseNos.GetContainer()}." +
                $"The object in the  base element should either have its SetByDerived or ExposedInDerived to true";
        }


        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = baseNos;
        }

        public override bool GetIfIsFixed()
        {
            var baseElement = baseNos.GetContainer();
            var derivedElement = derivedNos.GetContainer();
            if (baseElement == null)
            {
                return true;
            }
            else if(derivedElement == null)
            {
                return true;
            }
            else if(baseNos.SetByDerived || baseNos.ExposedInDerived)
            {
                return true;
            }
            else if(baseNos.InstanceName != derivedNos.InstanceName)
            {
                return true;
            }
            else if(ObjectFinder.Self.GetAllBaseElementsRecursively(derivedElement).Contains(baseElement) == false)
            {
                // no longer inheriting
                return true;
            }

            return false;
        }
    }
}
