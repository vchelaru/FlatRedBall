using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.Glue.ViewModels
{
    public enum FileLocationType
    {
        Local,
        Download
    }

    public class AddExistingFileViewModel : ViewModel
    {
        public string SearchText
        {
            get => Get<string>(); 
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
            get => Get<string>();
            set => Set(value); 
        }
        public System.Collections.IList SelectedListBoxItems { get; set; }

        public FileLocationType FileLocationType
        {
            get => Get<FileLocationType>();
            set => Set(value);
        }

        [DependsOn(nameof(FileLocationType))]
        public bool IsLocalFilesChecked
        {
            get => FileLocationType == FileLocationType.Local;
            set
            {
                if(value)
                {
                    FileLocationType = FileLocationType.Local;
                }
            }
        }

        [DependsOn(nameof(FileLocationType))]
        public bool IsDownloadFileChecked
        {
            get => FileLocationType == FileLocationType.Download;
            set
            {
                if(value)
                {
                    FileLocationType = FileLocationType.Download;
                }
            }
        }

        [DependsOn(nameof(FileLocationType))]
        public Visibility LocalUiVisibility =>
            (FileLocationType == FileLocationType.Local).ToVisibility();

        [DependsOn(nameof(FileLocationType))]
        public Visibility DownloadFileVisibility =>
            (FileLocationType == FileLocationType.Download).ToVisibility();

        public string DownloadUrl
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<FilePath> Files
        {
            get;
            private set;
        } = new List<FilePath>();

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

        public ObservableCollection<IndividualFileAddDownloadViewModel> DownloadedFilesList
        {
            get; set;
        } = new ObservableCollection<IndividualFileAddDownloadViewModel>();

        public string ContentFolder { get; internal set; }

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
