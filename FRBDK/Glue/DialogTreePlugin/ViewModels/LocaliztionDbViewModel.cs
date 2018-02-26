using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO.Csv;
using System.Linq;
using System.ComponentModel;

namespace DialogTreePlugin.ViewModels
{
    public class LocaliztionDbViewModel : ViewModel
    {
        CsvHeader[] csvHeaders;
        public CsvHeader[] CsvHeader
        {
            get => csvHeaders;
            set { base.ChangeAndNotify(ref csvHeaders, value); }
        }

        LocalizedTextViewModel[] localizedText;
        public LocalizedTextViewModel[] LocalizedText
        {
            get => localizedText;
            set { base.ChangeAndNotify(ref localizedText, value); }
        }

        public string[] LocalizedTextAsStringArray => localizedText.Select(item => item.Text).ToArray();

        public void SetFrom(string[] localizationDbEntry)
        {
            LocalizedText = localizationDbEntry.Select(item =>
            {
                var viewModel = new LocalizedTextViewModel
                {
                    Text = item
                };

                viewModel.PropertyChanged += HandleChildPropertyChangedEvents;

                return viewModel;
            }).ToArray();
        }

        public void DeregisterFromEvents()
        {
            foreach(var viewModel in LocalizedText)
            {
                viewModel.PropertyChanged -= HandleChildPropertyChangedEvents;
            }
        }

        private void HandleChildPropertyChangedEvents(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }
    }

    public class LocalizedTextViewModel : ViewModel
    {
        string text;
        public string Text
        {
            get => text;
            set { base.ChangeAndNotify(ref text, value); }
        }
    }
}
