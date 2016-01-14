using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailablePrimitiveTypeArraysStringConverter : TypeConverter
    {

        static string[] availablePrimitiveTypes = new string[]
        {
            "<NONE>",
            "string[]",
            "string"
        };


		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}


		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
            StandardValuesCollection svc = new StandardValuesCollection(availablePrimitiveTypes);

			return svc;
		} 
    }
}
