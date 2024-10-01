using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO.Csv;
using FlatRedBall.PlatformerPlugin.Generators;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformerPlugin.Logic
{
    static class PlatformerValuesCreationLogic
    {
        public static void GetCsvValues(EntitySave currentEntitySave,
            out Dictionary<string, PlatformerValues> csvValues)
        {
            csvValues = new Dictionary<string, PlatformerValues>();
            var filePath = CsvGenerator.Self.CsvPlatformerFileFor(currentEntitySave);

            if (filePath.Exists())
            {
                try
                {
                    CsvFileManager.CsvDeserializeDictionary<string, PlatformerValues>(filePath.FullPath, csvValues);
                }
                catch (Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load platformer csv:\n" + e.ToString());
                }
            }

        }
    }
}
