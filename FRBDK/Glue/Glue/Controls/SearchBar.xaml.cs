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
        public event Action DismissHintTextClicked;

        public event Action EnterPressed;
        public event Action EscapePressed;
        public event Action<Key> ArrowKeyPushed;
        public SearchBar()
        {
            InitializeComponent();
        }


        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.SearchBoxText = string.Empty;
                EscapePressed?.Invoke();
            }
            else if(e.Key == Key.Enter)
            {
                EnterPressed?.Invoke();
            }
        }


        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if(vm != null)
            {
                vm.SearchBoxText = null;
            }
            ClearSearchButtonClicked?.Invoke();
        }

        private void ClearHintTextClickedInternal(object sender, RoutedEventArgs e)
        {
            DismissHintTextClicked?.Invoke();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = false;
        }

        public void FocusTextBox() => SearchTextBox.Focus();

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                ArrowKeyPushed?.Invoke(e.Key);
            }
        }
    }
}
