using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.TypeConversions
{
    public enum GetterOrSetter
    {
        Getter,
        Setter
    }

    public static class TypeConverterHelper
    {
        public static Dictionary<string, CustomTypeConverter> TypeConverters = null;

        public static void InitializeClasses()
        {
            Debug.Assert(TypeConverters == null);
            TypeConverters = new Dictionary<string, CustomTypeConverter>();
            CreateDefaultConverter();
        }

        private static void CreateDefaultConverter()
        {
            var converter = new CustomTypeConverter();

            converter.AddConversion("int", "string", "value.ToString()");
            converter.AddConversion("string", "int", "int.Parse(value)");

            converter.AddConversion("float", "string", "value.ToString()");
            converter.AddConversion("string", "float", "float.Parse(value)");

            converter.AddConversion("long", "string", "value.ToString()");
            converter.AddConversion("string", "long", "long.Parse(value)");

            converter.AddConversion("string", "string", "value");
            converter.AddConversion("bool", "bool", "value");
            converter.AddConversion("int", "int", "value");
            converter.AddConversion("float", "float", "value");
            converter.AddConversion("double", "double", "value");

            converter.AddConversion("decimal", "decimal", "value");

            TypeConverters.Add("<default>", converter);

            var commaSeparatingConverter = new CustomTypeConverter();
            commaSeparatingConverter.AddConversion("int", "string", "value.ToString(\"n0\")");
            commaSeparatingConverter.AddConversion("string", "int", "int.Parse(value.Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, \"\"))");

            commaSeparatingConverter.AddConversion("float", "string", "string.Format(\"{0:n}\", value)");
            commaSeparatingConverter.AddConversion("string", "float", "float.Parse(value.Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, \"\"))");

            commaSeparatingConverter.AddConversion("long", "string", "value.ToString(\"n0\")");
            commaSeparatingConverter.AddConversion("string", "long", "long.Parse(value.Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, \"\"))");

            commaSeparatingConverter.AddConversion("string", "string", "value");

            // gotta get rid of commas before int.Parse
            // using CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator

            TypeConverters.Add("Comma Separating", commaSeparatingConverter);

            var minuteSecondsConverter = new CustomTypeConverter();
            minuteSecondsConverter.AddConversion("int", "string", 
                "string.Format(\"{0}:{1}\", (value / 60).ToString(\"D1\"), (value % 60).ToString(\"D2\"))");
            TypeConverters.Add("Minutes:Seconds", minuteSecondsConverter);

            var floatToStringConverter = new CustomTypeConverter();
            floatToStringConverter.AddConversion("float", "string",
                "string.Format(\"{0}:{1}{2}\", ((int)value / 60).ToString(\"D1\"), ((int)value % 60).ToString(\"D2\"), (value - (int)value).ToString(\".00\"))");
            TypeConverters.Add("Minutes:Seconds.Hundredths", floatToStringConverter);
        }

        public static string Convert(CustomVariable customVariable, GetterOrSetter getterOrSetter, string value)
        {
            if (string.IsNullOrEmpty(customVariable.TypeConverter))
            {
                return value;
            }

            var converter = TypeConverters[customVariable.TypeConverter];
            if (getterOrSetter == GetterOrSetter.Getter)
            {
                return converter.GetConversion(customVariable.Type, customVariable.OverridingPropertyType, value);
            }

            return converter.GetConversion(customVariable.OverridingPropertyType, customVariable.Type, value);
        }
    }
}
