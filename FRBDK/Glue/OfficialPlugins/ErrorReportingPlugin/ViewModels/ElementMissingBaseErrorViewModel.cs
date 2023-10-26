using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    public class ElementMissingBaseErrorViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        public GlueElement Element { get; private set; }
        public NamedObjectSave NamedObjectSave { get; private set; }

        public ElementMissingBaseErrorViewModel(GlueElement element, NamedObjectSave nos)
        {
            this.NamedObjectSave = nos;
            this.Element = element;
            var screenOrEntity = element is ScreenSave ? "Screen" : "Entity";
            this.Details = $"{element} has an object {nos.InstanceName} with DefinedByBase set to true, but the {screenOrEntity} does not have a base";
        }

        public override bool GetIfIsFixed()
        {
            var hasError = GetIfHasError(Element, NamedObjectSave);

            return !hasError;
        }

        public static bool GetIfHasError(GlueElement element, NamedObjectSave namedObjectSave)
        {
            if (!string.IsNullOrEmpty(element.BaseElement)) return false;

            if (namedObjectSave.DefinedByBase == false) return false;

            if (element.AllNamedObjects.Contains(namedObjectSave) == false) return false;

            return true;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentElement = Element;
        }
    }
}
