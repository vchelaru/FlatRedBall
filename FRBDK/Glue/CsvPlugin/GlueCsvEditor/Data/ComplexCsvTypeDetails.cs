using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Utilities;

namespace GlueCsvEditor.Data
{
    public class ComplexCsvTypeDetails
    {
        #region Fields

        private const char PropertyCollectionStartChar = '(';
        private const char PropertyCollectionEndChar = ')';

        #endregion



        [Category("General Information")]
        public string TypeName { get; set; }

        [Category("General Information")]
        public string Namespace { get; set; }

        [Category("General Information")]
        [DisplayName("Use \"new\"")]
        public bool UseNewSyntax { get; set; }

        [Browsable(false)]
        public string DefaultType { get; set; }


        public List<ComplexTypeProperty> Properties { get; private set; }

        public static bool IsLonghandComplexType(string value)
        {
            return value != null && value.StartsWith("new ", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsShorthandComplexDefinition(string value)
        {
            return string.IsNullOrWhiteSpace(value) == false && value.Contains("=");
        }

        public ComplexCsvTypeDetails()
        {
            Properties = new List<ComplexTypeProperty>();
        }

        public static ComplexCsvTypeDetails ParseValue(string value)
        {
            return ParseLonghandTypeFormat(value) 
                ?? ParseShorthandTypeFormat(value);
        }

        private static ComplexCsvTypeDetails ParseLonghandTypeFormat(string value)
        {
            var result = new ComplexCsvTypeDetails { UseNewSyntax = true };


            // Figure out if the type is in the format of 
            //   new Namespace.Type( Property1 = abc, Property2 = cdef )
            value = (value ?? string.Empty).Trim();
            if (IsLonghandComplexType(value))
            {
                // Get indexes for braces/parenthesis
                int start = value.IndexOf(PropertyCollectionStartChar);
                int end = value.LastIndexOf(PropertyCollectionEndChar);

                if (start < 0 || end < 0)
                    return null; // Invalid format

                // Isolate the type string
                string isolatedTypeString = value.Substring(3, start - 3);
                if (isolatedTypeString.Contains("."))
                {
                    result.Namespace = isolatedTypeString.Remove(isolatedTypeString.LastIndexOf(".", System.StringComparison.Ordinal)).Trim();
                    result.TypeName = isolatedTypeString.Substring(isolatedTypeString.LastIndexOf(".", System.StringComparison.Ordinal) + 1).Trim();
                }
                else
                {
                    result.Namespace = string.Empty;
                    result.TypeName = isolatedTypeString.Trim();
                }

                // Figure out the properties
                string propertiesString = value.Substring(start + 1, end - start - 1);
                var propertyDefinitions = propertiesString.Split(new char[] { ',' });
                foreach (var propertyDefinition in propertyDefinitions)
                {
                    var pair = propertyDefinition.Split(new char[] { '=' });
                    if (pair.Length != 2)
                        continue; // not a valid property definition

                    result.Properties.Add(new ComplexTypeProperty
                    {
                        Name = pair[0].Trim(),
                        Value = pair[1].Trim()
                    });
                }
            }
            else
            {
                result = null;
            }
            return result;
        }
        private static ComplexCsvTypeDetails ParseShorthandTypeFormat(string value)
        {
            var result = new ComplexCsvTypeDetails { UseNewSyntax = false };

            // Parse type specified in "property = value, property2 = value2" format
            if (IsShorthandComplexDefinition(value))
            {

                var splitProperties = FlatRedBall.Instructions.Reflection.PropertyValuePair.SplitProperties(value);
                foreach (var property in splitProperties)
                {
                    // Invalid propery definitions are ignored
                    var parts = property.Split('=');
                    if (parts.Length != 2)
                        continue;

                    // First part is the property name
                    if (parts[0].Trim() == string.Empty)
                        continue;

                    result.Properties.Add(new ComplexTypeProperty
                    {
                        Name = parts[0].Trim(),
                        Value = parts[1].Trim()
                    });
                }
            }
            else
            {
                result = null;
            }
            return result;
        }

        public override string ToString()
        {
            if (UseNewSyntax)
            {
                return ToLonghandString();
            }
            else
            {
                return ToShorthandString();
            }
        }

        private string ToShorthandString()
        {
            var output = new StringBuilder();

            var filledProperties = Properties.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                                             .ToList();

            for (int x = 0; x < filledProperties.Count; x++)
            {
                var prop = filledProperties[x];

                if (x > 0)
                    output.Append(", ");

                output.Append(prop.Name);
                output.Append(" = ");
                output.Append(prop.Value);
            }

            return output.ToString();
        }

        private string ToLonghandString()
        {
            var output = new StringBuilder("new ");

            if (string.IsNullOrEmpty(TypeName))
            {
                output.Append(DefaultType);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Namespace))
                {
                    output.Append(Namespace);
                    output.Append(".");
                }

                output.Append(TypeName);
            }

            // Get a list of all properties that do not hav empty values
            // Victor Chelaru says: The user should be able to set empty
            // strings here, and have them apply.  Therefore we need to differentiate
            // between empty strings and null values.
            var filledProperties = Properties.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                                             .ToList();

            output.Append(PropertyCollectionStartChar);
            output.Append(" ");

            // Add defined properties
            if (filledProperties.Any())
            {
                for (int x = 0; x < filledProperties.Count; x++)
                {
                    var prop = filledProperties[x];

                    if (x > 0)
                        output.Append(", ");

                    output.Append(prop.Name);
                    output.Append(" = ");
                    output.Append(prop.Value);
                }
            }

            output.Append(" ");
            output.Append(PropertyCollectionEndChar);

            return output.ToString();
        }

    }
}
