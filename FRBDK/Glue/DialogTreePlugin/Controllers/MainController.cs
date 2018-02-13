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
        public RuntimeCsvRepresentation LocalizationDb;

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
                mainControlViewModel.PropertyChanged += ReactToPropertyChangedEvent;
            }

            return mainControl;
        }

        internal void ReactToLocalizationDbChange(string filename)
        {
            LocalizationDb = CsvFileManager.CsvDeserializeToRuntime(filename);
            if(LocalizationDb != null && dialogIds != null)
            {
                mainControlViewModel?.SetFrom(LocalizationDb, dialogIds);
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

            if(LocalizationDb == null)
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

            mainControlViewModel.SetFrom(LocalizationDb, dialogIds);
        }

        internal void ReactToPropertyChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            var senderAsTextViewModel = sender as LocaliztionDbViewModel;
            if(senderAsTextViewModel != null)
            {
                //For now, make sure we are not allowing the edit of the DialogId for now.
                if (e.PropertyName == nameof(LocalizedTextViewModel.Text))
                {

                    var dbRecord = LocalizationDb.Records.FirstOrDefault(item => item[0] == senderAsTextViewModel.LocalizedText[0].Text);

                    if (dbRecord != null)
                    {
                        var newRecord = senderAsTextViewModel.LocalizedTextAsStringArray;
                        for(int i = 0; i < newRecord.Length; i++)
                        {
                            dbRecord[i] = newRecord[i];
                        }
                    }
                    else
                    {
                        LocalizationDb.Records.Add(senderAsTextViewModel.LocalizedTextAsStringArray);
                    }

                    TaskManager.Self.AddAsyncTask(
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
}
