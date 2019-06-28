using FlatRedBall.Glue.IO;
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
        #region Fields/Properties

        public const string StrippedCsvFile = "TopDownValues";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";

        #endregion

        public FilePath CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        internal void GenerateFor(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            string contents = GetCsvContents(entity, viewModel);

            string fileName = CsvFileFor(entity).FullPath;

            GlueCommands.Self.TryMultipleTimes(() =>
            {
                FileManager.SaveText(contents, fileName);
            });
        }

        private string GetCsvContents(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            List<TopDownValues> values = new List<TopDownValues>();

            // create a default entry:
            var defaultValue = new TopDownValues();
            defaultValue.Name = "DefaultValues";
            defaultValue.MaxSpeed = 250;
            defaultValue.AccelerationTime = 1;
            defaultValue.DecelerationTime = .5f;
            defaultValue.UpdateDirectionFromVelocity = true;

            values.Add(defaultValue);

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);

            var nameHeader = rcr.Headers[0];

            nameHeader.IsRequired = true;
            // Setting it to IsRequired is not sufficient, need to
            // modify the "Original Text" prop
            // chop off the closing quote, and add ", required)"
            nameHeader.OriginalText = nameHeader.OriginalText.Substring(0, nameHeader.OriginalText.Length - 1) + ", required)";

            rcr.Headers[0] = nameHeader;

            var movementDefaults = new string[]
            {

            };

            rcr.Records.Add(movementDefaults);

            var toReturn = rcr.GenerateCsvString();

            return toReturn;
        }


    }
}
