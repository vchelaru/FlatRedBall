using DialogTreePlugin.Generators;
using DialogTreePlugin.SaveClasses;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DialogTreePlugin.Controllers
{
    public class DialogTreeFileController : Singleton<DialogTreeFileController>
    {
        public RuntimeCsvRepresentation LocalizationDb;

        //We assume there is a file named LocalizationDatabase.csv in GlobalContent
        internal static string GlobalContent = "GlobalContent";
        internal static string StrippedLocalizationDbCsvFile = "LocalizationDatabase";
        internal static string RelativeToGlobalContentLocalizationDbCsvFile = $"{GlobalContent}/{StrippedLocalizationDbCsvFile}.csv";

        public DialogTreeFileController()
        {
            try
            {
                var filePath = GlueCommands.Self.GetAbsoluteFileName(RelativeToGlobalContentLocalizationDbCsvFile, false);
                SetLocalizationDb(filePath);
            }
            catch (Exception e)
            {

            }
        }

        private void SetLocalizationDb(string filePath)
        {
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                LocalizationDb = CsvFileManager.CsvDeserializeToRuntime(filePath);
            }, 5);
        }

        private void ResetLocalizationDb()
        {
            var filePath = GlueCommands.Self.GetAbsoluteFileName(RelativeToGlobalContentLocalizationDbCsvFile, false);
            SetLocalizationDb(filePath);
        }

        public void SerializeConvertedDialogTree(DialogTreeConverted.Rootobject convertedDialogTree, string fileName)
        {
            Stream createStream = new FileStream(fileName, FileMode.Create);
            var serializer = new DataContractJsonSerializer(typeof(DialogTreeConverted.Rootobject));
            serializer.WriteObject(createStream, convertedDialogTree);
        }

        public DialogTreeRaw.Rootobject DeserializeRawDialogTree(string fileName)
        {
            DialogTreeRaw.Rootobject deserializedDialogTree;
            try
            {
                Stream openStream = new FileStream(fileName, FileMode.Open);
                var serializer = new DataContractJsonSerializer(typeof(DialogTreeRaw.Rootobject));
                deserializedDialogTree = (DialogTreeRaw.Rootobject)serializer.ReadObject(openStream);
            }
            catch
            {
                deserializedDialogTree = null;
                GlueCommands.Self.PrintError($"Json file could not be deserialized to a DialogTree:\n{fileName}");
            }
            return deserializedDialogTree;
        }

        public DialogTreeConverted.Rootobject DeserializeConvertedDialogTree(string fileName)
        {
            Stream openStream = new FileStream(fileName, FileMode.Open);
            var serializer = new DataContractJsonSerializer(typeof(DialogTreeConverted.Rootobject));
            var dialogTreeNew = (DialogTreeConverted.Rootobject)serializer.ReadObject(openStream);
            return dialogTreeNew;
        }

        public string[] GetLocalizationDbEntryOrDefault(string stringId)
        {
            var toReturn = LocalizationDb?.Records.FirstOrDefault(item => item[0] == stringId);

            if(toReturn == null)
            {
                if(LocalizationDb == null)
                {
                    toReturn = new string[2];
                }
                else
                {
                    toReturn = new string[LocalizationDb.Headers.Length];
                }
            }

            return toReturn;
        }

        public void UpdateLocalizationDb(string[][] localizationDbEntries, bool isGlsnChange = false)
        {
            if (LocalizationDb != null)
            {
                //Make sure we are getting the most recent version of the localizationDb.
                //This prevents us from erasing entries not added by this plugin.
                ResetLocalizationDb();

                if(isGlsnChange)
                {
                    var firstKey = localizationDbEntries[0][0];
                    var currentStringIdPrefix = firstKey.Substring(0, firstKey.IndexOf("Passage"));

                    //We want to remove all entries for this DT because we could be removing a key.
                    LocalizationDb.Records.RemoveAll(item => item[0].StartsWith(currentStringIdPrefix));
                }

                foreach (var entry in localizationDbEntries)
                {
                    var dbRecord = LocalizationDb.Records.FirstOrDefault(item => item[0] == entry[0]);

                    if (dbRecord != null)
                    {

                        for (int i = 0; i < entry.Length; i++)
                        {
                            dbRecord[i] = entry[i];
                        }
                    }
                    else
                    {
                        LocalizationDb.Records.Add(entry);
                    }

                }
                TaskManager.Self.Add(
                    () =>
                    {
                        string fileName = GlueCommands.Self.GetAbsoluteFileName(RelativeToGlobalContentLocalizationDbCsvFile, false);

                        CsvGenerator.Self.GenerateFor(fileName, LocalizationDb.GenerateCsvString());
                    },
                    "Dialog Tree Manager: Regenerating LocalizationDatabase."
                    );
            }
        }
    }
}
