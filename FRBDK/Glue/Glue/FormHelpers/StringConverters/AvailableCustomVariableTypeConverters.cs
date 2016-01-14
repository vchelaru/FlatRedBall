using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.TypeConversions;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableCustomVariableTypeConverters : TypeConverter
    {


        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public static List<string> GetAvailableConverters()
        {
            List<string> converters = new List<string>();
            
            foreach (KeyValuePair<string, CustomTypeConverter> kvp in TypeConverterHelper.TypeConverters)
            {
                converters.Add(kvp.Key);
            }

            return converters;
        }


        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> converters = GetAvailableConverters();

            StandardValuesCollection svc = new StandardValuesCollection(converters);

            return svc;
        } 
    }
}
