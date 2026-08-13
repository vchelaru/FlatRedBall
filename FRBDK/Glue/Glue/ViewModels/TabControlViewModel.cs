using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using FlatRedBall.Glue.SaveClasses;

namespace GlueFormsCore.ViewModels
{

    public class TabContainerViewModel : ViewModel
    {
        public PluginTab this[int index]
        {
            get => Tabs[index];
            set => Tabs[index] = value;
        }
        public void Add(PluginTab item) => Tabs.Add(item);
        public void Remove(PluginTab item) => Tabs.Remove(item);

        public PluginTab SelectedTab
        {
            get => Get<PluginTab>();
            set => Set(value);
        }

        [DependsOn(nameof(Count))]
        public Visibility Visibility => Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        public TabLocation Location { get; init; }

        public int Count => Tabs.Count;
        public ObservableCollection<PluginTab> Tabs { get; private set; } = new ObservableCollection<PluginTab>();

        // Per-node-type tab memory (issue #2068). Keyed by type name rather than a single global
        // "most recently clicked" timestamp (which is what issue #1593 actually broke - see
        // ShowMostRecentTabFor) so a tab manually chosen while viewing one node type never overrides
        // the default/bookmark tab for an unrelated type.
        public Dictionary<string, PluginTab> TabsForTypes { get; private set; } = new Dictionary<string, PluginTab>();

        public TabContainerViewModel()
        {
            Tabs.CollectionChanged += OnTabsCollectionChanged;
        }

        private void OnTabsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(Count));
            e.NewItems?.OfType<PluginTab>().ToList().ForEach(tab => tab.ParentContainer = this);
            if (Count == 1)
            {
                SelectedTab = Tabs.FirstOrDefault();
            }
        }

        public void SetTabForCurrentType(PluginTab tab)
        {
            var treeNode = GlueState.Self.CurrentTreeNode;
            var selectedType = treeNode?.Tag?.GetType()?.Name ?? treeNode?.Text;

            if (selectedType != null)
            {
                TabsForTypes[selectedType] = tab;
            }
        }

        public void ShowMostRecentTabFor(string typeName)
        {
            if (Count == 0 || typeName is null)
            {
                return;
            }

            SelectedTab = TabsForTypes.TryGetValue(typeName, out PluginTab tab)
                ? tab
                : Tabs.OrderByDescending(item => item.IsPreferredDisplayerForType(typeName))
                    .ThenByDescending(item => item.LastTimeClicked)
                    .First();
        }
    }

    public class TabControlViewModel : ViewModel
    {
        public static bool IsRecordingSelection { get; set; } = true;
        #region Fields/Properties

        public TabContainerViewModel TopTabItems { get; } = new() { Location = TabLocation.Top };
        public TabContainerViewModel BottomTabItems { get; } = new() { Location = TabLocation.Bottom };
        public TabContainerViewModel LeftTabItems { get; } = new() { Location = TabLocation.Left };
        public TabContainerViewModel RightTabItems { get; } = new() { Location = TabLocation.Right };
        public TabContainerViewModel CenterTabItems { get; } = new() { Location = TabLocation.Center };

        public GridLength TopPanelHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength LeftPanelWidth
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength RightPanelWidth
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength BottomPanelHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        [DependsOn(nameof(TopTabItems))]
        public Visibility TopSplitterVisibility => TopTabItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        [DependsOn(nameof(RightTabItems))]
        public Visibility RightSplitterVisibility => RightTabItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        [DependsOn(nameof(BottomTabItems))]
        public Visibility BottomSplitterVisibility => BottomTabItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        #endregion

        private IReadOnlyDictionary<string, TabContainerViewModel> Containers { get; }

        public TabControlViewModel()
        {
            Containers = new Dictionary<string, TabContainerViewModel>
            {
                { nameof(TopTabItems), TopTabItems },
                { nameof(BottomTabItems), BottomTabItems },
                { nameof(LeftTabItems), LeftTabItems },
                { nameof(RightTabItems), RightTabItems },
                { nameof(CenterTabItems), CenterTabItems }
            };

            foreach (var (name, vm) in Containers)
            {
                vm.Tabs.CollectionChanged += (_, args) => AdjustGrid(vm, args);

                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(TabContainerViewModel.Count))
                    {
                        NotifyPropertyChanged(name);
                    }
                };
            };
        }

        private void AdjustGrid(TabContainerViewModel tab, NotifyCollectionChangedEventArgs args)
        {
            GridLength? gridLength = null;

            if (args.NewItems is not null && tab.Count == 1)
            {
                double? length = tab.Location switch
                {
                    TabLocation.Left => GlueState.Self.GlueSettingsSave.LeftTabWidthPixels,
                    TabLocation.Right => GlueState.Self.GlueSettingsSave.RightTabWidthPixels,
                    TabLocation.Bottom => GlueState.Self.GlueSettingsSave.BottomTabHeightPixels,
                    _ => null
                };

                // A saved width of 0 (or less) is not a deliberate preference - it can only get persisted
                // by closing the app while a panel is transiently empty (see AdjustGrid's `count == 0`
                // branch below, and GitHub issue #2043: plugins like the Properties panel add a tab then
                // immediately hide it again during their own StartUp, before anything is selected). Treat
                // it the same as a missing value so the panel recovers to a usable width instead of
                // staying collapsed to the MinPanelConverter floor forever.
                gridLength = new GridLength(length is > 0 ? length.Value : 220, GridUnitType.Pixel);
            }
            else if (tab.Count == 0)
            {
                gridLength = new GridLength(0, GridUnitType.Pixel);
            }

            if (gridLength is not { } gl) return;

            switch (tab.Location)
            {
                case TabLocation.Left:
                    LeftPanelWidth = gl;
                    break;
                case TabLocation.Right:
                    RightPanelWidth = gl;
                    break;
                case TabLocation.Top:
                    TopPanelHeight = gl;
                    break;
                case TabLocation.Bottom:
                    BottomPanelHeight = gl;
                    break;
            }
        }

        internal void UpdateToSelection(ITreeNode selectedTreeNode)
        {
            if(selectedTreeNode == null)
            {
                return;
            }
            string selectedType = selectedTreeNode.Tag?.GetType().Name ?? selectedTreeNode.Text;

            foreach (var vm in Containers.Values)
            {
                vm.ShowMostRecentTabFor(selectedType);
            }
        }
    }
}
