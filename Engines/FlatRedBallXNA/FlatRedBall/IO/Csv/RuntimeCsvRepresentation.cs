using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;
using System.Xml.Serialization;
using FlatRedBall.Utilities;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace FlatRedBall.IO.Csv
{
    #region CsvHeader Class

    public struct CsvHeader : IEquatable<CsvHeader>
    {



        public static CsvHeader Empty = new CsvHeader(null);

        /// <summary>
        /// The name of the header - such as "Health"
        /// </summary>
        public string Name;

        /// <summary>
        /// Whether objects must have a value in this header.  This is true for dictionaries, and to
        /// help the csv deserialization understand when a column belongs to a new instance or if it is
        /// a column used for lists. Note that setting this value to true will not automatically update the
        /// OriginalText, which should have "required" if the value is set to true
        /// </summary>
        public bool IsRequired;


        public string OriginalText;

        public static bool operator ==(CsvHeader c1, CsvHeader c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(CsvHeader c1, CsvHeader c2)
        {
            return !c1.Equals(c2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // This gets set in BuildMemberTypeIndexInformation
        public MemberTypes MemberTypes;

        public CsvHeader(string name)
        {
            Name = name;
            OriginalText = name;
            IsRequired = false;
            MemberTypes = (MemberTypes)0;
        }

        public override string ToString()
        {
            if (IsRequired)
            {
                return Name + " (required)";
            }
            else
            {
                return Name;
            }
        }

#region IEquatable<CsvHeader> Members

        public bool Equals(CsvHeader other)
        {
            return Name == other.Name &&
                IsRequired == other.IsRequired &&
                MemberTypes == other.MemberTypes;
        }

#endregion

		public static string GetClassNameFromHeader(string memberName)
		{
            if (string.IsNullOrEmpty(memberName) || memberName.Contains("(") == false)
            {
                return null;
            }
            else
            {
                string className = StringFunctions.GetWordAfter("(", memberName);

                if (className == "required" || className == "required,")
                {
                    int indexOfOpenParen = memberName.IndexOf('(');

                    className = StringFunctions.GetWordAfter(className, memberName, indexOfOpenParen + 1);
                }

                className = className.Replace("required", "").Replace(" ", "").Replace(")", "").Replace(",", "");

                if (string.IsNullOrEmpty(className))
                {
                    return "string";
                }
                else
                {
                    return className;
                }
            }
		}

        public static string GetNameWithoutParentheses(string text)
        {
            string nameWithoutParentheses = FlatRedBall.Utilities.StringFunctions.RemoveWhitespace(text);

            if (nameWithoutParentheses.Contains("("))
            {
                int openingIndex = nameWithoutParentheses.IndexOf('(');


                nameWithoutParentheses = nameWithoutParentheses.Substring(0, openingIndex);
            }
            return nameWithoutParentheses;
        }
    }

    #endregion

    #region Enums

    public enum DuplicateDictionaryEntryBehavior
    {
        ThrowException,
        Replace,
        PreserveFirst
    }


    #endregion

    #region XML Docs
    /// <summary>
    /// Represents the raw data loaded from a csv file.  This is
    /// used if the data must be processed or converted by hand to
    /// other object types.
    /// </summary>
    #endregion
    public class RuntimeCsvRepresentation
    {
        #region Fields

        public CsvHeader[] Headers;
        public List<string[]> Records;

#endregion

        public string GetFirstDuplicateHeader
        {
            get
            {
                for (int i = 0; i < Headers.Length - 1; i++)
                {
                    for (int j = i + 1; j < Headers.Length; j++)
                    {
                        if (Headers[i].Name == Headers[j].Name)
                        {
                            return Headers[i].Name;
                        }
                    }
                }

                return null;
            }
        }

        public string FirstDuplicateRequiredField
        {
            get
            {
                int requiredIndex = GetRequiredIndex();

                if (requiredIndex == -1)
                {
                    // It may be that the headers do specify something is required
                    // but the identified header hasn't been identified yet.
                    for (int i = 0; i < Headers.Length; i++)
                    {
                        if (IsHeaderRequired(Headers[i].Name))
                        {
                            requiredIndex = i;
                            break;
                        }
                    }
                }

                if (requiredIndex != -1)
                {
                    Dictionary<string, object> dictionaryToFill = new Dictionary<string,object>(Records.Count);

                    foreach (string[] row in Records)
                    {
                        string stringAtIndex = row[requiredIndex];

                        if (!string.IsNullOrEmpty(stringAtIndex))
                        {
                            if (dictionaryToFill.ContainsKey(stringAtIndex))
                            {
                                return stringAtIndex;
                            }
                            else
                            {
                                dictionaryToFill.Add(stringAtIndex, null);
                            }
                        }
                    }
                }

                return null;
            }
        }

        public static RuntimeCsvRepresentation FromList<T>(IList<T> items)
        {
            RuntimeCsvRepresentation rcrToReturn = new RuntimeCsvRepresentation();

            Type itemType = null;
            
            itemType = typeof(T);

            List<CsvHeader> headers = new List<CsvHeader>();

            List<FieldInfo> fieldList;
            List<PropertyInfo> propertyList;

            GetSerializableFieldsAndTypes(itemType, out fieldList, out propertyList);


            foreach (FieldInfo field in fieldList)
            {
                CsvHeader header = new CsvHeader(field.Name);
                header.OriginalText = field.Name + " (" + TypeAsFriendlyString(field.FieldType) + ")";
                headers.Add(header);
            }

            foreach (PropertyInfo property in propertyList)
            {
                CsvHeader header = new CsvHeader(property.Name);
                header.OriginalText = property.Name + " (" + TypeAsFriendlyString(property.PropertyType) + ")";
                headers.Add(header);
            }

            rcrToReturn.Headers = headers.ToArray();
            rcrToReturn.Records = new List<string[]>();
            int totalLength = fieldList.Count + propertyList.Count;

            foreach (T item in items)
            {

                AddItemToRcr<T>(rcrToReturn, fieldList, propertyList, totalLength, item);
            }

            return rcrToReturn;


        }

        private static void AddItemToRcr<T>(RuntimeCsvRepresentation rcrToReturn, List<FieldInfo> fieldList, List<PropertyInfo> propertyList, int totalLength, T item)
        {
            int numberOfRows = 1;

            // First determine the number of rows:
            foreach (FieldInfo field in fieldList)
            {
                object value = field.GetValue(item);

                if (value is IList)
                {
                    numberOfRows = System.Math.Max((value as IList).Count, numberOfRows);
                }
            }

            foreach (PropertyInfo property in propertyList)
            {
                object valueBeforeToString = property.GetValue(item, null);
                object value = null;
                if (valueBeforeToString != null)
                {
                    value = valueBeforeToString.ToString();

                }

                if (value is IList)
                {
                    numberOfRows = System.Math.Max((value as IList).Count, numberOfRows);
                }
            }

            for (int rowInEntry = 0; rowInEntry < numberOfRows; rowInEntry++ )
            {
                // Now we write this entry
                string[] record = new string[totalLength];

                int currentIndex = 0;

                foreach (FieldInfo field in fieldList)
                {
                    object value = field.GetValue(item);

                    SetRecordAtIndex(rowInEntry, record, currentIndex, value);
                    

                    currentIndex++;
                }

                foreach (PropertyInfo property in propertyList)
                {
                    object value = property.GetValue(item, null);
                    SetRecordAtIndex(rowInEntry, record, currentIndex, value);
                    currentIndex++;
                }


                rcrToReturn.Records.Add(record);
            }
        }

        private static void SetRecordAtIndex(int rowInEntry, string[] record, int currentIndex, object value)
        {
            if (value != null)
            {
                if (value is IList)
                {
                    if (rowInEntry < (value as IList).Count)
                    {
                        object valueInList = (value as IList)[rowInEntry];

                        record[currentIndex] = ValueAsCsvFriendlyString(valueInList, value.GetType().GetGenericArguments()[0]);
                    }
                }
                else if (rowInEntry == 0)
                {

                    record[currentIndex] = ValueAsCsvFriendlyString(value, value.GetType());
                }
            }
        }

        

        private static void GetSerializableFieldsAndTypes(Type itemType, out List<FieldInfo> fieldList, out List<PropertyInfo> propertyList)
        {
#if !WINDOWS_8 && !UWP
            FieldInfo[] fields = itemType.GetFields();
            PropertyInfo[] properties = itemType.GetProperties();

            fieldList = new List<FieldInfo>();
            propertyList = new List<PropertyInfo>();
            foreach (FieldInfo field in fields.Where(field => field.IsStatic == false && field.IsPublic))
            {
                object[] attributes = field.GetCustomAttributes(typeof(XmlIgnoreAttribute), true);

                if (attributes == null || attributes.Length == 0)
                {
                    fieldList.Add(field);
                }
            }

            foreach (PropertyInfo property in properties)
            {
                var getMethod = property.GetGetMethod(false);
                var setMethod = property.GetSetMethod(false);

                if (getMethod != null && setMethod != null && getMethod.IsPublic && setMethod.IsPublic && getMethod.IsStatic == false && setMethod.IsStatic == false)
                {
                    object[] attributes = property.GetCustomAttributes(typeof(XmlIgnoreAttribute), true);

                    if (attributes == null || attributes.Length == 0)
                    {

                        propertyList.Add(property);
                    }
                }
            }
#else
            throw new NotImplementedException();       
#endif
        }

        private static string ValueAsCsvFriendlyString(object value, Type type)
        {
            
#if DEBUG && WINDOWS

            // It is possible
            // that the user is
            // trying to serialize
            // an object which has infinite
            // recursion through its properties.
            // In this case, we want to make sure we
            // don't have a StackOverflowException.  We'll
            // do this by counting the number of items in the
            // stack and throwing our own exception after a big
            // number.
            // Update - this is prohibitively slow:
            //StackTrace st = new StackTrace(new StackFrame(true));
            //if (st.FrameCount > 500)
            //{
            //    // I arbitrarily picked 500, but it seems like a good number;
            //    throw new Exception("To prevent infinite recursion, ValueAsCsvFriendlyString only supports a StackFrame of 500.  This number has been exceeded");
            //}
            
#endif

			////////////Early out////////////////////
			if(value == null)
			{
				return null;
			}
			////////End early out//////////////////////

            string returnValue = PropertyValuePair.ConvertTypeToString(value);

            if (string.IsNullOrEmpty(returnValue) && type != typeof(string))
            {
                returnValue = "new " + TypeAsFriendlyString(type) + "(" ;


                List<FieldInfo> fields;
                List<PropertyInfo> properties;

                GetSerializableFieldsAndTypes(type, out fields, out properties);

                bool isFirst = true;

                foreach (var field in fields)
                {
                    string rightSide = ValueAsCsvFriendlyString(field.GetValue(value), field.FieldType);

                    if (!string.IsNullOrEmpty(rightSide))
                    {

                        string fieldSet = field.Name + " = " + rightSide;

                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            fieldSet = ", " + fieldSet;
                        }

                        returnValue += fieldSet;
                    }
                }

                foreach (var property in properties)
                {
                    string rightSide = null;
                    try
                    {
                        rightSide = ValueAsCsvFriendlyString(property.GetValue(value, null), property.PropertyType);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error trying to get value for property " + property.Name + "\n" + e.ToString());
                    }
                    if (!string.IsNullOrEmpty(rightSide))
                    {
                        string propertySet = property.Name + " = " + rightSide;

                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            propertySet = ", " + propertySet;

                        }

                        returnValue += propertySet;
                    }
                }

                returnValue += ")";
            }

            return returnValue;
        }

        private static string TypeAsFriendlyString(Type type)
        {
            if (type.Name == "List`1")
            {
                return string.Format("List<{0}>", TypeAsFriendlyString(type.GetGenericArguments()[0]));
            }
            else
            {
                return type.ToString().Replace("+", ".");
            }
        }

        static bool IsIgnored()
        {
            return false;
        }


        [XmlIgnore]
        public string Name
        {
            get;
            set;
        }

        public string GenerateCsvString()
        {
            return GenerateCsvString(',');
        }

        public string GenerateCsvString(char delimiter)
        {
            var result = new StringBuilder();

            // Load the headers
            bool firstItem = true;

            // I think it's okay to have null Headers
            if (Headers != null)
            {
                foreach (var header in Headers)
                {
                    if (!firstItem)
                        result.Append(delimiter);

                    if (header == null || header.OriginalText == null)
                    {
                        result.Append(string.Empty);
                    }
                    else if (header.OriginalText.Contains(delimiter.ToString()))
                    {
                        result.Append("\"");
                        result.Append(header.OriginalText);
                        result.Append("\"");
                    }
                    else
                    {
                        result.Append(header.OriginalText);
                    }

                    firstItem = false;
                }
            }

            // Load the fields
            foreach (var fields in Records)
            {
                firstItem = true;
                result.Append(Environment.NewLine);

                foreach (var field in fields)
                {
                    if (!firstItem)
                        result.Append(delimiter);

                    // Convert a null field to an empty string
                    if (field == null)
                    {
                        result.Append("");
                    }

                    // If the field has quotes, the delimeter, or a newline character 
                    //   then wrap the whole value in quotes
                    else if (field.Contains(delimiter.ToString()) || field.Contains("\"") || field.Contains(Environment.NewLine))
                    {
                        result.Append("\"");
                        result.Append(field.Replace("\"", "\"\""));
                        result.Append("\"");
                    }
                    else
                    {
                        result.Append(field.Replace("\"", "\"\""));
                    }

                    firstItem = false;
                }
            }

            return result.ToString();
        }

        public void CreateObjectList(Type typeOfElement, IList listToPopulate)
        {
            CreateObjectList(typeOfElement, listToPopulate, CsvFileManager.ContentManagerName);
        }

        public void CreateObjectList(Type typeOfElement, IList listToPopulate, string contentManagerName)
        {
            #region If primitive or string

#if WINDOWS_8 || UWP
            bool isPrimitive = typeOfElement.IsPrimitive();
#else
            bool isPrimitive = typeOfElement.IsPrimitive ;
#endif
            if (isPrimitive || typeOfElement == typeof(string))
            {
                if (typeOfElement == typeof(string))
                {
                    listToPopulate.Add(this.Headers[0].OriginalText);

                    for (int i = 0; i < this.Records.Count; i++)
                    {
                        listToPopulate.Add(this.Records[i][0]);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
#endregion

            else if (typeOfElement == typeof(List<string>))
            {
                for (int i = 0; i < this.Records.Count; i++)
                {
                    string[] record = Records[i];
                    List<string> row = new List<string>();
                    listToPopulate.Add(row);
                    row.AddRange(record);
                }

            }
            else if (typeOfElement == typeof(string[]))
            {
                for (int i = 0; i < this.Records.Count; i++)
                {
                    listToPopulate.Add(Records[i]);
                }

            }

            #region Not primitive or string (class/struct)
            else
            {
                CreateNonPrimitiveList(typeOfElement, listToPopulate, contentManagerName);
            }
#endregion


        }

        [Obsolete("Use FillObjectDictionary since that more accurately describes what this method is doing")]
        public void CreateObjectDictionary<KeyType, ValueType>(Dictionary<KeyType, ValueType> dictionaryToPopulate, string contentManagerName)
        {
            FillObjectDictionary(dictionaryToPopulate, contentManagerName, DuplicateDictionaryEntryBehavior.ThrowException);
        }

        public void FillObjectDictionary<KeyType, ValueType>(Dictionary<KeyType, ValueType> dictionaryToPopulate, 
            // Tools may not have access to FRBServices, so just hardcode the string
            string contentManagerName = "Global",
            DuplicateDictionaryEntryBehavior duplicateDictionaryEntryBehavior = DuplicateDictionaryEntryBehavior.ThrowException)
        {
            Type typeOfElement = typeof(ValueType);




#if DEBUG
    #if UWP
            bool isPrimitive = typeOfElement.IsPrimitive();
    #else
            bool isPrimitive = typeOfElement.IsPrimitive;
    #endif

            if (isPrimitive || typeOfElement == typeof(string))
            {
                throw new InvalidOperationException("Can't create dictionaries of primitives or strings because they don't have a key");
            }
#endif

            MemberTypeIndexPair[] memberTypeIndexPairs;
            IEnumerable<PropertyInfo> propertyInfosEnumerable;
            IEnumerable<FieldInfo> fieldInfosEnumerable;
            GetReflectionInformation(typeOfElement, out memberTypeIndexPairs, out propertyInfosEnumerable, out fieldInfosEnumerable);

            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(propertyInfosEnumerable);
            List<FieldInfo> fieldInfos = new List<FieldInfo>(fieldInfosEnumerable);

            #region Get the required header which we'll use for the key

            CsvHeader csvHeaderForKey = CsvHeader.Empty;

            int headerIndex = 0;

            foreach (CsvHeader header in Headers)
            {
                if (header.IsRequired)
                {
                    csvHeaderForKey = header;
                    break;
                }
                headerIndex++;
            }

            if (csvHeaderForKey == CsvHeader.Empty)
            {
                throw new InvalidOperationException("Could not find a property to use as the key.  One of the columns needs to be marked as required. " + 
                    "For example \"Name (string, required)\"");
            }

#endregion

            int numberOfColumns = Headers.Length;

            object lastElement = null;
            bool wasRequiredMissing = false;


            for (int row = 0; row < Records.Count; row++)
            {
                object newElement;
                bool newElementFailed;

                wasRequiredMissing = TryCreateNewObjectFromRow(
                    typeOfElement,

                    contentManagerName,
                    memberTypeIndexPairs,
                    propertyInfos,
                    fieldInfos,
                    numberOfColumns,
                    lastElement,
                    wasRequiredMissing,
                    row,
                    out newElement,
                    out newElementFailed);

                if (!newElementFailed && !wasRequiredMissing)
                {
                    KeyType keyToUse = GetKeyToUse<KeyType, ValueType>(typeOfElement, csvHeaderForKey, headerIndex, newElement);

                    if (dictionaryToPopulate.ContainsKey(keyToUse))
                    {
                        switch(duplicateDictionaryEntryBehavior)
                        {
                            case DuplicateDictionaryEntryBehavior.ThrowException:
                                throw new InvalidOperationException("The key " + keyToUse +
                                    " is already part of the dictionary.");
                                break;
                            case DuplicateDictionaryEntryBehavior.Replace:
                                dictionaryToPopulate[keyToUse] = (ValueType)newElement;
                                break;
                            case DuplicateDictionaryEntryBehavior.PreserveFirst:
                                // do nothing
                                break;

                        }
                    }
                    else
                    {
                        dictionaryToPopulate.Add(keyToUse, (ValueType)newElement);
                    }

                    lastElement = newElement;
                }
            }

                

        }

        public void UpdateValues<KeyType, ValueType>(Dictionary<KeyType, ValueType> dictionaryToUpdate, string contentManagerName)
        {
            var newDictionary = new Dictionary<KeyType, ValueType>(dictionaryToUpdate.Count);

            FillObjectDictionary(newDictionary, contentManagerName);

            var type = typeof(ValueType);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            fields = fields.Where(fi => fi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null).ToArray();
            properties = properties.Where(pi => pi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null).ToArray();

            foreach(var newKvp in newDictionary)
            {
                var key = newKvp.Key;

                var newItem = newKvp.Value;

                ValueType oldItem = default;
                if(dictionaryToUpdate.ContainsKey(key))
                {
                    oldItem = dictionaryToUpdate[key];
                }

                if(oldItem == null)
                {
                    dictionaryToUpdate[key] = newItem;
                }
                else
                {
                    foreach(var field in fields)
                    {
                        var valueOnNew = field.GetValue(newItem);
                        field.SetValue(oldItem, valueOnNew);
                    }

                    foreach(var property in properties)
                    {
                        var valueOnNew = property.GetValue(newItem, null);
                        property.SetValue(oldItem, valueOnNew, null);
                    }
                }
            }
        }

        private static KeyType GetKeyToUse<KeyType, ValueType>(Type typeOfElement, CsvHeader csvHeaderForKey, int headerIndex, object newElement)
        {
            KeyType keyToUse = default(KeyType);

            if (typeOfElement == typeof(string[]))
            {
                keyToUse = (KeyType)(((string[])newElement)[headerIndex] as object);
            }
            else
            {

                if (csvHeaderForKey.MemberTypes == MemberTypes.Property)
                {
                    keyToUse = LateBinder<ValueType>.Instance.GetProperty<KeyType>((ValueType)newElement, csvHeaderForKey.Name);
                }
                else
                {
                    keyToUse = LateBinder<ValueType>.Instance.GetField<KeyType>((ValueType)newElement, csvHeaderForKey.Name);
                }
            }

            return keyToUse;
        }

        private void CreateNonPrimitiveList(Type typeOfElement, IList listToPopulate, string contentManagerName)
        {
            MemberTypeIndexPair[] memberTypeIndexPairs;
            IEnumerable<PropertyInfo> propertyInfosEnumerable;
            IEnumerable<FieldInfo> fieldInfosEnumerable;
            GetReflectionInformation(typeOfElement, out memberTypeIndexPairs, out propertyInfosEnumerable, out fieldInfosEnumerable);

            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(propertyInfosEnumerable);
            List<FieldInfo> fieldInfos = new List<FieldInfo>(fieldInfosEnumerable);

            int numberOfColumns = Headers.Length;

            object lastElement = null;
            bool wasRequiredMissing = false;


            for (int row = 0; row < Records.Count; row++)
            {
                object newElement;
                bool newElementFailed;

                wasRequiredMissing = TryCreateNewObjectFromRow(
                    typeOfElement, 
                    contentManagerName, 
                    memberTypeIndexPairs, 
                    propertyInfos, 
                    fieldInfos, 
                    numberOfColumns, 
                    lastElement, 
                    wasRequiredMissing, 
                    row, 
                    out newElement, 
                    out newElementFailed);

                if (!newElementFailed && !wasRequiredMissing)
                {
                    listToPopulate.Add(newElement);

                    lastElement = newElement;
                }
            }

        }

        private bool TryCreateNewObjectFromRow(Type typeOfElement, string contentManagerName, MemberTypeIndexPair[] memberTypeIndexPairs, 
            List<PropertyInfo> propertyInfos, List<FieldInfo> fieldInfos, int numberOfColumns, object lastElement, bool wasRequiredMissing, int row, out object newElement, out bool newElementFailed)
        {
            wasRequiredMissing = false;
            newElementFailed = false;



#region Special-case handle string[].  We use these for localization
            if (typeOfElement == typeof(string[]))
            {
                int requiredColumn = -1;

                for (int i = 0; i < Headers.Length; i++)
                {
                    if (Headers[i].IsRequired)
                    {
                        requiredColumn = i;
                        break;
                    }
                }

                //bool isRequired =;

                if (requiredColumn != -1 && string.IsNullOrEmpty(Records[row][requiredColumn]))
                {
                    wasRequiredMissing = true;
                    newElement = null;
                }
                else
                {
                    string[] returnObject = new string[numberOfColumns];

                    for (int column = 0; column < numberOfColumns; column++)
                    {
                        returnObject[column] = Records[row][column];
                    }

                    newElement = returnObject;
                }
            }

#endregion

            else
            {

                bool isComment = Records[row] != null && Records[row][0].StartsWith("//");

                if(isComment)
                {
                    wasRequiredMissing = true;
                    newElementFailed = true;
                    newElement = null;
                }
                else
                {

                    newElement = Activator.CreateInstance(typeOfElement);
                    wasRequiredMissing = AsssignValuesOnElement(
                        contentManagerName, 
                        memberTypeIndexPairs, 
                        propertyInfos, 
                        fieldInfos, 
                        numberOfColumns, 
                        lastElement, 
                        wasRequiredMissing, 
                        row, 
                        newElement);
                }
            }
            return wasRequiredMissing;
        }

        private bool AsssignValuesOnElement(string contentManagerName, MemberTypeIndexPair[] memberTypeIndexPairs, List<PropertyInfo> propertyInfos, List<FieldInfo> fieldInfos, int numberOfColumns, object lastElement, bool wasRequiredMissing, int row, object newElement)
        {
            for (int column = 0; column < numberOfColumns; column++)
            {
                if (memberTypeIndexPairs[column].Index != -1)
                {

                    object objectToSetValueOn = newElement;
                    if (wasRequiredMissing)
                    {
                        objectToSetValueOn = lastElement;
                    }
                    int columnIndex = memberTypeIndexPairs[column].Index;

                    bool isRequired = Headers[column].IsRequired;

                    if (isRequired && string.IsNullOrEmpty(Records[row][column]))
                    {
                        wasRequiredMissing = true;
                        continue;
                    }

#region If the member is a Property, so set the value obtained from converting the string.
                    if (memberTypeIndexPairs[column].MemberType == MemberTypes.Property)
                    {
                        PropertyInfo propertyInfo = propertyInfos[memberTypeIndexPairs[column].Index];

                        var propertyType = propertyInfo.PropertyType;

                        bool isList = propertyInfo.PropertyType.Name == "List`1";
                        if (isList)
                        {
                            propertyType = propertyType.GetGenericArguments()[0];
                        }

                        string valueAtRowColumn = Records[row][column];

                        object valueToSet = null;

                        string cellValue = Records[row][column];
                        if (string.IsNullOrEmpty(cellValue))
                        {
                            valueToSet = null;
                        }
                        else
                        {
                            try
                            {


                                valueToSet = PropertyValuePair.ConvertStringToType(
                                Records[row][column],
                                propertyType,
                                contentManagerName);
                            }
                            catch (ArgumentException e)
                            {
                                throw new Exception("Could not set variable " + propertyInfo.Name + " to " + cellValue + "\n\n" + e, e);
                            }
                            catch (FormatException)
                            {
                                throw new Exception("Error parsing the value " + cellValue + " for the property " + propertyInfo.Name);
                            }
                        }

                        if (isList)
                        {
                            // todo - need to support adding to property lists
                            object objectToCallOn = propertyInfo.GetValue(objectToSetValueOn, null);
                            if (objectToCallOn == null)
                            {
                                objectToCallOn = Activator.CreateInstance(propertyInfo.PropertyType);

                                propertyInfo.SetValue(objectToSetValueOn, objectToCallOn, null);
                            }

                            if (valueToSet != null)
                            {
                                MethodInfo methodInfo = propertyInfo.PropertyType.GetMethod("Add");

                                methodInfo.Invoke(objectToCallOn, new object[] { valueToSet });
                            }
                        }
                        else if (!wasRequiredMissing)
                        {
                            propertyInfo.SetValue(
                                objectToSetValueOn,
                                valueToSet,
                                null);
                        }

                    }
#endregion

#region Else, it's a Field, so set the value obtained from converting the string.
                    else if (memberTypeIndexPairs[column].MemberType == MemberTypes.Field)
                    {
                        //try
                        {
                            FieldInfo fieldInfo;
                            bool isList;
                            object valueToSet;
                            GetFieldValueToSet(contentManagerName, fieldInfos, row, column, columnIndex, out fieldInfo, out isList, out valueToSet);

                            if (isList)
                            {
                                // Check to see if the list is null.
                                // If so, create it. We want to make the
                                // list even if we're not going to add anything
                                // to it.  Maybe we'll change this in the future
                                // to improve memory usage?
                                object objectToCallOn = fieldInfo.GetValue(objectToSetValueOn);
                                if (objectToCallOn == null)
                                {
                                    objectToCallOn = Activator.CreateInstance(fieldInfo.FieldType);

                                    fieldInfo.SetValue(objectToSetValueOn, objectToCallOn);
                                }

                                if (valueToSet != null)
                                {
                                    MethodInfo methodInfo = fieldInfo.FieldType.GetMethod("Add");

                                    methodInfo.Invoke(objectToCallOn, new object[] { valueToSet });
                                }
                            }
                            else if (!wasRequiredMissing)
                            {
                                fieldInfo.SetValue(objectToSetValueOn, valueToSet);
                            }
                        }
                        // May 5, 2011:
                        // This code used
                        // to try/catch and
                        // just throw away failed
                        // attempts to instantiate
                        // a new object.  This caused
                        // debugging problems.  I think
                        // we should be stricter with this
                        // and let the exception occur so that
                        // developers can fix any problems related
                        // to CSV deseiralization.  Silent bugs could
                        // be difficult/annoying to track down.
                        //catch
                        //{
                        //    // don't worry, just skip for now.  May want to log errors in the future if this
                        //    // throw-away of exceptions causes difficult debugging.
                        //    newElementFailed = true;
                        //    break;
                        //}
                    }
#endregion
                }
            }

            return wasRequiredMissing;
        }

        private void GetFieldValueToSet(string contentManagerName, List<FieldInfo> fieldInfos, int row, int column, int columnIndex, out FieldInfo fieldInfo, out bool isList, out object valueToSet)
        {
            fieldInfo = fieldInfos[columnIndex];

            Type type = fieldInfo.FieldType;
            string name = fieldInfo.Name;


            isList = type.Name == "List`1";

            Type typeOfObjectInCell = type;

            if (isList)
            {
                typeOfObjectInCell = type.GetGenericArguments()[0];
            }

            string cellValue = Records[row][column];
            if (string.IsNullOrEmpty(cellValue))
            {
                valueToSet = null;
            }
            else
            {
                try
                {
                    valueToSet = PropertyValuePair.ConvertStringToType(
                    Records[row][column],
                    typeOfObjectInCell,
                    contentManagerName);
                }
                catch (ArgumentException e)
                {
                    throw new Exception($"Could not set variable {name} to {cellValue} at Row {row} Column {column}\n\n" + e, e);
                }
                catch (FormatException)
                {
                    throw new Exception($"Error parsing the value {cellValue} for the property  at Row {row} Column {column}" + name);
                }
            }
        }

        private void GetReflectionInformation(Type typeOfElement, out MemberTypeIndexPair[] memberTypeIndexPairs, out IEnumerable<PropertyInfo> propertyInfos, out IEnumerable<FieldInfo> fieldInfos)
        {
            memberTypeIndexPairs = new MemberTypeIndexPair[Headers.Length];

            propertyInfos = typeOfElement.GetProperties();
            fieldInfos = typeOfElement.GetFields();

            RemoveHeaderWhitespaceAndDetermineIfRequired();


            BuildMemberTypeIndexInformation(memberTypeIndexPairs, propertyInfos, fieldInfos);
        }

        public void RemoveHeaderWhitespaceAndDetermineIfRequired()
        {
#region Remove whitespace and commas, identify if the types are required.
            // The headers may be include spaces to be more "human readable".
            // Of course members can't have spaces.  Therefore "Max HP" is acceptable
            // as a header, but the member might be MaxHP.  Therefore, we need to remove
            // whitespace.
            for (int i = 0; i < Headers.Length; i++)
            {
                string text = Headers[i].OriginalText;
                bool isRequired = IsHeaderRequired(text);

                string nameWithoutParentheses = CsvHeader.GetNameWithoutParentheses(text);



                Headers[i].Name = nameWithoutParentheses;
                Headers[i].IsRequired = isRequired;
            }
#endregion
        }



        private static bool IsHeaderRequired(string header)
        {
            bool isRequired = false;
            int openingIndex = header.IndexOf('(');
            if (openingIndex == -1)
            {
                isRequired = false;
            }
            else
            {
                string qualifiers = header.Substring(openingIndex + 1, header.Length - (openingIndex + 1) - 1);

                if (qualifiers.Contains(","))
                {
                    string[] brokenUp = qualifiers.Split(',');

                    foreach (string s in brokenUp)
                    {
                        if (s.Trim() == "required")
                        {
                            isRequired = true;
                        }
                    }
                }
                else
                {
                    if (qualifiers == "required")
                    {
                        isRequired = true;
                    }
                }
            }
            return isRequired;

        }

        private void BuildMemberTypeIndexInformation(MemberTypeIndexPair[] memberTypeIndexPairs, IEnumerable propertyInfos, IEnumerable fieldInfos)
        {
            for (int i = 0; i < Headers.Length; i++)
            {
                memberTypeIndexPairs[i].Index = -1;

                #region See if the header at index i is a field
                int j = 0;
                foreach(FieldInfo fieldInfo in fieldInfos)
                {
                    if (fieldInfo.Name == Headers[i].Name)
                    {
                        Headers[i].MemberTypes = MemberTypes.Field;

                        memberTypeIndexPairs[i] = new MemberTypeIndexPair();
                        memberTypeIndexPairs[i].Index = j;
                        memberTypeIndexPairs[i].MemberType = MemberTypes.Field;
                        break;
                    }
                    j++;
                }

                if (memberTypeIndexPairs[i].Index != -1 && memberTypeIndexPairs[i].MemberType == MemberTypes.Field)
                {
                    continue;
                }
#endregion

                #region If we got this far, then it's not a field, so check if it's a property

                j = 0;
                foreach(PropertyInfo propertyInfo in propertyInfos)
                {
                    if (propertyInfo.Name == Headers[i].Name)
                    {
                        Headers[i].MemberTypes = MemberTypes.Property;

                        memberTypeIndexPairs[i] = new MemberTypeIndexPair();
                        memberTypeIndexPairs[i].Index = j;
                        memberTypeIndexPairs[i].MemberType = MemberTypes.Property;
                        break;
                    }
                    j++;
                }

                if (memberTypeIndexPairs[i].Index != -1 && memberTypeIndexPairs[i].MemberType == MemberTypes.Property)
                {
                    continue;
                }
#endregion

                // Is this needed:
                memberTypeIndexPairs[i].Index = -1;
            }
        }

        public int GetRequiredIndex()
        {
            int requiredIndex = -1;

            for (int i = 0; i < Headers.Length; i++)
            {
                if (Headers[i].IsRequired)
                {
                    requiredIndex = i;
                    break;
                }
            }
            return requiredIndex;
        }
    }
}
