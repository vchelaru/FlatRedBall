using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableDerivedElementsConverter : TypeConverter
    {
        GlueElement BaseElement;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableDerivedElementsConverter(GlueElement baseElement)
            : base()
        {
            BaseElement = baseElement;
        }

        public override StandardValuesCollection
             GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> stringsToReturn = new List<string>();

            stringsToReturn.Clear();
            stringsToReturn.Add("<NONE>");

            var derived = ObjectFinder.Self.GetAllDerivedElementsRecursive(BaseElement);

            var names = derived.Select(item => item.Name).OrderBy(item => item);
            stringsToReturn.AddRange(names);

            return new StandardValuesCollection(stringsToReturn);
        }
    }
}
