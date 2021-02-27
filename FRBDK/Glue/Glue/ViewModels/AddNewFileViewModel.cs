using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

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
            set
            {
                if (Set(value))
                {
                    IsNameDefault = false;
                }
            }
        }

        /// <summary>
        /// If true, then changing the selection will rename
        /// the file. If false, the name will stick regardless
        /// of the selection
        /// </summary>
        public bool IsNameDefault
        {
            get => Get<bool>();
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

        public AssetTypeInfo ForcedType
        {
            get => Get<AssetTypeInfo>();
            set
            {
                if (Set(value) && value != null)
                {
                    SelectedAssetTypeInfo = value;
                }
            }
        }

        [DependsOn(nameof(ForcedType))]
        public Visibility FileTypeSelectionVisibility => ForcedType == null ? Visibility.Visible : Visibility.Collapsed;

        [DependsOn(nameof(ForcedType))]
        public SizeToContent SizeToContent => ForcedType == null ? SizeToContent.Manual : SizeToContent.Height;

        [DependsOn(nameof(ForcedType))]
        public int AdditionalUiStackColumn => ForcedType == null ? 1 : 0;

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
            IsNameDefault = true;
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
