using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToolsUtilities;

namespace OfficialPluginsCore.Wizard.ViewModels
{
    public enum UrlStatus
    {
        Unknown,
        Failed,
        Succeeded
    }

    public class ElementImportItemViewModel : ViewModel
    {
        public UrlStatus UrlStatus
        {
            get => Get<UrlStatus>();
            set => Set(value);
        }
        public string Url
        {
            get => Get<string>();
            set
            {
                var oldStatus = UrlStatus;

                // force it to unknown before the change happens
                UrlStatus = UrlStatus.Unknown;

                if (Set(value))
                {
                    UpdateUrlStatus();
                }
                else
                {
                    UrlStatus = oldStatus;
                }
            }
        }

        [DependsOn(nameof(Url))]
        public Visibility XButtonVisibility => 
            (!string.IsNullOrEmpty(Url)).ToVisibility();

        [DependsOn(nameof(UrlStatus))]
        public Visibility CheckVisibility => 
            (UrlStatus == UrlStatus.Succeeded).ToVisibility();

        [DependsOn(nameof(UrlStatus))]
        public Visibility ErrorVisibility =>
            (UrlStatus == UrlStatus.Failed).ToVisibility();

        public string ErrorMessage
        {
            get => Get<string>();
            set => Set(value);
        }


        public async void UpdateUrlStatus()
        {
            UrlStatus = UrlStatus.Unknown;

            if(!string.IsNullOrEmpty(Url))
            {
                var extension = FileManager.GetExtension(Url);
                var isValidExtension = extension == "entz" || extension == "scrz";

                if(!isValidExtension)
                {
                    ErrorMessage = "Inavlid extension - must be .entz or srcz";
                    UrlStatus = UrlStatus.Failed;
                }
                else
                {
                    var succeeded = await RemoteFileExists(Url);

                    if(succeeded)
                    {
                        UrlStatus = UrlStatus.Succeeded;
                    }
                    else
                    {
                        UrlStatus = UrlStatus.Failed;
                    }
                }
            }
            else
            {
                ErrorMessage = null;
            }
        }

        private async Task<bool> RemoteFileExists(string url)
        {
            ErrorMessage = null;
            try
            {
                //Creating the HttpWebRequest
                var request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                var response = (await request.GetResponseAsync()) as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                var toReturn = (response.StatusCode == HttpStatusCode.OK);
                response.Close();
                return toReturn;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                //Any exception will returns false.
                return false;
            }
        }
    }

    public class ElementImportViewModel : ViewModel
    {
        public ObservableCollection<ElementImportItemViewModel> Items
        {
            get; set;
        } = new ObservableCollection<ElementImportItemViewModel>();

        public bool IsValid
        {
            get => Get<bool>();
            private set => Set(value);
        }

        public ElementImportViewModel()
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems != null)
            {
                foreach(var item in e.NewItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged += HandlePropertyChanged;
                }
            }

            if(e.OldItems != null)
            {
                foreach(var item in e.OldItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged -= HandlePropertyChanged;
                }
            }

            RefreshIsValid();
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ElementImportItemViewModel.Url))
            {
                var itemViewModel = sender as ElementImportItemViewModel;
                var itemViewModelIndex = Items.IndexOf(itemViewModel);
                var isLast = itemViewModelIndex == Items.Count - 1;

                var newUrl = itemViewModel.Url;

                if(string.IsNullOrEmpty( newUrl ))
                {
                    if(!isLast)
                    {
                        Items.RemoveAt(itemViewModelIndex);
                    }
                }
                else
                {
                    if(isLast)
                    {
                        Items.Add(new ElementImportItemViewModel());
                    }
                }

                RefreshIsValid();
            }
            else if(e.PropertyName == nameof(ElementImportItemViewModel.UrlStatus))
            {
                RefreshIsValid();
            }
        }

        void RefreshIsValid ()
        {
            IsValid = Items.All(item =>
            {
                return string.IsNullOrEmpty(item.Url) || item.UrlStatus == UrlStatus.Succeeded;
            });
        }
    }
}
