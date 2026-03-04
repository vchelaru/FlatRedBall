using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.Primitives;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    partial class MainTreeViewViewModel : ViewModel, ISearchBarViewModel
    {
        #region Search-related

        // October 21, 2024
        // By assigning this to
        // to a string, it breaks
        // the treeview population.
        // Not sure why yet, so suppressing
        // this assignment
        //public static string SearchText = string.Empty;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static string SearchText;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public static string PrefixText = string.Empty;

        public string SearchBoxText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    PrefixText = String.Empty;
                    if (string.IsNullOrEmpty(value))
                    {
                        SearchText = String.Empty;
                    }
                    else
                    {
                        if (
                            value.StartsWith("f ") ||
                            value.StartsWith("e ") ||
                            value.StartsWith("s ") ||
                            value.StartsWith("o ") ||
                            value.StartsWith("v ")
                            )
                        {
                            SearchText = value.Substring(2);
                            PrefixText = value.Substring(0, 1).ToLowerInvariant();

                        }
                        else
                        {
                            SearchText = value;
                        }

                    }
                    PushSearchToContainedObject();
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

        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchListVisibility => (!string.IsNullOrEmpty(SearchBoxText)).ToVisibility();

        [DependsOn(nameof(IsSearchBoxFocused))]
        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchPlaceholderVisibility =>
            (IsSearchBoxFocused == false && string.IsNullOrWhiteSpace(SearchBoxText)).ToVisibility();

        #endregion

        #region Fields/Properties

        public NodeViewModel ScreenRootNode { get; private set; }
        public NodeViewModel EntityRootNode { get; private set; }
        public NodeViewModel GlobalContentRootNode { get; private set; }

        public NodeViewModel RootModel { get; set; }

        public ObservableCollection<NodeViewModel> FlattenedItems { get; private set; } = new ObservableCollection<NodeViewModel>();

        public NodeViewModel? FlattenedSelectedItem
        {
            get => Get<NodeViewModel?>();
            set => Set(value);
        }


        public IEnumerable Root
        {
            get;
            private set;
        }

        public ObservableCollection<NodeViewModel> VisibleRoot { get; private set; } = new ObservableCollection<NodeViewModel>();

        public IEnumerable Children
        {
            get
            {
                return RootModel.Children;
            }
        }

        public string Title { get; set; }

        public int Count { get; set; }

        [DependsOn(nameof(SearchBoxText))]
        public Visibility MainTreeViewVisibility => (string.IsNullOrEmpty(SearchBoxText)).ToVisibility();
        public bool HasUserDismissedTips
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasUserDismissedTips))]
        public Visibility TipsVisibility
        {
            get
            {
                if (HasUserDismissedTips)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;

                }
            }
        }



        [DependsOn(nameof(SearchBoxText))]
        public string FilterResultsInfo =>
            SearchBoxText?.StartsWith("f ") == true ? Localization.Texts.FilteredToFiles :
            SearchBoxText?.StartsWith("e ") == true ? Localization.Texts.FilteredToEntities :
            SearchBoxText?.StartsWith("s ") == true ? Localization.Texts.FilteredToScreens :
            SearchBoxText?.StartsWith("o ") == true ? Localization.Texts.FilteredToObjects :
            SearchBoxText?.StartsWith("v ") == true ? Localization.Texts.FilteredToVariables :
            Localization.Texts.FilterResultsDescription;

        public bool IsForwardButtonEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBackButtonEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string? SelectedItemInfoDisplay
        {
            get => Get<string?>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedItemInfoDisplay))]
        public Visibility SelectedItemInfoVisibility =>
            string.IsNullOrEmpty(SelectedItemInfoDisplay) ? Visibility.Collapsed : Visibility.Visible;

        #endregion

        #region Bookmark

        public bool IsBookmarkListVisible
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    if(value== false)
                    {
                        OldBookmarkRowHeight = BookmarkRowHeight;
                        BookmarkRowHeight = new GridLength(0, GridUnitType.Pixel);
                    }
                    else
                    {
                        BookmarkRowHeight = OldBookmarkRowHeight;
                    }
                }
            }
        }

        [DependsOn(nameof(IsBookmarkListVisible))]
        public Visibility BookmarkListVisibility => IsBookmarkListVisible.ToVisibility();

        public ObservableCollection<BookmarkViewModel> Bookmarks { get; private set; } = new ObservableCollection<BookmarkViewModel>();

        public BookmarkViewModel? SelectedBookmark
        {
            get => Get<BookmarkViewModel?>();
            set => Set(value);
        }

        public GridLength OldBookmarkRowHeight { get; set; }

        public GridLength BookmarkRowHeight
        {
            get=> Get<GridLength>();
            set => Set(value);
        }

        #endregion

        public MainTreeViewViewModel()
        {
            ScreenRootNode =
                new NodeViewModel(TreeNodeType.ScreenRootNode) { Text = "Screens" };

            EntityRootNode =
                new NodeViewModel(TreeNodeType.EntityRootNode) { Text = "Entities" };

            GlobalContentRootNode =
                new NodeViewModel(TreeNodeType.GlobalContentRootNode) { Text = "Global Content Files" };

            Root = new List<NodeViewModel>()
            {
                EntityRootNode,
                ScreenRootNode,
                GlobalContentRootNode,
            };

            BookmarkRowHeight = GridLength.Auto;

            PushSearchToContainedObject();

            //this.AddRecursive(ScreenRootNode, 4, 4);
            //this.Title = "TreeListBox (N=" + this.Count + ")";

        }
    }
}
