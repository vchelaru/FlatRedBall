using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ArrowDataConversion
{
    public class MemberInvestigator
    {
        public static bool IsDefault(object instance, string memberName, object value)
        {
            Type type = instance.GetType();

            PropertyInfo property = type.GetProperty(memberName);
            if (property != null)
            {
                var attribute = property.GetCustomAttribute<DefaultValueAttribute>();
                var xmlIgnoreAttribute = property.GetCustomAttribute<XmlIgnoreAttribute>();

                if (xmlIgnoreAttribute != null)
                {
                    return true;
                }

                if (attribute != null)
                {
                    Type membertype = property.PropertyType;

                    bool returnValue = IdentifyWhetherDefault(value, attribute, membertype);

                    return returnValue;
                }

            }

            FieldInfo field = type.GetField(memberName);
            if (field != null)
            {
                var attribute = field.GetCustomAttribute<DefaultValueAttribute>();
                var xmlIgnoreAttribute = field.GetCustomAttribute<XmlIgnoreAttribute>();

                if (xmlIgnoreAttribute != null)
                {
                    return true;
                }
                if (attribute != null)
                {
                    Type membertype = field.FieldType;

                    bool returnValue = IdentifyWhetherDefault(value, attribute, membertype);

                    return returnValue;
                }
            }

            return false;

        }

        private static bool IdentifyWhetherDefault(object value, DefaultValueAttribute attribute, Type membertype)
        {
            bool returnValue;

            if (membertype == typeof(string))
            {
                string defaultValue = (string)attribute.Value;
                string currentValue = (string)value;

                if (string.IsNullOrEmpty(defaultValue) && string.IsNullOrEmpty(currentValue))
                {
                    returnValue = true;
                }
                else
                {
                    returnValue = defaultValue == currentValue;
                }
            }
            else
            {

                returnValue = (attribute.Value == null && value == null) ||
                    (attribute != null && attribute.Value != null && attribute.Value.Equals(value));
            }
            return returnValue;
        }
    }

    public static class MemberInfoExtensions
    {
        public static T GetCustomAttribute<T>(this PropertyInfo propertyInfo)
        {
            var list = propertyInfo.GetCustomAttributes(typeof(T), true);

            if (list.Length == 0)
            {
                return default(T);
            }
            else
            {
                return (T)list[0];
            }

        }

        public static T GetCustomAttribute<T>(this FieldInfo fieldInfo)
        {
            var list = fieldInfo.GetCustomAttributes(typeof(T), true);

            if (list.Length == 0)
            {
                return default(T);
            }
            else
            {
                return (T)list[0];
            }

        }

    }
}
