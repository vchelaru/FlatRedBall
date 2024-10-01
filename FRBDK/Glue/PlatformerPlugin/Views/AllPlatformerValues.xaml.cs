using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.PlatformerPlugin.Data;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Utilities;
using Newtonsoft.Json;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for AllPlatformerValues.xaml
    /// </summary>
    public partial class AllPlatformerValues : UserControl
    {
        PlatformerEntityViewModel ViewModel =>
            DataContext as PlatformerEntityViewModel;

        public AllPlatformerValues()
        {
            InitializeComponent();
        }

        private void AddEmptyPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Unnamed";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddDefaultGroundPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Ground";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddDefaultAirPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Air";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }



        private void AddPlatformerValues(string predefinedName)
        {
            var values = PredefinedPlatformerValues.GetValues(predefinedName);

            string newItemName = predefinedName;
            while(ViewModel.PlatformerValues.Any(item=>item.Name == newItemName))
            {
                newItemName = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(newItemName);
            }

            // Update August 31, 2021
            // New objects should use the
            // override so that they aren't
            // considered inherited values when
            // added to a derived entity.
            values.InheritOrOverwrite = GlueCommon.Models.InheritOrOverwrite.Overwrite;

            values.Name = newItemName;

            ViewModel.PlatformerValues.Add(values);

            // This adds new items to the dropdowns:
            GlueCommands.Self.RefreshCommands.RefreshVariables();
        }



        private void PlatformerValuesXClick(object sender, RoutedEventArgs e)
        {
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            bool contains = ViewModel.PlatformerValues.Contains(valuesViewModel);

            if (contains)
            {
                ViewModel.PlatformerValues.Remove(valuesViewModel);
            }
        }

        private void PlatformerValuesView_MoveUpClicked(object sender, RoutedEventArgs args)
        {
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            var index = ViewModel.PlatformerValues.IndexOf(valuesViewModel);

            if(index > 0)
            {
                ViewModel.PlatformerValues.Move(index, index - 1);
            }
        }

        private void PlatformerValuesView_MoveDownClicked(object sender, RoutedEventArgs args)
        {
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            var index = ViewModel.PlatformerValues.IndexOf(valuesViewModel);

            if (index > -1 && index < ViewModel.PlatformerValues.Count-1)
            {
                ViewModel.PlatformerValues.Move(index, index + 1);
            }
        }

        private void PlatformerValuesView_DuplicateClicked(object sender, RoutedEventArgs args)
        {
            // todo....
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            var newInstance = JsonConvert.DeserializeObject<PlatformerValuesViewModel>( JsonConvert.SerializeObject(valuesViewModel));
            if(!StringFunctions.HasNumberAtEnd(newInstance.Name))
            {
                newInstance.Name += 2;
            }
            while (ViewModel.PlatformerValues.Any(item => item.Name == newInstance.Name))
            {
                newInstance.Name = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(newInstance.Name);
            }
            ViewModel.PlatformerValues.Add(newInstance);
        }

    }
}
