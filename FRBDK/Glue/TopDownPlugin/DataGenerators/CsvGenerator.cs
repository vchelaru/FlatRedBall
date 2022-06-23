using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using GlueCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Models;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.DataGenerators
{
    public class CsvGenerator : Singleton<CsvGenerator>
    {
        #region Fields/Properties

        public static string StrippedCsvFile
        {
            get
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CsvInheritanceSupport)
                {
                    // As of this version, we put the top down values in a static and then we have a virtual instance
                    // property for inheritance
                    return "TopDownValuesStatic";
                }
                else
                {
                    return "TopDownValues";
                }
            }
        }
        public static string RelativeCsvFile => StrippedCsvFile + ".csv";

        #endregion

        public FilePath CsvTopdownFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        internal Task GenerateFor(EntitySave entity, bool inheritsFromTopDown, TopDownEntityViewModel viewModel, CsvHeader[] lastHeaders)
        {
            return TaskManager.Self.AddAsync(() =>
            {
                string newContents = GenerateCsvContents(inheritsFromTopDown, viewModel, lastHeaders);

                var fileName = CsvTopdownFileFor(entity);

                try
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        var shouldSave = true;
                        if (fileName.Exists())
                        {
                            var existingContents = System.IO.File.ReadAllText(fileName.FullPath);
                            shouldSave = existingContents != newContents;

                        }
                        if (shouldSave)
                        {
                            FileManager.SaveText(newContents, fileName.FullPath);
                        }
                    });
                }
                catch (System.IO.IOException)
                {
                    GlueCommands.Self.PrintError($"Trying to save top down CSV {fileName} but failed due to IO - maybe file is open?");
                }

            }, $"Generating Platformer CSV for {entity}");
        }

        /// <summary>
        /// Converts the TopDownEntityViewModel to a CSV string.
        /// </summary>
        /// <param name="inheritsFromTopDown">Whether the entity being generated inherits from a top-down implementing entity.</param>
        /// <param name="viewModel">The view model containing the values</param>
        /// <param name="oldHeaders">The headers from the previously-loaded CSV</param>
        /// <returns>The CSV string</returns>
        private string GenerateCsvContents(bool inheritsFromTopDown, TopDownEntityViewModel viewModel, CsvHeader[] oldHeaders)
        {
            List<TopDownValues> values = new List<TopDownValues>();

            foreach(var valuesViewModel in viewModel.TopDownValues)
            {
                var topDownValues = valuesViewModel.ToValues();

                var shouldInclude = inheritsFromTopDown == false || topDownValues.InheritOrOverwrite == InheritOrOverwrite.Overwrite;
                if(shouldInclude)
                {
                    values.Add(topDownValues);
                }

            }

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);

            var newHeaders = rcr.Headers.ToList();

            if(oldHeaders != null)
            {
                foreach(var oldHeader in oldHeaders)
                {
                    if(newHeaders.Any(item => item.Name == oldHeader.Name) == false)
                    {
                        newHeaders.Add(oldHeader);
                    }
                }
            }


            for(int rowIndex = 0; rowIndex < values.Count; rowIndex++)
            {
                //foreach(var value in values)
                var value = values[rowIndex];

                var row = rcr.Records[rowIndex];

                if(row.Length != newHeaders.Count)
                {
                    var newRow = row.ToList();
                    while(newRow.Count < newHeaders.Count)
                    {
                        newRow.Add(""); // will be filled in later
                    }

                    rcr.Records[rowIndex] = newRow.ToArray();
                    row = rcr.Records[rowIndex];
                }

                foreach (var additionalValue in value.AdditionalValues)
                {
                    var matchingHeader = newHeaders.FirstOrDefault(item => item.Name == additionalValue.Key);

                    // this better not be null
                    var index = newHeaders.IndexOf(matchingHeader);

                    if(index > 0)
                    {
                        row[index] = additionalValue.Value?.ToString();
                    }
                }
            }

            //if(headers != null)
            //{
            //    rcr.Headers = headers;

            //    for(int rowIndex = 0; rowIndex < rcr.Records.Count; rowIndex++)
            //    {
            //        var row = rcr.Records[rowIndex];
            //        var topDownValues = values[rowIndex];   

            //        var rowRecordAsList = row.ToList();

            //        for (int columnIndex = row.Length; columnIndex < headers.Length; columnIndex++)
            //        {
            //            var headerName = headers[columnIndex].Name;

            //            if (topDownValues.AdditionalValues.ContainsKey(headerName))
            //            {
            //                var value = topDownValues.AdditionalValues[headerName] as TypedValue;

            //                // does this need to account for culture?
            //                rowRecordAsList.Add(value?.Value?.ToString());

            //            }
            //        }

            //        rcr.Records[rowIndex] = rowRecordAsList.ToArray();
            //    }
            //}

            // assume header[0] is name, so make it required:
            if(rcr.Headers.Length > 0)
            {
                rcr.Headers[0].IsRequired = true;
                rcr.Headers[0].OriginalText = rcr.Headers[0].Name + " (string, required)";
            }

            var toReturn = rcr.GenerateCsvString();


            return toReturn;
        }


    }
}
