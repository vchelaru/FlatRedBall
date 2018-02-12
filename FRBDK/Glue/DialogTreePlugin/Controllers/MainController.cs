using DialogTreePlugin.SaveClasses;
using DialogTreePlugin.Views;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using System.Runtime.Serialization.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO.Csv;
using DialogTreePlugin.ViewModels;
using DialogTreePlugin.Generators;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;

namespace DialogTreePlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields
        MainControl mainControl;
        MainControlViewModel mainControlViewModel;

        DialogTree.Rootobject dialogTree;
        string[] dialogIds;
        RuntimeCsvRepresentation localizationDb;

        //We assume there is a file named LocalizationDatabase.csv in GlobalContent
        internal static string GlobalContent = "GlobalContent";
        internal static string StrippedLocalizationDbCsvFile = "LocalizationDatabase";
        internal static string RelativeToGlobalContentLocalizationDbCsvFile = $"{GlobalContent}/{StrippedLocalizationDbCsvFile}.csv";
        #endregion 

        public MainControl GetControl()
        {
            if(mainControl == null)
            {
                mainControl = new MainControl();
                mainControlViewModel = new MainControlViewModel();
                mainControl.DataContext = mainControlViewModel;
                mainControlViewModel.PropertyChanged += MainController.Self.ReactToPropertyChangedEvent;
            }

            return mainControl;
        }

        internal void ReactToLocalizationDbChange(string filename)
        {
            localizationDb = CsvFileManager.CsvDeserializeToRuntime(filename);
            if(localizationDb != null && dialogIds != null)
            {
                mainControlViewModel?.SetFrom(localizationDb, dialogIds);
            }
        }

        internal void UpdateTo(ReferencedFileSave currentFileSave)
        {
            var fileName = GlueCommands.Self.GetAbsoluteFileName(currentFileSave);
            try
            {
                Stream fileStream = new FileStream(fileName, FileMode.Open);
                var serializer = new DataContractJsonSerializer(typeof(DialogTree.Rootobject));
                dialogTree = (DialogTree.Rootobject)serializer.ReadObject(fileStream);
            }
            catch (Exception e)
            {

            }

            var idList = new List<string>();
            foreach(var passage in dialogTree.passages)
            {
                idList.Add(passage.name);
                if (passage.links != null)
                {
                    foreach (var link in passage.links)
                    {
                        idList.Add(link.name);
                    }
                }
            }

            dialogIds = idList.ToArray();

            if(localizationDb == null)
            {
                try
                {
                    var filePath = GlueCommands.Self.GetAbsoluteFileName(RelativeToGlobalContentLocalizationDbCsvFile, false);
                    ReactToLocalizationDbChange(filePath);
                }
                catch (Exception e)
                {

                }
            }

            mainControlViewModel.SetFrom(localizationDb, dialogIds);
        }

        internal void ReactToPropertyChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            var senderAsEntry = sender as DialogTreeLocalizationEntryViewModel;
            if(senderAsEntry != null)
            {
                //For now, make sure we are not allowing the edit of the DialogId for now.
                if (e.PropertyName == nameof(senderAsEntry.LocalizedText))
                {
                    var dbRecord = localizationDb.Records.FirstOrDefault(item => item[0] == senderAsEntry.DialogId);

                    if (dbRecord != null)
                    {
                        dbRecord[1] = senderAsEntry.LocalizedText;
                    }
                    else
                    {
                        localizationDb.Records.Add(new string[] { senderAsEntry.DialogId, senderAsEntry.LocalizedText });
                    }

                    TaskManager.Self.AddAsyncTask(
                        () =>
                        {
                            string fileName = GlueCommands.Self.GetAbsoluteFileName(RelativeToGlobalContentLocalizationDbCsvFile, false);

                            CsvGenerator.Self.GenerateFor(fileName, localizationDb.GenerateCsvString());
                        },
                        "Dialog Tree Manager: Regenerating LocalizationDatabase."
                        );
                }
            }
        }
    }
}
