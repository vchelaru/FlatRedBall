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

namespace GlueFormsCore.ViewModels
{

    public class TabContainerViewModel : ViewModel
    {
        public PluginTabPage this[int index]
        {
            get => Tabs[index];
            set => Tabs[index] = value;
        }
        public void Add(PluginTabPage item) => Tabs.Add(item);
        public void Remove(PluginTabPage item) => Tabs.Remove(item);

        public PluginTabPage? SelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public TabLocation Location { get; init; }

        public int Count => Tabs.Count;
        public ObservableCollection<PluginTabPage> Tabs { get; private set; } = new ObservableCollection<PluginTabPage>();

        public Dictionary<string, PluginTabPage> TabsForTypes { get; private set; } = new Dictionary<string, PluginTabPage>();

        public TabContainerViewModel()
        {
            Tabs.CollectionChanged += (_, _) =>
            {
                NotifyPropertyChanged(nameof(Count));
                SelectedTab ??= Tabs.FirstOrDefault();
            };
        }

        public void SetTabForCurrentType(PluginTabPage tab)
        {
            var treeNode = GlueState.Self.CurrentTreeNode;
            var selectedType = treeNode?.Tag?.GetType()?.Name ?? treeNode?.Text;

            if(selectedType != null)
            {
                TabsForTypes[selectedType] = tab;
            }
        }

        public void ShowMostRecentTabFor(string typeName)
        {
            if (Count < 2)
            {
                return;
            }

            if (!TabsForTypes.TryGetValue(typeName, out PluginTabPage tab)) 
            { 
                List<PluginTabPage> ordered = Tabs
                    .OrderBy(item => !item.IsPreferredDisplayerForType(typeName))
                    .ThenByDescending(item => item.LastTimeClicked)
                    .ToList();

                if (ordered[0].LastTimeClicked != ordered[1].LastTimeClicked)
                {
                    tab = ordered[0];
                }
            }

            SelectedTab = tab;
        }
    }

    public class TabControlViewModel : ViewModel
    {
        public static bool IsRecordingSelection { get; set; } = true;
        #region Fields/Properties

        public TabContainerViewModel TopTabItems { get; } = new() { Location = TabLocation.Top };
        public TabContainerViewModel BottomTabItems { get; } = new () { Location = TabLocation.Bottom };
        public TabContainerViewModel LeftTabItems { get; } = new () { Location = TabLocation.Left };
        public TabContainerViewModel RightTabItems { get; } =  new () { Location = TabLocation.Right };
        public TabContainerViewModel CenterTabItems { get; } = new () { Location = TabLocation.Center };

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
                int length = tab.Location == TabLocation.Left ? 230 : 200;
                gridLength = new GridLength(length, GridUnitType.Pixel);
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
            string selectedType = selectedTreeNode?.Tag?.GetType().Name ?? selectedTreeNode?.Text;

            foreach (var (_, vm) in Containers)
            {
                if(selectedTreeNode != null)
                {
                    vm.ShowMostRecentTabFor(selectedType);
                }
            }
        }
    }
}
