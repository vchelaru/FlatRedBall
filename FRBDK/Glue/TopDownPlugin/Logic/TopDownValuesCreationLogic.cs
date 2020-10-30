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
        public static void GetCsvValues(EntitySave currentEntitySave,
            out Dictionary<string, TopDownValues> csvValues,
            out List<Type> additionalValueTypes,
            out CsvHeader[] headers)
        {
            csvValues = new Dictionary<string, TopDownValues>();
            var filePath = CsvGenerator.Self.CsvFileFor(currentEntitySave);
            headers = null;

            bool doesFileExist = filePath.Exists();

            additionalValueTypes = new List<Type>();

            if (doesFileExist)
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

                    //CsvFileManager.CsvDeserializeDictionary<string, TopDownValues>(filePath.FullPath, csvValues);
                }
                catch (Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load top down csv:\n" + e.ToString());
                }
            }

        }

        private static bool GetIfShouldIncludeInAdditionalValues(string headerName)
        {
            return headerName != nameof(TopDownValues.Name) &&
                headerName != nameof(TopDownValues.UsesAcceleration) &&
                headerName != nameof(TopDownValues.MaxSpeed) &&
                headerName != nameof(TopDownValues.AccelerationTime) &&
                headerName != nameof(TopDownValues.DecelerationTime) &&
                headerName != nameof(TopDownValues.UpdateDirectionFromVelocity) &&
                headerName != nameof(TopDownValues.IsUsingCustomDeceleration) &&
                headerName != nameof(TopDownValues.CustomDecelerationValue) 
                ;
        }

        private static Type GetTypeFromTypeName(string typeAsString)
        {
            switch (typeAsString)
            {
                case "float":
                    return typeof(float);
                case "int":
                    return typeof(int);
                case "bool":
                    return typeof(bool);
                case "long":
                    return typeof(long);
                case "double":
                    return typeof(double);
                case "byte":
                    return typeof(byte);
            }

            return typeof(string);
        }

        private static object CastValue(string value, string originalText)
        {
            string typeAsString = GetTypeFromFullHeader(originalText);

            switch (typeAsString)
            {
                case "float":
                    return System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
                case "int":
                    return System.Convert.ToInt32(value);
                case "bool":
                    return System.Convert.ToBoolean(value);
                case "long":
                    return System.Convert.ToInt64(value);
                case "double":
                    return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                case "byte":
                    return System.Convert.ToByte(value);
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
