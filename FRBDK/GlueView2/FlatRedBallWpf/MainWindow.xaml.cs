using System;
using System.Windows;

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

            Loaded += OnLoaded;
        }

        private void OnLoaded(Object sender, RoutedEventArgs routedEventArgs)
        {
            _mainGame = new Game1(flatRedBallControl);
        }
    }
}