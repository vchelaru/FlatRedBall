using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.ViewModels
{
    public class AddExistingFileViewModel : ViewModel
    {
        public string SearchText
        {
            get { return Get<string>(); }
            set
            {
                if(Set(value))
                {
                    RefreshFilteredList();

                }
            }
        }

        public string SelectedListBoxItem
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public List<string> Files
        {
            get;
            private set;
        } = new List<string>();

        public List<string> UnfilteredFileList
        {
            get;
            private set;
        } = new List<string>();

        public ObservableCollection<string> FilteredFileList
        {
            get;
            private set;
        } = new ObservableCollection<string>();

        public string ContentFolder { get; internal set; }

        public AddExistingFileViewModel()
        {

        }

        public void RefreshFilteredList()
        {
            FilteredFileList.Clear();

            string toLower = SearchText?.ToLowerInvariant();

            foreach(var item in UnfilteredFileList)
            {
                var shouldAdd =
                    string.IsNullOrEmpty(toLower) ||
                    item.ToLowerInvariant().Contains(toLower);

                if(shouldAdd)
                {
                    FilteredFileList.Add(item);
                }

            }

            // pre-select the first (if there is one)
            if (FilteredFileList.Count > 0)
            {
                SelectedListBoxItem = FilteredFileList[0];
            }
        }
    }
}
