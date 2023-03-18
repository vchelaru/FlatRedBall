using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework;
using OfficialPlugins.PointEditingPlugin.Views;
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

        private void HandleAddPolygonPointsClicked(object sender, RoutedEventArgs e)
        {
            var tiw = new CustomizableTextInputWindow();
            tiw.Message = "Enter the number of points for the new polygon. At least 3 points are required. Note that the last point is automatically repeated, " +
                "so only enter the number of visible points. For example, 3 for a triangle.";

            if(ViewModel.Points?.Count > 0)
            {
                tiw.Message += "\n\nThis will completely replace the existing points on this polygon.";
            }

            var trueFalseResult = tiw.ShowDialog();

            if(trueFalseResult == true)
            {
                var value = tiw.Result;

                var succeeded = int.TryParse(value, out int intResult);

                if(!succeeded)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox($"Only integer values are allowed. Invalid value: {value}");
                }
                else if(intResult <= 2)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox($"At least 3 points are required. Invalid value: {intResult}");
                }
                else 
                {
                    SetPolygonPoints(intResult);
                }

            }
        }

        private void AddRectanglePoints()
        {

            ViewModel.Points.Clear();

            float radius = 8;

            ViewModel.Points.Add(new Vector2(-radius, radius));
            ViewModel.Points.Add(new Vector2( radius, radius));
            ViewModel.Points.Add(new Vector2( radius,-radius));
            ViewModel.Points.Add(new Vector2(-radius,-radius));
            ViewModel.Points.Add(new Vector2(-radius, radius));


            ListBox.SelectedIndex = ListBox.Items.Count - 1;
        }

        private void SetPolygonPoints(int numberPoints)
        {

            ViewModel.Points.Clear();

            var radius = 8;


            for(int i = 0; i < numberPoints; i++)
            {
                var radians = i * MathHelper.TwoPi / numberPoints;

                var x = MathF.Cos(radians) * radius;
                var y = MathF.Sin(radians) * radius;

                ViewModel.Points.Add(new Vector2(x,y));
            }

            ViewModel.Points.Add(ViewModel.Points[0]);

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

        private void ResizePolygon(object sender, RoutedEventArgs e)
        {
            var resizePolygonWindow = new ResizePolygonWindow();

            var result = resizePolygonWindow.ShowDialog();

            if(result == true)
            {
                var resizeWidthPercentage = resizePolygonWindow.WidthPercentage;
                var resizeHeightPercentage = resizePolygonWindow.HeightPercentage;


                for (int i = 0; i < ViewModel.Points.Count; i++)
                {
                    Vector2 point = ViewModel.Points[i];

                    if(resizeWidthPercentage != 100.0)
                    {
                        point.X *= (float)(resizeWidthPercentage / 100);
                    }
                    if(resizeHeightPercentage != 100.0)
                    {
                        point.Y *= (float)(resizeHeightPercentage / 100);
                    }

                    ViewModel.Points[i] = point;
                }
            }
        }
    }
}
