using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
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
        public const string StrippedCsvFile = "TopDownValues";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";

        public string CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        internal void GenerateFor(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            string contents = GetCsvContents(entity, viewModel);

            string fileName = CsvFileFor(entity);

            GlueCommands.Self.TryMultipleTimes(() =>
            {
                FileManager.SaveText(contents, fileName);
            });
        }

        private string GetCsvContents(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            List<TopDownValues> values = new List<TopDownValues>();

            foreach (var valuesViewModel in viewModel.TopDownValues)
            {
                // todo - finish here
                //values.Add(valuesViewModel.ToValues());
            }

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);

            var nameHeader = rcr.Headers[0];

            nameHeader.IsRequired = true;
            // Setting it to IsRequired is not sufficient, need to
            // modify the "Original Text" prop
            // chop off the closing quote, and add ", required)"
            nameHeader.OriginalText = nameHeader.OriginalText.Substring(0, nameHeader.OriginalText.Length - 1) + ", required)";

            rcr.Headers[0] = nameHeader;

            var toReturn = rcr.GenerateCsvString();

            return toReturn;
        }


    }
}
