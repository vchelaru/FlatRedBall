using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Csv;
using System.Collections.ObjectModel;
using FlatRedBall.Utilities;

namespace FlatRedBall.Localization
{
    #region LocalizationEntry Class

    public class LocalizationEntry
    {
        public string Key { get; set; }

        /// <summary>
        /// Each row represents one page of dialog. Games which do not use multiple
        /// pages of dialog per string ID will always access Rows[0][LanguageIndex]. Games which
        /// use multiple pages will access Rows[PageIndex][LanguageIndex]
        /// </summary>
        public List<string[]> Rows { get; set; } = new List<string[]>();

        //public string[] Values { get; set; }
        public override string ToString()
        {
            if (Rows.Count == 0) return "<no text>";
            else
            {
                return Rows[0].FirstOrDefault();
            }
        }
    }

    #endregion

    public static class LocalizationManager
    {
        #region Fields/Properties

        public static ReadOnlyCollection<string> Languages
        {
            get;
            set;
        }

        static Dictionary<string, LocalizationEntry> mStringDatabase = new Dictionary<string, LocalizationEntry>();

        public static bool HasDatabase
        {
            get;
            private set;
        }

        public static int CurrentLanguage
        {
            get;
            set;
        }

        static List<string> databaseFileNames;
        static ReadOnlyCollection<string> databaseFileNamesReadOnly;
        public static ReadOnlyCollection<string> DatabaseFileNames
        {
            get => databaseFileNamesReadOnly;
        }

        #endregion

        static LocalizationManager()
        {
            Languages = new ReadOnlyCollection<string>(new List<string>());
            databaseFileNames = new List<string>();
            databaseFileNamesReadOnly = new ReadOnlyCollection<string>(databaseFileNames);
        }

        public static void AddDatabase(string fileName, char delimiter)
        {
            databaseFileNames.Add(fileName);
            RuntimeCsvRepresentation rcr;

            char oldDelimiter = CsvFileManager.Delimiter;
            CsvFileManager.Delimiter = delimiter;

            // We used to deserialize to a dictionary, but there could be rows that don't have a primary key for long text, so we're going to support that:
            //CsvFileManager.CsvDeserializeDictionary<string, string[]>(fileName, entryDictionary, out rcr);
            rcr = CsvFileManager.CsvDeserializeToRuntime(fileName);
            CsvFileManager.Delimiter = oldDelimiter;


            List<string> headerList = new List<string>();

            foreach (CsvHeader header in rcr.Headers)
            {
                headerList.Add(header.Name);
            }

            Languages = new ReadOnlyCollection<string>(headerList);

            mStringDatabase.Clear();

            LocalizationEntry lastEntry = new LocalizationEntry();

            foreach (var row in rcr.Records)
            {
                // assume the first row is the key:
                var key = row[0];

                var values = row.ToArray();

                LocalizationEntry localizationEntry;
                if(!string.IsNullOrEmpty(key))
                {
                    localizationEntry = new LocalizationEntry();
                    localizationEntry.Key = key;
                    mStringDatabase[key] = localizationEntry;
                }
                else
                {
                    localizationEntry = lastEntry;
                }
                localizationEntry.Rows.Add(values);
                lastEntry = localizationEntry;
            }


            HasDatabase = true;
        }

        private static void CreateStringDatabase(Dictionary<string, string[]> entryDictionary)
        {
            mStringDatabase = new Dictionary<string, LocalizationEntry>();
            foreach (var kvp in entryDictionary)
            {
                var entry = new LocalizationEntry
                {
                    Key = kvp.Key
                };
                entry.Rows.Add(kvp.Value);
                mStringDatabase[kvp.Key] = entry;
            }
        }

        public static void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList)
        {
            Languages = new ReadOnlyCollection<string>(headerList);
            CreateStringDatabase( entryDictionary);
            HasDatabase = true;
        }

        public static void ClearDatabase()
        {
            databaseFileNames.Clear();

            mStringDatabase.Clear();
            HasDatabase = false;
            Languages = new ReadOnlyCollection<string>(new List<string>());
        }

        private static bool ShouldExcludeFromTranslation(string stringID)
        {
            if (string.IsNullOrEmpty(stringID))
            {
                return true;
            }
            else if (StringFunctions.IsNumber(stringID))
            {
                return true;
            }
            else if (IsPercentage(stringID))
            {
                return true;
            }
            else if (IsTime(stringID))
            {
                return true;
            }
            else if (stringID == "!" || stringID == "+" || stringID == "-" ||
                stringID == "*" || stringID == "/" || stringID == "#" || stringID == ":")
            {
                return true;
            }
            return false;
        }

        private static bool IsTime(string stringID)
        {
            for (int i = 0; i < stringID.Length; i++)
            {
                char cAti = stringID[i];
                if (char.IsDigit(cAti) == false && (cAti == ':') == false && (cAti == '.') == false)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPercentage(string stringID)
        {
            return stringID.CountOf('%') == 1 && stringID.Length > 1 && stringID.EndsWith("%") && StringFunctions.IsNumber(stringID.Substring(0, stringID.Length - 1));

        }

        public static string Translate(string stringID)
        {
            return TranslateForLanguage(stringID, CurrentLanguage);
        }

        public static string Translate(string stringID, params object[] args)
        {
            string translated =
                string.Format(Translate(stringID),
                args);

            return translated;
        }

        /// <summary>
        /// Returns the translated string for the argument language. If stringId is null, "NULL STRING" is returned.
        /// </summary>
        /// <param name="stringID">The stringId to translate.</param>
        /// <param name="language">The language index. Typically 0 is the string IDs, and languages begin with index 1</param>
        /// <returns>The translated string.</returns>
        public static string TranslateForLanguage(string stringID, int language)
        {

            if (stringID == null)
            {
                return "NULL STRING";
            }
            else if (mStringDatabase.ContainsKey(stringID))
            {
                var entry = mStringDatabase[stringID];
                if (entry.Rows[0].Count() > language)
                {
                    return mStringDatabase[stringID].Rows[0][language];
                }
                else
                {
                    return $"Error accessing string {stringID} for language {language} because it is greater than the max language index of {entry.Rows[0].Count()-1}";
                }
            }
            else if (ShouldExcludeFromTranslation(stringID))
            {
                return stringID;
            }
            else
            {
                return stringID + " - UNTRANSLATED";
            }
        }

        public static string[] TranslateMultiple(string stringID, int? forcedLanguage = null)
        {
            if (stringID == null)
            {
                return new string[] { "NULL STRING" };
            }
            else if (mStringDatabase.ContainsKey(stringID))
            {
                var language = forcedLanguage ?? CurrentLanguage;
                var entry = mStringDatabase[stringID];
                var toReturn = entry.Rows.Select(item => item[language]);

                return toReturn.ToArray();
            }
            else if (ShouldExcludeFromTranslation(stringID))
            {
                return new string[] { stringID };
            }
            else
            {
                return new string[] { stringID + " - UNTRANSLATED" };
            }
        }

		/// <summary>
		/// Grabs a dictionary with a copy of all the localization database keys and the current language values. 
		/// </summary>
		/// <returns></returns>
		public static Dictionary<string, string> GetLanguageDatabaseCopy()
		{
			var dict = new Dictionary<string, string>();
			foreach (var key in mStringDatabase.Keys)
			{
				dict.Add(key, mStringDatabase[key].Rows[0][CurrentLanguage]);
			}
			return dict;
		}
	}
}
