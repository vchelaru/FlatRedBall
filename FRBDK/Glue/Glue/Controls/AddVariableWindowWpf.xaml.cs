
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Extensions;
using GlueFormsCore.ViewModels;
using System.Windows;
using System.Windows.Input;

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
                GlueCommands.Self.DialogCommands.MoveToCursor(this);

            };
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            HandleOkClicked();
        }

        private void HandleOkClicked()
        {
            if(!string.IsNullOrEmpty(ViewModel.FailureText))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(ViewModel.FailureText);
            }
            else
            {
                this.DialogResult = true;
            }
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
                HandleOkClicked();
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
