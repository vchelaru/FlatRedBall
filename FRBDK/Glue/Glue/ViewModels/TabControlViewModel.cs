using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels
{

    public class TabContainerViewModel
    {
        public PluginTabPage this[int index]
        {
            get => Tabs[index];
            set => Tabs[index] = value;
        }
        public void Add(PluginTabPage item) => Tabs.Add(item);
        public void Remove(PluginTabPage item) => Tabs.Remove(item);
        public int Count => Tabs.Count;
        public ObservableCollection<PluginTabPage> Tabs { get; private set; } = new ObservableCollection<PluginTabPage>();

        public Dictionary<Type, PluginTabPage> TabsForTypes { get; private set; } = new Dictionary<Type, PluginTabPage>();
        public void SetTabForCurrentType(PluginTabPage tab)
        {
            var selectedType = GlueState.Self.CurrentTreeNode.Tag?.GetType();

            if(selectedType != null)
            {
                TabsForTypes[selectedType] = tab;
            }
        }
    }

    public class TabControlViewModel : ViewModel
    {
        #region Fields/Properties

        public TabContainerViewModel TopTabItems { get; private set; } =    new TabContainerViewModel();
        public TabContainerViewModel BottomTabItems { get; private set; } = new TabContainerViewModel();
        public TabContainerViewModel LeftTabItems { get; private set; } =   new TabContainerViewModel();
        public TabContainerViewModel RightTabItems { get; private set; } =  new TabContainerViewModel();
        public TabContainerViewModel CenterTabItems { get; private set; } = new TabContainerViewModel();

        public PluginTabPage TopSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage BottomSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage LeftSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage RightSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage CenterSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public GridLength TopSplitterHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

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

        public GridLength LeftSplitterWidth
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength BottomSplitterHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength BottomPanelHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        #endregion

        public TabControlViewModel()
        {
            TopTabItems.Tabs.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(TopTabItems));
            BottomTabItems.Tabs.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(BottomTabItems));
            LeftTabItems.Tabs.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(LeftTabItems));
            RightTabItems.Tabs.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(RightTabItems));
            CenterTabItems.Tabs.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(CenterTabItems));

            this.PropertyChanged += (sender, args) => HandlePropertyChanged(args.PropertyName);

            ExpandAndCollapseColumnAndRowWidths();
        }

        private void HandlePropertyChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(TopTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (TopTabItems.Count > 0 && TopSelectedTab == null)
                    {
                        TopSelectedTab = TopTabItems[0];
                    }
                    break;
                case nameof(BottomTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (BottomTabItems.Count > 0 && BottomSelectedTab == null)
                    {
                        BottomSelectedTab = BottomTabItems[0];
                    }
                    break;
                case nameof(LeftTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (LeftTabItems.Count > 0 && LeftSelectedTab == null)
                    {
                        LeftSelectedTab = LeftTabItems[0];
                    }
                    break;
                case nameof(RightTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (RightTabItems.Count > 0 && RightSelectedTab == null)
                    {
                        RightSelectedTab = RightTabItems[0];
                    }
                    break;
                case nameof(CenterTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (CenterTabItems.Count > 0 && CenterSelectedTab == null)
                    {
                        CenterSelectedTab = CenterTabItems[0];
                    }
                    break;
            }
        }

        private void ExpandAndCollapseColumnAndRowWidths()
        {
            var shouldShrinkLeft = LeftTabItems.Count == 0 && LeftSplitterWidth.Value > 0;
            var shouldExpandLeft = LeftTabItems.Count > 0 && LeftSplitterWidth.Value == 0;

            var shouldShrinkTop = TopTabItems.Count == 0 && TopSplitterHeight.Value > 0;
            var shouldExpandTop = TopTabItems.Count > 0 && TopSplitterHeight.Value == 0;

            var shouldShrinkBottom = BottomTabItems.Count == 0 && BottomSplitterHeight.Value > 0;
            var shouldExpandBottom = BottomTabItems.Count > 0 && BottomSplitterHeight.Value == 0;

            if (shouldShrinkLeft)
            {
                LeftSplitterWidth = new GridLength(0);
                LeftPanelWidth = new GridLength(0, GridUnitType.Pixel);
            }
            else if (shouldExpandLeft)
            {
                LeftSplitterWidth = new GridLength(4);
                LeftPanelWidth = new GridLength(230, GridUnitType.Pixel);
                //LeftPanelWidth = new GridLength(1, GridUnitType.Star);
            }

            if (shouldShrinkTop)
            {
                TopSplitterHeight = new GridLength(0);
                TopPanelHeight = new GridLength(0);
            }
            else if (shouldExpandTop)
            {
                TopSplitterHeight = new GridLength(4);
                //TopPanelHeight = new GridLength(1, GridUnitType.Star);
                TopPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

            if (shouldShrinkBottom)
            {
                BottomSplitterHeight = new GridLength(0);
                BottomPanelHeight = new GridLength(0);
            }
            else if (shouldExpandBottom)
            {
                BottomSplitterHeight = new GridLength(4);
                //BottomPanelHeight = new GridLength(1, GridUnitType.Star);
                BottomPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

        }

        internal void UpdateToSelection(ITreeNode selectedTreeNode)
        {
            var selectedType = selectedTreeNode.Tag?.GetType();

            ShowMostRecentTabFor(TopTabItems,
                (item) => TopSelectedTab = item, 
                selectedType);

            ShowMostRecentTabFor(BottomTabItems,
                (item) => BottomSelectedTab = item, 
                selectedType);

            ShowMostRecentTabFor(LeftTabItems,
                (item) => LeftSelectedTab = item, 
                selectedType);

            ShowMostRecentTabFor(CenterTabItems,
                (item) => CenterSelectedTab = item, 
                selectedType);

            ShowMostRecentTabFor(RightTabItems,
                (item) => RightSelectedTab = item, 
                selectedType);
        }


        private static void ShowMostRecentTabFor(TabContainerViewModel items, Action<PluginTabPage> action, Type type)
        {
            if (items.Count > 1)
            {
                // Is there a tab for this type?
                if(type != null && items.TabsForTypes.ContainsKey(type))
                {
                    action(items.TabsForTypes[type]);

                }
                else
                {
                    var ordered = items.Tabs.OrderByDescending(item => item.LastTimeClicked).ToList();

                    if (ordered[0].LastTimeClicked != ordered[1].LastTimeClicked)
                    {
                        action(ordered[0]);
                    }
                }
            }

        }
    }

}
