using FlatRedBall.Glue.ViewModels;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for SearchBar.xaml
    /// </summary>
    public partial class SearchBar : UserControl
    {
        ISearchBarViewModel ViewModel => DataContext as ISearchBarViewModel;
        public event Action ClearSearchButtonClicked;
        public SearchBar()
        {
            InitializeComponent();
        }


        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.SearchBoxText = string.Empty;
            }
        }


        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchButtonClicked?.Invoke();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = false;
        }


    }
}
