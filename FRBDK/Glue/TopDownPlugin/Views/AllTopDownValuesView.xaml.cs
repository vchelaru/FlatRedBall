using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
            // since this is brand new, make it overwrite:
            values.InheritOrOverwrite = GlueCommon.Models.InheritOrOverwrite.Overwrite;

            values.Name = newItemName;

            ViewModel.TopDownValues.Add(values);

            // This adds new items to the dropdowns:
            GlueCommands.Self.RefreshCommands.RefreshVariables();
        }


        private void TopDownValuesXClick(object sender, RoutedEventArgs e)
        {
            var valuesViewModel = (sender as UserControl).DataContext as TopDownValuesViewModel;

            if (ViewModel.TopDownValues.Contains(valuesViewModel))
            {
                ViewModel.TopDownValues.Remove(valuesViewModel);
            }
        }
    }
}
