using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ParticleEditorControls.TypeConverters
{
    public class EnumToString : TypeConverter
    {
        public Type EnumType
        {
            get;
            set;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        List<string> values = new List<string>();
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (EnumType == null)
            {
                throw new InvalidOperationException("EnumType must be set before calling GetStandardValues");
            }

            values.Clear();

            foreach (var value in Enum.GetNames(EnumType))
            {
                values.Add(value);
            }

            StandardValuesCollection svc = new StandardValuesCollection(values);

            return svc;
        }



    }
}
