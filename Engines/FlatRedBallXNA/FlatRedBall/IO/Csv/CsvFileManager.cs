using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
#if FRB_XNA

using FlatRedBall.Instructions.Reflection;
#endif
using System.IO;

#if FRB_XNA
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.IO.Csv
{
#region XML Docs
    /// <summary>
    /// Class providing methods for interacting with .CSV spreadsheet files.
    /// </summary>
#endregion
    public static class CsvFileManager
    {
        public static char Delimiter = ',';

#if FRB_RAW
        public static string ContentManagerName = "Global";
#else
        public static string ContentManagerName = FlatRedBallServices.GlobalContentManager;
#endif

        public static List<object> CsvDeserializeList(Type typeOfElement, string fileName)
        {

            List<object> listOfObjects = new List<object>();


            CsvDeserializeList(typeOfElement, fileName, listOfObjects);

            return listOfObjects;
        }

        public static void CsvDeserializeList(Type typeOfElement, string fileName, IList listToPopulate)
        {
            RuntimeCsvRepresentation rcr = CsvDeserializeToRuntime(fileName);

            rcr.CreateObjectList(typeOfElement, listToPopulate, ContentManagerName);
        }

        public static void CsvDeserializeDictionary<KeyType, ValueType>(string fileName, Dictionary<KeyType, ValueType> dictionaryToPopulate, DuplicateDictionaryEntryBehavior duplicateDictionaryEntryBehavior = DuplicateDictionaryEntryBehavior.ThrowException)
        {
            var rcr = CsvDeserializeToRuntime(fileName);

            rcr.FillObjectDictionary<KeyType, ValueType>(dictionaryToPopulate, ContentManagerName, duplicateDictionaryEntryBehavior);
        }

        public static void CsvDeserializeDictionary<KeyType, ValueType>(string fileName, Dictionary<KeyType, ValueType> dictionaryToPopulate, out RuntimeCsvRepresentation rcr)
        {
            rcr = CsvDeserializeToRuntime(fileName);

            rcr.FillObjectDictionary<KeyType, ValueType>(dictionaryToPopulate, ContentManagerName);
        }

        public static void UpdateDictionaryValuesFromCsv<KeyType, ValueType>(Dictionary<KeyType, ValueType> dictionaryToUpdate, string fileName)
        {
            var rcr = CsvDeserializeToRuntime(fileName);

            rcr.UpdateValues(dictionaryToUpdate, ContentManagerName);
        }

        public static void CsvDeserializeDictionary<KeyType, ValueType>(Stream stream, Dictionary<KeyType, ValueType> dictionaryToPopulate)
        {
            var rcr = CsvDeserializeToRuntime<RuntimeCsvRepresentation>(stream);

            rcr.FillObjectDictionary<KeyType, ValueType>(dictionaryToPopulate, ContentManagerName);
        }

        public static RuntimeCsvRepresentation CsvDeserializeToRuntime(string fileName)
        {
            return CsvDeserializeToRuntime<RuntimeCsvRepresentation>(fileName);
        }

        public static T CsvDeserializeToRuntime<T>(string fileName) where T : RuntimeCsvRepresentation, new()
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }

#if ANDROID || IOS
			fileName = fileName.ToLowerInvariant();
#endif

            FileManager.ThrowExceptionIfFileDoesntExist(fileName);

            T runtimeCsvRepresentation = null;

            var extension = FileManager.GetExtension(fileName);
            if (string.Equals(extension, "csv", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, "txt", StringComparison.OrdinalIgnoreCase))
            {
#if MONOGAME
                
                Stream stream = FileManager.GetStreamForFile(fileName);
#else
                // Creating a filestream then using that enables us to open files that are open by other apps.
                FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#endif
                runtimeCsvRepresentation = CsvDeserializeToRuntime<T>(stream);
                FileManager.Close(stream);
                stream.Dispose();
            }
#if FRB_XNA
            else
            {
                if (extension != String.Empty)
                {
#if DEBUG
                    if (extension != "xnb")
                        throw new ArgumentException(string.Format("CSV files with extension '.{0}' are not supported", extension));
#endif
                    fileName = FileManager.RemoveExtension(fileName);
                }
                runtimeCsvRepresentation = FlatRedBallServices.Load<T>(fileName);
            }
#endif
            return runtimeCsvRepresentation;
        }

        public static T CsvDeserializeToRuntime<T>(Stream stream) where T : RuntimeCsvRepresentation, new()
        {
            T runtimeCsvRepresentation;

            using (System.IO.StreamReader streamReader = new StreamReader(stream))
            using (CsvReader csv = new CsvReader(streamReader, true, Delimiter, CsvReader.DefaultQuote, CsvReader.DefaultEscape, CsvReader.DefaultComment, true, CsvReader.DefaultBufferSize))
            {
                runtimeCsvRepresentation = new T();

                string[] fileHeaders = csv.GetFieldHeaders();
                runtimeCsvRepresentation.Headers = new CsvHeader[fileHeaders.Length];

                for (int i = 0; i < fileHeaders.Length; i++)
                {
                    runtimeCsvRepresentation.Headers[i] = new CsvHeader(fileHeaders[i]);
                }

                int numberOfHeaders = runtimeCsvRepresentation.Headers.Length;

                runtimeCsvRepresentation.Records = new List<string[]>();

                int recordIndex = 0;
                int columnIndex = 0;
                string[] newRecord = null;
                try
                {
                    while (csv.ReadNextRecord())
                    {
                        newRecord = new string[numberOfHeaders];

                        bool anyNonEmpty = false;
                        for (columnIndex = 0; columnIndex < numberOfHeaders; columnIndex++)
                        {
                            string record = csv[columnIndex];

                            newRecord[columnIndex] = record;
                            if (record != "")
                            {
                                anyNonEmpty = true;
                            }
                        }

                        if (anyNonEmpty)
                        {
                            runtimeCsvRepresentation.Records.Add(newRecord);
                        }
                        recordIndex++;
                    }
                }
                catch (Exception e)
                {
                    string message =
                        "Error reading record " + recordIndex + " at column " + columnIndex;

                    if (columnIndex != 0 && newRecord != null)
                    {
                        foreach (string s in newRecord)
                        {

                            message += "\n" + s;
                        }
                    }

                    throw new Exception(message, e);

                }
            }

            return runtimeCsvRepresentation;
        }

        public static void Serialize(RuntimeCsvRepresentation rcr, string fileName)
        {
            if (rcr == null)
                throw new ArgumentNullException("rcr");

            string toSave = rcr.GenerateCsvString(Delimiter);

            FileManager.SaveText(toSave, fileName);
        }

        private static void AppendMemberValue(StringBuilder stringBuilder, ref bool first, Type type, Object valueAsObject)
        {
            if (first)
                first = false;
            else
                stringBuilder.Append(" ,");

            String value;
            bool isString = false;

            if (type == typeof(string)) //check if the value is a string if so, it should be surrounded in quotes
            {
                isString = true;
            }

            if (valueAsObject == null)
            {
                value = "";
                stringBuilder.Append(value);
            }
            else                                  //if not null, append the value
            {
                if (isString)
                {
                    stringBuilder.Append("\"");
                }

                value = valueAsObject.ToString();
                value = value.Replace('\n', ' ');      //replace newlines 

                stringBuilder.Append(value);

                if (isString)
                {
                    stringBuilder.Append("\"");
                }
            }
        }
    }
}