// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.Collections;
using System.ComponentModel;

namespace Alsing.Design
{
    public class FilteredTypeConverter : TypeConverter
    {
        protected virtual void FilterProperties(IDictionary Properties, object value) {}

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value,
                                                                   Attribute[] attributes)
        {
            PropertyDescriptorCollection propps = propps = TypeDescriptor.GetProperties(value, attributes, false);

            var arr = new Hashtable();
            foreach (PropertyDescriptor pd in propps)
                arr[pd.Name] = pd;

            FilterProperties(arr, value);

            //copy the modified propp arr into a typed propertydescriptor[] 
            var arr2 = new PropertyDescriptor[arr.Values.Count];
            arr.Values.CopyTo(arr2, 0);

            //return the new propertydescriptorcollection
            return new PropertyDescriptorCollection(arr2);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}