using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO.Csv;
using GlueCsvEditor.KnownValues;

namespace GlueCsvEditor.Data
{
    public class CsvData
    {
        #region Fields

        private readonly string _csvPath;
        private char _delimiter;
        private CachedTypes _cachedTypes;
        public RuntimeCsvRepresentation CsvRepresentation { get; private set; }

        #endregion

        #region Properties

        public string CsvPath { get { return _csvPath; } }

        public int RowCount { get { return  CsvRepresentation.Records.Count; } }

        public int ColumnCount 
        { 
            get 
            {
                if (CsvRepresentation.Records.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return CsvRepresentation.Records[0].Length;
                }
            } 
        }

        #endregion

        #region Events 

        /// <summary>
        /// Event raised before a value is changed.  The values are Row, Column.
        /// </summary>
        public event Action<int, int> BeforeValueChange;

        /// <summary>
        /// Event raised after a value is changed.  The values are Row, Column
        /// </summary>
        public event Action<int, int> AfterValueChange;

        #endregion

        #region Methods

        public CsvData(string csvPath, CachedTypes cachedTypes, char delimiter = ',')
        {
            _csvPath = csvPath;
            _delimiter = delimiter;
            _cachedTypes = cachedTypes;
            Reload();
        }


        /// <summary>
        /// Adds a new row at the specified index
        /// </summary>
        /// <returns></returns>
        public void AddRow(int index)
        {
            // If index is less than 0, set it to be zero as the minimum
            if (index < 0)
                index = 0;

            CsvRepresentation.Records.Insert(index, new string[CsvRepresentation.Headers.Length]);
        }

        /// <summary>
        /// Removes the specified data row
        /// </summary>
        /// <param name="index"></param>
        public void RemoveRow(int index)
        {
            if (index >= CsvRepresentation.Records.Count)
                throw new ArgumentOutOfRangeException("index");

            CsvRepresentation.Records.RemoveAt(index);
        }

        /// <summary>
        /// Adds a column to the CSV
        /// </summary>
        /// <param name="index"></param>
        public void AddColumn(int index)
        {
            string headerName = "NewColumn" + index;

            List<string> headerNames = new List<string>();
            headerNames.AddRange(CsvRepresentation.Headers.Select(item => item.Name));

            headerName = FlatRedBall.Utilities.StringFunctions.MakeStringUnique(
                headerName, headerNames);
            // Add this column to the RCR
            var headers = new List<CsvHeader>(CsvRepresentation.Headers);
            headers.Insert(index, new CsvHeader { Name = headerName, OriginalText = headerName + " (string)" });
            CsvRepresentation.Headers = headers.ToArray();

            // Add the column to all the records
            for (int x = 0; x < CsvRepresentation.Records.Count; x++)
            {
                var values = new List<string>(CsvRepresentation.Records[x]);
                values.Insert(index, string.Empty);
                CsvRepresentation.Records[x] = values.ToArray();
            }
        }

        /// <summary>
        /// Removes the specified column from the csv data
        /// </summary>
        /// <param name="index"></param>
        public void RemoveColumn(int index)
        {
            // Remove this column to the RCR
            var headers = new List<CsvHeader>(CsvRepresentation.Headers);
            headers.RemoveAt(index);
            CsvRepresentation.Headers = headers.ToArray();

            // Remove the column to all the records
            for (int x = 0; x < CsvRepresentation.Records.Count; x++)
            {
                var values = new List<string>(CsvRepresentation.Records[x]);
                values.RemoveAt(index);
                CsvRepresentation.Records[x] = values.ToArray();
            }
        }

        /// <summary>
        /// Returns the number of records in the csv
        /// </summary>
        /// <returns></returns>
        public int GetRecordCount()
        {
            return CsvRepresentation.Records.Count;
        }

        /// <summary>
        /// Gets the value in the specified cell
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public string GetValue(int row, int column)
        {
            if (row >= CsvRepresentation.Records.Count)
                throw new ArgumentOutOfRangeException("row");

            if (column >= CsvRepresentation.Records[row].Length)
                throw new ArgumentOutOfRangeException("column");

            return CsvRepresentation.Records[row][column];
        }

        public bool TryGetValue(int row, int column, out string value)
        {
            value = null;
            if (row < CsvRepresentation.Records.Count && column < CsvRepresentation.Records[row].Length)
            {

                value = CsvRepresentation.Records[row][column];
                return true;
            }

            return false;
        }

        public bool TrySetValue(int row, int column, string value)
        {
            if(row < CsvRepresentation.Records.Count &&
                column < CsvRepresentation.Records[row].Length)
            {
                SetValue(row, column, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the value in the specified row and column
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        public void SetValue(int row, int column, string value)
        {
            if (row >= CsvRepresentation.Records.Count)
                throw new ArgumentOutOfRangeException("row");

            if (column >= CsvRepresentation.Records[row].Length)
                throw new ArgumentOutOfRangeException("column");

            if (BeforeValueChange != null)
            {
                BeforeValueChange(row, column);
            }

            CsvRepresentation.Records[row][column] = value;

            if (AfterValueChange != null)
            {
                AfterValueChange(row, column);
            }
        }

        /// <summary>
        /// Retrieves a list of headers for the CSV
        /// </summary>
        /// <returns></returns>
        public List<string> GetHeaderText()
        {
            return CsvRepresentation.Headers
                       .Select(x => x.OriginalText)
                       .ToList();
        }

        /// <summary>
        /// Retrieves information about the specific column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public CsvColumnHeader GetHeaderDetails(int column)
        {
            if (column >= CsvRepresentation.Headers.Length)
                throw new ArgumentOutOfRangeException("column");

            bool isList;
            var header = CsvRepresentation.Headers[column];
            string type = CsvHeader.GetClassNameFromHeader(header.OriginalText) ?? "string";

            int typeDataIndex = header.Name.IndexOf("(", StringComparison.Ordinal);
            if (typeDataIndex < 0)
                typeDataIndex = header.Name.Length;

            // Strip out the List< and > values
            if (type.Contains("List<"))
            {
                isList = true;
                type = type.Replace("List<", "");
                if (type.Contains(">"))
                    type = type.Remove(type.LastIndexOf(">", StringComparison.Ordinal), 1);
            }
            else
            {
                isList = false;
            }

            return new CsvColumnHeader
            {
                Name = header.Name.Substring(0, typeDataIndex),
                Type = type,
                IsRequired = header.IsRequired,
                IsList = isList
            };
        }

        /// <summary>
        /// Sets the specified column header with specific values
        /// </summary>
        /// <param name="column"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="isRequired"></param>
        /// <param name="isList"></param>
        /// <returns>Returns the new display string for the header</returns>
        public string SetHeader(int column, string name, string type, bool isRequired, bool isList)
        {
            if (column >= CsvRepresentation.Headers.Length)
                throw new ArgumentOutOfRangeException("column");

            // Form the new text value
            var text = new StringBuilder();
            text.Append(name.Trim());
            text.Append(" (");

            if (isList)
                text.Append("List<");

            text.Append(type.Trim());

            if (isList)
                text.Append(">");

            if (isRequired)
                text.Append(", required");

            text.Append(")");

            // Update the header details
            var header = CsvRepresentation.Headers[column];
            header.OriginalText = text.ToString();
            header.Name = text.ToString();
            header.IsRequired = isRequired;

            CsvRepresentation.Headers[column] = header;
            CsvRepresentation.RemoveHeaderWhitespaceAndDetermineIfRequired();
            return text.ToString();
        }

        /// <summary>
        /// Searches the CSV for the next cell containing a string, 
        /// starting from the specified row and column
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <param name="ignoreStartingCell"></param>
        /// <param name="reverse"></param>
        /// <returns></returns>
        public FoundCell FindNextValue(string searchString, int startRow, int startColumn, bool ignoreStartingCell = false, bool reverse = false)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return null;            

            int row = startRow;
            int column = startColumn;
            searchString = searchString.Trim();
            bool isFirstSearchedCell = true;

            // Traverse through the records of the RCR until we find the next match
            do
            {
                if ((ignoreStartingCell && !isFirstSearchedCell) || !ignoreStartingCell)
                {
                    var recordAtLocation = CsvRepresentation.Records[row][column];
                    if (recordAtLocation != null && recordAtLocation.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new FoundCell { ColumnIndex = column, RowIndex = row };
                    }
                }
                // This cell doesn't have the record, go to the next
                if (!reverse)
                {
                    column++;
                    if (column >= CsvRepresentation.Headers.Length)
                    {
                        column = 0;
                        row++;

                        if (row >= CsvRepresentation.Records.Count)
                            row = 0;
                    }
                }

                else
                {
                    column--;
                    if (column < 0)
                    {
                        column = CsvRepresentation.Headers.Length - 1;
                        row--;

                        if (row < 0)
                            row = CsvRepresentation.Records.Count - 1;
                    }
                }

                isFirstSearchedCell = false;

            } while (row != startRow || column != startColumn);

            return null;
        }

        /// <summary>
        /// Reloads the CSV data from disk
        /// </summary>
        public void Reload()
        {
            CsvFileManager.Delimiter = _delimiter;
            CsvRepresentation = CsvFileManager.CsvDeserializeToRuntime(_csvPath);
            CsvRepresentation.RemoveHeaderWhitespaceAndDetermineIfRequired();
        }

        /// <summary>
        /// Saves all csv data
        /// </summary>
        public GeneralResponse SaveCsv()
        {
            GeneralResponse toReturn = new GeneralResponse();

            CsvRepresentation.RemoveHeaderWhitespaceAndDetermineIfRequired();

            FileInfo fileInfo = new FileInfo(_csvPath);
            if (fileInfo.IsReadOnly)
            {
                toReturn.Succeeded = false;
                toReturn.Message = "CSV file is marked readonly so it cannot be saved:\n" + _csvPath +
                    "\nPerhaps Excel is open?";
            }
            else
            {
                CsvFileManager.Delimiter = _delimiter;
                try
                {
                    CsvFileManager.Serialize(CsvRepresentation, _csvPath);
                    toReturn.Succeeded = true;
                }
                catch (IOException)
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = "Could not save the CSV file:\n\t" + 
                        _csvPath + "\n\tGlue will not be able to save the file if it is open in Excel.";
                }
                catch (Exception e)
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = "Error saving the file " + _csvPath + "\n" + e.ToString();
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Retrieves any known values for the specified cell
        /// </summary>
        /// <param name="column"></param>
        public IEnumerable<string> GetKnownValues(int column)
        {
            string type = CsvHeader.GetClassNameFromHeader(CsvRepresentation.Headers[column].OriginalText);
            type = type ?? string.Empty;

            // Remove the List<> if exists
            type = type.Replace("List<", "").Replace(">", "");

            var foundTypes = GetKnownValuesForType(type);
            var knownValues = foundTypes as string[] ?? foundTypes.ToArray();
            if (!knownValues.Any())
                return new UsedRcrColumnValueRetriever(CsvRepresentation, column).GetKnownValues(type);

            return knownValues;
        }

        public IEnumerable<string> GetKnownValuesForType(string type)
        {
            if (!_cachedTypes.IsCacheReady)
                return new string[0];

            if (type != null)
            {
                // Remove the List<> if exists
                type = type.Replace("List<", "").Replace(">", "");

                // This list is prioritized.  The first retriever to get a value is the only one used
                var knownValueRetrievers = new List<IKnownValueRetriever>
                    {
                    new EnumReflectionValueRetriever(),
                    new FrbStateValueRetriever(),
                    new ParsedEnumValueRetriever(_cachedTypes.ProjectEnums),
                    new InterfaceImplementationsValueRetriever(_cachedTypes.ProjectClasses)
                };

                // Loop through the value retrievers until one returns a valid results
                foreach (var retriever in knownValueRetrievers)
                {
                    var values = retriever.GetKnownValues(type);
                    var knownValuesForType = values as string[] ?? values.ToArray();
                    if (knownValuesForType.Any())
                        return knownValuesForType;
                }
            }

            // No values were found
            return new string[0];
        }

        public IEnumerable<ComplexTypeProperty> GetKnownProperties(int columnIndex)
        {
            if (!_cachedTypes.IsCacheReady)
                return new ComplexTypeProperty[0];

            string typeName = CsvHeader.GetClassNameFromHeader(CsvRepresentation.Headers[columnIndex].OriginalText);

            return GetKnownProperties(typeName);
        }

        public IEnumerable<ComplexTypeProperty> GetKnownProperties(string typeName)
        {
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                // Remove the List<> if exists
                typeName = typeName.Replace("List<", "").Replace(">", "");


                // Check if the type matches a ParsedClass
                var parsedClass = _cachedTypes.ProjectClasses
                                              .FirstOrDefault(x => string.Concat(x.Namespace, ".", x.Name).Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (parsedClass != null)
                {
                    return parsedClass.ParsedProperties
                                      .Select(x =>
                                      {
                                          var toReturn = new ComplexTypeProperty
                                          {
                                              Name = x.Name,
                                              Type = x.Type.Name,
                                          };

                                          toReturn.Attributes.AddRange(x.Attributes);

                                          return toReturn;
                                      })
                                      .ToArray();
                }
                else
                {

                    var foundType = _cachedTypes.AssemblyClasses
                        .FirstOrDefault(x => x.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

                    if (foundType != null)
                    {
                        List<ComplexTypeProperty> toReturn = new List<ComplexTypeProperty>();

                        foreach (var field in foundType.GetFields().Where(
                            (f) =>
                            {
                                return f.IsStatic == false && f.IsPublic == true;
                            }))
                        {
                            ComplexTypeProperty toAdd = new ComplexTypeProperty();
                            toAdd.Name = field.Name;
                            toAdd.Type = field.FieldType.Name;
                            toReturn.Add(toAdd);
                        }


                        foreach (var property in foundType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var found = property.GetCustomAttributes(typeof(BrowsableAttribute), true);
                            bool shouldSkip = false;
                            foreach (BrowsableAttribute attribute in found)
                            {
                                if (attribute != null && attribute.Browsable == false)
                                {
                                    shouldSkip = true;
                                }
                            }
                            if (shouldSkip == false)
                            {
                                ComplexTypeProperty toAdd = new ComplexTypeProperty();

                                toAdd.Name = property.Name;
                                toAdd.Type = property.PropertyType.Name;
                                toReturn.Add(toAdd);
                            }
                        }


                        return toReturn;

                    }
                }
            }

            return new ComplexTypeProperty[0];
        }

        #endregion
    }
}
