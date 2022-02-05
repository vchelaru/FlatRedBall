using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace OfficialPluginsCore.Compiler.ViewModels
{
    public class RunnerToolbarViewModel : ViewModel, ISearchBarViewModel
    {
        public string StartupScreenName
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<string> AllScreens { get; set; } = new List<string>();

        public ObservableCollection<string> AvailableScreens
        {
            get; set;
        } = new ObservableCollection<string>();

        public bool IsPlayVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsPlayVisible))]
        public Visibility PlayVisibility => IsPlayVisible.ToVisibility();

        public string SearchBoxText 
        { 
            get => Get<string>(); 
            set
            {
                if(Set(value))
                {
                    RefreshAvailableScreens();
                }
            }
        }
        public bool IsSearchBoxFocused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchButtonVisibility => (!string.IsNullOrEmpty(SearchBoxText)).ToVisibility();

        public Visibility TipsVisibility => Visibility.Collapsed;

        [DependsOn(nameof(IsSearchBoxFocused))]
        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchPlaceholderVisibility =>
            (IsSearchBoxFocused == false && string.IsNullOrWhiteSpace(SearchBoxText)).ToVisibility();

        public string FilterResultsInfo => null;

        public RunnerToolbarViewModel()
        {
            IsPlayVisible = true;
        }

        public void RefreshAvailableScreens()
        {
            var searchTextToLowerInvariant = SearchBoxText?.ToLowerInvariant();
            AvailableScreens.Clear();

            foreach(var item in AllScreens)
            {
                var shouldInclude =
                    string.IsNullOrWhiteSpace(searchTextToLowerInvariant) ||
                    item.ToLowerInvariant().Contains(searchTextToLowerInvariant);
                if (shouldInclude)
                {   
                    AvailableScreens.Add(item);
                }
            }
        }
    }
}
