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
using System.ComponentModel;
using System.Globalization;

namespace Alsing.Design
{
    public class Int32AutoConverter : Int32Converter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof (string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type t)
        {
            if (t == typeof (string))
                return true;

            return base.CanConvertTo(context, t);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                if (value.ToString().ToLower() == "auto")
                    return 0;

                return Convert.ToInt32(value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                                         Type destinationType)
        {
            if (destinationType == typeof (string))
            {
                int v = Convert.ToInt32(value);
                if (v == 0)
                    return "auto";

                return v.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}