using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.DataGenerators;
using TopDownPlugin.Models;

namespace TopDownPlugin.Logic
{
    static class TopDownValuesCreationLogic
    {
        static List<CsvHeader> requiredCsvHeaders;
        public static List<CsvHeader> RequiredCsvHeaders
        {
            get
            {
                if(requiredCsvHeaders == null)
                {
                    FillRequiredCsvHeaders();
                }
                return requiredCsvHeaders;
            }
        }

        private static void FillRequiredCsvHeaders()
        {
            requiredCsvHeaders = new List<CsvHeader>();

            CsvHeader Add(string propertyName, string suffix, bool isRequired = false)
            {
                var header = new CsvHeader
                {
                    OriginalText = propertyName + " " + suffix,
                    IsRequired = isRequired,
                    Name = propertyName,
                    MemberTypes = System.Reflection.MemberTypes.Property
                };
                requiredCsvHeaders.Add(header);
                return header;
            }

            Add(nameof(TopDownValues.Name), "(string, required)", true);

            Add(nameof(TopDownValues.UsesAcceleration), "(bool)");

            Add(nameof(TopDownValues.MaxSpeed), "(float)");

            Add(nameof(TopDownValues.AccelerationTime), "(float)");

            Add(nameof(TopDownValues.DecelerationTime), "(float)");


            Add(nameof(TopDownValues.UpdateDirectionFromInput), "(bool)");


            Add(nameof(TopDownValues.IsUsingCustomDeceleration), "(bool)");

            Add(nameof(TopDownValues.CustomDecelerationValue), "(float)");

            Add(nameof(TopDownValues.InheritOrOverwriteAsInt), "(int)");

            Add(nameof(TopDownValues.UpdateDirectionFromVelocity), "(bool)");

            // I think new values should be added at the end of they will mess up existing CSVs
        }


        public static void GetCsvValues(EntitySave currentEntitySave,
            out Dictionary<string, TopDownValues> csvValues,
            out List<Type> additionalValueTypes,
            out CsvHeader[] headers)
        {
            csvValues = new Dictionary<string, TopDownValues>();
            var filePath = CsvGenerator.Self.CsvTopdownFileFor(currentEntitySave);
            headers = null;

            additionalValueTypes = new List<Type>();

            if (filePath.Exists())
            {
                try
                {
                    var rawValues = CsvFileManager.CsvDeserializeToRuntime(filePath.FullPath);

                    List<TopDownValues> values = new List<TopDownValues>();

                    rawValues.CreateObjectList(typeof(TopDownValues), values);

                    headers = rawValues.Headers;

                    for(int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                    {
                        string headerName = headers[columnIndex].Name;
                        bool shouldInclude = GetIfShouldIncludeInAdditionalValues(headerName);
                        if(shouldInclude)
                        {
                            string typeAsString = GetTypeFromFullHeader(headers[columnIndex].OriginalText);

                            var type = GetTypeFromTypeName(typeAsString);

                            additionalValueTypes.Add(type);
                        }


                    }

                    for (int i = 0; i < rawValues.Records.Count; i++)
                    {
                        for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                        {
                            string headerName = headers[columnIndex].Name;
                            bool shouldInclude = GetIfShouldIncludeInAdditionalValues(headerName);

                            if (shouldInclude)
                            {


                                var castedValue = CastValue(
                                    rawValues.Records[i][columnIndex],
                                    headers[columnIndex].OriginalText);
                                values[i].AdditionalValues.Add(headerName, castedValue);
                            }
                        }

                        csvValues.Add(values[i].Name, values[i]);

                    }
                }
                catch (Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load top down csv:\n" + e.ToString());
                }
            }

        }

        private static bool GetIfShouldIncludeInAdditionalValues(string headerName)
        {
            return RequiredCsvHeaders.Any(item => item.Name == headerName) == false;
        }

        private static Type GetTypeFromTypeName(string typeAsString)
        {
            return TypeManager.GetTypeFromString(typeAsString);
        }

        private static object CastValue(string value, string originalText)
        {
            string typeAsString = GetTypeFromFullHeader(originalText);

            if(TypeManager.TryCastValue(typeAsString, value, out var convertedValue))
            {
                return convertedValue;
            }

            return value;
        }

        private static string GetTypeFromFullHeader(string originalText)
        {
            string typeAsString = null;

            if (originalText?.Contains("(") == true)
            {
                var closingParen = originalText.LastIndexOf(")");
                var startIndex = originalText.IndexOf("(") + 1;
                typeAsString = originalText.Substring(startIndex, (closingParen) - startIndex);
            }

            return typeAsString;
        }
    }
}
