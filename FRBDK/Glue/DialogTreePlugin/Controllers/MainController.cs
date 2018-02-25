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
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO.Csv;
using DialogTreePlugin.ViewModels;
using DialogTreePlugin.Generators;
using System.Windows.Forms;

namespace DialogTreePlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        public List<string> TrackedDialogTrees;

        #region Fields
        MainControl mainControl;
        MainControlViewModel mainControlViewModel;

        DialogTreeConverted.Rootobject dialogTree;
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


        internal void ReactToLocalizationDbChange()
        {
            if(DialogTreeFileController.Self.LocalizationDb != null && dialogIds != null)
            {
                mainControlViewModel?.SetFrom(DialogTreeFileController.Self.LocalizationDb, dialogIds);
            }
        }

        internal void UpdateTo(ReferencedFileSave currentFileSave)
        {
            var fileName = GlueCommands.Self.GetAbsoluteFileName(currentFileSave);
            try
            {
                DialogTreeConverted.Rootobject dialogTreeNew = DialogTreeFileController.Self.DeserializeConvertedDialogTree(fileName);
                dialogTree = dialogTreeNew;

            }
            catch (Exception e)
            {

            }

            var idList = new List<string>();
            foreach(var passage in dialogTree.passages)
            {
                idList.Add(passage.stringid);
                if (passage.links != null)
                {
                    foreach (var link in passage.links)
                    {
                        idList.Add(link.stringid);
                    }
                }
            }

            dialogIds = idList.ToArray();


            ReactToLocalizationDbChange();

            //mainControlViewModel.SetFrom(LocalizationDb, dialogIds);
        }

        internal void ReactToPropertyChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            var senderAsTextViewModel = sender as LocaliztionDbViewModel;
            if(senderAsTextViewModel != null)
            {
                //For now, make sure we are not allowing the edit of the DialogId for now.
                if (e.PropertyName == nameof(LocalizedTextViewModel.Text))
                {
                    DialogTreeFileController.Self.UpdateLocalizationDb(new string[][] { senderAsTextViewModel.LocalizedTextAsStringArray });
                }
            }
        }
    }
}
