using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    class AvailableEvents : TypeConverter
    {
        public NamedObjectSave NamedObjectSave
        {
            get;
            set;
        }

        public GlueElement Element
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


        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> listToFill = new List<string>();

            var list  = ExposedEventManager.GetExposableEventsFor(NamedObjectSave, Element);
            foreach (var item in list)
            {
                listToFill.Add(item.Name);
            }

            StandardValuesCollection svc = new StandardValuesCollection(listToFill);

			return svc;
        }

        

    }
}
