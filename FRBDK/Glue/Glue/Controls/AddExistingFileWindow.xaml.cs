using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.ViewModels;
using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for AddExistingFileControl.xaml
    /// </summary>
    public partial class AddExistingFileWindow : Window
    {
        private AddExistingFileViewModel ViewModel
        {
            get
            {
                return DataContext as AddExistingFileViewModel;
            }
        }

        public AddExistingFileWindow()
        {
            InitializeComponent();

            Left = MainGlueWindow.MousePosition.X - this.Width / 2;
            Top = MainGlueWindow.MousePosition.Y - Height / 2;

            SearchTextBox.Focus();
        }

        private void HandleBrowseClicked(object sender, RoutedEventArgs e)
        {
            // add externally built file, add external file, add built file
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var element = GlueState.Self.CurrentElement;
                    string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();

                    ViewModel.Files.Clear();
                    ViewModel.Files.AddRange(openFileDialog.FileNames);
                    this.DialogResult = true;
                }
            }
        }

        private void OkButtonClicked(object sender, RoutedEventArgs e)
        {
            DoAcceptLogic();
        }

        private void DoAcceptLogic()
        {
            var selectedItem = ViewModel.SelectedListBoxItem;

            if (!string.IsNullOrEmpty(selectedItem))
            {
                ViewModel.Files.Clear();
                ViewModel.Files.Add(ViewModel.ContentFolder + ViewModel.SelectedListBoxItem);
                this.DialogResult = true;
            }
            else
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Select a file or click the Browse button");
            }
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                    DoAcceptLogic();
                    break;
                case Key.Escape:
                    this.DialogResult = false;
                    break;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index < ViewModel.FilteredFileList.Count - 1)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index + 1];
                        }
                    }
                    break;
                case Key.Up:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index > 0)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index - 1];
                        }
                    }
                    break;
            }
        }

        private void ListBoxInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxInstance.ScrollIntoView(ViewModel.SelectedListBoxItem);

        }
    }
}
