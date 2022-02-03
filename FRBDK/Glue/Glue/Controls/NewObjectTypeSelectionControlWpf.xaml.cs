using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Extensions;
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
using System.Windows.Shapes;

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

            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // uses winforms:
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            this.Left = point.X - this.Width / 2;
            // not sure why this is so high
            //this.Top = point.Y - this.Height/2;
            this.Top = point.Y - 50;

            this.ShiftWindowOntoScreen();

            SearchTextBox.Focus();
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

        private void HandleListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = true;
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            if(ViewModel?.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.Gum)
            {
                var selectedItem = ViewModel.SelectedItem;
                var oldName = ViewModel.ObjectName;
                // force it here because this type is used in the rest of Glue (for now)
                // Update February 2, 2022
                // We've actually started to support Gum types instead of just FRB types and
                // there is now code that depends on the differentiation so let's keep it as it
                // ViewModel.SourceType = FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType;

                // this changes the name...
                //ViewModel.SelectedItem = selectedItem;
                // ... so change it back
                //ViewModel.ObjectName = oldName;
            }
            this.DialogResult = true;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
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

            HandleEnterEscape(e);

            if (selectedIndex != null && selectedIndex < ViewModel.FilteredItems.Count)
            {
                ViewModel.SelectedItem = ViewModel.FilteredItems[selectedIndex.Value];
                if (ViewModel.SelectedItem != null)
                {
                    MainListBox.ScrollIntoView(ViewModel.SelectedItem);
                }
            }


        }

        private void HandleEnterEscape(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
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

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            HandleEnterEscape(e);

        }

        private void NameTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            HandleEnterEscape(e);

        }
    }
}
