using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace GlueCsvEditor.Data
{
    public class AvailableKnownValuesTypeConverter : StringConverter
    {
        protected IEnumerable<string> _knownValues;

        public AvailableKnownValuesTypeConverter(IEnumerable<string> knownValues)
        {
            _knownValues = knownValues;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new List<string> (_knownValues));
        }
    }
}
