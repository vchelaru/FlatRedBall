using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using TMXGlueLib;

namespace TmxEditor.PropertyGridDisplayers
{
    public static class DisplayerExtensionMethods
    {
        public static void DisplayProperties(this PropertyGridDisplayer displayer, IEnumerable<property> propertyList)
        {
            foreach (var property in propertyList)
            {
                displayer.IncludeProperty(propertyList, property );
            }

        }

        private static TypeConverter GetTypeConverterForProperty(property property)
        {
            int lastOpen = property.name.LastIndexOf('(');
            int lastClosed = property.name.LastIndexOf(')');
            string type = null;
            if (lastOpen != -1 && lastClosed != -1 && lastClosed > lastOpen)
            {
                type = property.name.Substring(lastOpen + 1, lastClosed - (lastOpen + 1));
            }

            switch (type)
            {
                case "float":
                case "Single":
                case "System.Single":
                    return new System.ComponentModel.SingleConverter();
                //break;
                case "int":
                case "System.Int32":
                    return new System.ComponentModel.Int32Converter();
                //break;
                case "bool":
                case "Boolean":
                case "System.Boolean":
                    return new System.ComponentModel.BooleanConverter();
                //break;
                case "long":
                case "System.Int64":
                    return new System.ComponentModel.Int64Converter();
                //break;
                case "double":
                case "Double":
                case "System.Double":
                    return new System.ComponentModel.DoubleConverter();
                // break;
                default:
                    return null;
            }
        }


        private static void IncludeProperty(this PropertyGridDisplayer displayer, IEnumerable<property> propertyList, TMXGlueLib.property property)
        {
            string name = property.name;

            TypeConverter typeConverter = GetTypeConverterForProperty(property);

            displayer.IncludeMember(property.name, typeof(string),
                (sender, args) =>
                {
                    var foundProperty = GetPropertyByName(args.Member, propertyList);

                    if (foundProperty != null)
                    {
                        if (args.Value == null)
                        {
                            foundProperty.value = null;
                        }
                        else
                        {
                            foundProperty.value = args.Value.ToString();
                        }
                    }


                }
                ,
                () =>
                {
                    var found = propertyList.FirstOrDefault((candidate) => candidate.name == name);

                    if (property != null)
                    {
                        return property.value;
                    }
                    return null;
                },
                typeConverter
                );
        }

        public static property GetPropertyByName(string name, IEnumerable<property> properties)
        {
            property property = null;


            property = properties.FirstOrDefault((candidate) => candidate.name == name);

            return property;
        }

    }
}
