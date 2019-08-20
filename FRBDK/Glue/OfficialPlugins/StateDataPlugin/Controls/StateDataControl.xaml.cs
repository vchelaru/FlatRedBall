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
using LinqToVisualTree;

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

    // from:
    // https://stackoverflow.com/questions/5471405/create-datatemplate-in-code-behind
    /// <summary>
    /// Class that helps the creation of control and data templates by using delegates.
    /// </summary>
    public static class TemplateGenerator
    {
        private sealed class _TemplateGeneratorControl :
          ContentControl
        {
            internal static readonly DependencyProperty FactoryProperty = DependencyProperty.Register("Factory", typeof(Func<object>), typeof(_TemplateGeneratorControl), new PropertyMetadata(null, _FactoryChanged));

            private static void _FactoryChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
            {
                var control = (_TemplateGeneratorControl)instance;
                var factory = (Func<object>)args.NewValue;
                control.Content = factory();
            }
        }

        /// <summary>
        /// Creates a data-template that uses the given delegate to create new instances.
        /// </summary>
        public static DataTemplate CreateDataTemplate(Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            var frameworkElementFactory = new FrameworkElementFactory(typeof(_TemplateGeneratorControl));
            frameworkElementFactory.SetValue(_TemplateGeneratorControl.FactoryProperty, factory);

            var dataTemplate = new DataTemplate(typeof(DependencyObject));
            dataTemplate.VisualTree = frameworkElementFactory;
            return dataTemplate;
        }

        /// <summary>
        /// Creates a control-template that uses the given delegate to create new instances.
        /// </summary>
        public static ControlTemplate CreateControlTemplate(Type controlType, Func<object> factory)
        {
            if (controlType == null)
                throw new ArgumentNullException("controlType");

            if (factory == null)
                throw new ArgumentNullException("factory");

            var frameworkElementFactory = new FrameworkElementFactory(typeof(_TemplateGeneratorControl));
            frameworkElementFactory.SetValue(_TemplateGeneratorControl.FactoryProperty, factory);

            var controlTemplate = new ControlTemplate(controlType);
            controlTemplate.VisualTree = frameworkElementFactory;
            return controlTemplate;
        }
    }

    /// <summary>
    /// Interaction logic for StateDataControl.xaml
    /// </summary>
    public partial class StateDataControl : UserControl
    {
        int? CurrentRowIndex
        {
            get
            {
                var selectedItem = DataGridInstance.CurrentItem;
                if(selectedItem != null)
                {
                    return ViewModel.States.IndexOf(selectedItem as StateViewModel);
                }
                return null;
            }
        }

        int? CurrentColumnIndex
        {
            get
            {
                return DataGridInstance.CurrentColumn?.DisplayIndex;
            }
        }

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
            DataGridInstance.PreparingCellForEdit += HandleCellEdit;
            DataGridInstance.CurrentCellChanged += HandleCurrentCellChanged;
        }

        private void HandleCurrentCellChanged(object sender, EventArgs e)
        {
            var selectedItem = DataGridInstance.CurrentItem;

            if(selectedItem != null)
            {
                var currentColumnIndex = CurrentColumnIndex;
                var currentRowIndex = CurrentRowIndex;

                if (currentColumnIndex > -1 && currentRowIndex > -1 && currentColumnIndex != null)
                {
                    FocusTextBoxAt(currentColumnIndex, currentRowIndex);
                }

            }
            //var rowIndex = DataGridInstance.SelectedIndex;
            //var state = ViewModel.States[rowIndex];
            //var value = DataGridInstance.SelectedValue;
            //var path = DataGridInstance.SelectedValuePath;

            //var textBox = DataGridInstance.Descendants<TextBox>()
            //    .OfType<TextBox>()
            //    .ToArray();
            //var cell = DataGridInstance.CurrentItem;


            //var foundTextBox = textBox.FirstOrDefault(item => item.DataContext == cell);

            //int m = 3;
        }

        private bool FocusTextBoxAt(int? currentColumnIndex, int? currentRowIndex)
        {
            var currentRow = DataGridInstance.Descendants<DataGridRow>()
                                    .OfType<DataGridRow>().ToList()[currentRowIndex.Value];


            var childrenOfChildren = currentRow.Descendants<DataGridCell>()
                .OfType<DataGridCell>().ToArray()[currentColumnIndex.Value];

            var textBox = childrenOfChildren.Descendants<TextBox>()
                .OfType<TextBox>()
                .ToArray();

            var focused = false;
            if (textBox.Length > 0)
            {
                textBox[0].Focus();
                focused = true;
            }

            return focused;
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

        private void HandleCellEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            int m = 3;
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
                comboBoxColumn.SelectedItemBinding = new Binding($"Variables[{i}].UiValue")
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
                comboBoxColumn.SelectedItemBinding = new Binding($"Variables[{i}].UiValue")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                column = comboBoxColumn;
            }
            else
            {
                var templateColumn = new DataGridTemplateColumn();
                templateColumn.CellTemplate = TemplateGenerator.CreateDataTemplate(() =>
                {
                    var textBox = new TextBox();

                    //textBox.SetBinding(TextBox.TextProperty, $"Variables[{i}]");
                    {
                        Binding myBinding = new Binding();
                        //myBinding.Source = ViewModel;
                        myBinding.Path = new PropertyPath($"Variables[{i}].UiValue");
                        myBinding.Mode = BindingMode.TwoWay;
                        myBinding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                        BindingOperations.SetBinding(textBox, TextBox.TextProperty, myBinding);
                    }

                    textBox.SetBinding(TextBox.ForegroundProperty, 
                        $"Variables[{i}].{nameof(StateVariableViewModel.TextColor)}");

                    textBox.BorderThickness = new Thickness(0);
                    textBox.BorderBrush = Brushes.Transparent;

                    textBox.LostFocus += (not, used) =>
                    {
                        var textBoxDataContext = textBox.DataContext;
                        ((StateViewModel)textBoxDataContext).Variables[i].IsFocused = false;

                        //(textBox.DataContext as StateVariableViewModel).IsFocused = false;
                        //HandleTextBoxLostFocus(textBox, i);
                    };

                    textBox.GotFocus += (not, used) =>
                    {
                        var textBoxDataContext = textBox.DataContext;
                        (( StateViewModel)textBoxDataContext).Variables[i].IsFocused = true;

                        textBox.SelectAll();
                    };

                    return textBox;
                });

                

                column = templateColumn;
                //var textColumn = new DataGridTextColumn();
                //textColumn.Binding = new Binding($"Variables[{i}]")
                //{
                    //UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                //};
                
                //column = textColumn;
            }



            column.Header = viewModelColumn;
            DataGridInstance.Columns.Add(column);
        }

        private void HandleTextBoxLostFocus(TextBox textBox, int index)
        {
            //var value = ViewModel.States[ViewModel.SelectedIndex].Variables[index];

            //ViewModel.ApplyViewModelVariableToStateAtIndex(
            //    value, index, ViewModel.States[ViewModel.SelectedIndex]);

            //int m = 3;
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
                    if(!string.IsNullOrEmpty(bindingPath))
                    {
                        pathsToDelete.Add(new StateVmPath
                        {
                            ViewModel = rowVm,
                            Path = bindingPath
                        });

                    }

                }


                foreach(var path in pathsToDelete)
                {
                    DeleteValue(path.ViewModel, path.Path);

                }

                e.Handled = true;
            }
            else if(e.Key == Key.Tab)
            {
                var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                var currentRowIndex = CurrentRowIndex;
                var currentColumnIndex = CurrentColumnIndex;
                if(shift && currentRowIndex != null && currentColumnIndex > 0)
                {
                    var focusedTextBox =  FocusTextBoxAt(currentColumnIndex-1, currentRowIndex);
                    e.Handled = focusedTextBox;
                }
            }
            else if(e.Key == Key.Up)
            {
                var currentRowIndex = CurrentRowIndex;
                var currentColumnIndex = CurrentColumnIndex;
                if (currentRowIndex > 0 && currentColumnIndex != null)
                {
                    var focusedTextBox = FocusTextBoxAt(currentColumnIndex, currentRowIndex - 1);
                    e.Handled = focusedTextBox;
                }
            }
            else if (e.Key == Key.Down)
            {
                var currentRowIndex = CurrentRowIndex;
                var currentColumnIndex = CurrentColumnIndex;
                if (currentRowIndex < ViewModel.States.Count -1 && currentColumnIndex != null)
                {
                    var focusedTextBox = FocusTextBoxAt(currentColumnIndex, currentRowIndex + 1);
                    e.Handled = focusedTextBox;
                }
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

                rowVm.Variables[number].Value = null;
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
            else if(column is DataGridTemplateColumn)
            {
                // doesn't exist:
                //binding = ((DataGridTemplateColumn)column).Binding as Binding;
            }

            return binding?.Path?.Path;
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
