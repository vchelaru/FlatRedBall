using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using FlatRedBall.IO.Csv;
using DialogTreePlugin.Controllers;

namespace DialogTreePlugin.ViewModels
{
    public class MainControlViewModel : ViewModel
    {
        public Collection<DialogTreeLocalizationEntryViewModel> csvEntry;
        public Collection<DialogTreeLocalizationEntryViewModel> CsvEntry
        {
            get => csvEntry;
            set { base.ChangeAndNotify(ref csvEntry, value); }
        }

        internal void SetFrom(RuntimeCsvRepresentation localizationDb, string[] dialogIds)
        {
            if(CsvEntry != null)
            {
                foreach(var entry in CsvEntry)
                {
                    entry.PropertyChanged -= MainController.Self.ReactToPropertyChangedEvent;
                }
            }
            var tempList = new Collection<DialogTreeLocalizationEntryViewModel>();
            foreach(var id in dialogIds)
            {
                var record = localizationDb.Records.FirstOrDefault(item => item[0] == id);
                string recordValue = record == null ? string.Empty : record[1];

                var viewModel = new DialogTreeLocalizationEntryViewModel()
                {
                    DialogId = id,
                    LocalizedText = recordValue
                };

                viewModel.PropertyChanged += MainController.Self.ReactToPropertyChangedEvent;

                tempList.Add(viewModel);
            }

            CsvEntry = tempList;
        }
    }
}
