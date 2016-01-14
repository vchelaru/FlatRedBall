using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace GlueViewOfficialPlugins.Scripting
{
    public static class ReflectionExtensionMethods
    {
        public static void AddRangeUnique(this List<FieldInfo> fields, IEnumerable whatToAdd)
        {
            foreach (FieldInfo fieldInfo in whatToAdd)
            {
                if (!fields.ContainsFieldName(fieldInfo.Name))
                {
                    fields.Add(fieldInfo);
                }
            }
        }

        public static void AddRangeUnique(this List<PropertyInfo> properties, IEnumerable whatToAdd)
        {
            foreach (PropertyInfo propertyInfo in whatToAdd)
            {
                if (!properties.ContainsPropertyName(propertyInfo.Name))
                {
                    properties.Add(propertyInfo);
                }
            }
        }


        public static FieldInfo GetFieldByName(this IEnumerable fields, string name)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.Name == name)
                {
                    return field;
                }
            }
            return null;

        }
        public static PropertyInfo GetPropertyByName(this IEnumerable properties, string name)
        {
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == name)
                {
                    return property;
                }
            }
            return null;
        }




        public static bool ContainsFieldName(this IEnumerable fields, string name)
        {
            return fields.GetFieldByName(name) != null;
        }



        public static bool ContainsPropertyName(this IEnumerable properties, string name)
        {
            return properties.GetPropertyByName(name) != null;
        }


    }
}
