using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue;

using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Parsing;

namespace GlueViewOfficialPlugins.Scripting
{
    public static class CsvParser
    {
        public static object GetValueFromCsv(string rowValue, string variableName, string csvType, IElement element, ElementRuntime elementRuntime)
        {
            // We should check the csvType (which might be a class already).
            // If we don't get anything back then we should try stripping off
            // the extension and path
            object foundValue = null;


            ReferencedFileSave rfs = ObjectFinder.Self.GetFirstCsvUsingClass(csvType, element);

            if (rfs == null && FileManager.GetExtension(csvType) == "csv")
            {
                string strippedType = FileManager.RemovePath(FileManager.RemoveExtension(csvType));

                rfs = ObjectFinder.Self.GetFirstCsvUsingClass(strippedType, element);
            }

            if (rfs != null)
            {
                RuntimeCsvRepresentation rcr = null;

                // This could be global content or it could be a RFS in the element
                if (element.ContainsRecursively(rfs))
                {
                    var loadedFile = elementRuntime.LoadReferencedFileSave(rfs, true, element);
                    rcr = loadedFile.RuntimeObject as RuntimeCsvRepresentation;
                }
                else
                {
                    var loadedFile =
                        GluxManager.GlobalContentFilesRuntime.LoadReferencedFileSave(rfs, true, element);
                    // Load this thing from global content
                    rcr = loadedFile.RuntimeObject as
                        RuntimeCsvRepresentation;
                }
                if (rcr.GetRequiredIndex() == -1)
                {
                    rcr.RemoveHeaderWhitespaceAndDetermineIfRequired();
                }
                int indexOfColumn = 0;
                for (int i = 0; i < rcr.Headers.Length; i++)
                {
                    if (rcr.Headers[i].Name == variableName)
                    {
                        indexOfColumn = i;
                        break;
                    }
                }

                if (rcr != null)
                {
                    // Right now I'm writing this to only support dictionaries.
                    int indexOfRequired = rcr.GetRequiredIndex();
                    string[] matchingRecord = null;

                    foreach (string[] record in rcr.Records)
                    {
                        try
                        {
                            if (record.Length > indexOfRequired &&
                                record[indexOfRequired] == rowValue)
                            {
                                if (indexOfColumn >= record.Length)
                                {
                                    int m = 3;
                                }
                                foundValue = record[indexOfColumn];

                                foundValue = ConvertValueToType(foundValue, rcr.Headers[indexOfColumn].OriginalText);


                                break;
                            }
                        }
                        catch
                        {
                            int m = 3;
                        }
                    }

                }
            }

            // We need to find a RFS either in this IElement or in GlobalContentFiles that match this name
            return foundValue;
        }

        public static object ConvertValueToType(object foundValue, string csvHeaderText)
        {
            if (csvHeaderText.Contains("("))
            {
                // We should use whatever logic Glue uses here....and we should probably cache it so that it's fast
                string typeToConvertTo = CsvHeader.GetClassNameFromHeader(csvHeaderText);

                Type type = TypeManager.GetTypeFromString(typeToConvertTo);
                return PropertyValuePair.ConvertStringToType((string)foundValue, type);
            }
            else
            {
                return foundValue;
            }
        }



    }
}
