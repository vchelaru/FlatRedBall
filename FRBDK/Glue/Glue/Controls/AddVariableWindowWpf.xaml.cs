
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Extensions;
using GlueFormsCore.ViewModels;
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

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for AddVariableWindowWpf.xaml
    /// </summary>
    public partial class AddVariableWindowWpf : Window
    {
        AddCustomVariableViewModel ViewModel => DataContext as AddCustomVariableViewModel;
        GlueElement element;

        public AddVariableWindowWpf()
        {
            InitializeComponent();

            Loaded += (not, used) =>
            {
                if(ViewModel?.DesiredVariableType == CustomVariableType.New)
                {
                    NewVariableTextBox.Focus();
                }

                this.MoveToCursor();

            };
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyboardKey(e);
        }

        private void HandleKeyboardKey(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.DialogResult = true;
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
            }
        }

        private void TunneledVariableTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyboardKey(e);
        }
    }
}
