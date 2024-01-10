using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Extensions;
using System.Windows;
using System.Windows.Input;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for NewObjectTypeSelectionControlWpf.xaml
    /// </summary>
    public partial class NewObjectTypeSelectionControlWpf : Window
    {
        AddObjectViewModel ViewModel => DataContext as AddObjectViewModel;

        public NewObjectTypeSelectionControlWpf()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;

            Loaded += (_, _) =>
            {
                SearchTextBox.Focus();

                GlueCommands.Self.DialogCommands.MoveToCursor(this);

            };
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.StrongSelect += () => DialogResult = true;

                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.FilteredItems):
                    if(ViewModel.SelectedItem != null)
                    {
                        MainListBox.ScrollIntoView(ViewModel.SelectedItem);
                    }
                    break;
            }
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int? selectedIndex = null;
            void GetSelectedIndex()
            {
                if (ViewModel.SelectedItem != null)
                {
                    selectedIndex = ViewModel.FilteredItems.IndexOf(ViewModel.SelectedItem);
                }
            }

            if (e.Key == Key.Down)
            {
                GetSelectedIndex();

                if (selectedIndex == null)
                {
                    selectedIndex = 0;
                }
                else
                {
                    selectedIndex++;
                    if (selectedIndex >= ViewModel.FilteredItems.Count)
                    {
                        selectedIndex = 0;
                    }
                }
            }
            else if (e.Key == Key.Up)
            {
                GetSelectedIndex();

                if (selectedIndex == null)
                {
                    selectedIndex = 0;
                }
                else
                {
                    selectedIndex--;
                    if (selectedIndex < 0)
                    {
                        selectedIndex = ViewModel.FilteredItems.Count - 1;
                    }
                }
            }

            HandleEnterEscape(sender, e);

            if (selectedIndex != null && selectedIndex < ViewModel.FilteredItems.Count)
            {
                ViewModel.SelectedItem = ViewModel.FilteredItems[selectedIndex.Value];
                if (ViewModel.SelectedItem != null)
                {
                    MainListBox.ScrollIntoView(ViewModel.SelectedItem);
                }
            }
        }

        private void HandleEnterEscape(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ViewModel.IsOkButtonEnabled)
            {
                DialogResult = true;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }
    }
}
