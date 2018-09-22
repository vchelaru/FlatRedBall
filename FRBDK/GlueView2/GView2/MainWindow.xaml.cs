using GlueView2.AppState;
using GlueView2.Plugin;
using System;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.AvalonDock.Layout;

namespace FlatRedBallWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game1 _mainGame;

        public MainWindow()
        {
            InitializeComponent();
            GlueViewState.Self.MainWindow = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(Object sender, RoutedEventArgs routedEventArgs)
        {
            var flatRedBallControl = new FlatRedBallControl();
            _mainGame = new Game1(flatRedBallControl);
            var frbTab = AddTab(flatRedBallControl);
            frbTab.Title = "Game";

            StackPanel stackPanel = new StackPanel();

            stackPanel.Children.Add(new Button { Content = "asdf" });
            stackPanel.Children.Add(new Button { Content = "asdf2" });
            stackPanel.Children.Add(new Button { Content = "asdf3" });
            stackPanel.Children.Add(new Button { Content = "asdf4" });

            AddTab(stackPanel);

            PluginManager.Self.Initialize();

        }

        internal LayoutAnchorable AddTab(UIElement element)
        {
            LayoutAnchorablePane pane = new LayoutAnchorablePane();
            pane.DockWidth = new GridLength(200);
            LayoutPanel.Children.Add(pane);

            LayoutAnchorable anchorable = new LayoutAnchorable();
            anchorable.AutoHideWidth = 240;
            pane.Children.Add(anchorable);

            anchorable.Content = element;

            return anchorable;
        }
    }
}