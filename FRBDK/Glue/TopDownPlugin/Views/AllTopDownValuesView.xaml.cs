using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TopDownPlugin.Data;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for MovementValuesView.xaml
    /// </summary>
    public partial class AllTopDownValuesView : UserControl
    {
        TopDownEntityViewModel ViewModel =>
            DataContext as TopDownEntityViewModel;

        public AllTopDownValuesView()
        {
            InitializeComponent();
        }

        private void AddDefaultValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Default";
            AddTopDownValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddTopDownValues(string predefinedName)
        {
            var values = PredefinedTopDownValues.GetValues(predefinedName);

            string newItemName = predefinedName;
            while (ViewModel.TopDownValues.Any(item => item.Name == newItemName))
            {
                newItemName = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(newItemName);
            }

            // See MainControl.Xaml.cs for platformer to see a discussion about whether this should use Overwrite as its default behavior instead of inherit....

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
