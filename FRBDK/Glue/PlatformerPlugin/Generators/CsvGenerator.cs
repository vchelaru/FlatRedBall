using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO.Csv;
using FlatRedBall.PlatformerPlugin.SaveClasses;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class CsvGenerator : Singleton<CsvGenerator>
    {

        public string CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        public const string StrippedCsvFile = "PlatformerValues";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";


        internal void GenerateFor(EntitySave entity, PlatformerEntityViewModel viewModel)
        {
            string contents = GetCsvContents(entity, viewModel);

            string fileName = CsvFileFor(entity);

            int numberOfTimesToTry = 4;
            int numberOfFailures = 0;
            bool succeeded = false;
            while (numberOfFailures < numberOfTimesToTry)
            {
                try
                {
                    FileManager.SaveText(contents, fileName);

                    succeeded = true;
                    break;
                }
                catch (Exception e)
                {
                    numberOfFailures++;
                    if (numberOfFailures == numberOfTimesToTry)
                    {
                        PluginManager.ReceiveError(e.ToString());
                    }
                }
            }
        }

        private string GetCsvContents(EntitySave entity, PlatformerEntityViewModel viewModel)
        {
            List<PlatformerValues> values = new List<PlatformerValues>();

            foreach(var valuesViewModel in viewModel.PlatformerValues)
            {
                values.Add(valuesViewModel.ToValues());
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
