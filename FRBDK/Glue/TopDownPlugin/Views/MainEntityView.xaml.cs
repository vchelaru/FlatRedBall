using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TopDownPlugin.Data;
using TopDownPlugin.Models;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainEntityView.xaml
    /// </summary>
    public partial class MainEntityView : UserControl
    {
        TopDownEntityViewModel ViewModel =>
            DataContext as TopDownEntityViewModel;

        public MainEntityView()
        {
            InitializeComponent();
        }

        private void AddEmptyTopDownValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Unnamed";
            AddTopDownValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddTopDownValues(string predefinedName)
        {
            var values = PredefinedTopDownValues.GetValues(predefinedName);

            string newItemName = predefinedName;
            while(ViewModel.TopDownValues.Any(item => item.Name == newItemName))
            {
                newItemName = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(newItemName);
            }

            values.Name = newItemName;

            ViewModel.TopDownValues.Add(values);
        }

        private void TopDownValuesXClick(object sender, RoutedEventArgs e)
        {
            var valuesViewModel = (sender as UserControl).DataContext as TopDownValuesViewModel;

            bool contains = ViewModel.TopDownValues.Contains(valuesViewModel);

            if (contains)
            {
                ViewModel.TopDownValues.Remove(valuesViewModel);
            }
        }
    }
}
