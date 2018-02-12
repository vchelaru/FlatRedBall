using DialogTreePlugin.SaveClasses;
using DialogTreePlugin.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogTreePlugin.Generators
{
    public class CsvGenerator: Singleton<CsvGenerator>
    {
        public string CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        public const string StrippedCsvFile = "LocalizationDatabase";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";


        internal void GenerateFor(string fileName, string contents)
        {

  

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
    }
}
