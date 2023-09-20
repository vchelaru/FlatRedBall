using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels;
using GlueFormsCore.Extensions;
using System;
using System.Windows;
using System.Windows.Input;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views
{
    /// <summary>
    /// Interaction logic for LoadRecentWindow.xaml
    /// </summary>
    public partial class LoadRecentWindow : Window
    {
        LoadRecentViewModel ViewModel => DataContext as LoadRecentViewModel;

        public LoadRecentWindow()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            this.MoveToCursor();

            this.SearchBar.FocusTextBox();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.SelectedItem != null)
            {
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ViewModel.SelectedItem != null)
            {
                DialogResult = true;
            }
        }
    }
}
