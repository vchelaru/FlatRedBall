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

        IElement mContainer;
        string mAbsoluteFile;
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

        public static List<string> GetAvailableValues(string absoluteFile, bool shouldAppendFileName)
        {
            List<string> stringsToReturn = new List<string>();
            stringsToReturn.Clear();
            stringsToReturn.Add("<NULL>");


            if (FileManager.IsRelative(absoluteFile))
            {
                throw new ArgumentException("The argument absoluteFile must be absolute.  It is passed as " + absoluteFile);
            }

            //List<string> filesToSearchIn = GetAllFilesUsingClass(absoluteFile);

            //foreach (string file in filesToSearchIn)
            {
//                AddAvailableValuesFromFileToList(absoluteFile, stringsToReturn);
            }

            AddAvailableValuesFromFileToList(absoluteFile, stringsToReturn, shouldAppendFileName);


            return stringsToReturn;
        }

        private static void AddAvailableValuesFromFileToList(string absoluteFile, List<string> stringsToReturn, bool shouldAppendFileName)
        {
            if (System.IO.File.Exists(absoluteFile))
            {
                try
                {
                    string toAppend = "";
                    if (shouldAppendFileName)
                    {
                        // Eventually we want to make this relative to the container, not just the folename
                        toAppend = " in " + FileManager.RemovePath(absoluteFile);
                    }

                    RuntimeCsvRepresentation rcr =
                        CsvFileManager.CsvDeserializeToRuntime(absoluteFile);

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


        public AvailableSpreadsheetValueTypeConverter(FilePath filePath, IElement container)
        {
            this.mContainer = container;
            this.mAbsoluteFile = filePath.FullPath;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection svc = new StandardValuesCollection(GetAvailableValues(mAbsoluteFile, ShouldAppendFileName));

            return svc;
        }
    }
}
