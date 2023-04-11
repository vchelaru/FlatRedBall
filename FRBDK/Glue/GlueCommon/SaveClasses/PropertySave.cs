using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using GlueSaveClasses;

namespace FlatRedBall.Glue.SaveClasses
{

    public class PropertySave
    {
        public string Name;

        [XmlElement("ValueAsString", typeof(string))]
        [XmlElement("ValueAsFloat", typeof(float))]
        [XmlElement("ValueAsInt", typeof(int))]
        [XmlElement("ValueAsBool", typeof(bool))]
        [XmlElement("ValueAsLong", typeof(long))]
        [XmlElement("ValueAsDouble", typeof(double))]
        [XmlElement("ValueAsObject", typeof(object))]
        [XmlElement("ValueAsIntRectangle", typeof(Rectangle))]
        [XmlElement("ValueAsRectangle", typeof(FloatRectangle))]
        // Can't do this because it's an abstract class
        //[XmlElement("ValueAsEnum", typeof(Enum))]
        public object Value;

        // This is only needed for JSON serialization, as XML can use the XmlElement attribute to know its type:
        public string Type { get; set; }

        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }

    public static class PropertySaveListExtensions
    {
        public static bool ContainsValue(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (PropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }

        public static object GetValue(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (PropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return propertySave.Value;
                }
            }
            return null ;
        }

        /// <summary>
        /// Returns the value for the argument property name if it exists, otherwise returns the default 
        /// value for the type (such as false for a bool). This will not throw an exception if the property name is missing.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="propertySaveList">The property list.</param>
        /// <param name="nameToSearchFor">The name of the property to find.</param>
        /// <returns>The found property value, or the default for the type if not found.</returns>
        public static T GetValue<T>(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            var copy = propertySaveList.ToArray();
            foreach (PropertySave propertySave in copy)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    var uncastedValue = propertySave.Value;
                    if( typeof(T) == typeof(int) && uncastedValue is long asLong)
                    {
                        return (T)((object)(int)asLong);
                    }
                    else if(typeof(T) == typeof(float) && uncastedValue is double asDouble)
                    {
                        return (T)((object)(float)asDouble);
                    }
                    else if (typeof(T) == typeof(decimal) && uncastedValue is double asDouble2)
                    {
                        return (T)((object)(float)asDouble2);
                    }
                    else if(typeof(T).IsEnum && uncastedValue is long asLong2)
                    {
                        return (T)((object)(int)asLong2);
                    }
                    else if (typeof(T).IsEnum && uncastedValue is int asInt)
                    {
                        return (T)((object)asInt);
                    }
                    else
                    {
                        return (T)propertySave.Value;
                    }
                }
            }
            return default(T);
        }

        public static void Remove(this List<PropertySave> propertySaveList, string nameToRemove)
        {
            for (int i = 0; i < propertySaveList.Count; i++)
            {
                if (propertySaveList[i].Name == nameToRemove)
                {
                    propertySaveList.RemoveAt(i);
                    break;
                }
            }
        }

        public static void SetValue<T>(this List<PropertySave> propertySaveList, string nameToSearchFor, T value)
        {

            bool isDefault = IsValueDefault(value);

            if (isDefault)
            {
                propertySaveList.RemoveAll(item => item.Name == nameToSearchFor);
            }
            else
            {
                var existingProperty = propertySaveList.FirstOrDefault(item => item.Name == nameToSearchFor);
                if(existingProperty != null)
                {

                    existingProperty.Value = value;
                }
                else
                {
                    // If we got here then that means there isn't already something in place for this
                    PropertySave newPropertySave = new PropertySave();
                    newPropertySave.Name = nameToSearchFor;
                    newPropertySave.Value = value;

                    if(typeof(T) == typeof(int))
                    {
                        newPropertySave.Type = "int";
                    }
                    else if(typeof(T) == typeof(float))
                    {
                        newPropertySave.Type = "float";
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        newPropertySave.Type = "decimal";
                    }
                    else
                    {
                        newPropertySave.Type = typeof(T).Name;
                    }


                    propertySaveList.Add(newPropertySave);
                }
            }
        }

        public static void SetValuePersistIfDefault(this List<PropertySave> propertySaveList, string nameToSearchFor, object value)
        {
            var existingProperty = propertySaveList.FirstOrDefault(item => item.Name == nameToSearchFor);
            if (existingProperty != null)
            {

                existingProperty.Value = value;
            }
            else
            {
                // If we got here then that means there isn't already something in place for this
                PropertySave newPropertySave = new PropertySave();
                newPropertySave.Name = nameToSearchFor;
                newPropertySave.Value = value;
                propertySaveList.Add(newPropertySave);
            }
        }

        static bool IsValueDefault(object value)
        {
            if(value is bool)
            {
                return ((bool)value) == false;
            }
            if (value is byte)
            {
                return ((byte)value) == (byte)0;
            }
            if(value is double)
            {
                return ((double)value) == (double)0;
            }           
            
            if (value is float)
            {
                return ((float)value) == 0.0f;
            }
            if (value is int)
            {
                return ((int)value) == 0;
            }

            if(value is long)
            {
                return ((long) value) == (long)0;
            }

            if (value is string)
            {
                return string.IsNullOrEmpty((string)value);
            }

            if(value == null)
            {
                return true;
            }

            return false;
            

        }

    }
}
