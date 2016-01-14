using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using FlatRedBall.Gui;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableDelegateTypeConverter : TypeConverter
    {
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
            return true;
		}

        public static List<string> GetAvailableDelegates()
        {
            List<string> availableTypes = new List<string>();
            availableTypes.Add(typeof(EventHandler).FullName);
            availableTypes.Add(typeof(WindowEvent).FullName);
            availableTypes.Add("System.Action<T>");

            return availableTypes;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection svc = new StandardValuesCollection(GetAvailableDelegates());

            return svc;
        }

    }
}
