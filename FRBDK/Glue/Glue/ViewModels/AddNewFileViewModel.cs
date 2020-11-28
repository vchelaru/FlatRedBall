using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GlueFormsCore.ViewModels
{
    public class AddNewFileViewModel : ViewModel
    {
        public AssetTypeInfo SelectedAssetTypeInfo
        {
            get => Get<AssetTypeInfo>();
            set => Set(value);
        }

        public string FileName
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<AssetTypeInfo> AllOptions
        {
            get;
            private set;
        } = new ObservableCollection<AssetTypeInfo>();

        public ObservableCollection<AssetTypeInfo> FilteredOptions
        {
            get;
            private set;
        } = new ObservableCollection<AssetTypeInfo>();

        public string FilterText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    RefreshFilteredOptions();
                }
            }
        }

        public AddNewFileViewModel()
        {
            AllOptions.CollectionChanged += (not, used) => RefreshFilteredOptions();
        }

        private void RefreshFilteredOptions()
        {
            var selected = SelectedAssetTypeInfo;
            FilteredOptions.Clear();

            var filterTextToLower = FilterText?.ToLowerInvariant();

            foreach(var item in AllOptions.OrderBy(item => item.FriendlyName))
            {
                var shouldAdd = string.IsNullOrEmpty(filterTextToLower) ||
                    item.FriendlyName.ToLowerInvariant().Contains(filterTextToLower) == true;

                if(shouldAdd)
                {
                    FilteredOptions.Add(item);
                }
            }

            SelectedAssetTypeInfo = selected;

        }
    }
}
