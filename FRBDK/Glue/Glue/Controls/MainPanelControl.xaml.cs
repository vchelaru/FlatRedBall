using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlueFormsCore.Controls
{

    #region TabControlViewModel - migrate this to its own file?
    public class TabControlViewModel : ViewModel
    {
        public ObservableCollection<PluginTabPage> TopTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> BottomTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> LeftTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> RightTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> CenterTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();

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

        public TabControlViewModel()
        {
            TopTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(TopTabItems));
            BottomTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(BottomTabItems));
            LeftTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(LeftTabItems));
            RightTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(RightTabItems));
            CenterTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(CenterTabItems));

            this.PropertyChanged += (sender, args) => HandlePropertyChanged(args.PropertyName);

            RefreshColumnAndRowWidths();
        }

        private void HandlePropertyChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(TopTabItems):
                    RefreshColumnAndRowWidths();
                    if(TopTabItems.Count > 0 && TopSelectedTab == null)
                    {
                        TopSelectedTab = TopTabItems[0];
                    }
                    break;
                case nameof(BottomTabItems):
                    RefreshColumnAndRowWidths();
                    if (BottomTabItems.Count > 0 && BottomSelectedTab == null)
                    {
                        BottomSelectedTab = BottomTabItems[0];
                    }
                    break;
                case nameof(LeftTabItems):
                    RefreshColumnAndRowWidths();
                    if (LeftTabItems.Count > 0 && LeftSelectedTab == null)
                    {
                        LeftSelectedTab = LeftTabItems[0];
                    }
                    break;
                case nameof(RightTabItems):
                    RefreshColumnAndRowWidths();
                    if (RightTabItems.Count > 0 && RightSelectedTab == null)
                    {
                        RightSelectedTab = RightTabItems[0];
                    }
                    break;
                case nameof(CenterTabItems):
                    RefreshColumnAndRowWidths();
                    if (CenterTabItems.Count > 0 && CenterSelectedTab == null)
                    {
                        CenterSelectedTab = CenterTabItems[0];
                    }
                    break;
            }
        }

        private void RefreshColumnAndRowWidths()
        {
            var shouldShrinkLeft = LeftTabItems.Count == 0 && LeftSplitterWidth.Value > 0;
            var shouldExpandLeft = LeftTabItems.Count > 0 && LeftSplitterWidth.Value == 0;

            var shouldShrinkTop = TopTabItems.Count == 0 && TopSplitterHeight.Value > 0;
            var shouldExpandTop = TopTabItems.Count > 0 && TopSplitterHeight.Value == 0;

            var shouldShrinkBottom = BottomTabItems.Count == 0 && BottomSplitterHeight.Value > 0;
            var shouldExpandBottom = BottomTabItems.Count > 0 && BottomSplitterHeight.Value == 0;

            if(shouldShrinkLeft)
            {
                LeftSplitterWidth = new GridLength(0);
                LeftPanelWidth = new GridLength(0);
            }
            else if(shouldExpandLeft)
            {
                LeftSplitterWidth = new GridLength(4);
                LeftPanelWidth = new GridLength(200, GridUnitType.Pixel);
                //LeftPanelWidth = new GridLength(1, GridUnitType.Star);
            }

            if(shouldShrinkTop)
            {
                TopSplitterHeight = new GridLength(0);
                TopPanelHeight = new GridLength(0);
            }
            else if(shouldExpandTop)
            {
                TopSplitterHeight = new GridLength(4);
                //TopPanelHeight = new GridLength(1, GridUnitType.Star);
                TopPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

            if(shouldShrinkBottom)
            {
                BottomSplitterHeight = new GridLength(0);
                BottomPanelHeight = new GridLength(0);
            }
            else if(shouldExpandBottom)
            {
                BottomSplitterHeight = new GridLength(4);
                //BottomPanelHeight = new GridLength(1, GridUnitType.Star);
                BottomPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

        }
    }

    #endregion

    /// <summary>
    /// Interaction logic for MainPanelControl.xaml
    /// </summary>
    public partial class MainPanelControl : UserControl
    {
        public static string AppTheme = "Light";
        public static ResourceDictionary ResourceDictionary { get; private set; }

        public MainPanelControl()
        {
            InitializeComponent();

            this.Resources.MergedDictionaries[0].Source =
                new Uri($"/Themes/{AppTheme}.xaml", UriKind.Relative);


            Style style = this.TryFindResource("UserControlStyle") as Style;
            if (style != null)
            {
                this.Style = style;
            }

            ResourceDictionary = Resources;

            var viewModel = new TabControlViewModel();
            this.DataContext = viewModel;

            TopTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.TopTabItems));
            TopTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.TopSelectedTab));

            BottomTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.BottomTabItems));
            BottomTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.BottomSelectedTab));

            LeftTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.LeftTabItems));
            LeftTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.LeftSelectedTab));

            RightTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.RightTabItems));
            RightTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.RightSelectedTab));

            CenterTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.CenterTabItems));
            CenterTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.CenterSelectedTab));

            PluginManager.SetTabs(viewModel);
            PluginManager.SetToolbarTray(ToolbarControl);


            //TopTabControl
        }
    }
}
