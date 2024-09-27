using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LinqToVisualTree;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace OfficialPlugins.StateDataPlugin.Controls
{
    #region Classes/Structs
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

    #endregion

    /// <summary>
    /// Interaction logic for StateDataControl.xaml
    /// </summary>
    public partial class StateDataControl : UserControl
    {
        #region Fields/Properties

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

        int? CurrentColumnIndex => DataGridInstance.CurrentColumn?.DisplayIndex;

        StateCategoryViewModel ViewModel => DataContext as StateCategoryViewModel;

        #endregion

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

                if (currentColumnIndex.HasValue && currentColumnIndex > -1 && currentRowIndex > -1)
                {
                    FocusTextBoxAt(currentColumnIndex, currentRowIndex);
                }

            }

            ViewModel.SelectedState = DataGridInstance.CurrentItem as StateViewModel;
            if(ViewModel.SelectedState == null)
            {
                ViewModel.SelectedIndex  = -1;
            }
            else
            {
                ViewModel.SelectedIndex = ViewModel.States.IndexOf(ViewModel.SelectedState);
            }
        }

        private bool FocusTextBoxAt(int? currentColumnIndex, int? currentRowIndex)
        {
            var rows = DataGridInstance.Descendants<DataGridRow>()
                                    .OfType<DataGridRow>().ToList();

            // It's possible that the rows may not all exist due to virtualization, so we have to find the row with the matching data context
            var dataToMatch = ViewModel.States[currentRowIndex.Value];

            var currentRow = rows.FirstOrDefault(item => item.DataContext == dataToMatch);

            /////////////Early Out///////////////////
            if(currentRow == null)
            {
                return false;
            }
            ///////////End Early Out/////////////////


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
            if(e.OldValue is StateCategoryViewModel oldVm)
            {
                oldVm.PropertyChanged -= HandleViewModelPropertyChanged;
            }
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;

            if(propertyName == nameof(ViewModel.SelectedState))
            {
                StateSave highlightedState = ViewModel.SelectedState?.BackingData;

                if(highlightedState != null)
                {
                    // Broadcast that this changed to the level editor plugin:
                    PluginManager.CallPluginMethod("Glue Compiler", "ShowState",
                        new object[]
                        {
                            highlightedState.Name,
                            GlueState.Self.CurrentStateSaveCategory?.Name
                        });

                }
            }
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

                var variable =
                    // This may not match 1:1 if we exclude variables
                    ViewModel.Element.CustomVariables.Find(item => item.Name == ViewModel.Columns[i]);
                    //ViewModel.Element.CustomVariables[i];

                var shouldInclude = ViewModel.IncludedVariables.Contains(variable.Name);

                if(shouldInclude)
                {
                    AddColumnForVariableAtIndex(i, variable);
                }
            }
        }

        private void HandleCellEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
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

                // TextBox uses a cell template so it can adjust the brush
                // The combo box doesn't so we'll have to deal with it....

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

                    MultiDataTrigger multiTrigger = new()
                    {
                        Conditions =
                        {
                            new()
                            {
                                Binding = new Binding($"Variables[{i}].{nameof(StateVariableViewModel.IsDefaultValue)}"),
                                Value = true
                            },
                            new()
                            {
                                Binding = new Binding($"Variables[{i}].{nameof(StateVariableViewModel.IsFocused)}"),
                                Value = false
                            }
                        },
                        Setters = { new Setter(TextBox.ForegroundProperty, StateVariableViewModel.IsDefaultBrush) }
                    };

                    textBox.Style = new (typeof(TextBox),
                        (Style)Application.Current.FindResource(typeof(TextBox)))
                    {
                        Triggers = { multiTrigger }
                    };
                    textBox.Background = Brushes.Transparent;
                    
                    textBox.BorderThickness = new Thickness(0);
                    textBox.BorderBrush = Brushes.Transparent;

                    textBox.LostFocus += (not, used) =>
                    {
                        var textBoxDataContext = textBox.DataContext;

                        var stateViewModel = textBoxDataContext as StateViewModel;

                        if(stateViewModel != null)
                        {
                            stateViewModel.Variables[i].IsFocused = false;

                        }

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
            }



            column.Header = viewModelColumn;
            DataGridInstance.Columns.Add(column);
        }

        private void DataGridInstance_KeyDown(object sender, KeyEventArgs e)
        {
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


            // Update November 20, 2021
            // When deleting a state, the grid doesn't update. Not sure why. I
            // will eventually want to move to an external editor for states anyway,
            // so I'll just "hack" a solution by resetting the VM on a delete:
            var dataContext = this.DataContext;
            this.DataContext = null;
            this.DataContext = dataContext;
        }

        private static string GetColumnBindingPath(DataGridColumn column)
        {
            Binding binding = null;
            switch (column)
            {
                case DataGridComboBoxColumn boxColumn:
                    binding = boxColumn.SelectedItemBinding as Binding;
                    break;
                case DataGridCheckBoxColumn boxColumn:
                    binding = boxColumn.Binding as Binding;
                    break;
                case DataGridTextColumn textColumn:
                    binding = textColumn.Binding as Binding;
                    break;
                case DataGridTemplateColumn:
                    // doesn't exist:
                    //binding = ((DataGridTemplateColumn)column).Binding as Binding;
                    break;
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

    }
}
