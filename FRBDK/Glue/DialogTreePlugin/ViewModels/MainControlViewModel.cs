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
        public Collection<LocaliztionDbViewModel> csvEntry;
        public Collection<LocaliztionDbViewModel> CsvEntry
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
                    entry.DeregisterFromEvents();
                }
            }
            var tempList = new Collection<LocaliztionDbViewModel>();
            foreach(var id in dialogIds)
            {
                var record = localizationDb.Records.FirstOrDefault(item => item[0] == id);
                string[] recordValue = record;

                if(recordValue == null)
                {
                    //Default to an empty record
                    recordValue = new string[localizationDb.Headers.Length];
                    recordValue[0] = id;
                }

                var viewModel = new LocaliztionDbViewModel()
                {
                    CsvHeader = localizationDb.Headers
                };

                viewModel.SetFrom(recordValue);

                viewModel.PropertyChanged += MainController.Self.ReactToPropertyChangedEvent;

                tempList.Add(viewModel);
            }

            CsvEntry = tempList;
        }
    }
}
