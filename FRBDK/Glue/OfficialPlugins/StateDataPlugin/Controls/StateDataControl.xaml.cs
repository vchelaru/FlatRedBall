using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OfficialPlugins.StateDataPlugin.Controls
{
    struct StateVmPath
    {
        public string Path;
        public StateViewModel ViewModel;

        public override string ToString()
        {
            return $"{Path} on {ViewModel?.BackingData?.Name}";
        }
    }
    /// <summary>
    /// Interaction logic for StateDataControl.xaml
    /// </summary>
    public partial class StateDataControl : UserControl
    {
        StateCategoryViewModel ViewModel
        {
            get
            {
                return DataContext as StateCategoryViewModel;
            }
        }

        public StateDataControl()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
            //var nameColumn = CreateColumnForType("string","Name", false) as DataGridTextColumn;
            //nameColumn.Header = "Name";
            
            //DataGridInstance.Columns.Add(nameColumn);

        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        public void RefreshColumns()
        {
            // get rid of all the extra columns
            while(DataGridInstance.Columns.Count > 1)
            {
                DataGridInstance.Columns.RemoveAt(DataGridInstance.Columns.Count - 1);
            }

            for(int i = 0; i < ViewModel.Columns.Count; i++)
            {
                var variable = ViewModel.Element.CustomVariables[i];

                var shouldInclude = ViewModel.IncludedVariables.Contains(variable.Name);

                if(shouldInclude)
                {
                    AddColumnForVariableAtIndex(i, variable);
                }
            }
        }

        private void AddColumnForVariableAtIndex(int i, CustomVariable variable)
        {
            var viewModelColumn = ViewModel.Columns[i];


            DataGridColumn column;

            TypeConverter converter = variable.GetTypeConverter(ViewModel.Element);
            if (converter != null)
            {
                if (converter is TypeConverterWithNone)
                {
                    ((TypeConverterWithNone)converter).IncludeNoneOption = false;
                }

                var comboBoxColumn = new DataGridComboBoxColumn();
                comboBoxColumn.ItemsSource = converter.GetStandardValues();
                comboBoxColumn.SelectedItemBinding = new Binding($"Variables[{i}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                column = comboBoxColumn;
            }
            else if (variable.Type == "bool")
            {
                // I used checkbox but null vs false is not clear (indetermine state), and 
                // I'm not sure users want that, or if they even can. Dropdown is clearer.
                var comboBoxColumn = new DataGridComboBoxColumn();
                // capitalize true/false so they ToString properly
                comboBoxColumn.ItemsSource = new object[] { "", true, false };
                comboBoxColumn.SelectedItemBinding = new Binding($"Variables[{i}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                column = comboBoxColumn;
            }
            else
            {
                var textColumn = new DataGridTextColumn();
                textColumn.Binding = new Binding($"Variables[{i}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                };
                column = textColumn;
            }



            column.Header = viewModelColumn;
            DataGridInstance.Columns.Add(column);
        }

        private void DataGridInstance_KeyDown(object sender, KeyEventArgs e)
        {
            int m = 3;
        }

        private void DataGridInstance_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // in case something changes in deleteRow:
                List<StateVmPath> pathsToDelete = new List<StateVmPath>();

                foreach(var cell in DataGridInstance.SelectedCells)
                {
                    var rowVm = cell.Item as StateViewModel;

                    var column = cell.Column;

                    var bindingPath = GetColumnBindingPath(column);

                    pathsToDelete.Add(new StateVmPath
                    {
                        ViewModel = rowVm,
                        Path = bindingPath
                    });

                }


                foreach(var path in pathsToDelete)
                {
                    DeleteValue(path.ViewModel, path.Path);
                }

                e.Handled = true;
            }
        }

        private void DeleteValue(StateViewModel rowVm, string bindingPath)
        {
            if(bindingPath == nameof(StateViewModel.Name))
            {
                rowVm.Name = null;
            }
            else if(bindingPath.StartsWith(nameof(StateViewModel.Variables) + '['))
            {
                var numberIndex = bindingPath.IndexOf('[') + 1;
                var endingIndex = bindingPath.IndexOf(']');

                var numberString = bindingPath.Substring(numberIndex, endingIndex - numberIndex);

                var number = int.Parse(numberString);

                rowVm.Variables[number] = null;
            }
        }

        private string GetColumnBindingPath(DataGridColumn column)
        {
            Binding binding = null;
            if(column is DataGridComboBoxColumn)
            {
                binding = ((DataGridComboBoxColumn)column).SelectedItemBinding as Binding;
            }
            else if (column is DataGridCheckBoxColumn)
            {
                binding = ((DataGridCheckBoxColumn)column).Binding as Binding;
            }
            else if(column is DataGridTextColumn)
            {
                binding = ((DataGridTextColumn)column).Binding as Binding;
            }

            return binding.Path.Path;
        }

        private void ExcludeButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ExcludeSelected();

            RefreshColumns();
        }

        private void IncludeButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModel.IncludedSelected();

            RefreshColumns();
        }

        private void ExpandVariableManagementClick(object sender, RoutedEventArgs e)
        {
            ViewModel.VariableManagementVisibility = Visibility.Visible;
        }

        private void CollapseVariableManagementClick(object sender, RoutedEventArgs e)
        {
            ViewModel.VariableManagementVisibility = Visibility.Collapsed;

        }
    }
}
