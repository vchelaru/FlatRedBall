using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Reflection;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    class AvailableCustomVariableTypes : TypeConverter
    {
        public bool AllowNone
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

        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {

            var list = ExposedVariableManager.GetAvailableNewVariableTypes(AllowNone);

            StandardValuesCollection svc = new StandardValuesCollection(list);

            return svc;


        }


    }

}
