using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;

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
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                FileManager.SaveText(contents, fileName);
            }, 20);
        }
    }
}
