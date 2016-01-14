using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailablePrimitiveTypeStringConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        static string[] availablePrimitiveTypes = new string[]
        {
            "<NONE>",
            "int",
            "long",
            "string",
            "float"

        };


        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection svc = new StandardValuesCollection(availablePrimitiveTypes);

            return svc;
        } 
    }
}
