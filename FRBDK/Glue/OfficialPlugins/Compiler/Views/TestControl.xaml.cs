using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    public partial class TestControl : UserControl
    {
        TestViewModel ViewModel => DataContext as TestViewModel;
        public TestControl()
        {
            var viewModel = new TestViewModel();
            this.DataContext = viewModel;

            viewModel.PropertyChanged += HandlePropertyChanged;

            InitializeComponent();
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.Health):


                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Health--;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.Health++;

        }
    }
}
