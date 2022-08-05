using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework;
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

namespace OfficialPlugins.PointEditingPlugin
{
    /// <summary>
    /// Interaction logic for PointEditControl.xaml
    /// </summary>
    public partial class PointEditControl : UserControl
    {
        PointEditingViewModel ViewModel => DataContext as PointEditingViewModel;

        public PointEditControl()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleVmPropertyChanged;
            }
        }

        private void HandleVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.SelectedPoint))
            {
                if (!XTextBox.IsFocused)
                {
                    XTextBox.Text = ViewModel.SelectedPoint?.X.ToString();
                }
                if (!YTextBox.IsFocused)
                {
                    YTextBox.Text = ViewModel.SelectedPoint?.Y.ToString();
                }
            }
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Points != null)
            {
                ViewModel.Points.Add(new Vector2());

                ListBox.SelectedIndex = ListBox.Items.Count - 1;
            }
        }

        private void RemoveButtonClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Points != null && ListBox.SelectedItem != null)
            {
                int indexToRemove = ListBox.SelectedIndex;

                ViewModel.Points.RemoveAt(indexToRemove);
            }
        }

        private void XTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (ViewModel.SelectedPoint != null && float.TryParse(XTextBox.Text, out float outValue))
            {
                int index = this.ListBox.SelectedIndex;

                if (index != -1)
                {
                    Vector2 vector = ViewModel.SelectedPoint ?? new Vector2();
                    if (outValue != vector.X)
                    {
                        vector.X = outValue;

                        ViewModel.Points[index] = vector;

                        ViewModel.SelectedPoint = vector;
                    }
                }
            }
        }

        private void YTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (ViewModel.SelectedPoint != null && float.TryParse(YTextBox.Text, out float outValue))
            {
                int index = this.ListBox.SelectedIndex;

                if (index != -1)
                {
                    Vector2 vector = ViewModel.SelectedPoint ?? new Vector2();
                    if (outValue != vector.Y)
                    {
                        vector.Y = outValue;

                        ViewModel.Points[index] = vector;

                        ViewModel.SelectedPoint = vector;
                    }
                }
            }
        }

        private void HandleAddRectanglePointsClicked(object sender, RoutedEventArgs e)
        {
            if(ViewModel.Points?.Count > 0)
            {
                var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox("Would you like to clear the points and replace them with points for a rectangle shape?");
                if(result == MessageBoxResult.Yes)
                {
                    AddRectanglePoints();
                }
            }
            else
            {
                AddRectanglePoints();
            }
        }

        private void AddRectanglePoints()
        {

            ViewModel.Points.Clear();

            ViewModel.Points.Add(new Vector2(-16, 16));
            ViewModel.Points.Add(new Vector2( 16, 16));
            ViewModel.Points.Add(new Vector2( 16,-16));
            ViewModel.Points.Add(new Vector2(-16,-16));
            ViewModel.Points.Add(new Vector2(-16, 16));


            ListBox.SelectedIndex = ListBox.Items.Count - 1;
        }

        private void MovePointUp(object sender, RoutedEventArgs e)
        {
            var oldSelectedIndex = ViewModel.SelectedIndex;
            var newSelectedIndex = ViewModel.SelectedIndex - 1;
            ViewModel.Points.Move(oldSelectedIndex, newSelectedIndex);
            ViewModel.SelectedIndex = newSelectedIndex;
        }

        private void MovePointDown(object sender, RoutedEventArgs e)
        {
            var oldSelectedIndex = ViewModel.SelectedIndex;
            var newSelectedIndex = ViewModel.SelectedIndex + 1;
            ViewModel.Points.Move(oldSelectedIndex, newSelectedIndex);
            ViewModel.SelectedIndex = newSelectedIndex;

        }
    }
}
