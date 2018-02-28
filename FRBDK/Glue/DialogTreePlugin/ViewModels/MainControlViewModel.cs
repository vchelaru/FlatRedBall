using FlatRedBall.Glue.MVVM;
using System.Collections.ObjectModel;
using System.Linq;
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


        public MainControlViewModel()
        {
            csvEntry = new Collection<LocaliztionDbViewModel>();
        }

        internal void SetFrom(RuntimeCsvRepresentation localizationDb, string[] dialogIds)
        {
            foreach(var entry in CsvEntry)
            {
                entry.PropertyChanged -= TabConroller.Self.ReactToPropertyChangedEvent;
                entry.DeregisterFromEvents();
            }
            CsvEntry.Clear();

            //We use a temp list so we can notify anything listening that the collection has changed.
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

                viewModel.PropertyChanged += TabConroller.Self.ReactToPropertyChangedEvent;

                tempList.Add(viewModel);
            }

            CsvEntry = tempList;
        }
    }
}
