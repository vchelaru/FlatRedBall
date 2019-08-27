using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using RacingPlugin.Models;
using RacingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.DataGenerators
{
    public class CsvGenerator : Singleton<CsvGenerator>
    {
        #region Fields/Properties

        public const string StrippedCsvFile = "RacingEntityValues";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";

        #endregion

        public FilePath CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }


        internal void GenerateFor(EntitySave entity, RacingEntityViewModel viewModel)
        {
            string contents = GetCsvContents(entity, viewModel);

            string fileName = CsvFileFor(entity).FullPath;

            GlueCommands.Self.TryMultipleTimes(() =>
            {
                FileManager.SaveText(contents, fileName);
            });
        }

        private string GetCsvContents(EntitySave entity, RacingEntityViewModel viewModel)
        {
            List<RacingEntityValues> values = new List<RacingEntityValues>();

            var defaultValues = new RacingEntityValues();
            defaultValues.Name = "DefaultValues";
            // leave the defaults from the class

            values.Add(defaultValues);

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);

            var nameHeader = rcr.Headers[0];

            // Setting it to IsRequired is not sufficient, need to
            // modify the "Original Text" prop
            // chop off the closing quote, and add ", required)"
            nameHeader.OriginalText = nameHeader.OriginalText.Substring(0, nameHeader.OriginalText.Length - 1) + ", required)";

            rcr.Headers[0] = nameHeader;

            // if we want more defaults here...
            rcr.Records.Add(new string[0]);

            var toReturn = rcr.GenerateCsvString();

            return toReturn;

        }
    }
}
