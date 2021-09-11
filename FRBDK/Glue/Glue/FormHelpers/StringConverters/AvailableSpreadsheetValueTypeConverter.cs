using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.IO.Csv;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.GuiDisplay
{
    public class AvailableSpreadsheetValueTypeConverter : TypeConverter
    {

        #region Fields

        List<FilePath> filePaths = new List<FilePath>();

        #endregion

        #region Properties

        public string ContentDirectory
        {
            get;
            set;
        }

        public bool ShouldAppendFileName
        {
            get;
            set;
        }

        #endregion

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public static List<string> GetAvailableValues(FilePath absoluteFile, bool shouldAppendFileName)
        {
            return GetAvailableValues(new List<FilePath>() { absoluteFile }, shouldAppendFileName);
        }

        public static List<string> GetAvailableValues(IEnumerable<FilePath> absoluteFiles, bool shouldAppendFileName)
        {
            List<string> stringsToReturn = new List<string>();
            stringsToReturn.Clear();
            stringsToReturn.Add("<NULL>");

            AddAvailableValuesFromFileToList(absoluteFiles, stringsToReturn, shouldAppendFileName);

            stringsToReturn.Sort();

            return stringsToReturn;
        }

        private static void AddAvailableValuesFromFileToList(IEnumerable<FilePath> absoluteFiles, List<string> stringsToReturn, bool shouldAppendFileName)
        {
            foreach(var file in absoluteFiles)
            {
                if (file.Exists())
                {
                    try
                    {
                        string toAppend = "";
                        if (shouldAppendFileName)
                        {
                            // Eventually we want to make this relative to the container, not just the folename
                            toAppend = " in " + file.NoPath;
                        }

                        RuntimeCsvRepresentation rcr =
                            CsvFileManager.CsvDeserializeToRuntime(file.FullPath);

                        rcr.RemoveHeaderWhitespaceAndDetermineIfRequired();

                        int requiredIndex = -1;

                        for (int i = 0; i < rcr.Headers.Length; i++)
                        {
                            if (rcr.Headers[i].IsRequired)
                            {
                                requiredIndex = i;
                                break;
                            }
                        }

                        if (requiredIndex != -1)
                        {
                            foreach (string[] record in rcr.Records)
                            {
                                string possibleValue = record[requiredIndex];
                                if (!string.IsNullOrEmpty(possibleValue))
                                {
                                    if (shouldAppendFileName)
                                    {
                                        stringsToReturn.Add(possibleValue + toAppend);
                                    }
                                    else
                                    {
                                        stringsToReturn.Add(possibleValue);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // do nothing for now...
                    }
                }

            }


        }


        public AvailableSpreadsheetValueTypeConverter(FilePath filePath)
        {
            filePaths.Add(filePath);
        }

        public AvailableSpreadsheetValueTypeConverter(IEnumerable<FilePath> filePaths)
        {
            this.filePaths.AddRange(filePaths);
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection svc = new StandardValuesCollection(GetAvailableValues(filePaths, ShouldAppendFileName));

            return svc;
        }
    }
}
