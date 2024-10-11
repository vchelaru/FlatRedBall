using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace OfficialPlugins.Compiler.ViewModels
{
    #region ScreenReferenceViewModel class
    public class ScreenReferenceViewModel : ViewModel
    {
        public string ScreenName
        {
            get;set;
        }

        public bool IsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public override string ToString() => ScreenName;
    }
    #endregion

    public class RunnerToolbarViewModel : ViewModel, ISearchBarViewModel
    {
        public string StartupScreenName
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<ScreenReferenceViewModel> AllScreens { get; set; } = new List<ScreenReferenceViewModel>();

        public ObservableCollection<ScreenReferenceViewModel> AvailableScreens
        {
            get; set;
        } = new ObservableCollection<ScreenReferenceViewModel>();

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
                    item.ScreenName.ToLowerInvariant().Contains(searchTextToLowerInvariant);
                if (shouldInclude)
                {   
                    AvailableScreens.Add(item);
                }
            }

            var selected = AvailableScreens.FirstOrDefault(item => item.IsSelected);
            if(selected == null){
                if(AvailableScreens.Count > 0)
                    AvailableScreens[0].IsSelected = true;
                else
                    StartupScreenName = "";
            }
        }
    }
}
