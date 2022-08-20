using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class DelegateBasedTypeConverter : TypeConverter
    {
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

        public Func<List<string>> CustomDelegate;


        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if(CustomDelegate == null)
            {
                return new StandardValuesCollection(new List<string>());
            }
            else
            {
                return new StandardValuesCollection(CustomDelegate());
            }
        }
    }
}
